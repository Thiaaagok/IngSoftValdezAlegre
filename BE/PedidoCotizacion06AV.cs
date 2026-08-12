using SER;
using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Pedido de cotización a un proveedor (RFN2). Numero es la PK; NumeroCompra es la FK
    /// (por Id) a la OrdenCompra06AV. Una orden de compra puede tener varios pedidos a lo
    /// largo del tiempo (desaprobados) hasta uno aprobado.
    /// </summary>
    public class PedidoCotizacion06AV
    {
        public int Numero { get; set; }

        /// <summary>Orden de compra que originó el pedido (número de compra).</summary>
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
