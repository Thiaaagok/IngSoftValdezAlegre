-- ============================================================
--  30_seed_demo_pcfactory.sql
--  Carga de DATOS DE DEMOSTRACIÓN para todo lo nuevo de PC Factory:
--  Componentes, Insumos (con faltantes para RFN2), Proveedores,
--  Líneas de ensamblaje, Clientes y Modelos estándar (con sus
--  componentes). Con esto se puede recorrer RFN1 y RFN2 de punta a punta.
--
--  Requisitos: correr antes 99_pcfactory_all.sql (crea las tablas).
--  Es idempotente: usa "WHERE NOT EXISTS", se puede correr varias veces.
--
--  Referencia de Tipo de componente (enum BE.TipoComponente06AV):
--    0=Procesador 1=MemoriaRAM 2=Disco 3=PlacaMadre 4=Fuente
--    5=Gabinete 6=PlacaDeVideo 7=Refrigeracion 8=Otro
-- ============================================================

SET NOCOUNT ON;
PRINT '>>> Cargando datos de demostración de PC Factory...';

-- ── 1) COMPONENTES ───────────────────────────────────────────
INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
SELECT v.Codigo, v.Descripcion, v.Tipo, v.Marca, v.Modelo, v.PrecioUnitario, v.StockDisponible
FROM (VALUES
    (N'CPU-001', N'Procesador 6 núcleos',      0, N'Intel',   N'Core i5-12400',   180.00, 25),
    (N'CPU-002', N'Procesador 6 núcleos',      0, N'AMD',     N'Ryzen 5 5600',    160.00, 20),
    (N'RAM-001', N'Memoria RAM 16GB DDR4',     1, N'Kingston',N'Fury 3200',        55.00, 40),
    (N'RAM-002', N'Memoria RAM 32GB DDR4',     1, N'Corsair', N'Vengeance 3600',  105.00, 15),
    (N'SSD-001', N'Disco SSD 1TB NVMe',        2, N'Samsung', N'980 Pro',          80.00, 30),
    (N'HDD-001', N'Disco HDD 2TB',             2, N'Seagate', N'Barracuda',        60.00, 18),
    (N'MB-001',  N'Placa madre B660',          3, N'ASUS',    N'Prime B660',      130.00, 20),
    (N'MB-002',  N'Placa madre B550',          3, N'Gigabyte',N'B550 Aorus',      120.00, 16),
    (N'PSU-001', N'Fuente 650W 80+ Bronze',    4, N'EVGA',    N'650 BR',           70.00, 22),
    (N'GAB-001', N'Gabinete ATX con vidrio',   5, N'NZXT',    N'H510',             65.00, 25),
    (N'GPU-001', N'Placa de video RTX 3060',   6, N'MSI',     N'Ventus 3060',     330.00, 10),
    (N'COOL-001',N'Cooler para CPU',           7, N'CoolerMaster', N'Hyper 212',   35.00, 30)
) v(Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
WHERE NOT EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = v.Codigo);
PRINT '   Componentes cargados.';

-- ── 2) INSUMOS (algunos por debajo del mínimo → faltantes RFN2) ─
INSERT INTO Insumos (Codigo, Descripcion, Stock, StockMinimo)
SELECT v.Codigo, v.Descripcion, v.Stock, v.StockMinimo
FROM (VALUES
    (N'INS-001', N'Pasta térmica',              5,  10),   -- FALTANTE
    (N'INS-002', N'Cables SATA',               50,  20),
    (N'INS-003', N'Tornillos (bolsa x100)',     8,  15),   -- FALTANTE
    (N'INS-004', N'Bridas plásticas',         100,  30),
    (N'INS-005', N'Alcohol isopropílico',       3,  12),   -- FALTANTE
    (N'INS-006', N'Guantes antiestáticos',     40,  10)
) v(Codigo, Descripcion, Stock, StockMinimo)
WHERE NOT EXISTS (SELECT 1 FROM Insumos i WHERE i.Codigo = v.Codigo);
PRINT '   Insumos cargados (3 quedan bajo stock para probar Compras).';

-- ── 3) PROVEEDORES ───────────────────────────────────────────
INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion)
SELECT v.Nombre, v.Cuit, v.Email, v.Telefono, v.Direccion
FROM (VALUES
    (N'Insumos del Sur SRL', N'30-11111111-1', N'ventas@insumosdelsur.com', N'011-4001-1000', N'Av. Mitre 1234, Avellaneda'),
    (N'TecnoParts SA',       N'30-22222222-2', N'compras@tecnoparts.com',   N'011-4002-2000', N'Calle Falsa 456, CABA'),
    (N'Distribuidora Norte', N'30-33333333-3', N'info@distnorte.com',       N'0351-500-3000', N'Bv. San Juan 789, Córdoba'),
    (N'ComponentesYA',       N'30-44444444-4', N'hola@componentesya.com',   N'0341-600-4000', N'Pellegrini 321, Rosario')
) v(Nombre, Cuit, Email, Telefono, Direccion)
WHERE NOT EXISTS (SELECT 1 FROM Proveedores p WHERE p.Cuit = v.Cuit);
PRINT '   Proveedores cargados.';

