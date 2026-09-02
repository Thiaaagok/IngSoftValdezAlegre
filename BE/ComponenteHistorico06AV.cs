using System;

namespace BE
{
    /// <summary>
    /// Una versión histórica de un componente: cómo estaba el componente en el
    /// momento en que se lo dio de alta, se lo modificó o se lo dio de baja.
    ///
    /// Las filas de Componentes_C las escriben EXCLUSIVAMENTE los triggers de la
    /// base (TR_Componentes_Insert / TR_Componentes_Update). La aplicación las lee
    /// y, a lo sumo, pide restaurar una versión: la restauración se hace
    /// actualizando Componentes, nunca escribiendo el histórico a mano.
    /// </summary>
    public class ComponenteHistorico06AV
    {
        public int IdHistorico { get; set; }
        public string CodigoComponente { get; set; }

        /// <summary>Fecha en que quedó asentada la versión.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>Hora en que quedó asentada la versión.</summary>
        public TimeSpan Hora { get; set; }

        public string Descripcion { get; set; }
        public TipoComponente06AV Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }

        /// <summary>El componente estaba dado de baja en esta versión.</summary>
        public bool BajaLogica { get; set; }

        /// <summary>Es la versión vigente del componente (Act = 1 en la base).</summary>
        public bool Activo { get; set; }

        /// <summary>Fecha y hora unificadas, para ordenar y mostrar.</summary>
        public DateTime FechaHora => Fecha.Date.Add(Hora);

        public override string ToString() =>
            $"{CodigoComponente} - {Descripcion} ({FechaHora:dd/MM/yyyy HH:mm:ss})";
    }
}
