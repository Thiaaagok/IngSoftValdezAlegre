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
        public string Numero { get; set; }               // PK (GeneradorCodigo06AV, ej. "CO-2026-0001")
        public string NumeroCompra { get; set; }         // FK a OrdenCompra06AV.Id

        public List<DetalleComponente06AV> ComponentesPedidos { get; set; } = new List<DetalleComponente06AV>();
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public EstadoCotizacion06AV Estado { get; set; } = EstadoCotizacion06AV.PorAprobar;
        public Proveedor06AV Proveedor { get; set; }

        /// <summary>
        /// Costo total ofrecido: la suma de los subtotales de <see cref="ComponentesPedidos"/>
        /// cuando la cotización tiene precio por ítem.
        /// </summary>
        public decimal Costo { get; set; }

        /// <summary>
        /// Todas las líneas tienen precio unitario. Las cotizaciones cargadas antes del
        /// precio por ítem solo tienen el total y dan false.
        /// </summary>
        public bool TienePreciosPorItem =>
            ComponentesPedidos != null && ComponentesPedidos.Count > 0 &&
            ComponentesPedidos.TrueForAll(d => d.PrecioUnitario > 0);

        /// <summary>Precio unitario cotizado para un componente, o 0 si no está en la cotización.</summary>
        public decimal PrecioDe(string codigoComponente)
        {
            DetalleComponente06AV d = ComponentesPedidos?.Find(x =>
                x.Componente != null && string.Equals(x.Componente.Codigo, codigoComponente, StringComparison.OrdinalIgnoreCase));
            return d?.PrecioUnitario ?? 0m;
        }
        public string Condiciones { get; set; }

        /// <summary>Gerente de compras que aprobó o desaprobó (null hasta que se resuelve).</summary>
        public Usuario06AV GerenteAprobador { get; set; }   // SER.Usuario06AV (rol GerenteCompras)

        public override string ToString() =>
            $"Cotización #{Numero} - {Proveedor?.Nombre} - ${Costo:0.00} - {Estado}";
    }
}
