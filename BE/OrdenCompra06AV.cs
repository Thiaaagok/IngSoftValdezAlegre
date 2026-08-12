using SER;
using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>
    /// Orden de compra de componentes faltantes (RFN2). Id es la PK técnica;
    /// NumeroCompra es el número de negocio visible al usuario.
    /// </summary>
    public class OrdenCompra06AV
    {
        public string Id { get; set; }                    // PK (GeneradorCodigo06AV, ej. "OC-2026-0001")
        public int NumeroCompra { get; set; }              // número de negocio visible

        public List<DetalleComponente06AV> ComponentesFaltantes { get; set; } = new List<DetalleComponente06AV>();
        public DateTime FechaLimite { get; set; }
        public Usuario06AV RepositorSolicitante { get; set; }   // SER.Usuario06AV (rol Repositor)
        public EstadoOrdenCompra06AV Estado { get; set; } = EstadoOrdenCompra06AV.Pendiente;
        public DateTime? FechaCierre { get; set; }
        public override string ToString() => $"OC #{NumeroCompra} - {Estado}";
    }
}
