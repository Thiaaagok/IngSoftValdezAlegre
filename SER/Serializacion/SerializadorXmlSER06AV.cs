using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SER.Serializacion
{
    public class SerializadorXmlSER06AV
    {
        private static readonly Encoding Utf8SinBom = new UTF8Encoding(false);

        public void Serializar<T>(T objeto, string ruta)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentException("Falta la ruta del archivo.", nameof(ruta));

            string temporal = ruta + ".tmp";
            try
            {
                using (var escritor = XmlWriter.Create(temporal, Configuracion(Utf8SinBom)))
                    Serializador(objeto.GetType()).Serialize(escritor, objeto, EspaciosDeNombre());
                if (File.Exists(ruta)) File.Delete(ruta);
                File.Move(temporal, ruta);
            }
            finally
            {
                if (File.Exists(temporal)) File.Delete(temporal);
            }
        }

        public T Deserializar<T>(string ruta)
        {
            if (!File.Exists(ruta)) throw new FileNotFoundException("No existe el archivo.", ruta);

            var configuracion = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true
            };
            using (var lector = XmlReader.Create(ruta, configuracion))
            {
                XmlSerializer serializador = Serializador(typeof(T));
                if (!serializador.CanDeserialize(lector))
                    throw new InvalidOperationException($"El archivo no contiene un {typeof(T).Name}.");
                return (T)serializador.Deserialize(lector);
            }
        }

        public string ATexto<T>(T objeto)
        {
            if (objeto == null) return "";
            var texto = new StringBuilder();
            using (var escritor = XmlWriter.Create(texto, Configuracion(Utf8SinBom)))
                Serializador(objeto.GetType()).Serialize(escritor, objeto, EspaciosDeNombre());
            return texto.ToString();
        }

        public string LeerTexto(string ruta) => File.ReadAllText(ruta, Encoding.UTF8);

        private static XmlSerializer Serializador(Type tipo) => new XmlSerializer(tipo);

        private static XmlWriterSettings Configuracion(Encoding codificacion) =>
            new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = codificacion };

        private static XmlSerializerNamespaces EspaciosDeNombre()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            return ns;
        }
    }
}