-- ── 4) LÍNEAS DE ENSAMBLAJE ──────────────────────────────────
INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible)
SELECT v.Nombre, v.Descripcion, v.Disponible
FROM (VALUES
    (N'Línea A', N'Armado general',      CONVERT(BIT,1)),
    (N'Línea B', N'Equipos gamer',       CONVERT(BIT,1)),
    (N'Línea C', N'Equipos de oficina',  CONVERT(BIT,1))
) v(Nombre, Descripcion, Disponible)
WHERE NOT EXISTS (SELECT 1 FROM LineasEnsamblaje l WHERE l.Nombre = v.Nombre);
PRINT '   Líneas de ensamblaje cargadas.';

-- ── 5) CLIENTES ──────────────────────────────────────────────
INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
SELECT v.Dni, v.Nombre, v.Apellido, v.Telefono, v.Direccion
FROM (VALUES
    (N'30111222', N'Juan',   N'Pérez',     N'11-5001-0001', N'San Martín 100, CABA'),
    (N'28999888', N'María',  N'Gómez',     N'11-5001-0002', N'Belgrano 200, CABA'),
    (N'33444555', N'Carlos', N'López',     N'351-500-0003', N'Colón 300, Córdoba'),
    (N'27888999', N'Ana',    N'Torres',    N'341-600-0004', N'Córdoba 400, Rosario'),
    (N'31222333', N'Lucía',  N'Fernández', N'11-5001-0005', N'Rivadavia 500, CABA')
) v(Dni, Nombre, Apellido, Telefono, Direccion)
WHERE NOT EXISTS (SELECT 1 FROM Clientes c WHERE c.Dni = v.Dni);
PRINT '   Clientes cargados.';

-- ── 6) MODELOS ESTÁNDAR ──────────────────────────────────────
INSERT INTO ModelosEstandar (Nombre, Descripcion)
SELECT v.Nombre, v.Descripcion
FROM (VALUES
    (N'PC Oficina', N'Equipo para tareas de oficina y navegación'),
    (N'PC Gamer',   N'Equipo de alto rendimiento con placa de video dedicada'),
    (N'PC Básica',  N'Equipo económico para uso general')
) v(Nombre, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM ModelosEstandar m WHERE m.Nombre = v.Nombre);
PRINT '   Modelos estándar cargados.';

-- Componentes de cada modelo (resuelve el Id del modelo por Nombre).
INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
SELECT m.Id, v.Codigo
FROM (VALUES
    -- PC Oficina
    (N'PC Oficina', N'CPU-001'), (N'PC Oficina', N'RAM-001'), (N'PC Oficina', N'SSD-001'),
    (N'PC Oficina', N'MB-002'),  (N'PC Oficina', N'PSU-001'), (N'PC Oficina', N'GAB-001'),
    -- PC Gamer
    (N'PC Gamer', N'CPU-002'), (N'PC Gamer', N'RAM-002'), (N'PC Gamer', N'SSD-001'),
    (N'PC Gamer', N'MB-001'),  (N'PC Gamer', N'PSU-001'), (N'PC Gamer', N'GAB-001'),
    (N'PC Gamer', N'GPU-001'), (N'PC Gamer', N'COOL-001'),
    -- PC Básica
    (N'PC Básica', N'CPU-001'), (N'PC Básica', N'RAM-001'), (N'PC Básica', N'HDD-001'),
    (N'PC Básica', N'MB-002'),  (N'PC Básica', N'PSU-001'), (N'PC Básica', N'GAB-001')
) v(Modelo, Codigo)
JOIN ModelosEstandar m ON m.Nombre = v.Modelo
WHERE EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = v.Codigo)
  AND NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes mc
                  WHERE mc.IdModelo = m.Id AND mc.CodigoComponente = v.Codigo);
PRINT '   Componentes de modelos estándar asociados.';

