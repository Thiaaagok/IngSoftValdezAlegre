using System;

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

        public UsuarioValidacionException(string campo, string mensaje)
            : base(mensaje, "USR_VALIDACION")
        {
            Campo = campo;
        }
    }

    public class UsuarioNoEncontradoException : UsuarioException
    {
        public string Dni { get; }

        public UsuarioNoEncontradoException(string dni)
            : base(GestorIdioma06AV.Instancia.Obtener("exc_usr_no_encontrado", dni), "USR_NOT_FOUND")
        {
            Dni = dni;
        }
    }

    public class UsuarioDuplicadoException : UsuarioException
    {
        public string Dni { get; }

        public UsuarioDuplicadoException(string dni)
            : base(GestorIdioma06AV.Instancia.Obtener("exc_usr_duplicado", dni), "USR_DUPLICADO")
        {
            Dni = dni;
        }
    }

    public class UsuarioEstadoInvalidoException : UsuarioException
    {
        public string Dni { get; }
        public string EstadoActual { get; }
        public string AccionIntentada { get; }

        public UsuarioEstadoInvalidoException(string dni, string estadoActual, string accionIntentada)
            : base(GestorIdioma06AV.Instancia.Obtener("exc_usr_estado_invalido", dni, estadoActual, accionIntentada), "USR_ESTADO_INVALIDO")
        {
            Dni = dni;
            EstadoActual = estadoActual;
            AccionIntentada = accionIntentada;
        }
    }

    public class ContraseniaInvalidaException : UsuarioException
    {
        public string Dni { get; }

        public ContraseniaInvalidaException(string dni)
            : base(GestorIdioma06AV.Instancia.Obtener("exc_contrasenia_invalida", dni), "USR_CONTRASENIA_INVALIDA")
        {
            Dni = dni;
        }
    }

    public class UsuarioAccesoDatosException : UsuarioException
    {
        public UsuarioAccesoDatosException(string operacion, Exception inner)
            : base(GestorIdioma06AV.Instancia.Obtener("exc_acceso_datos", operacion), inner, "USR_DB_ERROR")
        {
        }
    }
}
