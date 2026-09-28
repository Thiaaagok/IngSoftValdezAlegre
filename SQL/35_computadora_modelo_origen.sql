-- ============================================================
--  35_computadora_modelo_origen.sql
--  MODELO DE ORIGEN DE CADA COMPUTADORA VENDIDA (RFN1 · patrón Builder).
--
--  El armado de computadoras usa ahora el patrón Builder (BLL/Armado). El
--  ComputadoraEstandarBuilder06AV, además de nombrar el equipo, lo deja
--  vinculado al modelo del catálogo del que sale: Computadora06AV.ModeloOrigen.
--  Hasta ahora ese dato se perdía: la venta de una "PC Gamer" guardaba el
--  nombre como texto, pero no QUÉ modelo se vendió.
--
--  Este script:
--    1. Agrega Computadoras.IdModeloOrigen (INT NULL, FK a ModelosEstandar).
--         NULL  → equipo armado a medida ("Armá tu PC").
--         valor → equipo de catálogo.
--       La FK es ON DELETE SET NULL: borrar un modelo del catálogo no se
--       bloquea por las ventas que ya tuvo; esas computadoras conservan sus
--       componentes y su nombre, y sólo pierden el vínculo.
--    2. Completa el dato en las computadoras que ya existen: las estándar
--       cuyo nombre coincide con UN modelo del catálogo (es el caso de los
--       datos de demostración).
--    3. Redefine sp_Computadoras_Agregar (nuevo parámetro @IdModeloOrigen,
--       opcional) y sp_Computadoras_ObtenerPorId (devuelve el modelo).
--
--  Es idempotente: se puede correr varias veces.
--
--  ⚠  DÍGITO VERIFICADOR
--  Computadoras es una tabla protegida por DVH/DVV. Al agregarle una columna
--  cambian sus dígitos, así que en el próximo ingreso el sistema va a marcar
--  la tabla Computadoras como inconsistente. Es esperable: entrar con un
--  usuario que tenga la patente RepararIntegridad (admin) y en la pantalla
--  de reparación elegir "Recalcular dígito verificador".
--
--  ORDEN: correr este script ANTES de usar la versión nueva de la
--  aplicación. La app manda @IdModeloOrigen al registrar cada venta.
-- ============================================================

-- ── 1) Columna + FK + índice ─────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Computadoras') AND name = 'IdModeloOrigen')
BEGIN
    ALTER TABLE Computadoras ADD IdModeloOrigen INT NULL;
    PRINT '   Columna Computadoras.IdModeloOrigen agregada.';
END
ELSE
    PRINT '   La columna Computadoras.IdModeloOrigen ya existía.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Computadora_ModeloOrigen')
BEGIN
    ALTER TABLE Computadoras
        ADD CONSTRAINT FK_Computadora_ModeloOrigen
        FOREIGN KEY (IdModeloOrigen) REFERENCES ModelosEstandar(Id)
        ON DELETE SET NULL;
    PRINT '   FK Computadoras.IdModeloOrigen -> ModelosEstandar.Id creada.';
END
GO

-- Para contar ventas por modelo sin recorrer toda la tabla.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Computadoras_ModeloOrigen')
    CREATE INDEX IX_Computadoras_ModeloOrigen ON Computadoras (IdModeloOrigen);
GO

-- ── 2) Completar las computadoras existentes ─────────────────
-- Sólo las estándar (TipoConfiguracion = 0) cuyo nombre coincide con
-- exactamente UN modelo: si hubiera dos modelos con el mismo nombre no se
-- adivina cuál fue, y la fila queda en NULL.
UPDATE c
SET    c.IdModeloOrigen = m.Id
FROM   Computadoras c
       JOIN ModelosEstandar m ON m.Nombre = c.Nombre
WHERE  c.TipoConfiguracion = 0
  AND  c.IdModeloOrigen IS NULL
  AND  (SELECT COUNT(*) FROM ModelosEstandar m2 WHERE m2.Nombre = c.Nombre) = 1;

PRINT '   Computadoras existentes vinculadas a su modelo: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO

-- ── 3) Procedimientos ────────────────────────────────────────
-- @IdModeloOrigen es opcional (DEFAULT NULL): un llamado con los tres
-- parámetros de antes sigue funcionando y registra un equipo sin modelo.
CREATE OR ALTER PROCEDURE sp_Computadoras_Agregar
    @Nombre            NVARCHAR(150),
    @TipoConfiguracion INT,
    @PrecioTotal       DECIMAL(12,2),
    @IdModeloOrigen    INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal, IdModeloOrigen)
    VALUES (@Nombre, @TipoConfiguracion, @PrecioTotal, @IdModeloOrigen);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.Id, c.Nombre, c.TipoConfiguracion, c.PrecioTotal,
            c.IdModeloOrigen,
            m.Nombre       AS ModeloNombre,
            m.Descripcion  AS ModeloDescripcion
    FROM    Computadoras c
            LEFT JOIN ModelosEstandar m ON m.Id = c.IdModeloOrigen
    WHERE   c.Id = @Id;
END
GO

-- ── Verificación ─────────────────────────────────────────────
-- Una fila por computadora: de qué modelo salió, o "(a medida)".
SELECT  c.Id,
        c.Nombre,
        CASE c.TipoConfiguracion WHEN 0 THEN 'Estandar' ELSE 'Configurable' END AS Tipo,
        ISNULL(m.Nombre, '(a medida)') AS ModeloOrigen
FROM    Computadoras c
        LEFT JOIN ModelosEstandar m ON m.Id = c.IdModeloOrigen
ORDER BY c.Id;
GO

PRINT '>>> 35: Computadoras.IdModeloOrigen listo.';
PRINT '    Recordá: en el próximo ingreso, recalcular el dígito verificador';
PRINT '    (usuario con patente RepararIntegridad > "Recalcular dígito verificador").';
GO
