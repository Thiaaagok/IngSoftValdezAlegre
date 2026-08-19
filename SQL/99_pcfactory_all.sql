-- ============================================================
--  99_pcfactory_all.sql  — Script MAESTRO de PC Factory.
--  Concatena 20..28 (tablas, SP, patentes). Incluye: venta y orden de
--  producción como documentos separados, stock disponible/reservado,
--  control de calidad con Nº de serie, cotización con costo/condiciones
--  y catálogo de modelos estándar.
--  Idempotente. Correlo en SSMS sobre IngSoftValdezAlegre.
-- ============================================================

-- ==== INICIO 20_pcfactory_clientes.sql ====

-- ============================================================
--  20_pcfactory_clientes.sql
--  Tabla Clientes (PC Factory) + procedimientos ABM.
--  Consumidos por DAL/ClientesDAL06AV.cs (EjecutarSP / EjecutarSPNonQuery).
--  Los nombres de columnas coinciden con MPP/ClientesMPP06AV.cs.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clientes')
CREATE TABLE Clientes (
    Dni       NVARCHAR(20)  NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Apellido  NVARCHAR(100) NOT NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL
);
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    ORDER BY Apellido, Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerPorDni
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Agregar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
    VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Direccion);
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Modificar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Clientes
    SET    Nombre = @Nombre, Apellido = @Apellido,
           Telefono = @Telefono, Direccion = @Direccion
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Eliminar
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Clientes WHERE Dni = @Dni;
END
GO

PRINT 'Tabla Clientes y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 20_pcfactory_clientes.sql

-- ==== INICIO 21_pcfactory_componentes.sql ====

-- ============================================================
--  21_pcfactory_componentes.sql
--  Tabla Componentes (PC Factory) + procedimientos ABM.
--  Tipo se guarda como int (ordinal del enum BE.TipoComponente06AV).
--
--  STOCK EN DOS NIVELES (RFN1):
--    Stock : unidades físicas en depósito.
--    StockReservado  : unidades comprometidas por ventas registradas cuya
--                      orden de producción todavía no se cerró.
--    Stock libre      = Stock - StockReservado  (lo vendible).
--
--  La venta RESERVA (CU01) y el cierre de la orden de producción CONSUME
--  la reserva descontando el stock físico (CU06, componentes efectivamente
--  utilizados). Anular la venta o volver atrás la orden LIBERA la reserva.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Componentes')
CREATE TABLE Componentes (
    Codigo          NVARCHAR(50)   NOT NULL PRIMARY KEY,
    Descripcion     NVARCHAR(200)  NOT NULL,
    Tipo            INT            NOT NULL,
    Marca           NVARCHAR(100)  NULL,
    Modelo          NVARCHAR(100)  NULL,
    PrecioUnitario  DECIMAL(12,2)  NOT NULL CONSTRAINT DF_Comp_Precio DEFAULT (0),
    Stock           INT            NOT NULL CONSTRAINT DF_Comp_Stock   DEFAULT (0),
    StockMinimo     INT            NOT NULL CONSTRAINT DF_Comp_StockMin DEFAULT (0),
    StockReservado  INT            NOT NULL CONSTRAINT DF_Comp_Reserva DEFAULT (0)
);
GO

-- Alta de la columna en bases creadas con una versión anterior del script.
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Componentes') AND name = 'StockMinimo')
    ALTER TABLE Componentes
        ADD StockMinimo INT NOT NULL CONSTRAINT DF_Comp_StockMin DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Componentes') AND name = 'StockReservado')
    ALTER TABLE Componentes
        ADD StockReservado INT NOT NULL CONSTRAINT DF_Comp_Reserva DEFAULT (0);
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado
    FROM   Componentes ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado
    FROM   Componentes WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @Stock INT, @StockMinimo INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, Stock, StockMinimo)
    VALUES (@Codigo, @Descripcion, @Tipo, @Marca, @Modelo, @PrecioUnitario, @Stock, @StockMinimo);
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @Stock INT, @StockMinimo INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET Descripcion = @Descripcion, Tipo = @Tipo, Marca = @Marca, Modelo = @Modelo,
        PrecioUnitario = @PrecioUnitario, Stock = @Stock, StockMinimo = @StockMinimo
    WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Componentes WHERE Codigo = @Codigo;
END
GO

-- ── Stock: reservar / liberar / consumir ─────────────────────

-- CU01: al registrar la venta se reservan las unidades necesarias.
-- Solo reserva si hay stock LIBRE suficiente; si no, no toca nada y avisa.
CREATE OR ALTER PROCEDURE sp_Componentes_ReservarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    StockReservado = StockReservado + @Cantidad
    WHERE  Codigo = @Codigo
      AND (Stock - StockReservado) >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51001, 'Stock libre insuficiente o componente inexistente al reservar.', 1;
