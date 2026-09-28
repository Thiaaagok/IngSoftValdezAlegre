using BE;
using BLL.Excepciones;
using System.Collections.Generic;

namespace BLL.Armado
{
    /// <summary>
    /// PATRÓN BUILDER — rol CONCRETE BUILDER (equipo de catálogo).
    ///
    /// Arma una computadora de un modelo estándar. El producto sale registrado
    /// como <see cref="TipoConfiguracion06AV.Estandar"/>, lleva el nombre del
    /// modelo — que es como lo conoce el cliente y como figura en el recibo, la
    /// factura y el tablero de producción ("PC Gamer", "PC Oficina") — y queda
    /// vinculado a ese modelo en <see cref="Computadora06AV.ModeloOrigen"/>, lo que
    /// permite saber después qué modelo del catálogo se vendió.
    /// </summary>
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
