-- 38_cotizacion_precio_por_item.sql
-- RFN2: la cotización guarda el precio unitario de cada componente de la orden.
--
--   · Tabla PedidoCotizacionDetalle: una línea por componente con su cantidad y el
--     precio unitario que ofreció el proveedor.
--   · sp_Cotizacion_AgregarConDetalle: graba cabecera y líneas en una transacción;
--     PedidosCotizacion.Costo queda como la suma de precio × cantidad.
--   · sp_Cotizacion_ObtenerDetalle: las líneas con los datos del componente.
--
-- Las cotizaciones anteriores quedan como estaban (solo total): la aplicación las
-- sigue mostrando y, al recibir, prorratea el total por unidades.
-- Requiere 26_pcfactory_compras.sql. Idempotente.
--
-- La tabla nueva es protegida (dígito verificador): después de correrlo hay que
-- recalcularlo.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PedidoCotizacionDetalle')
CREATE TABLE PedidoCotizacionDetalle (
    NumeroCotizacion NVARCHAR(30)  NOT NULL,
    CodigoComponente NVARCHAR(50)  NOT NULL,
    Cantidad         INT           NOT NULL,
    PrecioUnitario   DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_PCD PRIMARY KEY (NumeroCotizacion, CodigoComponente),
    CONSTRAINT FK_PCD_Cotizacion FOREIGN KEY (NumeroCotizacion) REFERENCES PedidosCotizacion(Numero) ON DELETE CASCADE,
    CONSTRAINT FK_PCD_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo),
    CONSTRAINT CK_PCD_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_PCD_Precio   CHECK (PrecioUnitario > 0)
);
GO

-- @Detalle: <detalle><d c="CPU-001" q="8" p="120.50"/>...</detalle>
-- El total se calcula acá con los mismos subtotales redondeados que usa la BLL.
CREATE OR ALTER PROCEDURE sp_Cotizacion_AgregarConDetalle
    @Numero        NVARCHAR(30),
    @IdOrdenCompra NVARCHAR(30),
    @IdProveedor   INT,
    @Condiciones   NVARCHAR(500),
    @Detalle       XML
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Lineas TABLE (Codigo NVARCHAR(50) PRIMARY KEY, Cantidad INT, Precio DECIMAL(18,2));
    INSERT INTO @Lineas (Codigo, Cantidad, Precio)
    SELECT x.value('@c', 'NVARCHAR(50)'),
           x.value('@q', 'INT'),
           x.value('@p', 'DECIMAL(18,2)')
    FROM   @Detalle.nodes('/detalle/d') AS t(x);

    IF NOT EXISTS (SELECT 1 FROM @Lineas)
        THROW 50001, 'La cotización no tiene líneas.', 1;

    BEGIN TRANSACTION;

    INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones)
    SELECT @Numero, @IdOrdenCompra, @IdProveedor, 0 /*PorAprobar*/,
           SUM(ROUND(Cantidad * Precio, 2)), @Condiciones
    FROM   @Lineas;

    INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
    SELECT @Numero, Codigo, Cantidad, Precio FROM @Lineas;

    COMMIT TRANSACTION;
END
GO

-- PrecioCotizado es el de la oferta; PrecioUnitario, el del catálogo (lo usa el mapeo del componente).
CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerDetalle @Numero NVARCHAR(30) AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.CodigoComponente, d.Cantidad, d.PrecioUnitario AS PrecioCotizado,
           c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario, c.Stock, c.StockMinimo, c.Tipo
    FROM   PedidoCotizacionDetalle d
           INNER JOIN Componentes c ON c.Codigo = d.CodigoComponente
    WHERE  d.NumeroCotizacion = @Numero
    ORDER  BY c.Tipo, d.CodigoComponente;
END
GO

PRINT '>>> 38: precio por ítem en las cotizaciones listo.';
PRINT '    Recordá: recalcular el dígito verificador.';
GO
