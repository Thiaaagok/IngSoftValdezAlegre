-- ============================================================
--  25_pcfactory_produccion.sql   (RFN1: Venta / Producción)
--  Computadoras + sus componentes, Órdenes de Producción y Pagos.
--  Estados de OP (int): 0 Pendiente, 1 Planificada, 2 EnEnsamblaje,
--                       3 Finalizada, 4 Entregada.
--  Tipo de pago (int): 0 Sena, 1 SaldoFinal.
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
    IdComputadora   INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_CompComp PRIMARY KEY (IdComputadora, CodigoComponente),
    CONSTRAINT FK_CompComp_Comp FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_CompComp_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── Orden de producción ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesProduccion')
CREATE TABLE OrdenesProduccion (
    NumeroOrden         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DniCliente          NVARCHAR(20)  NOT NULL,
    IdComputadora       INT           NOT NULL,
    FechaEntrega        DATE          NOT NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OP_Estado DEFAULT (0),
    IdLinea             INT           NULL,
    FechaInicioPrevista DATE          NULL,
    ResponsableTecnico  NVARCHAR(150) NULL,
    CONSTRAINT FK_OP_Cliente FOREIGN KEY (DniCliente) REFERENCES Clientes(Dni),
    CONSTRAINT FK_OP_Computadora FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_OP_Linea FOREIGN KEY (IdLinea) REFERENCES LineasEnsamblaje(Id)
);
GO

-- ── Pagos (seña / saldo final) ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Pagos')
CREATE TABLE Pagos (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroOrden INT           NOT NULL,
    Tipo        INT           NOT NULL,   -- 0 Sena, 1 SaldoFinal
    Monto       DECIMAL(12,2) NOT NULL,
    Fecha       DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    CONSTRAINT FK_Pago_OP FOREIGN KEY (NumeroOrden) REFERENCES OrdenesProduccion(NumeroOrden)
);
GO

-- ── SPs Computadora ──────────────────────────────────────────
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
    @IdComputadora INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ComputadoraComponentes
                   WHERE IdComputadora = @IdComputadora AND CodigoComponente = @CodigoComponente)
        INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente)
        VALUES (@IdComputadora, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerComponentes
    @IdComputadora INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario, c.StockDisponible
    FROM   Componentes c
    INNER JOIN ComputadoraComponentes cc ON cc.CodigoComponente = c.Codigo
    WHERE  cc.IdComputadora = @IdComputadora;
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

-- ── SPs Orden de producción ──────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OP_Agregar
    @DniCliente NVARCHAR(20), @IdComputadora INT, @FechaEntrega DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesProduccion (DniCliente, IdComputadora, FechaEntrega, Estado)
    VALUES (@DniCliente, @IdComputadora, @FechaEntrega, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion ORDER BY NumeroOrden DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorNumero
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion WHERE NumeroOrden = @NumeroOrden;
END
GO

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

CREATE OR ALTER PROCEDURE sp_OP_CambiarEstado
    @NumeroOrden INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET Estado = @Estado WHERE NumeroOrden = @NumeroOrden;
END
GO

-- ── SPs Pagos ────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Pagos_Agregar
    @NumeroOrden INT, @Tipo INT, @Monto DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Pagos (NumeroOrden, Tipo, Monto) VALUES (@NumeroOrden, @Tipo, @Monto);
END
GO

CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerPorOrden
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, NumeroOrden, Tipo, Monto, Fecha FROM Pagos WHERE NumeroOrden = @NumeroOrden;
END
GO

PRINT 'Tablas y procedimientos de Producción (RFN1) creados/actualizados.';
GO
