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

        /// <summary>
        /// Modelo del catálogo del que salió el equipo. Lo completa el
        /// ComputadoraEstandarBuilder06AV al armarlo; en los equipos a medida es null.
        ///
        /// Al leer una venta se trae como referencia liviana (Id, Nombre y Descripción):
        /// su lista de componentes NO se carga, porque los componentes reales del equipo
        /// son los de <see cref="Componentes"/>, que pueden diferir si el modelo se
        /// modificó después de la venta.
        /// </summary>
        public ModeloEstandar06AV ModeloOrigen { get; set; }

        /// <summary>
        /// Precio al que se vendió el equipo (Computadoras.PrecioTotal). Lo completa la
        /// MPP al leer una venta; en un equipo recién armado es null.
        /// </summary>
        public decimal? PrecioPactado { get; set; }

        /// <summary>
        /// En una venta ya registrada es el precio pactado, aunque después cambie el
        /// precio de algún componente; en un equipo recién armado, la suma de sus piezas.
        /// </summary>
        public decimal PrecioTotal => PrecioPactado ?? Componentes?.Sum(c => c.PrecioUnitario) ?? 0m;

        public override string ToString() =>
            $"{(string.IsNullOrEmpty(Nombre) ? "Computadora" : Nombre)} " +
            $"({TipoConfiguracion}) - ${PrecioTotal:0.00}";
    }
}