END
GO

-- Devuelve unidades reservadas al stock libre (venta anulada, orden vuelta atrás).
CREATE OR ALTER PROCEDURE sp_Componentes_LiberarReserva
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    StockReservado = CASE WHEN StockReservado - @Cantidad < 0
                                 THEN 0 ELSE StockReservado - @Cantidad END
    WHERE  Codigo = @Codigo;
END
GO

-- CU06: al cerrar la orden se descuenta el stock físico de los componentes
-- efectivamente utilizados y se libera la reserva correspondiente.
CREATE OR ALTER PROCEDURE sp_Componentes_ConsumirReserva
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    Stock = Stock - @Cantidad,
           StockReservado  = CASE WHEN StockReservado - @Cantidad < 0
                                  THEN 0 ELSE StockReservado - @Cantidad END
    WHERE  Codigo = @Codigo AND Stock >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51002, 'Stock insuficiente o componente inexistente al consumir la reserva.', 1;
END
GO

-- Descuento directo de stock (se mantiene por compatibilidad con RFN2 / ajustes).
CREATE OR ALTER PROCEDURE sp_Componentes_DescontarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    Stock = Stock - @Cantidad
    WHERE  Codigo = @Codigo AND Stock >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51000, 'Stock insuficiente o componente inexistente al descontar stock.', 1;
END
GO

-- ── RFN2: reposición ─────────────────────────────────────────

-- Componentes que llegaron al mínimo: disparan la orden de compra.
CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerBajoStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado
    FROM   Componentes
    WHERE  Stock <= StockMinimo
    ORDER BY Descripcion;
END
GO

-- Suma al stock lo efectivamente recibido del proveedor (cierre de la orden de compra).
CREATE OR ALTER PROCEDURE sp_Componentes_SumarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes SET Stock = Stock + @Cantidad WHERE Codigo = @Codigo;

    IF @@ROWCOUNT = 0
        THROW 51003, 'Componente inexistente al sumar stock.', 1;
END
GO

PRINT 'Tabla Componentes y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 21_pcfactory_componentes.sql

-- ==== INICIO 23_pcfactory_proveedores.sql ====

-- ============================================================
--  23_pcfactory_proveedores.sql
--  Tabla Proveedores (PC Factory) + procedimientos ABM.
--  Id es autonumérico; el Agregar devuelve el Id generado.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Proveedores')
CREATE TABLE Proveedores (
    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(150) NOT NULL,
    Cuit      NVARCHAR(20)  NOT NULL,
    Email     NVARCHAR(150) NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL,
    CONSTRAINT UQ_Proveedores_Cuit UNIQUE (Cuit)
);
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorCuit
    @Cuit NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Cuit = @Cuit;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Agregar
    @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion)
    VALUES (@Nombre, @Cuit, @Email, @Telefono, @Direccion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Proveedores
    SET Nombre = @Nombre, Cuit = @Cuit, Email = @Email,
        Telefono = @Telefono, Direccion = @Direccion
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Proveedores WHERE Id = @Id;
END
GO

PRINT 'Tabla Proveedores y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 23_pcfactory_proveedores.sql

-- ==== INICIO 24_pcfactory_lineas.sql ====

-- ============================================================
--  24_pcfactory_lineas.sql
--  Tabla LineasEnsamblaje (PC Factory) + procedimientos ABM.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineasEnsamblaje')
CREATE TABLE LineasEnsamblaje (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(200) NULL,
    Disponible  BIT NOT NULL CONSTRAINT DF_Linea_Disp DEFAULT (1)
);
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Agregar
    @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible)
    VALUES (@Nombre, @Descripcion, @Disponible);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Modificar
    @Id INT, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE LineasEnsamblaje
    SET Nombre = @Nombre, Descripcion = @Descripcion, Disponible = @Disponible
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

PRINT 'Tabla LineasEnsamblaje y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 24_pcfactory_lineas.sql

-- ==== INICIO 25_pcfactory_produccion.sql ====

-- ============================================================
--  25_pcfactory_produccion.sql   (RFN1: VENTA y PRODUCCIÓN)
--
--  MODELO SEPARADO EN DOS DOCUMENTOS
--  ---------------------------------
--   1) VENTA (Ventas + Pagos)         → la registra el RECEPCIONISTA.
--      Cliente + computadora (estándar o armada en el momento) + seña
--      del 50% + saldo final al retirar.
--   2) ORDEN DE PRODUCCIÓN            → la registra el GERENTE sobre una
--      venta YA SEÑADA, con la fecha de entrega comprometida, y avanza
--      por planificación, ensamblaje, cierre con control de calidad y
--      entrega.
--
--  La orden NO duplica cliente ni computadora: apunta a la venta.
--
--  Estados de VENTA (int):  0 Pendiente, 1 Señada, 2 EnProduccion,
--                           3 Entregada, 4 Anulada.
--  Estados de OP (int):     0 Pendiente, 1 Planificada, 2 EnEnsamblaje,
--                           3 Finalizada, 4 Entregada, 5 EnRevision.
--  Tipo de pago (int):      0 Sena, 1 SaldoFinal.
--  Forma de pago (int):     0 Efectivo, 1 Transferencia, 2 Tarjeta.
-- ============================================================

