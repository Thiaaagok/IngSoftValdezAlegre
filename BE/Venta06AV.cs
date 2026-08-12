using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public class Venta06AV
    {
        public int NumeroVenta { get; set; }
        public Cliente06AV Cliente { get; set; }
        public Computadora06AV Computadora { get; set; }
        public DateTime FechaVenta { get; set; } = DateTime.Now;
        public DateTime FechaEntregaEstimada { get; set; } = DateTime.Today.AddDays(15);
        public EstadoVenta06AV Estado { get; set; } = EstadoVenta06AV.Pendiente;
        public string UsuarioRegistro { get; set; }
        public int? NumeroOrdenProduccion { get; set; }
        public List<Pago06AV> Pagos { get; set; } = new List<Pago06AV>();
        public decimal PrecioTotal => Computadora?.PrecioTotal ?? 0m;

        /// <summary>Porcentaje de seña </summary>
        public const decimal PorcentajeSena = 0.50m;
        public decimal MontoSenaRequerido => Math.Round(PrecioTotal * PorcentajeSena, 2);
        public Pago06AV Sena => Pagos?.FirstOrDefault(p => p.Tipo == TipoPago06AV.Sena);
        public bool TieneSena => Sena != null;
        public decimal TotalAbonado => Pagos?.Sum(p => p.Monto) ?? 0m;
        public decimal SaldoPendiente => PrecioTotal - TotalAbonado;
        public bool ListaParaProducir =>
            Estado == EstadoVenta06AV.Senada && NumeroOrdenProduccion == null;
        public override string ToString() =>
            $"Venta #{NumeroVenta} - {(Cliente != null ? Cliente.NombreCompleto : "")} - ${PrecioTotal:0.00}";
    }
}
