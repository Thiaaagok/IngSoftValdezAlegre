using BE;
using BLL.Excepciones;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    public abstract class ComputadoraBuilderBase06AV : IComputadoraBuilder06AV
    {
        private readonly Dictionary<TipoComponente06AV, List<Componente06AV>> _bahias =
            new Dictionary<TipoComponente06AV, List<Componente06AV>>();

        protected ComputadoraBuilderBase06AV()
        {
            Reiniciar();
        }

        public void Reiniciar() => _bahias.Clear();

        public void ColocarProcesador(Componente06AV procesador)       => Colocar(TipoComponente06AV.Procesador, procesador);
        public void ColocarPlacaMadre(Componente06AV placaMadre)       => Colocar(TipoComponente06AV.PlacaMadre, placaMadre);
        public void ColocarMemoriaRam(Componente06AV memoria)          => Colocar(TipoComponente06AV.MemoriaRAM, memoria);
        public void ColocarAlmacenamiento(Componente06AV disco)        => Colocar(TipoComponente06AV.Disco, disco);
        public void ColocarPlacaDeVideo(Componente06AV placaDeVideo)   => Colocar(TipoComponente06AV.PlacaDeVideo, placaDeVideo);
        public void ColocarFuente(Componente06AV fuente)               => Colocar(TipoComponente06AV.Fuente, fuente);
        public void ColocarGabinete(Componente06AV gabinete)           => Colocar(TipoComponente06AV.Gabinete, gabinete);
        public void ColocarRefrigeracion(Componente06AV refrigeracion) => Colocar(TipoComponente06AV.Refrigeracion, refrigeracion);
        public void ColocarAdicional(Componente06AV adicional)         => Colocar(TipoComponente06AV.Otro, adicional);

        public Computadora06AV ObtenerComputadora()
        {
            List<Componente06AV> piezas = PiezasEnOrden();

            List<TipoComponente06AV> faltantes = ArmadorComputadora06AV.Faltantes(piezas);
            if (faltantes.Count > 0)
                throw new ValidacionException06AV("componentes",
                    "La computadora está incompleta. Falta: " +
                    string.Join(", ", faltantes.Select(NombreBahia)) + ".");

            var pc = new Computadora06AV
            {
                TipoConfiguracion = TipoDeConfiguracion,
                Nombre = NombrarEquipo(piezas),
                ModeloOrigen = ModeloDeOrigen
            };
            pc.Componentes.AddRange(piezas);

            Reiniciar();
            return pc;
        }

        protected abstract TipoConfiguracion06AV TipoDeConfiguracion { get; }

        protected abstract string NombrarEquipo(IReadOnlyList<Componente06AV> piezas);

        protected virtual ModeloEstandar06AV ModeloDeOrigen => null;

        private void Colocar(TipoComponente06AV bahia, Componente06AV pieza)
        {
            if (pieza == null)
                throw new ValidacionException06AV("componente",
                    $"No se indicó qué colocar en la bahía de {NombreBahia(bahia)}.");

            if (pieza.Tipo != bahia)
                throw new ValidacionException06AV("componente",
                    $"'{pieza.Descripcion}' es {NombreBahia(pieza.Tipo)}: no va en la bahía de {NombreBahia(bahia)}.");

            if (pieza.BajaLogica)
                throw new ValidacionException06AV("componente",
                    $"'{pieza.Descripcion}' está dado de baja y no se puede usar en un armado.");

            if (!_bahias.TryGetValue(bahia, out List<Componente06AV> lista))
            {
                lista = new List<Componente06AV>();
                _bahias[bahia] = lista;
            }

            if (lista.Count > 0 && !ArmadorComputadora06AV.AdmiteVarios(bahia))
                throw new ValidacionException06AV("componente",
                    $"El equipo ya tiene {NombreBahia(bahia)} ('{lista[0].Descripcion}'): esa bahía admite una sola pieza.");

            lista.Add(pieza);
        }

        protected List<Componente06AV> PiezasEnOrden()
        {
            var piezas = new List<Componente06AV>();
            foreach (PasoArmado06AV paso in ArmadorComputadora06AV.Pasos)
                if (_bahias.TryGetValue(paso.Tipo, out List<Componente06AV> lista))
                    piezas.AddRange(lista);
            return piezas;
        }

        protected static Componente06AV PiezaDe(IEnumerable<Componente06AV> piezas, TipoComponente06AV bahia) =>
            piezas.FirstOrDefault(c => c.Tipo == bahia);

        internal static string NombreBahia(TipoComponente06AV tipo)
        {
            switch (tipo)
            {
                case TipoComponente06AV.Procesador:    return "procesador";
                case TipoComponente06AV.PlacaMadre:    return "placa madre";
                case TipoComponente06AV.MemoriaRAM:    return "memoria RAM";
                case TipoComponente06AV.Disco:         return "almacenamiento";
                case TipoComponente06AV.PlacaDeVideo:  return "placa de video";
                case TipoComponente06AV.Fuente:        return "fuente";
                case TipoComponente06AV.Gabinete:      return "gabinete";
                case TipoComponente06AV.Refrigeracion: return "refrigeración";
                default:                               return "adicionales";
            }
        }
    }
}
