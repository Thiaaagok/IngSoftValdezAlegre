using System;

namespace BE
{
    /// <summary>
    /// Factura de venta: documenta la venta/entrega de una computadora armada al cliente.
    /// Total es un SNAPSHOT (se congela al emitir; no se recalcula aunque cambien precios en
    /// la orden). Los pagos NO se duplican: se navegan desde OrdenProduccion06AV.Pagos.
    /// </summary>
    public class FacturaVenta06AV
    {
        public string NumeroFactura { get; set; }
        public string NumeroOrden { get; set; }        // FK a OrdenProduccion06AV.Id
        public DateTime FechaEmision { get; set; }
        public decimal Total { get; set; }             // SNAPSHOT del PrecioTotal de la orden al emitir

        public override string ToString() => $"Factura venta {NumeroFactura} (OP {NumeroOrden}) - ${Total:0.00}";
    }
}
