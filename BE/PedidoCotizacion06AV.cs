using SER;
using System;
using System.Collections.Generic;

namespace BE
{
    public class PedidoCotizacion06AV
    {
        public string Numero { get; set; }
        public string NumeroCompra { get; set; }

        public List<DetalleComponente06AV> ComponentesPedidos { get; set; } = new List<DetalleComponente06AV>();
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public EstadoCotizacion06AV Estado { get; set; } = EstadoCotizacion06AV.PorAprobar;
        public Proveedor06AV Proveedor { get; set; }

        public decimal Costo { get; set; }

        public bool TienePreciosPorItem =>
            ComponentesPedidos != null && ComponentesPedidos.Count > 0 &&
            ComponentesPedidos.TrueForAll(d => d.PrecioUnitario > 0);

        public decimal PrecioDe(string codigoComponente)
        {
            DetalleComponente06AV d = ComponentesPedidos?.Find(x =>
                x.Componente != null && string.Equals(x.Componente.Codigo, codigoComponente, StringComparison.OrdinalIgnoreCase));
            return d?.PrecioUnitario ?? 0m;
        }
        public string Condiciones { get; set; }

        public Usuario06AV GerenteAprobador { get; set; }

        public override string ToString() =>
            $"Cotización #{Numero} - {Proveedor?.Nombre} - ${Costo:0.00} - {Estado}";
    }
}
