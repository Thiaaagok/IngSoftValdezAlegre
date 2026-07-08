using System;

namespace BLL.Excepciones
{
    /// <summary>Excepción base del dominio PC Factory (ventas, producción, compras).</summary>
    public class PcFactoryException06AV : Exception
    {
        public string CodigoError { get; }

        public PcFactoryException06AV(string mensaje, string codigoError = "PCF_ERROR")
            : base(mensaje)
        {
            CodigoError = codigoError;
        }

        public PcFactoryException06AV(string mensaje, Exception inner, string codigoError = "PCF_ERROR")
            : base(mensaje, inner)
        {
            CodigoError = codigoError;
        }
    }

    /// <summary>Error de validación de datos de entrada (campo obligatorio, formato, etc.).</summary>
    public class ValidacionException06AV : PcFactoryException06AV
    {
        public string Campo { get; }

        public ValidacionException06AV(string campo, string mensaje)
            : base(mensaje, "PCF_VALIDACION")
        {
            Campo = campo;
        }
    }

    /// <summary>La entidad buscada no existe.</summary>
    public class NoEncontradoException06AV : PcFactoryException06AV
    {
        public NoEncontradoException06AV(string mensaje)
            : base(mensaje, "PCF_NOT_FOUND") { }
    }

    /// <summary>Ya existe una entidad con la misma clave.</summary>
    public class DuplicadoException06AV : PcFactoryException06AV
    {
        public DuplicadoException06AV(string mensaje)
            : base(mensaje, "PCF_DUPLICADO") { }
    }

    /// <summary>Falla en el acceso a datos (BD).</summary>
    public class AccesoDatosException06AV : PcFactoryException06AV
    {
        public AccesoDatosException06AV(string mensaje, Exception inner)
            : base(mensaje, inner, "PCF_DATOS") { }
    }
}
