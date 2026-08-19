using System;

namespace BE
{
    public class OrdenProduccion06AV
    {
        public int NumeroOrden { get; set; }
        public Venta06AV Venta { get; set; }
        public int NumeroVenta { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaEntregaEstimada { get; set; }
        public EstadoOrdenProduccion06AV Estado { get; set; } = EstadoOrdenProduccion06AV.Pendiente;
        public LineaEnsamblaje06AV LineaEnsamblaje { get; set; }
        public DateTime? FechaInicioPrevista { get; set; }
        public string ResponsableTecnico { get; set; }
        public ControlCalidad06AV ControlCalidad { get; set; } = new ControlCalidad06AV();
        public string NumeroSerie { get; set; }
        public DateTime? FechaCierre { get; set; }
        public Cliente06AV Cliente => Venta?.Cliente;
        public Computadora06AV Computadora => Venta?.Computadora;
        public decimal PrecioTotal => Venta?.PrecioTotal ?? 0m;
        public decimal TotalAbonado => Venta?.TotalAbonado ?? 0m;
        public decimal SaldoPendiente => Venta?.SaldoPendiente ?? 0m;
        public override string ToString() => $"OP #{NumeroOrden} (Venta #{NumeroVenta}) - {Estado}";
    }
}
