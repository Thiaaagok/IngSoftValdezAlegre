using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Orden de compra de insumos faltantes que registra el repositor (RFN2, paso 1).
    /// </summary>
    public class OrdenCompra06AV
    {
        public int NumeroCompra { get; set; }
        public List<DetalleInsumo06AV> InsumosFaltantes { get; set; } = new List<DetalleInsumo06AV>();
        public DateTime FechaLimite { get; set; }
        public string RepositorSolicitante { get; set; }
        public EstadoOrdenCompra06AV Estado { get; set; } = EstadoOrdenCompra06AV.Pendiente;

        /// <summary>Fecha de cierre al recibir y verificar los insumos (paso 5).</summary>
        public DateTime? FechaCierre { get; set; }

        public override string ToString() => $"OC #{NumeroCompra} - {Estado}";
    }
}
