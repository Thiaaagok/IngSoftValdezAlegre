using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SER.Serializacion
{
    /// <summary>
    /// Serializa y des-serializa objetos a XML con XmlSerializer. No conoce las entidades
    /// del negocio: toma las propiedades públicas con get y set del tipo real del objeto.
    /// </summary>
    public class SerializadorXmlSER06AV
    {
        private static readonly Encoding Utf8SinBom = new UTF8Encoding(false);

        /// <summary>Graba el objeto en <paramref name="ruta"/>; si el archivo existe, lo reemplaza.</summary>
        public void Serializar<T>(T objeto, string ruta)
        {
            if (objeto == null) throw new ArgumentNullException(nameof(objeto));
            if (string.IsNullOrWhiteSpace(ruta)) throw new ArgumentException("Falta la ruta del archivo.", nameof(ruta));

            // Se escribe en un temporal de la misma carpeta y después se renombra: si la
            // grabación falla a mitad de camino no queda un XML cortado con el nombre final.
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

        /// <summary>Lee el archivo y reconstruye el objeto.</summary>
        /// <exception cref="InvalidOperationException">El XML no es un <typeparamref name="T"/> o está mal formado.</exception>
        public T Deserializar<T>(string ruta)
        {
            if (!File.Exists(ruta)) throw new FileNotFoundException("No existe el archivo.", ruta);

            // Sin DTD ni resolución de entidades: un XML que llega de afuera no puede
            // hacer que el sistema lea otros archivos del disco (ataque XXE).
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

        /// <summary>El XML del objeto en memoria, tal como quedaría en el archivo.</summary>
        public string ATexto<T>(T objeto)
        {
            if (objeto == null) return "";
            var texto = new StringBuilder();
            using (var escritor = XmlWriter.Create(texto, Configuracion(Utf8SinBom)))
                Serializador(objeto.GetType()).Serialize(escritor, objeto, EspaciosDeNombre());
            return texto.ToString();
        }

        public string LeerTexto(string ruta) => File.ReadAllText(ruta, Encoding.UTF8);

        // El constructor que recibe solo el tipo queda en caché del framework; los otros
        // generan un ensamblado nuevo en cada llamada.
        private static XmlSerializer Serializador(Type tipo) => new XmlSerializer(tipo);

        private static XmlWriterSettings Configuracion(Encoding codificacion) =>
            new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = codificacion };

        // Solo xsi, declarado una vez en la raíz: lo usan los nulos (xsi:nil) de los
        // campos int? y decimal?. Sin esto, cada nulo declara su propio prefijo (p4, p5…).
        private static XmlSerializerNamespaces EspaciosDeNombre()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            return ns;
        }
    }
}
