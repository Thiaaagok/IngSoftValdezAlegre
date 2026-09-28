using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>Resultado de comparar un objeto del archivo con su versión de referencia.</summary>
    public class ResultadoVerificacion06AV
    {
        /// <summary>Número de venta, DNI del cliente o Id del modelo.</summary>
        public string Clave { get; set; }
        public string Descripcion { get; set; }
        public EstadoVerificacion06AV Estado { get; set; }

        /// <summary>Qué campos difieren, con el valor del archivo y el de referencia.</summary>
        public string Detalle { get; set; }
    }

    /// <summary>
    /// Resultado de una verificación (paso 4 o paso 8). La referencia es la lista
    /// original en el paso 4 y la base de datos en el paso 8.
    /// </summary>
    public class InformeVerificacion06AV
    {
        public string Ruta { get; set; }
        public ClaseSerializable06AV Clase { get; set; }
        public int Cantidad { get; set; }

        /// <summary>El hash recalculado coincide y la cantidad declarada es la real.</summary>
        public bool ArchivoIntegro { get; set; }

        /// <summary>
        /// Error al consultar la base en el paso 8. La integridad del archivo ya quedó
        /// verificada; lo que no se pudo hacer es la comparación.
        /// </summary>
        public string ErrorComparacion { get; set; }

        public List<ResultadoVerificacion06AV> Resultados { get; set; } = new List<ResultadoVerificacion06AV>();

        public int Cuantos(EstadoVerificacion06AV estado) => Resultados.Count(r => r.Estado == estado);

        public bool TodoCoincide() =>
            ArchivoIntegro && ErrorComparacion == null && Resultados.Count > 0 &&
            Resultados.All(r => r.Estado == EstadoVerificacion06AV.Coincide);
    }
}
