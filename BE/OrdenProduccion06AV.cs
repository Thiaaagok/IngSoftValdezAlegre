using System;

namespace BE
{
    /// <summary>
    /// Orden de producción (venta). La crea el gerente seleccionando una Computadora06AV
    /// ya registrada y con seña cobrada. El cliente, los pagos y el precio se navegan desde
    /// la computadora (no se duplican).
    /// </summary>
    public class OrdenProduccion06AV
    {
        public int NumeroOrden { get; set; }
        public Cliente06AV Cliente { get; set; }
        public Computadora06AV Computadora { get; set; }

        public DateTime FechaEntrega { get; set; }
        public EstadoOrdenProduccion06AV Estado { get; set; } = EstadoOrdenProduccion06AV.Pendiente;
        public LineaEnsamblaje06AV LineaEnsamblaje { get; set; }
        public DateTime? FechaInicioPrevista { get; set; }
        public string ResponsableTecnico { get; set; }

        public List<Pago06AV> Pagos { get; set; } = new List<Pago06AV>();

        public decimal PrecioTotal => Computadora?.PrecioTotal ?? 0m;
        public decimal TotalAbonado => Pagos?.Sum(p => p.Monto) ?? 0m;
        public decimal SaldoPendiente => PrecioTotal - TotalAbonado;

        public override string ToString() => $"OP #{NumeroOrden} - {Estado}";
    }
}
