using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BE
{
    [XmlRoot("PaqueteSerializado")]
    public class PaqueteSerializado06AV
    {
        public ClaseSerializable06AV Clase { get; set; }

        /// <summary>Versión del formato del archivo, para rechazar archivos que el sistema no sabe leer.</summary>
        public int Version { get; set; }

        public DateTime FechaGeneracion { get; set; }

        public string GeneradoPor { get; set; }

        /// <summary>Cantidad declarada al generar; si no coincide con la de Objetos, el archivo se tocó.</summary>
        public int Cantidad { get; set; }

        public string Hash { get; set; }

        [XmlArray("Objetos")]
        [XmlArrayItem("Venta", typeof(Venta06AV))]
        public List<object> Objetos { get; set; } = new List<object>();
    }
}
