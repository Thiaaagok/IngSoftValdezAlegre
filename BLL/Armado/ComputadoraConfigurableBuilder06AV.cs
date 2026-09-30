using BE;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    public class ComputadoraConfigurableBuilder06AV : ComputadoraBuilderBase06AV
    {
        private const string Prefijo = "PC a medida";
        private const int LargoMaximo = 150;

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

        private static string Rotulo(Componente06AV c)
        {
            if (c == null) return null;
            string marcaModelo = string.Join(" ",
                new[] { c.Marca, c.Modelo }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            return marcaModelo.Length > 0 ? marcaModelo : c.Descripcion;
        }
    }
}
