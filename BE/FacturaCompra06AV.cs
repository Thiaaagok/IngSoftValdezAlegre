using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Factura de compra (RFN2, paso 5): documenta la recepción real de la mercadería y
    /// permite comparar lo pedido contra lo recibido. Entidad persistente (no reporte).
    /// El proveedor NO se guarda acá: se navega OrdenCompra → PedidoCotizacion aprobado → Proveedor.
    /// </summary>
    public class FacturaCompra06AV
    {
        public string NumeroFactura { get; set; }
        public string NumeroCompra { get; set; }        // FK a OrdenCompra06AV.Id

        public DateTime FechaEmision { get; set; }
        public DateTime FechaEntrega { get; set; }

        public List<DetalleComponente06AV> ComponentesRecibidos { get; set; } = new List<DetalleComponente06AV>();
        public decimal Total { get; set; }
        public string Observaciones { get; set; }        // diferencias entre lo pedido y lo recibido

        public override string ToString() => $"Factura compra {NumeroFactura} (OC {NumeroCompra}) - ${Total:0.00}";
    }
}
