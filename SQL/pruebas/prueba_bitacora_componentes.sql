-- ============================================================
--  prueba_bitacora_componentes.sql
--  Prueba de punta a punta de la bitácora de cambios (Componentes_C).
--
--  NO forma parte de la instalación: vive en SQL\pruebas\ justo para que el
--  Instalador (que lee solo el primer nivel de la carpeta SQL) no lo ejecute.
--  Correlo a mano en SSMS sobre IngSoftValdezAlegre.
--
--  Qué prueba, en orden:
--    1. El DELETE físico está bloqueado por el trigger.
--    2. Alta            → 1 versión, Act = 1.
--    3. Modificación    → 2 versiones, la vieja Act = 0.
--    4. Reserva de stock→ NO genera versión (solo toca StockReservado).
--    5. Baja lógica     → versión nueva con Bit_Lo_Bo = 1.
--    6. Activar versión → restaura valores y agrega una versión más.
--
--  Los pasos 2 a 6 corren dentro de una transacción que se DESHACE al final:
--  la base queda como estaba.
-- ============================================================

USE IngSoftValdezAlegre;
GO
SET NOCOUNT ON;
GO

PRINT '=== 1) El borrado físico debe fallar ===============================';
GO
BEGIN TRY
    DELETE FROM Componentes WHERE Codigo = N'ZZ-NO-EXISTE';
    PRINT '  ✗ FALLÓ: el DELETE pasó y no debería.';
END TRY
BEGIN CATCH
    PRINT '  ✓ OK, el trigger lo rechazó: ' + ERROR_MESSAGE();
END CATCH
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;   -- el trigger deshace la transacción
GO

-- ── Pasos 2 a 6 ─────────────────────────────────────────────
BEGIN TRANSACTION;
GO

DECLARE @Cod NVARCHAR(50) = N'ZZ-TEST-BITACORA';

PRINT '';
PRINT '=== 2) Alta ========================================================';
EXEC sp_Componentes_Agregar
     @Codigo = N'ZZ-TEST-BITACORA', @Descripcion = N'Componente de prueba',
     @Tipo = 0, @Marca = N'ACME', @Modelo = N'M1',
     @PrecioUnitario = 100.00, @Stock = 10, @StockMinimo = 2;

SELECT 'Tras el alta' AS Paso, IdHistorico, Fecha, Hora, Descripcion,
       Stock, PrecioUnitario, Bit_Lo_Bo, Act
FROM   Componentes_C WHERE CodigoComponente = @Cod
ORDER BY IdHistorico;
PRINT '  Esperado: 1 fila con Act = 1.';

PRINT '';
PRINT '=== 3) Modificación de stock y precio ==============================';
EXEC sp_Componentes_Modificar
     @Codigo = N'ZZ-TEST-BITACORA', @Descripcion = N'Componente de prueba',
     @Tipo = 0, @Marca = N'ACME', @Modelo = N'M1',
     @PrecioUnitario = 150.00, @Stock = 25, @StockMinimo = 2;

SELECT 'Tras modificar' AS Paso, IdHistorico, Stock, PrecioUnitario, Bit_Lo_Bo, Act
FROM   Componentes_C WHERE CodigoComponente = @Cod
ORDER BY IdHistorico;
PRINT '  Esperado: 2 filas; solo la última con Act = 1.';

PRINT '';
PRINT '=== 4) Reserva de stock: NO debe versionar =========================';
DECLARE @AntesReserva INT =
        (SELECT COUNT(*) FROM Componentes_C WHERE CodigoComponente = @Cod);

EXEC sp_Componentes_ReservarStock @Codigo = N'ZZ-TEST-BITACORA', @Cantidad = 5;

DECLARE @DespuesReserva INT =
        (SELECT COUNT(*) FROM Componentes_C WHERE CodigoComponente = @Cod);

IF @AntesReserva = @DespuesReserva
    PRINT '  ✓ OK: la reserva no generó una versión nueva.';
ELSE
    PRINT '  ✗ FALLÓ: la reserva generó una versión (ruido en la bitácora).';

PRINT '';
PRINT '=== 5) Baja lógica =================================================';
EXEC sp_Componentes_BajaLogica @Codigo = N'ZZ-TEST-BITACORA';

SELECT 'Tras la baja' AS Paso, IdHistorico, Stock, PrecioUnitario, Bit_Lo_Bo, Act
FROM   Componentes_C WHERE CodigoComponente = @Cod
ORDER BY IdHistorico;
PRINT '  Esperado: 3 filas; la vigente (Act = 1) con Bit_Lo_Bo = 1.';

IF EXISTS (SELECT 1 FROM Componentes WHERE Codigo = @Cod AND Bit_Lo_Bo = 1)
    PRINT '  ✓ OK: el componente quedó dado de baja, no borrado.';
ELSE
    PRINT '  ✗ FALLÓ: el componente no quedó dado de baja.';

PRINT '';
PRINT '=== 6) Restaurar la versión del alta ===============================';
DECLARE @IdPrimera INT =
        (SELECT MIN(IdHistorico) FROM Componentes_C WHERE CodigoComponente = @Cod);

EXEC sp_ComponentesC_Activar @IdHistorico = @IdPrimera;

SELECT 'Estado del componente' AS Paso, Codigo, Descripcion, PrecioUnitario,
       Stock, StockReservado, Bit_Lo_Bo
FROM   Componentes WHERE Codigo = @Cod;
PRINT '  Esperado: precio 100.00, stock 10, Bit_Lo_Bo = 0 (la restauración';
PRINT '  también deshace la baja, porque esa versión estaba vigente).';

SELECT 'Bitácora final' AS Paso, IdHistorico, Fecha, Hora, Stock,
       PrecioUnitario, Bit_Lo_Bo, Act
FROM   Componentes_C WHERE CodigoComponente = @Cod
ORDER BY IdHistorico;
PRINT '  Esperado: 4 filas; solo la última con Act = 1. Nada se pisó.';

PRINT '';
PRINT '=== 7) Un solo registro vigente por componente =====================';
IF EXISTS (SELECT CodigoComponente FROM Componentes_C WHERE Act = 1
           GROUP BY CodigoComponente HAVING COUNT(*) > 1)
    PRINT '  ✗ FALLÓ: hay componentes con más de una versión vigente.';
ELSE
    PRINT '  ✓ OK: ningún componente tiene dos versiones vigentes.';
GO

ROLLBACK TRANSACTION;
GO

PRINT '';
PRINT '=== Prueba terminada. La base quedó como estaba (rollback). ========';
GO
