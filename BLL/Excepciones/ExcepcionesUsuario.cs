using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SER.Excepciones
{
    public class UsuarioException : Exception
    {
        public string CodigoError { get; }

        public UsuarioException(string mensaje, string codigoError = "USR_ERROR")
            : base(mensaje)
        {
            CodigoError = codigoError;
        }

        public UsuarioException(string mensaje, Exception inner, string codigoError = "USR_ERROR")
            : base(mensaje, inner)
        {
            CodigoError = codigoError;
        }
    }

    public class UsuarioValidacionException : UsuarioException
    {
        public string Campo { get; }

        public UsuarioValidacionException(string campo, string mensaje): base(mensaje, "USR_VALIDACION")
        {
            Campo = campo;
        }
    }

    public class UsuarioNoEncontradoException : UsuarioException
    {
        public string Dni { get; }

        public UsuarioNoEncontradoException(string dni): base($"No se encontró ningún usuario con DNI '{dni}'.", "USR_NOT_FOUND")
        {
            Dni = dni;
        }
    }

    public class UsuarioDuplicadoException : UsuarioException
    {
        public string Dni { get; }

        public UsuarioDuplicadoException(string dni): base($"Ya existe un usuario con DNI '{dni}'.", "USR_DUPLICADO")
        {
            Dni = dni;
        }
    }

    public class UsuarioEstadoInvalidoException : UsuarioException
    {
        public string Dni { get; }
        public string EstadoActual { get; }
        public string AccionIntentada { get; }

        public UsuarioEstadoInvalidoException(string dni, string estadoActual, string accionIntentada): base($"No se puede ejecutar '{accionIntentada}' sobre el usuario '{dni}': estado actual es '{estadoActual}'.", "USR_ESTADO_INVALIDO")
        {
            Dni = dni;
            EstadoActual = estadoActual;
            AccionIntentada = accionIntentada;
        }
    }

    public class ContraseniaInvalidaException : UsuarioException
    {
        public string Dni { get; }

        public ContraseniaInvalidaException(string dni): base($"La contraseña actual ingresada para el usuario '{dni}' es incorrecta.", "USR_CONTRASENIA_INVALIDA")
        {
            Dni = dni;
        }
    }

    public class UsuarioAccesoDatosException : UsuarioException
    {
        public UsuarioAccesoDatosException(string operacion, Exception inner): base($"Error de acceso a datos durante '{operacion}'.", inner, "USR_DB_ERROR")
        {
        }
    }
}
