using BE;
using BLL.Excepciones;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    /// <summary>
    /// Base de los ConcreteBuilders. Guarda el equipo en construcción y hace
    /// cumplir las reglas físicas del armado, que son las mismas para cualquier
    /// computadora:
    ///
    ///   · cada pieza va en la bahía de su tipo (un disco no entra en el hueco del
    ///     procesador);
    ///   · las bahías únicas admiten una sola pieza (un procesador, una placa madre,
    ///     una fuente, un gabinete, una placa de video);
    ///   · no se colocan componentes dados de baja;
    ///   · el equipo sólo se entrega completo: con todas las bahías obligatorias
    ///     cubiertas.
    ///
    /// Lo que NO resuelve la base es cómo se representa el producto terminado —
    /// su tipo de configuración, su nombre y el modelo del que sale —, que es
    /// justamente lo que distingue a un ConcreteBuilder de otro.
    /// </summary>
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

        /// <summary>Estándar o Configurable: cómo se registra el equipo en la venta.</summary>
        protected abstract TipoConfiguracion06AV TipoDeConfiguracion { get; }

        /// <summary>Nombre con el que el equipo figura en la venta, el recibo y la factura.</summary>
        protected abstract string NombrarEquipo(IReadOnlyList<Componente06AV> piezas);

        /// <summary>Modelo del catálogo del que sale el equipo; null si es a medida.</summary>
        protected virtual ModeloEstandar06AV ModeloDeOrigen => null;

        // ══════════════════════════════════════════════════════════
        //  Reglas comunes
        // ══════════════════════════════════════════════════════════
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

        /// <summary>Las piezas colocadas, en el orden de la receta.</summary>
        protected List<Componente06AV> PiezasEnOrden()
        {
            var piezas = new List<Componente06AV>();
            foreach (PasoArmado06AV paso in ArmadorComputadora06AV.Pasos)
                if (_bahias.TryGetValue(paso.Tipo, out List<Componente06AV> lista))
                    piezas.AddRange(lista);
            return piezas;
        }

        /// <summary>Primera pieza de una bahía, o null si está vacía.</summary>
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
