using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public class ResultadoVerificacion06AV
    {
        public string Clave { get; set; }
        public string Descripcion { get; set; }
        public EstadoVerificacion06AV Estado { get; set; }

        public string Detalle { get; set; }
    }

    public class InformeVerificacion06AV
    {
        public string Ruta { get; set; }
        public ClaseSerializable06AV Clase { get; set; }
        public int Cantidad { get; set; }

        public bool ArchivoIntegro { get; set; }

        public string ErrorComparacion { get; set; }

        public List<ResultadoVerificacion06AV> Resultados { get; set; } = new List<ResultadoVerificacion06AV>();

        public int Cuantos(EstadoVerificacion06AV estado) => Resultados.Count(r => r.Estado == estado);

        public bool TodoCoincide() =>
            ArchivoIntegro && ErrorComparacion == null && Resultados.Count > 0 &&
            Resultados.All(r => r.Estado == EstadoVerificacion06AV.Coincide);
    }
}
