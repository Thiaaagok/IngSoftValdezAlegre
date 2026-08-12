using System;
using System.Collections.Generic;

namespace BE
{
    public class PedidoCotizacion06AV
    {
        public int Numero { get; set; }
        public int NumeroCompra { get; set; }
        public List<DetalleInsumo06AV> InsumosPedidos { get; set; } = new List<DetalleInsumo06AV>();
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public EstadoCotizacion06AV Estado { get; set; } = EstadoCotizacion06AV.PorAprobar;
        public Proveedor06AV Proveedor { get; set; }
        public decimal Costo { get; set; }
        public string Condiciones { get; set; }
        public override string ToString() =>
            $"Cotización #{Numero} - {Proveedor?.Nombre} - ${Costo:0.00} - {Estado}";
    }
}
