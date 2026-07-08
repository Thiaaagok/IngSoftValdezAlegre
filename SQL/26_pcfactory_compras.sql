-- ============================================================
--  26_pcfactory_compras.sql   (RFN2: Compras / Insumos)
--  Órdenes de compra (+ detalle) y pedidos de cotización.
--  Estado OC (int):        0 Pendiente, 1 Enviada, 2 Finalizada.
--  Estado Cotización (int):0 PorAprobar, 1 Aprobado, 2 Desaprobada.
--  La cotización referencia la OC y reutiliza su detalle de insumos.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesCompra')
CREATE TABLE OrdenesCompra (
    NumeroCompra        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FechaLimite         DATE          NOT NULL,
    RepositorSolicitante NVARCHAR(150) NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OC_Estado DEFAULT (0),
    FechaCierre         DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenCompraDetalle')
CREATE TABLE OrdenCompraDetalle (
    NumeroCompra INT          NOT NULL,
    CodigoInsumo NVARCHAR(50) NOT NULL,
    Cantidad     INT          NOT NULL,
    CONSTRAINT PK_OCD PRIMARY KEY (NumeroCompra, CodigoInsumo),
    CONSTRAINT FK_OCD_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_OCD_Insumo FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(Codigo)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PedidosCotizacion')
CREATE TABLE PedidosCotizacion (
    Numero       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroCompra INT       NOT NULL,
    IdProveedor  INT       NOT NULL,
    FechaEmision DATETIME  NOT NULL CONSTRAINT DF_Cot_Fecha DEFAULT (GETDATE()),
    Estado       INT       NOT NULL CONSTRAINT DF_Cot_Estado DEFAULT (0),
    CONSTRAINT FK_Cot_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_Cot_Prov FOREIGN KEY (IdProveedor) REFERENCES Proveedores(Id)
);
GO

-- ── SPs Orden de compra ──────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OC_Agregar
    @FechaLimite DATE, @RepositorSolicitante NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (FechaLimite, RepositorSolicitante, Estado)
    VALUES (@FechaLimite, @RepositorSolicitante, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_AgregarDetalle
    @NumeroCompra INT, @CodigoInsumo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM OrdenCompraDetalle
                   WHERE NumeroCompra = @NumeroCompra AND CodigoInsumo = @CodigoInsumo)
        INSERT INTO OrdenCompraDetalle (NumeroCompra, CodigoInsumo, Cantidad)
        VALUES (@NumeroCompra, @CodigoInsumo, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerPorNumero
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerDetalle
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.CodigoInsumo, d.Cantidad, i.Descripcion, i.Stock, i.StockMinimo
    FROM   OrdenCompraDetalle d
    INNER JOIN Insumos i ON i.Codigo = d.CodigoInsumo
    WHERE  d.NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_CambiarEstado
    @NumeroCompra INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = @Estado WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_Finalizar
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = 2, FechaCierre = GETDATE() WHERE NumeroCompra = @NumeroCompra;
END
GO

-- ── SPs Cotización ───────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @NumeroCompra INT, @IdProveedor INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (NumeroCompra, IdProveedor, Estado)
    VALUES (@NumeroCompra, @IdProveedor, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado
    FROM   PedidosCotizacion ORDER BY Numero DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorNumero
    @Numero INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado
    FROM   PedidosCotizacion WHERE Numero = @Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado = @Estado WHERE Numero = @Numero;
END
GO

-- ── Actualización de stock al recibir ────────────────────────
CREATE OR ALTER PROCEDURE sp_Insumos_SumarStock
    @Codigo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumos SET Stock = Stock + @Cantidad WHERE Codigo = @Codigo;
END
GO

PRINT 'Tablas y procedimientos de Compras (RFN2) creados/actualizados.';
GO