-- ── 7) VENTAS DE DEMOSTRACIÓN (RFN1) ─────────────────────────
--  Deja el circuito armado en distintos puntos para poder probar todo:
--    Venta #1  Pendiente      → falta registrar la seña (CU03).
--    Venta #2  Señada         → lista para que el gerente cree la orden (CU04).
--    Venta #3  En producción  → orden ya planificada en Línea A (CU05).
--  Solo se carga si todavía no hay ventas (para no duplicar al reejecutar).
IF NOT EXISTS (SELECT 1 FROM Ventas)
BEGIN
    DECLARE @Demo TABLE (Orden INT, Dni NVARCHAR(20), Modelo NVARCHAR(150), Etapa INT);
    --  Etapa: 0 = sin seña · 1 = con seña · 2 = con seña y orden planificada
    INSERT INTO @Demo (Orden, Dni, Modelo, Etapa) VALUES
        (1, N'30111222', N'PC Oficina', 0),
        (2, N'28999888', N'PC Gamer',   1),
        (3, N'33444555', N'PC Básica',  2);

    DECLARE @o INT, @dni NVARCHAR(20), @modelo NVARCHAR(150), @etapa INT;
    DECLARE @idModelo INT, @idPc INT, @total DECIMAL(12,2), @nroVenta INT, @nroOrden INT;
    DECLARE @idLinea INT = (SELECT TOP 1 Id FROM LineasEnsamblaje WHERE Nombre = N'Línea A');

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Orden, Dni, Modelo, Etapa FROM @Demo ORDER BY Orden;
    OPEN cur;
    FETCH NEXT FROM cur INTO @o, @dni, @modelo, @etapa;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @idModelo = Id FROM ModelosEstandar WHERE Nombre = @modelo;

        SELECT @total = ISNULL(SUM(c.PrecioUnitario), 0)
        FROM   ModeloEstandarComponentes mc
        JOIN   Componentes c ON c.Codigo = mc.CodigoComponente
        WHERE  mc.IdModelo = @idModelo;

        -- Computadora de la venta (copia del modelo estándar)
        INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal)
        VALUES (@modelo, 0, @total);
        SET @idPc = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente, Cantidad)
        SELECT @idPc, mc.CodigoComponente, 1
        FROM   ModeloEstandarComponentes mc WHERE mc.IdModelo = @idModelo;

        -- Venta
        INSERT INTO Ventas (DniCliente, IdComputadora, FechaEntregaEstimada, Estado, UsuarioRegistro)
        VALUES (@dni, @idPc, DATEADD(DAY, 15, CAST(GETDATE() AS DATE)), 0, N'admin');
        SET @nroVenta = CAST(SCOPE_IDENTITY() AS INT);

        -- La venta reserva los componentes (se consumen al cerrar la orden, CU06)
        UPDATE c
        SET    c.StockReservado = c.StockReservado + cc.Cantidad
        FROM   Componentes c
        JOIN   ComputadoraComponentes cc ON cc.CodigoComponente = c.Codigo
        WHERE  cc.IdComputadora = @idPc;

        IF @etapa >= 1
        BEGIN
            -- Seña del 50% (CU03)
            INSERT INTO Pagos (NumeroVenta, Tipo, Monto, FormaPago, Usuario)
            VALUES (@nroVenta, 0, ROUND(@total * 0.5, 2), 0, N'admin');

            UPDATE Pagos
            SET    NumeroRecibo = N'REC-' + RIGHT(N'00000000' + CAST(Id AS NVARCHAR(10)), 8)
            WHERE  Id = CAST(SCOPE_IDENTITY() AS INT);

            UPDATE Ventas SET Estado = 1 WHERE NumeroVenta = @nroVenta;   -- Señada
        END

        IF @etapa >= 2
        BEGIN
            -- Orden de producción planificada (CU04 + CU05)
            INSERT INTO OrdenesProduccion (NumeroVenta, FechaEntregaEstimada, Estado,
                                           IdLinea, FechaInicioPrevista, ResponsableTecnico)
            VALUES (@nroVenta, DATEADD(DAY, 15, CAST(GETDATE() AS DATE)), 1,
                    @idLinea, CAST(GETDATE() AS DATE), N'Técnico Demo');
            SET @nroOrden = CAST(SCOPE_IDENTITY() AS INT);

            UPDATE LineasEnsamblaje SET Disponible = 0 WHERE Id = @idLinea;
            UPDATE Ventas SET Estado = 2 WHERE NumeroVenta = @nroVenta;   -- En producción
        END

        FETCH NEXT FROM cur INTO @o, @dni, @modelo, @etapa;
    END

    CLOSE cur;
    DEALLOCATE cur;

    PRINT '   Ventas de demostración cargadas (pendiente / señada / en producción).';
END
ELSE
    PRINT '   Ya existían ventas: se omite la carga de ventas de demostración.';

-- ── Recalcular el Dígito Verificador tras la carga masiva ────
-- (opcional: la app lo recalcula sola; esto lo deja consistente ya mismo)
IF OBJECT_ID('DV') IS NOT NULL
    PRINT '   (El Dígito Verificador se recalculará al operar desde la app.)';

PRINT '>>> Datos de demostración cargados correctamente.';
GO
