using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>
    /// Computadora registrada por el recepcionista. Existe de forma independiente:
    /// se crea (y se le cobra la seña) ANTES de que el gerente arme la orden de producción.
    /// Sus Componentes y su precio son un SNAPSHOT (copia) del momento de armarla, tanto
    /// si viene de un ModeloEstandar06AV como si es Configurable.
    /// </summary>
    public class Computadora06AV
    {
        public string Id { get; set; }                     // PK (GeneradorCodigo06AV, ej. "PC-2026-0001")
        public Cliente06AV Cliente { get; set; }           // la PC queda asociada al cliente desde que se registra

        public string Nombre { get; set; }
        public TipoConfiguracion06AV TipoConfiguracion { get; set; }
        public ModeloEstandar06AV ModeloOrigen { get; set; }   // nullable; solo si TipoConfiguracion == Estandar

        public List<Componente06AV> Componentes { get; set; } = new List<Componente06AV>();   // snapshot (copia)
        public List<Pago06AV> Pagos { get; set; } = new List<Pago06AV>();                     // pagos hechos sobre esta PC

        public decimal PrecioTotal => Componentes?.Sum(c => c.PrecioUnitario) ?? 0m;          // snapshot vía Componentes copiados

        public override string ToString() =>
            $"{(string.IsNullOrEmpty(Nombre) ? "Computadora" : Nombre)} " +
            $"({TipoConfiguracion}) - ${PrecioTotal:0.00}";
    }
}
