using System;

namespace BE
{
    /// <summary>
    /// Factura de venta: documenta la entrega de la computadora al cliente (CU07).
    /// Total es un SNAPSHOT (se congela al emitir; no se recalcula aunque cambien los
    /// precios de los componentes). Los pagos NO se duplican acá: se navegan desde
    /// <see cref="Venta06AV.Pagos"/>.
    /// </summary>
    public class FacturaVenta06AV
    {
        public string NumeroFactura { get; set; }

        /// <summary>FK a <see cref="OrdenProduccion06AV.NumeroOrden"/>.</summary>
        public int NumeroOrden { get; set; }

        /// <summary>FK a <see cref="Venta06AV.NumeroVenta"/>.</summary>
        public int NumeroVenta { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        /// <summary>SNAPSHOT del precio total de la venta al momento de emitir.</summary>
        public decimal Total { get; set; }

        public override string ToString() =>
            $"Factura venta {NumeroFactura} (OP {NumeroOrden}) - ${Total:0.00}";
    }
}
