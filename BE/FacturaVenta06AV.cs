using System;

namespace BE
{
    public class FacturaVenta06AV
    {
        public string NumeroFactura { get; set; }

        public int NumeroOrden { get; set; }

        public int NumeroVenta { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        public decimal Total { get; set; }

        public override string ToString() =>
            $"Factura venta {NumeroFactura} (OP {NumeroOrden}) - ${Total:0.00}";
    }
}
