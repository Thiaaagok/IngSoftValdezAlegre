using BE;
using BLL.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
    /// <summary>
    /// Un paso del armado: qué bahía se completa, si es obligatoria y si admite más
    /// de una pieza (dos memorias, dos discos).
    /// </summary>
    public sealed class PasoArmado06AV
    {
        internal PasoArmado06AV(TipoComponente06AV tipo, bool obligatorio, bool admiteVarios)
        {
            Tipo = tipo;
            Obligatorio = obligatorio;
            AdmiteVarios = admiteVarios;
        }

        public TipoComponente06AV Tipo { get; }
        public bool Obligatorio { get; }
        public bool AdmiteVarios { get; }
    }

    /// <summary>
    /// PATRÓN BUILDER — rol DIRECTOR.
    ///
    /// Conoce la RECETA del armado: en qué orden se colocan las piezas y cuáles son
    /// indispensables para que el equipo funcione. No sabe qué tipo de computadora
    /// sale al final: eso depende del builder que se le pase. Con el mismo proceso,
    /// un <see cref="ComputadoraEstandarBuilder06AV"/> produce un modelo de catálogo y
    /// un <see cref="ComputadoraConfigurableBuilder06AV"/> produce un equipo a medida.
    ///
    /// La receta es la única fuente de verdad del sistema: el configurador "Armá tu
    /// PC" lee <see cref="Pasos"/> para dibujar sus bahías y decidir qué se puede
    /// saltear, en lugar de tener su propia lista.
    ///
    /// Uso (desde el Cliente):
    /// <code>
    ///   var builder  = new ComputadoraConfigurableBuilder06AV();
    ///   var armador  = new ArmadorComputadora06AV(builder);
    ///   armador.Armar(piezasElegidas);
    ///   Computadora06AV pc = builder.ObtenerComputadora();
    /// </code>
    /// </summary>
    public class ArmadorComputadora06AV
    {
        // ══════════════════════════════════════════════════════════
        //  La receta
        // ══════════════════════════════════════════════════════════
        private static readonly PasoArmado06AV[] Receta =
        {
            //                     bahía                                obligatoria  admite varios
            new PasoArmado06AV(TipoComponente06AV.Procesador,     true,  false),
            new PasoArmado06AV(TipoComponente06AV.PlacaMadre,     true,  false),
            new PasoArmado06AV(TipoComponente06AV.MemoriaRAM,     true,  true),
            new PasoArmado06AV(TipoComponente06AV.Disco,          true,  true),
            new PasoArmado06AV(TipoComponente06AV.PlacaDeVideo,   false, false),
            new PasoArmado06AV(TipoComponente06AV.Fuente,         true,  false),
            new PasoArmado06AV(TipoComponente06AV.Gabinete,       true,  false),
            new PasoArmado06AV(TipoComponente06AV.Refrigeracion,  false, true),
            new PasoArmado06AV(TipoComponente06AV.Otro,           false, true)
        };

        /// <summary>Los pasos del armado, en el orden en que el Director los ejecuta.</summary>
        public static IReadOnlyList<PasoArmado06AV> Pasos { get; } = Array.AsReadOnly(Receta);

        public static PasoArmado06AV Paso(TipoComponente06AV tipo) =>
            Receta.First(p => p.Tipo == tipo);

        public static bool EsObligatorio(TipoComponente06AV tipo) => Paso(tipo).Obligatorio;

        public static bool AdmiteVarios(TipoComponente06AV tipo) => Paso(tipo).AdmiteVarios;

        /// <summary>Bahías obligatorias que no tienen ninguna pieza en la lista dada.</summary>
        public static List<TipoComponente06AV> Faltantes(IEnumerable<Componente06AV> piezas)
        {
            var presentes = new HashSet<TipoComponente06AV>(
                (piezas ?? Enumerable.Empty<Componente06AV>()).Where(c => c != null).Select(c => c.Tipo));
            return Receta.Where(p => p.Obligatorio && !presentes.Contains(p.Tipo))
                         .Select(p => p.Tipo)
                         .ToList();
        }

        // ══════════════════════════════════════════════════════════
        //  El Director
        // ══════════════════════════════════════════════════════════
        private IComputadoraBuilder06AV _builder;

        public ArmadorComputadora06AV(IComputadoraBuilder06AV builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// El builder con el que trabaja el Director. Se puede cambiar entre armados:
        /// la receta es la misma, lo que cambia es el producto.
        /// </summary>
        public IComputadoraBuilder06AV Builder
        {
            get { return _builder; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _builder = value;
            }
        }

        /// <summary>
        /// Arma un equipo con las piezas dadas, siguiendo la receta: primero el
        /// procesador, después la placa madre, y así hasta los adicionales. El orden
        /// en que vengan las piezas no importa: el Director las ordena.
        ///
        /// No devuelve el producto — como indica el patrón, el resultado se le pide
        /// al builder con <see cref="IComputadoraBuilder06AV.ObtenerComputadora"/>.
        /// </summary>
        public void Armar(IEnumerable<Componente06AV> piezas)
        {
            if (piezas == null)
                throw new ValidacionException06AV("piezas", "No se indicaron piezas para armar la computadora.");

            _builder.Reiniciar();

            ILookup<TipoComponente06AV, Componente06AV> porBahia =
                piezas.Where(c => c != null).ToLookup(c => c.Tipo);

            foreach (PasoArmado06AV paso in Receta)
                foreach (Componente06AV pieza in porBahia[paso.Tipo])
                    Colocar(paso.Tipo, pieza);
        }

        /// <summary>Arma un equipo a partir de la composición de un modelo del catálogo.</summary>
        public void ArmarDesdeModelo(ModeloEstandar06AV modelo)
        {
            if (modelo == null)
                throw new ValidacionException06AV("modelo", "Debe indicarse un modelo estándar.");
            Armar(modelo.Componentes);
        }

        /// <summary>Despacha cada pieza al paso del builder que le corresponde.</summary>
        private void Colocar(TipoComponente06AV bahia, Componente06AV pieza)
        {
            switch (bahia)
            {
                case TipoComponente06AV.Procesador:    _builder.ColocarProcesador(pieza);     break;
                case TipoComponente06AV.PlacaMadre:    _builder.ColocarPlacaMadre(pieza);     break;
                case TipoComponente06AV.MemoriaRAM:    _builder.ColocarMemoriaRam(pieza);     break;
                case TipoComponente06AV.Disco:         _builder.ColocarAlmacenamiento(pieza); break;
                case TipoComponente06AV.PlacaDeVideo:  _builder.ColocarPlacaDeVideo(pieza);   break;
                case TipoComponente06AV.Fuente:        _builder.ColocarFuente(pieza);         break;
                case TipoComponente06AV.Gabinete:      _builder.ColocarGabinete(pieza);       break;
                case TipoComponente06AV.Refrigeracion: _builder.ColocarRefrigeracion(pieza);  break;
                default:                               _builder.ColocarAdicional(pieza);      break;
            }
        }
    }
}
