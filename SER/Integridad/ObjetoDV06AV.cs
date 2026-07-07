using System;
using System.Collections.Generic;
using System.Linq;

namespace SER.Integridad
{
    public sealed class DigitoTabla06AV
    {
        public string Tabla { get; set; }
        public long DVH { get; set; }
        public long DVV { get; set; }

        public string DVHHex => DVH.ToString("X");
        public string DVVHex => DVV.ToString("X");
    }

    /// <summary>
    /// "OBJETO DV": los dígitos de toda la base. En GENERACIÓN se persiste; en
    /// REVISIÓN se genera igual pero solo en memoria para compararlo con lo almacenado.
    /// El DVH/DVV de la base es la suma de los de todas las tablas.
    /// </summary>
    public sealed class ObjetoDV06AV
    {
        public List<DigitoTabla06AV> Tablas { get; } = new List<DigitoTabla06AV>();

        public long DVHBaseDatos => Tablas.Sum(t => t.DVH);
        public long DVVBaseDatos => Tablas.Sum(t => t.DVV);

        public string DVHBaseDatosHex => DVHBaseDatos.ToString("X");
        public string DVVBaseDatosHex => DVVBaseDatos.ToString("X");

        public DigitoTabla06AV Buscar(string tabla)
        {
            return Tablas.FirstOrDefault(t =>
                string.Equals(t.Tabla, tabla, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Resultado de comparar el OBJETO DV generado contra el almacenado.</summary>
    public sealed class ResultadoVerificacion06AV
    {
        public bool EsConsistente { get; set; }

        // True cuando no había línea base en la tabla DV (primera ejecución): no es
        // una inconsistencia, hay que generar la base.
        public bool SinLineaBase { get; set; }

        public List<string> TablasInconsistentes { get; } = new List<string>();
        public List<string> Detalles { get; } = new List<string>();
    }
}