-- ── Computadora solicitada (estándar o personalizada) ────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Computadoras')
CREATE TABLE Computadoras (
    Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre            NVARCHAR(150) NULL,
    TipoConfiguracion INT           NOT NULL,   -- 0 Estandar, 1 Personalizada
    PrecioTotal       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Comp_Prec DEFAULT (0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComputadoraComponentes')
CREATE TABLE ComputadoraComponentes (
    IdComputadora    INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Cantidad         INT          NOT NULL CONSTRAINT DF_CompComp_Cant DEFAULT (1),
    CONSTRAINT PK_CompComp PRIMARY KEY (IdComputadora, CodigoComponente),
    CONSTRAINT FK_CompComp_Comp FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_CompComp_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('ComputadoraComponentes') AND name = 'Cantidad')
    ALTER TABLE ComputadoraComponentes
        ADD Cantidad INT NOT NULL CONSTRAINT DF_CompComp_Cant DEFAULT (1);
GO

-- ── VENTA (CU01) ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ventas')
CREATE TABLE Ventas (
    NumeroVenta          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DniCliente           NVARCHAR(20)  NOT NULL,
    IdComputadora        INT           NOT NULL,
    FechaVenta           DATETIME      NOT NULL CONSTRAINT DF_Venta_Fecha DEFAULT (GETDATE()),
    FechaEntregaEstimada DATE          NOT NULL,
    Estado               INT           NOT NULL CONSTRAINT DF_Venta_Estado DEFAULT (0),
    UsuarioRegistro      NVARCHAR(150) NULL,
    CONSTRAINT FK_Venta_Cliente     FOREIGN KEY (DniCliente)    REFERENCES Clientes(Dni),
    CONSTRAINT FK_Venta_Computadora FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id)
);
GO

-- ── Pagos de la venta: seña (CU03) y saldo final (CU07) ──────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Pagos')
CREATE TABLE Pagos (
    Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroVenta  INT           NOT NULL,
    Tipo         INT           NOT NULL,   -- 0 Sena, 1 SaldoFinal
    NumeroRecibo NVARCHAR(30)  NULL,
    Monto        DECIMAL(12,2) NOT NULL,
    FormaPago    INT           NOT NULL CONSTRAINT DF_Pago_Forma DEFAULT (0),
    Referencia   NVARCHAR(100) NULL,
    Fecha        DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    Usuario      NVARCHAR(150) NULL,
    CONSTRAINT FK_Pago_Venta FOREIGN KEY (NumeroVenta) REFERENCES Ventas(NumeroVenta)
);
GO

-- Una sola seña por venta.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Pago_SenaUnica')
    CREATE UNIQUE INDEX UX_Pago_SenaUnica
        ON Pagos (NumeroVenta) WHERE Tipo = 0;
GO

-- ── ORDEN DE PRODUCCIÓN (CU04 → CU07) ────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesProduccion')
CREATE TABLE OrdenesProduccion (
    NumeroOrden          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroVenta          INT           NOT NULL,
    FechaRegistro        DATETIME      NOT NULL CONSTRAINT DF_OP_FReg DEFAULT (GETDATE()),
    FechaEntregaEstimada DATE          NOT NULL,
    Estado               INT           NOT NULL CONSTRAINT DF_OP_Estado DEFAULT (0),
    -- Planificación (CU05)
    IdLinea              INT           NULL,
    FechaInicioPrevista  DATE          NULL,
    ResponsableTecnico   NVARCHAR(150) NULL,
    -- Cierre de producción (CU06)
    NumeroSerie          NVARCHAR(50)  NULL,
    FechaCierre          DATETIME      NULL,
    CcEncendido          BIT           NOT NULL CONSTRAINT DF_OP_CcEnc DEFAULT (0),
    CcConexiones         BIT           NOT NULL CONSTRAINT DF_OP_CcCon DEFAULT (0),
    CcSistemaOperativo   BIT           NOT NULL CONSTRAINT DF_OP_CcSO  DEFAULT (0),
    CcDrivers            BIT           NOT NULL CONSTRAINT DF_OP_CcDrv DEFAULT (0),
    CcObservaciones      NVARCHAR(500) NULL,
    CcFecha              DATETIME      NULL,
    CcResponsable        NVARCHAR(150) NULL,
    CONSTRAINT FK_OP_Venta FOREIGN KEY (NumeroVenta) REFERENCES Ventas(NumeroVenta),
    CONSTRAINT FK_OP_Linea FOREIGN KEY (IdLinea)     REFERENCES LineasEnsamblaje(Id),
    CONSTRAINT UQ_OP_Venta UNIQUE (NumeroVenta)      -- una orden por venta
);
GO

-- ============================================================
--  SPs · Computadora
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Computadoras_Agregar
    @Nombre NVARCHAR(150), @TipoConfiguracion INT, @PrecioTotal DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal)
    VALUES (@Nombre, @TipoConfiguracion, @PrecioTotal);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_AgregarComponente
    @IdComputadora INT, @CodigoComponente NVARCHAR(50), @Cantidad INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ComputadoraComponentes
               WHERE IdComputadora = @IdComputadora AND CodigoComponente = @CodigoComponente)
        UPDATE ComputadoraComponentes
        SET    Cantidad = Cantidad + @Cantidad
        WHERE  IdComputadora = @IdComputadora AND CodigoComponente = @CodigoComponente;
    ELSE
        INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente, Cantidad)
        VALUES (@IdComputadora, @CodigoComponente, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerComponentes
    @IdComputadora INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockReservado, cc.Cantidad
    FROM   Componentes c
    INNER JOIN ComputadoraComponentes cc ON cc.CodigoComponente = c.Codigo
    WHERE  cc.IdComputadora = @IdComputadora
    ORDER BY c.Tipo, c.Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, TipoConfiguracion, PrecioTotal FROM Computadoras WHERE Id = @Id;
END
GO

-- ============================================================
--  SPs · Venta (CU01)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Ventas_Agregar
    @DniCliente NVARCHAR(20), @IdComputadora INT,
    @FechaEntregaEstimada DATE, @UsuarioRegistro NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Ventas (DniCliente, IdComputadora, FechaEntregaEstimada, Estado, UsuarioRegistro)
    VALUES (@DniCliente, @IdComputadora, @FechaEntregaEstimada, 0, @UsuarioRegistro);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_Ventas_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.NumeroVenta, v.DniCliente, v.IdComputadora, v.FechaVenta,
           v.FechaEntregaEstimada, v.Estado, v.UsuarioRegistro,
           op.NumeroOrden AS NumeroOrdenProduccion
    FROM   Ventas v
    LEFT   JOIN OrdenesProduccion op ON op.NumeroVenta = v.NumeroVenta
    ORDER  BY v.NumeroVenta DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Ventas_ObtenerPorNumero
    @NumeroVenta INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.NumeroVenta, v.DniCliente, v.IdComputadora, v.FechaVenta,
           v.FechaEntregaEstimada, v.Estado, v.UsuarioRegistro,
           op.NumeroOrden AS NumeroOrdenProduccion
    FROM   Ventas v
    LEFT   JOIN OrdenesProduccion op ON op.NumeroVenta = v.NumeroVenta
    WHERE  v.NumeroVenta = @NumeroVenta;
END
GO

-- CU04 (escenario principal, paso 2): ventas con seña registrada y sin orden asociada.
CREATE OR ALTER PROCEDURE sp_Ventas_ObtenerParaProduccion
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.NumeroVenta, v.DniCliente, v.IdComputadora, v.FechaVenta,
           v.FechaEntregaEstimada, v.Estado, v.UsuarioRegistro,
           CAST(NULL AS INT) AS NumeroOrdenProduccion
    FROM   Ventas v
    WHERE  v.Estado = 1                                   -- Señada
      AND  EXISTS (SELECT 1 FROM Pagos p WHERE p.NumeroVenta = v.NumeroVenta AND p.Tipo = 0)
      AND  NOT EXISTS (SELECT 1 FROM OrdenesProduccion op WHERE op.NumeroVenta = v.NumeroVenta)
    ORDER  BY v.NumeroVenta DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Ventas_CambiarEstado
    @NumeroVenta INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Ventas SET Estado = @Estado WHERE NumeroVenta = @NumeroVenta;
END
GO

-- ============================================================
--  SPs · Pagos (CU03 seña / CU07 saldo final)
--  Genera el número de comprobante correlativo y lo devuelve.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_Pagos_Agregar
    @NumeroVenta INT, @Tipo INT, @Monto DECIMAL(12,2),
    @FormaPago INT = 0, @Referencia NVARCHAR(100) = NULL, @Usuario NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Pagos (NumeroVenta, Tipo, Monto, FormaPago, Referencia, Usuario)
    VALUES (@NumeroVenta, @Tipo, @Monto, @FormaPago, @Referencia, @Usuario);

    DECLARE @Id INT = CAST(SCOPE_IDENTITY() AS INT);
    DECLARE @Nro NVARCHAR(30) =
        CASE WHEN @Tipo = 0 THEN N'REC-' ELSE N'FAC-' END +
        RIGHT(N'00000000' + CAST(@Id AS NVARCHAR(10)), 8);

    UPDATE Pagos SET NumeroRecibo = @Nro WHERE Id = @Id;

    SELECT Id, NumeroVenta, Tipo, NumeroRecibo, Monto, FormaPago, Referencia, Fecha, Usuario
    FROM   Pagos WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerPorVenta
    @NumeroVenta INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, NumeroVenta, Tipo, NumeroRecibo, Monto, FormaPago, Referencia, Fecha, Usuario
    FROM   Pagos WHERE NumeroVenta = @NumeroVenta ORDER BY Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, NumeroVenta, Tipo, NumeroRecibo, Monto, FormaPago, Referencia, Fecha, Usuario
    FROM   Pagos ORDER BY Id;
END
GO

-- ============================================================
--  SPs · Orden de producción (CU04 → CU07)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_OP_Agregar
    @NumeroVenta INT, @FechaEntregaEstimada DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesProduccion (NumeroVenta, FechaEntregaEstimada, Estado)
    VALUES (@NumeroVenta, @FechaEntregaEstimada, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, NumeroVenta, FechaRegistro, FechaEntregaEstimada, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico,
           NumeroSerie, FechaCierre,
           CcEncendido, CcConexiones, CcSistemaOperativo, CcDrivers,
           CcObservaciones, CcFecha, CcResponsable
    FROM   OrdenesProduccion ORDER BY NumeroOrden DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorNumero
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, NumeroVenta, FechaRegistro, FechaEntregaEstimada, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico,
           NumeroSerie, FechaCierre,
           CcEncendido, CcConexiones, CcSistemaOperativo, CcDrivers,
           CcObservaciones, CcFecha, CcResponsable
    FROM   OrdenesProduccion WHERE NumeroOrden = @NumeroOrden;
END
GO

-- CU07: las órdenes en un estado dado. Lo usa la pantalla "Entrega de
-- computadoras" para listar las Finalizadas (listas para retirar) y las
-- Entregadas (histórico).
CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorEstado
    @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, NumeroVenta, FechaRegistro, FechaEntregaEstimada, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico,
           NumeroSerie, FechaCierre,
           CcEncendido, CcConexiones, CcSistemaOperativo, CcDrivers,
           CcObservaciones, CcFecha, CcResponsable
    FROM   OrdenesProduccion WHERE Estado = @Estado ORDER BY NumeroOrden DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorVenta
    @NumeroVenta INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, NumeroVenta, FechaRegistro, FechaEntregaEstimada, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico,
           NumeroSerie, FechaCierre,
           CcEncendido, CcConexiones, CcSistemaOperativo, CcDrivers,
           CcObservaciones, CcFecha, CcResponsable
    FROM   OrdenesProduccion WHERE NumeroVenta = @NumeroVenta;
END
GO

-- CU05: asigna línea, fecha de inicio y responsable → estado Planificada.
CREATE OR ALTER PROCEDURE sp_OP_Planificar
    @NumeroOrden INT, @IdLinea INT, @FechaInicioPrevista DATE, @ResponsableTecnico NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET IdLinea = @IdLinea, FechaInicioPrevista = @FechaInicioPrevista,
        ResponsableTecnico = @ResponsableTecnico, Estado = 1   -- Planificada
    WHERE NumeroOrden = @NumeroOrden;
END
GO

-- Deshace la planificación: la orden vuelve a Pendiente y se suelta la línea.
CREATE OR ALTER PROCEDURE sp_OP_Desplanificar
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET IdLinea = NULL, FechaInicioPrevista = NULL, ResponsableTecnico = NULL, Estado = 0
    WHERE NumeroOrden = @NumeroOrden;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_CambiarEstado
    @NumeroOrden INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET Estado = @Estado WHERE NumeroOrden = @NumeroOrden;
END
GO

-- CU06: guarda el checklist de control de calidad (aprobado o no).
CREATE OR ALTER PROCEDURE sp_OP_RegistrarControlCalidad
    @NumeroOrden INT,
    @Encendido BIT, @Conexiones BIT, @SistemaOperativo BIT, @Drivers BIT,
    @Observaciones NVARCHAR(500) = NULL, @Responsable NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET CcEncendido        = @Encendido,
        CcConexiones       = @Conexiones,
        CcSistemaOperativo = @SistemaOperativo,
        CcDrivers          = @Drivers,
        CcObservaciones    = @Observaciones,
        CcResponsable      = @Responsable,
        CcFecha            = GETDATE()
    WHERE NumeroOrden = @NumeroOrden;
END
GO

-- CU06: control aprobado → número de serie, fecha de cierre y estado Finalizada.
CREATE OR ALTER PROCEDURE sp_OP_Cerrar
    @NumeroOrden INT, @NumeroSerie NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET NumeroSerie = @NumeroSerie, FechaCierre = GETDATE(), Estado = 3  -- Finalizada
    WHERE NumeroOrden = @NumeroOrden;
END
GO

PRINT 'Tablas y procedimientos de Venta y Producción (RFN1) creados/actualizados.';
GO

-- <<< FIN 25_pcfactory_produccion.sql

-- ==== INICIO 26_pcfactory_compras.sql ====

-- ============================================================
--  26_pcfactory_compras.sql   (RFN2: COMPRA DE INSUMOS)
--
--  Reescrito tras la FUSIÓN Componente + Insumo: lo que se compra ya no es
--  un "insumo" aparte, es el mismo Componente06AV (Stock + StockMinimo).
--
--  Circuito: el sistema detecta componentes en o por debajo del mínimo →
--  el repositor arma la ORDEN DE COMPRA → pide COTIZACIÓN a un proveedor →
--  el gerente de compras la aprueba o desaprueba → al recibir la mercadería
--  se registra la FACTURA DE COMPRA y se suma el stock realmente recibido.
--
--  Claves: Id string legible (OC-2026-0001) + número de negocio correlativo.
--  Estados de OC (int):  0 Pendiente, 1 Enviada, 2 Finalizada.
--  Estados de cotización (int): 0 PorAprobar, 1 Aprobado, 2 Desaprobada.
--
--  Requiere: Componentes (21), Proveedores (23) y Usuarios (00_schema).
-- ============================================================

SET NOCOUNT ON;
GO

-- ── Limpieza del modelo viejo basado en Insumos ──────────────
IF OBJECT_ID('FacturaCompraDetalle','U')    IS NOT NULL DROP TABLE FacturaCompraDetalle;
IF OBJECT_ID('FacturaCompra','U')           IS NOT NULL DROP TABLE FacturaCompra;
IF OBJECT_ID('PedidoCotizacionDetalle','U') IS NOT NULL DROP TABLE PedidoCotizacionDetalle;
IF OBJECT_ID('PedidosCotizacion','U')       IS NOT NULL DROP TABLE PedidosCotizacion;
IF OBJECT_ID('OrdenCompraDetalle','U')      IS NOT NULL DROP TABLE OrdenCompraDetalle;
IF OBJECT_ID('OrdenesCompra','U')           IS NOT NULL DROP TABLE OrdenesCompra;
IF OBJECT_ID('Seq_NumeroCompra','SO')       IS NOT NULL DROP SEQUENCE Seq_NumeroCompra;
GO

-- Secuencia del número de compra visible al usuario
CREATE SEQUENCE Seq_NumeroCompra AS INT START WITH 1 INCREMENT BY 1;
GO

-- ── 6) OrdenesCompra (PK string Id + NumeroCompra correlativo) ───────────────
CREATE TABLE OrdenesCompra (
    Id           NVARCHAR(30) NOT NULL PRIMARY KEY,   -- "OC-2026-0001"
    NumeroCompra INT          NOT NULL CONSTRAINT DF_OC_Num DEFAULT (NEXT VALUE FOR Seq_NumeroCompra),
    FechaLimite  DATE         NOT NULL,
    DniRepositor NVARCHAR(20) NULL,                   -- FK Usuarios (repositor solicitante)
    Estado       INT          NOT NULL CONSTRAINT DF_OC_Estado DEFAULT (0),  -- EstadoOrdenCompra06AV
    FechaCierre  DATETIME     NULL,
    CONSTRAINT UQ_OC_Numero UNIQUE (NumeroCompra),
    CONSTRAINT FK_OC_Repositor FOREIGN KEY (DniRepositor) REFERENCES Usuarios(Dni)
);
GO

CREATE TABLE OrdenCompraDetalle (
    IdOrdenCompra    NVARCHAR(30) NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Cantidad         INT          NOT NULL,
    CONSTRAINT PK_OCD PRIMARY KEY (IdOrdenCompra, CodigoComponente),
    CONSTRAINT FK_OCD_OC   FOREIGN KEY (IdOrdenCompra)    REFERENCES OrdenesCompra(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OCD_Comp FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 7) PedidosCotizacion (FK a OC por Id, 1..N por orden, + gerente aprobador) ─
CREATE TABLE PedidosCotizacion (
    Numero              NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "CO-2026-0001"
    IdOrdenCompra       NVARCHAR(30)  NOT NULL,               -- FK OrdenesCompra
    IdProveedor         INT           NOT NULL,               -- FK Proveedores
    Estado              INT           NOT NULL CONSTRAINT DF_CO_Estado DEFAULT (0),  -- EstadoCotizacion06AV
    Costo               DECIMAL(18,2) NOT NULL CONSTRAINT DF_CO_Costo DEFAULT (0),
    Condiciones         NVARCHAR(500) NULL,
    FechaEmision        DATETIME      NOT NULL CONSTRAINT DF_CO_Fecha DEFAULT (GETDATE()),
    DniGerenteAprobador NVARCHAR(20)  NULL,                   -- FK Usuarios (null hasta aprobar/desaprobar)
    CONSTRAINT FK_CO_OC        FOREIGN KEY (IdOrdenCompra)       REFERENCES OrdenesCompra(Id),
    CONSTRAINT FK_CO_Proveedor FOREIGN KEY (IdProveedor)        REFERENCES Proveedores(Id),
    CONSTRAINT FK_CO_Gerente   FOREIGN KEY (DniGerenteAprobador) REFERENCES Usuarios(Dni)
);
GO

-- ── 8) FacturaCompra (recepción de mercadería) + detalle ─────────────────────
CREATE TABLE FacturaCompra (
    NumeroFactura NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "FC-2026-0001"
    IdOrdenCompra NVARCHAR(30)  NOT NULL,               -- FK OrdenesCompra
    FechaEmision  DATETIME      NOT NULL,
    FechaEntrega  DATETIME      NOT NULL,
    Total         DECIMAL(18,2) NOT NULL CONSTRAINT DF_FC_Total DEFAULT (0),
    Observaciones NVARCHAR(1000) NULL,                  -- diferencias pedido vs recibido
    CONSTRAINT FK_FC_OC FOREIGN KEY (IdOrdenCompra) REFERENCES OrdenesCompra(Id)
);
GO

CREATE TABLE FacturaCompraDetalle (
    NumeroFactura    NVARCHAR(30) NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Cantidad         INT          NOT NULL,             -- cantidad REAL recibida
    CONSTRAINT PK_FCD PRIMARY KEY (NumeroFactura, CodigoComponente),
    CONSTRAINT FK_FCD_FC   FOREIGN KEY (NumeroFactura)    REFERENCES FacturaCompra(NumeroFactura) ON DELETE CASCADE,
    CONSTRAINT FK_FCD_Comp FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ============================================================
--  SPs · Compras
-- ============================================================
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Agregar
    @Id NVARCHAR(30), @FechaLimite DATE, @DniRepositor NVARCHAR(20), @NumeroCompra INT OUTPUT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado)
    VALUES (@Id, @FechaLimite, @DniRepositor, 0);
    SELECT @NumeroCompra = NumeroCompra FROM OrdenesCompra WHERE Id=@Id;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenCompra_AgregarDetalle
    @IdOrdenCompra NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad)
    VALUES (@IdOrdenCompra, @CodigoComponente, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_CambiarEstado @Id NVARCHAR(30), @Estado INT AS
BEGIN SET NOCOUNT ON; UPDATE OrdenesCompra SET Estado=@Estado WHERE Id=@Id; END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Cerrar @Id NVARCHAR(30), @FechaCierre DATETIME AS
BEGIN SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado=2 /*Finalizada*/, FechaCierre=@FechaCierre WHERE Id=@Id;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroCompra, FechaLimite, DniRepositor, Estado, FechaCierre
    FROM OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenCompra_ObtenerDetalle @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT d.CodigoComponente, d.Cantidad, c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockMinimo, c.Tipo
    FROM OrdenCompraDetalle d JOIN Componentes c ON c.Codigo=d.CodigoComponente
    WHERE d.IdOrdenCompra=@IdOrdenCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @Numero NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @IdProveedor INT,
    @Costo DECIMAL(18,2), @Condiciones NVARCHAR(500) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones)
    VALUES (@Numero, @IdOrdenCompra, @IdProveedor, 0 /*PorAprobar*/, @Costo, @Condiciones);
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero NVARCHAR(30), @Estado INT, @DniGerenteAprobador NVARCHAR(20) AS
BEGIN SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado=@Estado, DniGerenteAprobador=@DniGerenteAprobador WHERE Numero=@Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorOrden @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion WHERE IdOrdenCompra=@IdOrdenCompra ORDER BY FechaEmision DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion ORDER BY FechaEmision DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_Agregar
    @NumeroFactura NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @FechaEmision DATETIME,
    @FechaEntrega DATETIME, @Total DECIMAL(18,2), @Observaciones NVARCHAR(1000) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones)
    VALUES (@NumeroFactura, @IdOrdenCompra, @FechaEmision, @FechaEntrega, @Total, @Observaciones);
