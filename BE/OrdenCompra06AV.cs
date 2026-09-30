using SER;
using System;
using System.Collections.Generic;

namespace BE
{
    public class OrdenCompra06AV
    {
        public string Id { get; set; }
        public int NumeroCompra { get; set; }

        public List<DetalleComponente06AV> ComponentesFaltantes { get; set; } = new List<DetalleComponente06AV>();
        public DateTime FechaLimite { get; set; }
        public Usuario06AV RepositorSolicitante { get; set; }
        public EstadoOrdenCompra06AV Estado { get; set; } = EstadoOrdenCompra06AV.Pendiente;
        public DateTime? FechaCierre { get; set; }

        public override string ToString() => $"OC #{NumeroCompra} - {Estado}";
    }
}
