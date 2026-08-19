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
