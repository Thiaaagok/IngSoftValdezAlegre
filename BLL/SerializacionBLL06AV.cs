using BE;
using BLL.Excepciones;
using SER;
using SER.Encriptador;
using SER.Serializacion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace BLL
{
    /// <summary>
    /// A03 Serialización. Los ocho pasos de la planilla:
    ///   CU-SER01  1 <see cref="ObtenerObjetos"/>, 2 <see cref="ValidarDestino"/>,
    ///             3 <see cref="Serializar"/>, 4 <see cref="VerificarSerializacion"/>.
    ///   CU-SER02  5 <see cref="ListarArchivos"/>, 6 <see cref="LeerContenido"/>,
    ///             7 <see cref="Deserializar"/>, 8 <see cref="VerificarDeserializacion"/>.
    ///
    /// Des-serializar no escribe en la base: reconstruye los objetos y los compara.
    /// Las partes que no dependen de la sesión ni de la base son estáticas.
    /// </summary>
    public class SerializacionBLL06AV
    {
        public const string Extension = ".xml";
        public const int VersionFormato = 1;

        // Se antepone al XML antes de calcular el hash: sin ella, quien edite el archivo
        // podría recalcular el SHA-256 a mano y dejarlo como íntegro.
        private const string SalHash = "PCFORGE-A03-06AV|";

        // Datos de inventario: cambian con cada venta o compra, no con el objeto
        // serializado, y harían que toda venta vieja figure como "Difiere".
        private static readonly HashSet<string> PropiedadesIgnoradas =
            new HashSet<string>(StringComparer.Ordinal) { "Stock", "StockReservado", "StockMinimo" };

        private const int MaximoDiferencias = 8;

        private static readonly SerializadorXmlSER06AV Serializador = new SerializadorXmlSER06AV();
        private static readonly EncriptacionSER06AV Encriptador = new EncriptacionSER06AV();

        private readonly VentasBLL06AV _ventas = new VentasBLL06AV();

        // ══════════════════════════════════════════════════════════
        //  CU-SER01 Serializar objetos
        // ══════════════════════════════════════════════════════════

        /// <summary>Paso 1: los objetos de la clase elegida, para seleccionarlos.</summary>
        /// <exception cref="ValidacionException06AV">Sin la patente Serializar.</exception>
        /// <exception cref="AccesoDatosException06AV">Si falla la consulta.</exception>
        public List<object> ObtenerObjetos(ClaseSerializable06AV clase)
        {
            ExigirPermiso();
            switch (clase)
            {
                case ClaseSerializable06AV.Venta: return _ventas.ObtenerTodas().Cast<object>().ToList();
                default: throw new ValidacionException06AV("clase", "La clase elegida no se puede serializar.");
            }
        }

        /// <summary>Paso 2: nombre sugerido, &lt;Clase&gt;_&lt;aaaaMMdd_HHmmss&gt;.xml.</summary>
        public static string ProponerNombreArchivo(ClaseSerializable06AV clase, DateTime ahora) =>
            clase + "_" + ahora.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + Extension;

        /// <summary>Carpeta donde se abren por primera vez los diálogos de ubicación.</summary>
        public static string CarpetaPorDefecto()
        {
            string carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PCFORGE", "Serializacion");
            try { Directory.CreateDirectory(carpeta); }
            catch (Exception) { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
            return carpeta;
        }

        /// <summary>
        /// Paso 2: la ruta tiene nombre y extensión .xml, la carpeta existe y se puede
        /// escribir en ella. No valida que el archivo no exista: eso lo confirma el usuario.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Con el motivo.</exception>
        public static void ValidarDestino(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ValidacionException06AV("ruta", "Elegí la carpeta de destino.");

            string carpeta, nombre;
            try
            {
                carpeta = Path.GetDirectoryName(ruta);
                nombre = Path.GetFileNameWithoutExtension(ruta);
            }
            catch (ArgumentException)
            {
                throw new ValidacionException06AV("ruta", "El nombre del archivo tiene caracteres no válidos.");
            }
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ValidacionException06AV("ruta", "Falta el nombre del archivo.");
            if (!string.Equals(Path.GetExtension(ruta), Extension, StringComparison.OrdinalIgnoreCase))
                throw new ValidacionException06AV("ruta", "El archivo tiene que tener extensión .xml.");
            if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta))
                throw new ValidacionException06AV("ruta", "La carpeta de destino no existe.");

            // La única forma confiable de saber si se puede escribir es intentarlo.
            string prueba = Path.Combine(carpeta, "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(prueba, "");
                File.Delete(prueba);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                throw new ValidacionException06AV("ruta", "No se puede escribir en la carpeta de destino.");
            }
        }

        /// <summary>
        /// Paso 3: arma el paquete con su hash y lo graba en <paramref name="ruta"/>,
        /// reemplazando el archivo si ya existe. Registra el evento en la bitácora.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Sin permiso, sin objetos o con destino inválido.</exception>
        /// <exception cref="AccesoDatosException06AV">Si falla la grabación.</exception>
        public PaqueteSerializado06AV Serializar(ClaseSerializable06AV clase, List<object> objetos, string ruta)
        {
            ExigirPermiso();
            PaqueteSerializado06AV paquete = ArmarPaquete(clase, objetos, DniOperador(), DateTime.Now);
            ValidarDestino(ruta);

            try { Serializador.Serializar(paquete, ruta); }
            catch (Exception ex)
            {
                Registrar(CategoriaBitacora.Error, CriticidadBitacora.Media,
                    $"Falló la serialización de {paquete.Cantidad} objetos {clase} en {ruta}: {ex.Message}");
                throw new AccesoDatosException06AV("No se pudo grabar el archivo: " + ex.Message, ex);
            }

            Registrar(CategoriaBitacora.Serializacion, CriticidadBitacora.Baja,
                $"Se serializaron {paquete.Cantidad} objetos {clase} en {ruta}.");
            return paquete;
        }

        /// <summary>Arma el paquete del paso 3 y le calcula el hash. No toca el disco.</summary>
        /// <exception cref="ValidacionException06AV">Sin objetos, con objetos de otra clase o repetidos.</exception>
        public static PaqueteSerializado06AV ArmarPaquete(ClaseSerializable06AV clase, IList<object> objetos,
                                                          string generadoPor, DateTime ahora)
        {
            ValidarObjetos(clase, objetos);
            var paquete = new PaqueteSerializado06AV
            {
                Clase = clase,
                Version = VersionFormato,
                FechaGeneracion = DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified),
                GeneradoPor = generadoPor,
                Cantidad = objetos.Count,
                Objetos = objetos.ToList()
            };
            paquete.Hash = CalcularHash(paquete);
            return paquete;
        }

        /// <exception cref="ValidacionException06AV">Con el motivo.</exception>
        public static void ValidarObjetos(ClaseSerializable06AV clase, IList<object> objetos)
        {
            if (objetos == null || objetos.Count == 0)
                throw new ValidacionException06AV("objetos", "Seleccioná al menos un objeto para serializar.");

            Type tipo = TipoDe(clase);
            if (objetos.Any(o => o == null || o.GetType() != tipo))
                throw new ValidacionException06AV("objetos", $"Todos los objetos tienen que ser de la clase {clase}.");

            var repetido = objetos.GroupBy(ClaveDe).FirstOrDefault(g => g.Count() > 1);
            if (repetido != null)
                throw new ValidacionException06AV("objetos", $"El objeto {repetido.Key} está seleccionado más de una vez.");
        }

        /// <summary>
        /// Paso 4: lee el archivo recién grabado y compara cada objeto original con el
        /// que se reconstruyó. Todo tiene que coincidir.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Sin permiso o si el archivo no se puede leer.</exception>
        public InformeVerificacion06AV VerificarSerializacion(string ruta, IList<object> originales)
        {
            ExigirPermiso();
            return VerificarContraOriginales(ruta, originales);
        }

        /// <summary>El paso 4 sin la sesión: se usa también desde las pruebas.</summary>
        public static InformeVerificacion06AV VerificarContraOriginales(string ruta, IList<object> originales)
        {
            PaqueteSerializado06AV paquete = LeerPaquete(ruta);
            InformeVerificacion06AV informe = NuevoInforme(paquete, ruta);

            var leidos = paquete.Objetos.GroupBy(ClaveDe).ToDictionary(g => g.Key, g => g.First());
            foreach (object original in originales ?? new List<object>())
            {
                string clave = ClaveDe(original);
                informe.Resultados.Add(leidos.TryGetValue(clave, out object leido)
                    ? Comparar(original, leido)
                    : new ResultadoVerificacion06AV
                    {
                        Clave = clave,
                        Descripcion = original.ToString(),
                        Estado = EstadoVerificacion06AV.FaltaEnArchivo,
                        Detalle = "El objeto no está en el archivo."
                    });
            }
            return informe;
        }

        // ══════════════════════════════════════════════════════════
        //  CU-SER02 Des-serializar objetos
        // ══════════════════════════════════════════════════════════

        /// <summary>Paso 5: los archivos .xml de la carpeta, del más nuevo al más viejo.</summary>
        /// <exception cref="ValidacionException06AV">Sin permiso, o carpeta inexistente o sin acceso.</exception>
        public List<string> ListarArchivos(string carpeta)
        {
            ExigirPermiso();
            if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta))
                throw new ValidacionException06AV("carpeta", "La carpeta de origen no existe.");
            try
            {
                // "*.xml" en Windows también trae ".xmlx": se vuelve a filtrar por extensión exacta.
                return Directory.GetFiles(carpeta, "*" + Extension)
                    .Where(f => string.Equals(Path.GetExtension(f), Extension, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetLastWriteTime)
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                throw new ValidacionException06AV("carpeta", "No se puede leer la carpeta de origen.");
            }
        }

        /// <summary>Paso 6: el texto del archivo elegido, para mostrarlo antes de leerlo.</summary>
        /// <exception cref="ValidacionException06AV">Sin permiso o si el archivo no existe.</exception>
        public string LeerContenido(string ruta)
        {
            ExigirPermiso();
            ValidarArchivoOrigen(ruta);
            try { return Serializador.LeerTexto(ruta); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ValidacionException06AV("archivo", "No se pudo leer el archivo: " + ex.Message);
            }
        }

        /// <summary>Paso 7: reconstruye el paquete y sus objetos. Registra el evento en la bitácora.</summary>
        /// <exception cref="ValidacionException06AV">Sin permiso, o archivo con formato o versión inválidos.</exception>
        public PaqueteSerializado06AV Deserializar(string ruta)
        {
            ExigirPermiso();
            PaqueteSerializado06AV paquete;
            try { paquete = LeerPaquete(ruta); }
            catch (ValidacionException06AV ex)
            {
                Registrar(CategoriaBitacora.Error, CriticidadBitacora.Media,
                    $"Se rechazó la des-serialización de {ruta}: {ex.Message}");
                throw;
            }

            Registrar(CategoriaBitacora.Deserializacion, CriticidadBitacora.Baja,
                $"Se des-serializaron {paquete.Objetos.Count} objetos {paquete.Clase} desde {ruta}.");
            return paquete;
        }

        /// <summary>
        /// Lee y valida un archivo: formato de paquete, versión conocida y objetos de la
        /// clase declarada. No verifica el hash: eso es el paso 8.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Con el motivo.</exception>
        public static PaqueteSerializado06AV LeerPaquete(string ruta)
        {
            ValidarArchivoOrigen(ruta);

            PaqueteSerializado06AV paquete;
            try { paquete = Serializador.Deserializar<PaqueteSerializado06AV>(ruta); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is XmlException)
            {
                throw new ValidacionException06AV("archivo",
                    "El archivo no tiene un formato válido: no es un paquete serializado de PCFORGE.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ValidacionException06AV("archivo", "No se pudo leer el archivo: " + ex.Message);
            }

            if (paquete == null)
                throw new ValidacionException06AV("archivo", "El archivo está vacío.");
            if (paquete.Version != VersionFormato)
                throw new ValidacionException06AV("archivo",
                    $"El archivo usa la versión {paquete.Version} del formato y el sistema lee la versión {VersionFormato}.");
            if (paquete.Objetos == null) paquete.Objetos = new List<object>();

            Type tipo = TipoDe(paquete.Clase);
            if (paquete.Objetos.Any(o => o == null || o.GetType() != tipo))
                throw new ValidacionException06AV("archivo",
                    $"El archivo declara objetos {paquete.Clase} pero trae objetos de otra clase.");
            return paquete;
        }

        /// <summary>
        /// Paso 8: recalcula el hash y compara cada objeto con su versión actual en la base.
        /// Si la base no responde, el informe conserva la verificación de integridad y
        /// deja el motivo en <see cref="InformeVerificacion06AV.ErrorComparacion"/>.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Sin la patente Serializar.</exception>
        public InformeVerificacion06AV VerificarDeserializacion(PaqueteSerializado06AV paquete, string ruta)
        {
            ExigirPermiso();
            InformeVerificacion06AV informe = VerificarContraBase(paquete, ruta, CrearBuscador(paquete.Clase));

            if (!informe.ArchivoIntegro)
                Registrar(CategoriaBitacora.Error, CriticidadBitacora.Alta,
                    $"El archivo {ruta} se modificó después de generarse: el hash no coincide.");
            return informe;
        }

        /// <summary>El paso 8 con la búsqueda en la base recibida por parámetro.</summary>
        public static InformeVerificacion06AV VerificarContraBase(PaqueteSerializado06AV paquete, string ruta,
                                                                  Func<string, object> buscarActual)
        {
            if (paquete == null) throw new ValidacionException06AV("paquete", "Primero des-serializá un archivo.");
            InformeVerificacion06AV informe = NuevoInforme(paquete, ruta);

            foreach (object objeto in paquete.Objetos)
            {
                string clave = ClaveDe(objeto);
                object actual;
                try { actual = buscarActual(clave); }
                catch (Exception ex)
                {
                    informe.ErrorComparacion = ex.Message;
                    break;
                }

                informe.Resultados.Add(actual == null
                    ? new ResultadoVerificacion06AV
                    {
                        Clave = clave,
                        Descripcion = objeto.ToString(),
                        Estado = EstadoVerificacion06AV.NoExisteEnBase,
                        Detalle = "No hay un objeto con esa clave en la base."
                    }
                    : Comparar(objeto, actual));
            }
            return informe;
        }

        // ══════════════════════════════════════════════════════════
        //  Hash, claves y comparación
        // ══════════════════════════════════════════════════════════

        /// <summary>SHA-256 (en Base64) del paquete serializado con Hash en null.</summary>
        public static string CalcularHash(PaqueteSerializado06AV paquete)
        {
            string guardado = paquete.Hash;
            try
            {
                paquete.Hash = null;
                return Encriptador.Encriptar(SalHash + Serializador.ATexto(paquete));
            }
            finally { paquete.Hash = guardado; }
        }

        /// <summary>El hash del archivo coincide con el recalculado y la cantidad declarada es la real.</summary>
        public static bool EsIntegro(PaqueteSerializado06AV paquete) =>
            paquete != null &&
            !string.IsNullOrEmpty(paquete.Hash) &&
            string.Equals(paquete.Hash, CalcularHash(paquete), StringComparison.Ordinal) &&
            paquete.Cantidad == paquete.Objetos.Count;

        public static Type TipoDe(ClaseSerializable06AV clase)
        {
            switch (clase)
            {
                case ClaseSerializable06AV.Venta: return typeof(Venta06AV);
                default: throw new ValidacionException06AV("clase", "La clase elegida no se puede serializar.");
            }
        }

        /// <summary>Número de venta, DNI del cliente o Id del modelo.</summary>
        public static string ClaveDe(object objeto)
        {
            switch (objeto)
            {
                case Venta06AV v: return v.NumeroVenta.ToString(CultureInfo.InvariantCulture);
                default: throw new ArgumentException("El objeto no es de una clase serializable.", nameof(objeto));
            }
        }

        /// <summary>
        /// Compara dos objetos propiedad por propiedad, incluidas las listas y los objetos
        /// que contienen. Las propiedades calculadas (sin set) no se comparan porque no se
        /// serializan: salen de las demás.
        /// </summary>
        public static ResultadoVerificacion06AV Comparar(object esperado, object obtenido)
        {
            var diferencias = new List<string>();
            Diferencias(esperado, obtenido, "", diferencias);

            string detalle = null;
            if (diferencias.Count > 0)
            {
                detalle = string.Join("; ", diferencias.Take(MaximoDiferencias));
                if (diferencias.Count > MaximoDiferencias)
                    detalle += $"; y {diferencias.Count - MaximoDiferencias} más";
            }
            return new ResultadoVerificacion06AV
            {
                Clave = ClaveDe(esperado),
                Descripcion = esperado.ToString(),
                Estado = diferencias.Count == 0 ? EstadoVerificacion06AV.Coincide : EstadoVerificacion06AV.Difiere,
                Detalle = detalle
            };
        }

        private static void Diferencias(object a, object b, string ruta, List<string> salida)
        {
            if (a == null && b == null) return;
            if (a == null || b == null || a.GetType() != b.GetType())
            {
                salida.Add($"{Nombre(ruta)}: {Texto(a)} → {Texto(b)}");
                return;
            }

            Type tipo = a.GetType();
            if (EsSimple(tipo))
            {
                if (!Equals(a, b)) salida.Add($"{Nombre(ruta)}: {Texto(a)} → {Texto(b)}");
                return;
            }

            if (a is IList listaA)
            {
                var listaB = (IList)b;
                if (listaA.Count != listaB.Count)
                {
                    salida.Add($"{Nombre(ruta)}: {listaA.Count} elementos → {listaB.Count}");
                    return;
                }
                for (int i = 0; i < listaA.Count; i++)
                    Diferencias(listaA[i], listaB[i], $"{ruta}[{i + 1}]", salida);
                return;
            }

            foreach (PropertyInfo p in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length > 0) continue;
                if (PropiedadesIgnoradas.Contains(p.Name)) continue;
                Diferencias(p.GetValue(a), p.GetValue(b), ruta == "" ? p.Name : ruta + "." + p.Name, salida);
            }
        }

        private static bool EsSimple(Type t) =>
            t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) ||
            t == typeof(DateTime) || t == typeof(Guid) || t == typeof(TimeSpan);

        private static string Nombre(string ruta) => ruta == "" ? "objeto" : ruta;

        private static string Texto(object v)
        {
            if (v == null) return "(vacío)";
            if (v is DateTime d) return d.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            if (v is decimal m) return m.ToString("0.00", CultureInfo.InvariantCulture);
            if (v is string s) return s.Length == 0 ? "\"\"" : "\"" + s + "\"";
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        // ══════════════════════════════════════════════════════════
        //  Auxiliares
        // ══════════════════════════════════════════════════════════

        private static InformeVerificacion06AV NuevoInforme(PaqueteSerializado06AV paquete, string ruta) =>
            new InformeVerificacion06AV
            {
                Ruta = ruta,
                Clase = paquete.Clase,
                Cantidad = paquete.Objetos.Count,
                ArchivoIntegro = EsIntegro(paquete)
            };

        private static void ValidarArchivoOrigen(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ValidacionException06AV("archivo", "Elegí un archivo.");
            if (!File.Exists(ruta))
                throw new ValidacionException06AV("archivo", "El archivo ya no existe en la carpeta.");
        }

        /// <summary>
        /// Búsqueda por clave (número de venta) en la base para el paso 8.
        /// </summary>
        private Func<string, object> CrearBuscador(ClaseSerializable06AV clase)
        {
            switch (clase)
            {
                case ClaseSerializable06AV.Venta:
                    return clave => int.TryParse(clave, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numero)
                        ? _ventas.ObtenerPorNumero(numero) : null;

                default: throw new ValidacionException06AV("clase", "La clase elegida no se puede serializar.");
            }
        }

        // El menú ya se oculta sin la patente; esto cubre llamadas que no pasen por la pantalla.
        private static void ExigirPermiso()
        {
            if (!UsuarioSesion06AV.Instancia().TienePermiso(PatenteEnum06AV.Serializar))
                throw new ValidacionException06AV("permiso", "No tenés permiso para serializar (patente Serializar).");
        }

        private static string DniOperador()
        {
            try { return UsuarioSesion06AV.Instancia().UsuarioActual?.Dni; }
            catch { return null; }
        }

        // Si la bitácora falla, la operación igual quedó hecha: no se revierte por eso.
        private static void Registrar(CategoriaBitacora categoria, CriticidadBitacora criticidad, string descripcion)
        {
            try { new BitacoraBLL06AV().Registrar(categoria, criticidad, descripcion, ModuloBitacora.Serializacion, DniOperador()); }
            catch { }
        }
    }
}
