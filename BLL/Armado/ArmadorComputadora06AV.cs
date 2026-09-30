using BE;
using BLL.Excepciones;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Armado
{
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

    public class ArmadorComputadora06AV
    {
        private static readonly PasoArmado06AV[] Receta =
        {
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

        public static IReadOnlyList<PasoArmado06AV> Pasos { get; } = Array.AsReadOnly(Receta);

        public static PasoArmado06AV Paso(TipoComponente06AV tipo) =>
            Receta.First(p => p.Tipo == tipo);

        public static bool EsObligatorio(TipoComponente06AV tipo) => Paso(tipo).Obligatorio;

        public static bool AdmiteVarios(TipoComponente06AV tipo) => Paso(tipo).AdmiteVarios;

        public static List<TipoComponente06AV> Faltantes(IEnumerable<Componente06AV> piezas)
        {
            var presentes = new HashSet<TipoComponente06AV>(
                (piezas ?? Enumerable.Empty<Componente06AV>()).Where(c => c != null).Select(c => c.Tipo));
            return Receta.Where(p => p.Obligatorio && !presentes.Contains(p.Tipo))
                         .Select(p => p.Tipo)
                         .ToList();
        }

        private IComputadoraBuilder06AV _builder;

        public ArmadorComputadora06AV(IComputadoraBuilder06AV builder)
        {
            Builder = builder;
        }

        public IComputadoraBuilder06AV Builder
        {
            get { return _builder; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _builder = value;
            }
        }

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

        public void ArmarDesdeModelo(ModeloEstandar06AV modelo)
        {
            if (modelo == null)
                throw new ValidacionException06AV("modelo", "Debe indicarse un modelo estándar.");
            Armar(modelo.Componentes);
        }

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
