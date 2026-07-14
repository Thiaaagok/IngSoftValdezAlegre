using System;
using System.Collections.Generic;

namespace BE
{
    public class OrdenCompra06AV
    {
        public int NumeroCompra { get; set; }
        public List<DetalleInsumo06AV> InsumosFaltantes { get; set; } = new List<DetalleInsumo06AV>();
        public DateTime FechaLimite { get; set; }
        public string RepositorSolicitante { get; set; }
        public EstadoOrdenCompra06AV Estado { get; set; } = EstadoOrdenCompra06AV.Pendiente;

        public DateTime? FechaCierre { get; set; }

        public override string ToString() => $"OC #{NumeroCompra} - {Estado}";
    }
}
