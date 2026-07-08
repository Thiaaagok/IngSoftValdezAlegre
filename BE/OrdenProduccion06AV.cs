using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>
    /// Registro formal para ensamblar una computadora solicitada por un cliente (RFN1).
    /// Contiene datos del cliente, la computadora, la planificación (línea, fechas,
    /// responsable), el estado y los pagos asociados (seña y saldo).
    /// </summary>
    public class OrdenProduccion06AV
    {
        public int NumeroOrden { get; set; }
        public Cliente06AV Cliente { get; set; }
        public Computadora06AV Computadora { get; set; }

        public DateTime FechaEntrega { get; set; }
        public EstadoOrdenProduccion06AV Estado { get; set; } = EstadoOrdenProduccion06AV.Pendiente;

        // Planificación (paso 5 de RFN1).
        public LineaEnsamblaje06AV LineaEnsamblaje { get; set; }
        public DateTime? FechaInicioPrevista { get; set; }
        public string ResponsableTecnico { get; set; }

        // Pagos (seña + saldo).
        public List<Pago06AV> Pagos { get; set; } = new List<Pago06AV>();

        public decimal PrecioTotal => Computadora?.PrecioTotal ?? 0m;
        public decimal TotalAbonado => Pagos?.Sum(p => p.Monto) ?? 0m;
        public decimal SaldoPendiente => PrecioTotal - TotalAbonado;

        public override string ToString() => $"OP #{NumeroOrden} - {Estado}";
    }
}
