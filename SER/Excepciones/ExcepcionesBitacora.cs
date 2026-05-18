using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SER.Excepciones
{
    public class BitacoraException : Exception
    {
        public string CodigoError { get; }
        public BitacoraException(string mensaje, string codigoError = "BIT_ERROR"): base(mensaje) => CodigoError = codigoError;
        public BitacoraException(string mensaje, Exception inner, string codigoError = "BIT_ERROR"): base(mensaje, inner) => CodigoError = codigoError;
    }

    public class BitacoraEventoNoEncontradoException : BitacoraException
    {
        public BitacoraEventoNoEncontradoException(string identificador): base($"No se encontró ningún evento con identificador '{identificador}'.", "BIT_NO_ENCONTRADO") { }
    }

    public class BitacoraRangoFechasInvalidoException : BitacoraException
    {
        public DateTime Desde { get; }
        public DateTime Hasta { get; }
        public BitacoraRangoFechasInvalidoException(DateTime desde, DateTime hasta) : base($"Rango inválido: 'desde' ({desde:dd/MM/yyyy}) no puede ser posterior a 'hasta' ({hasta:dd/MM/yyyy}).", "BIT_FECHAS_INVALIDAS")
        {
            Desde = desde;
            Hasta = hasta;
        }
    }

    public class BitacoraValidacionException : BitacoraException
    {
        public string Campo { get; }
        public BitacoraValidacionException(string campo, string mensaje): base(mensaje, "BIT_VALIDACION") => Campo = campo;
    }

    public class BitacoraAccesoDatosException : BitacoraException
    {
        public BitacoraAccesoDatosException(string operacion, Exception inner): base($"Error de acceso a datos durante '{operacion}'.", inner, "BIT_DB_ERROR") { }
    }
}
