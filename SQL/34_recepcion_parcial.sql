-- ============================================================
--  34_recepcion_parcial.sql
--  RECEPCIÓN DE MERCADERÍA CON CONTROL (RFN2, paso 5).
--
--  Antes, "recibir" una orden de compra asumía que llegaba todo lo pedido:
--  sumaba el stock completo y cerraba la orden. Ahora se declara qué llegó
--  realmente, como un control de recepción:
--    · se suma al stock sólo lo recibido;
--    · si llegó todo, la orden queda Finalizada (Estado = 2);
--    · si faltó algo, queda Recibida parcial (Estado = 3) y se puede recibir
--      el resto más adelante contra la MISMA orden.
--
--  Para saber cuánto falta hay que poder leer las recepciones ya registradas,
--  y para eso este script agrega los dos procedimientos de lectura que no
--  existían. NO cambia ninguna tabla: el detalle por componente y cantidad ya
--  se guardaba en FacturaCompraDetalle, y el estado nuevo es sólo un valor más
--  en la columna Estado de OrdenesCompra.
--
--  Es idempotente.
-- ============================================================

-- Recepciones registradas contra una orden de compra, de la más vieja a la más nueva.
CREATE OR ALTER PROCEDURE sp_FacturaCompra_ObtenerPorOrden
    @IdOrdenCompra NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  f.NumeroFactura,
            f.IdOrdenCompra,
            f.FechaEmision,
            f.FechaEntrega,
            f.Total,
            f.Observaciones
    FROM    FacturaCompra f
    WHERE   f.IdOrdenCompra = @IdOrdenCompra
    ORDER BY f.FechaEmision;
END
GO

-- Detalle de una recepción: qué componente llegó y cuántas unidades.
-- Devuelve las mismas columnas de componente que sp_OrdenCompra_ObtenerDetalle,
-- para que la capa MPP use el mismo mapeo.
CREATE OR ALTER PROCEDURE sp_FacturaCompra_ObtenerDetalle
    @NumeroFactura NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  d.CodigoComponente, d.Cantidad,
            c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario,
            c.Stock, c.StockMinimo, c.Tipo
    FROM    FacturaCompraDetalle d
            JOIN Componentes c ON c.Codigo = d.CodigoComponente
    WHERE   d.NumeroFactura = @NumeroFactura
    ORDER BY c.Descripcion;
END
GO

PRINT 'Procedimientos de lectura de recepciones creados/actualizados.';
PRINT 'Estados de OrdenesCompra: 0=Pendiente, 1=Enviada, 2=Finalizada, 3=Recibida parcial.';
GO

-- ── Verificación: recepciones por orden y cuánto falta ──────
SELECT  oc.NumeroCompra,
        CASE oc.Estado
             WHEN 0 THEN 'Pendiente'
             WHEN 1 THEN 'Enviada'
             WHEN 2 THEN 'Finalizada'
             WHEN 3 THEN 'Recibida parcial'
             ELSE CAST(oc.Estado AS VARCHAR(10))
        END                                    AS Estado,
        d.CodigoComponente,
        d.Cantidad                             AS Pedido,
        ISNULL(SUM(fd.Cantidad), 0)            AS Recibido,
        d.Cantidad - ISNULL(SUM(fd.Cantidad), 0) AS Pendiente
FROM    OrdenesCompra oc
        JOIN OrdenCompraDetalle d ON d.IdOrdenCompra = oc.Id
        LEFT JOIN FacturaCompra f ON f.IdOrdenCompra = oc.Id
        LEFT JOIN FacturaCompraDetalle fd
               ON fd.NumeroFactura = f.NumeroFactura
              AND fd.CodigoComponente = d.CodigoComponente
GROUP BY oc.NumeroCompra, oc.Estado, d.CodigoComponente, d.Cantidad
ORDER BY oc.NumeroCompra, d.CodigoComponente;
GO
