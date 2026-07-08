using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Pedido de cotización que el repositor envía a un proveedor (RFN2, paso 3). El
    /// gerente de compras lo aprueba o desaprueba (paso 4).
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

        public override string ToString() =>
            $"Cotización #{Numero} - {Proveedor?.Nombre} - {Estado}";
    }
}
