using BE;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    /// <summary>
    /// PATRÓN BUILDER — rol CONCRETE BUILDER (equipo a medida).
    ///
    /// Arma la computadora que el cliente configura pieza por pieza en "Armá tu PC".
    /// El producto sale registrado como <see cref="TipoConfiguracion06AV.Configurable"/>
    /// y, como no tiene un modelo detrás, se nombra por lo que lo distingue: su
    /// procesador y, si tiene, su placa de video. Así, en el tablero de producción
    /// y en el recibo se lee "PC a medida · AMD Ryzen 5 5600 · MSI Ventus 3060" en
    /// lugar de un genérico "Configurable".
    /// </summary>
    public class ComputadoraConfigurableBuilder06AV : ComputadoraBuilderBase06AV
    {
        private const string Prefijo = "PC a medida";
        private const int LargoMaximo = 150;   // columna Computadoras.Nombre NVARCHAR(150)

        protected override TipoConfiguracion06AV TipoDeConfiguracion => TipoConfiguracion06AV.Configurable;

        protected override string NombrarEquipo(IReadOnlyList<Componente06AV> piezas)
        {
            var partes = new List<string> { Prefijo };

            string cpu = Rotulo(PiezaDe(piezas, TipoComponente06AV.Procesador));
            string gpu = Rotulo(PiezaDe(piezas, TipoComponente06AV.PlacaDeVideo));
            if (cpu != null) partes.Add(cpu);
            if (gpu != null) partes.Add(gpu);

            string nombre = string.Join(" · ", partes);
            return nombre.Length <= LargoMaximo ? nombre : nombre.Substring(0, LargoMaximo);
        }

        /// <summary>"Marca Modelo" si están cargados; si no, la descripción.</summary>
        private static string Rotulo(Componente06AV c)
        {
            if (c == null) return null;
            string marcaModelo = string.Join(" ",
                new[] { c.Marca, c.Modelo }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            return marcaModelo.Length > 0 ? marcaModelo : c.Descripcion;
        }
    }
}
