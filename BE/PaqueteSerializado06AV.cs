using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// Contenido de un archivo de serialización (A03): los datos de control y los objetos.
    ///
    /// <see cref="Hash"/> es el SHA-256 del propio paquete calculado con Hash en null; al
    /// leer el archivo se recalcula para saber si alguien lo modificó.
    /// </summary>
    [XmlRoot("PaqueteSerializado")]
    public class PaqueteSerializado06AV
    {
        public ClaseSerializable06AV Clase { get; set; }

        /// <summary>Versión del formato del archivo, para rechazar archivos que el sistema no sabe leer.</summary>
        public int Version { get; set; }

        /// <summary>
        /// Sin zona horaria (Kind Unspecified): con hora local, XmlSerializer escribe el
        /// desfase (-03:00) y el mismo archivo leído en otra zona daría otro hash.
        /// </summary>
        public DateTime FechaGeneracion { get; set; }

        /// <summary>DNI del usuario que generó el archivo.</summary>
        public string GeneradoPor { get; set; }

        /// <summary>Cantidad declarada al generar; si no coincide con la de Objetos, el archivo se tocó.</summary>
        public int Cantidad { get; set; }

        public string Hash { get; set; }

        // Cada objeto va como <Venta>: con una lista de object, así sabe XmlSerializer
        // qué tipo reconstruir al leer.
        [XmlArray("Objetos")]
        [XmlArrayItem("Venta", typeof(Venta06AV))]
        public List<object> Objetos { get; set; } = new List<object>();
    }
}