END
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_AgregarDetalle
    @NumeroFactura NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompraDetalle (NumeroFactura, CodigoComponente, Cantidad)
    VALUES (@NumeroFactura, @CodigoComponente, @Cantidad);
END
GO

PRINT 'Tablas y procedimientos de Compras (RFN2) creados/actualizados.';
GO

-- <<< FIN 26_pcfactory_compras.sql

-- ==== INICIO 27_pcfactory_patentes.sql ====

-- ============================================================
--  27_pcfactory_patentes.sql
--  Patentes de acceso a las pantallas del dominio PC Factory.
--
--  Cada patente habilita la visibilidad de un módulo del sidebar
--  (ver ConfigurarModulosPcFactory / AgregarModuloPcFactory en
--   FRMMain.cs). Sin la patente, el módulo NO se agrega al menú.
--
--  Los Ids deben coincidir EXACTAMENTE con los valores del enum
--  PatenteEnum06AV (SER/Enums/PatenteEnum06AV.cs).
--
--  Se asignan directamente al rol Administrador (RolPatentes), igual
--  que GestionarRoles / GestionarFamilias.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        ('GestionarClientes',          'Gestionar clientes'),
        ('GestionarComponentes',       'Gestionar componentes'),
        ('GestionarInsumos',           'Gestionar insumos'),
        ('GestionarProveedores',       'Gestionar proveedores'),
        ('GestionarLineasEnsamblaje',  'Gestionar líneas de ensamblaje'),
        ('GestionarVentas',            'Gestionar ventas'),
        ('GestionarEntregas',          'Entregar computadoras'),
        ('GestionarProduccion',        'Gestionar producción'),
        ('GestionarCompras',           'Gestionar compras'),
        ('GestionarModelosEstandar',   'Gestionar modelos estándar');

    INSERT INTO Patentes (Id, Descripcion)
    SELECT p.Id, p.Descripcion
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

    -- ── ROL ADMINISTRADOR → PATENTES DIRECTAS ────────────────
    DECLARE @IdRolAdmin NVARCHAR(450);

    SELECT @IdRolAdmin = Id
    FROM   Roles
    WHERE  Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
        RAISERROR('No se encontró el rol Administrador (por Descripcion o Codigo=ADM).', 16, 1);

    INSERT INTO RolPatentes (IdRol, IdPatente)
    SELECT @IdRolAdmin, p.Id
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM RolPatentes rp
                       WHERE rp.IdRol = @IdRolAdmin AND rp.IdPatente = p.Id);

    COMMIT TRANSACTION;
    PRINT 'Seed de patentes PC Factory completado (asignadas al rol Administrador).';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed de patentes PC Factory: ' + ERROR_MESSAGE();
