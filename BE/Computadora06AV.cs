using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public class Computadora06AV
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public TipoConfiguracion06AV TipoConfiguracion { get; set; }

        public List<Componente06AV> Componentes { get; set; } = new List<Componente06AV>();

        public ModeloEstandar06AV ModeloOrigen { get; set; }

        public decimal? PrecioPactado { get; set; }

        public decimal PrecioTotal => PrecioPactado ?? Componentes?.Sum(c => c.PrecioUnitario) ?? 0m;

        public override string ToString() =>
            $"{(string.IsNullOrEmpty(Nombre) ? "Computadora" : Nombre)} " +
            $"({TipoConfiguracion}) - ${PrecioTotal:0.00}";
    }
}
