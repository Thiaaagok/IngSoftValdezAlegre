using BE;
using BLL.Excepciones;
using System.Collections.Generic;

namespace BLL.Armado
{
    public class ComputadoraEstandarBuilder06AV : ComputadoraBuilderBase06AV
    {
        private readonly ModeloEstandar06AV _modelo;

        public ComputadoraEstandarBuilder06AV(ModeloEstandar06AV modelo)
        {
            if (modelo == null)
                throw new ValidacionException06AV("modelo", "Un equipo estándar necesita el modelo del que sale.");
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                throw new ValidacionException06AV("modelo", "Un equipo estándar necesita el nombre de su modelo.");
            _modelo = modelo;
        }

        protected override TipoConfiguracion06AV TipoDeConfiguracion => TipoConfiguracion06AV.Estandar;

        protected override string NombrarEquipo(IReadOnlyList<Componente06AV> piezas) => _modelo.Nombre.Trim();

        protected override ModeloEstandar06AV ModeloDeOrigen => _modelo;
    }
}
