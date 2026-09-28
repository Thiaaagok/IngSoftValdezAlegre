using System;

namespace BE
{
    /// <summary>
    /// Factura de venta de la entrega (CU07). Ninguna capa la usa: la factura en PDF
    /// se arma con la venta y su pago de saldo final (ComprobantePcFactory06AV), y el
    /// número FAC-… lo genera sp_Pagos_Agregar. Los pagos se navegan desde
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