END CATCH
GO

-- <<< FIN 27_pcfactory_patentes.sql

-- ==== INICIO 28_pcfactory_modelos.sql ====

-- ============================================================
--  28_pcfactory_modelos.sql
--  Catálogo de modelos de computadora ESTÁNDAR (RFN1). Cada modelo
--  tiene un nombre y una lista de componentes predefinidos. Al armar
--  una orden "Estándar" se elige un modelo y se cargan sus componentes.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModelosEstandar')
CREATE TABLE ModelosEstandar (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(300) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModeloEstandarComponentes')
CREATE TABLE ModeloEstandarComponentes (
    IdModelo         INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_ModeloComp PRIMARY KEY (IdModelo, CodigoComponente),
    CONSTRAINT FK_ModeloComp_Modelo FOREIGN KEY (IdModelo)
        REFERENCES ModelosEstandar(Id),
    CONSTRAINT FK_ModeloComp_Comp FOREIGN KEY (CodigoComponente)
        REFERENCES Componentes(Codigo)
);
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion FROM ModelosEstandar ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Agregar
    @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ModelosEstandar (Nombre, Descripcion) VALUES (@Nombre, @Descripcion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ModelosEstandar SET Nombre = @Nombre, Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @Id;
    DELETE FROM ModelosEstandar WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_AgregarComponente
    @IdModelo INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes
                   WHERE IdModelo = @IdModelo AND CodigoComponente = @CodigoComponente)
        INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
        VALUES (@IdModelo, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_QuitarComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

-- Patente de acceso al ABM de modelos estándar (coincide con PatenteEnum06AV).
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarModelosEstandar')
    INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarModelosEstandar', 'Gestionar modelos estándar');
GO

DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';
IF @IdRolAdmin IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarModelosEstandar')
    INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarModelosEstandar');
GO

PRINT 'Catálogo de modelos estándar (RFN1) creado/actualizado.';
GO

-- <<< FIN 28_pcfactory_modelos.sql


PRINT '>>> PC Factory: todo creado/actualizado.';
GO
