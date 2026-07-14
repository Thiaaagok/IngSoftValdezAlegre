using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public class Computadora06AV
    {
        public int Id { get; set; }

        /// <summary>Nombre del modelo (para las estándar, ej: "PC Gamer Nivel 1").</summary>
        public string Nombre { get; set; }

        public TipoConfiguracion06AV TipoConfiguracion { get; set; }

        public List<Componente06AV> Componentes { get; set; } = new List<Componente06AV>();

        /// <summary>Precio total = suma del precio unitario de cada componente.</summary>
        public decimal PrecioTotal => Componentes?.Sum(c => c.PrecioUnitario) ?? 0m;

        public override string ToString() =>
            $"{(string.IsNullOrEmpty(Nombre) ? "Computadora" : Nombre)} " +
            $"({TipoConfiguracion}) - ${PrecioTotal:0.00}";
    }
}
