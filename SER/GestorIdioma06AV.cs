using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SER
{
    /// <summary>
    /// Singleton que gestiona el idioma activo de la sesión.
    /// Se inicializa al hacer Login con el idioma guardado del usuario.
    /// El usuario puede cambiarlo en cualquier momento (persiste via BLL).
    /// </summary>
    public sealed class GestorIdioma06AV
    {
        #region Singleton

        private static GestorIdioma06AV _instancia;
        private static readonly object _lock = new object();

        private GestorIdioma06AV()
        {
            _idiomaActual = IdiomaDefecto;
            CargarTraducciones();
        }

        public static GestorIdioma06AV Instancia
        {
            get
            {
                if (_instancia == null)
                    lock (_lock)
                        if (_instancia == null)
                            _instancia = new GestorIdioma06AV();
                return _instancia;
            }
        }

        #endregion

        #region Constantes

        public const string ES = "ES";
        public const string EN = "EN";
        public const string IdiomaDefecto = ES;

        public static readonly string[] IdiomasDisponibles = { ES, EN };

        /// <summary>
        /// Carpeta donde viven los archivos de traducción (relativa al ejecutable).
        /// Estructura esperada: Resources/Idiomas/es.json, Resources/Idiomas/en.json
        /// </summary>
        string raizProyecto = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        private string CarpetaIdiomas;

        #endregion

        #region Observer

        /// <summary>
        /// Se dispara cada vez que el idioma activo cambia.
        /// Todos los formularios y controles deben suscribirse a este evento
        /// para refrescarse automáticamente.
        /// </summary>
        public event System.Action IdiomaChanged;

        private void NotificarCambio() => IdiomaChanged?.Invoke();

        #endregion

        #region Estado

        private string _idiomaActual;

        /// <summary>Idioma actualmente activo en la sesión.</summary>
        public string IdiomaActual => _idiomaActual;

        /// <summary>
        /// Carga el idioma del usuario al iniciar sesión.
        /// No dispara el evento porque la UI aún no está construida en ese punto.
        /// </summary>
        public void Cargar(string idioma)
        {
            _idiomaActual = EsValido(idioma) ? idioma : IdiomaDefecto;
        }

        /// <summary>
        /// Cambia el idioma activo en memoria y notifica a todos los observadores.
        /// Para persistirlo a la base de datos llamar a UsuariosBLL06AV.CambiarIdioma().
        /// </summary>
        public void CambiarIdioma(string idioma)
        {
            if (!EsValido(idioma))
                throw new ArgumentException(
                    $"Idioma '{idioma}' no está disponible. Opciones: {string.Join(", ", IdiomasDisponibles)}");
            _idiomaActual = idioma;
            NotificarCambio();
        }

        public bool EsValido(string idioma)
        {
            if (string.IsNullOrWhiteSpace(idioma)) return false;
            foreach (var i in IdiomasDisponibles)
                if (i == idioma.ToUpper()) return true;
            return false;
        }

        #endregion

        #region Carga de JSON

        // Diccionario en memoria: idioma → (clave → texto)
        private Dictionary<string, Dictionary<string, string>> _traducciones
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Carga todos los archivos de idioma disponibles desde Resources/Idiomas/.
        /// Si un archivo no existe o está malformado, ese idioma queda vacío (se
        /// devuelve la clave como fallback) sin tirar excepción en runtime.
        /// </summary>
        private void CargarTraducciones()
        {
            CarpetaIdiomas = Path.Combine(raizProyecto,"Resources", "Idiomas");
            foreach (string codigo in IdiomasDisponibles)
            {
                string ruta = Path.Combine(CarpetaIdiomas, codigo.ToLower() + ".json");

                Console.WriteLine(ruta);

                _traducciones[codigo] = LeerJson(ruta);
            }
        }

        /// <summary>
        /// Fuerza la recarga de todos los archivos de idioma desde disco.
        /// Útil para desarrollo / hot-reload sin reiniciar la aplicación.
        /// </summary>
        public void RecargarTraducciones()
        {
            CargarTraducciones();
            NotificarCambio();
        }

        /// <summary>
        /// Lee un archivo JSON plano clave:valor y devuelve el diccionario.
        /// Implementado sin dependencias externas usando DataContractJsonSerializer.
        /// </summary>
        private static Dictionary<string, string> LeerJson(string ruta)
        {
            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(ruta))
                return resultado;

            try
            {
                string contenido = File.ReadAllText(ruta, Encoding.UTF8);

                // Parseo manual liviano para JSON plano { "clave": "valor", ... }
                // Evita dependencia de Newtonsoft o System.Text.Json en proyectos .NET Framework.
                contenido = contenido.Trim();
                if (contenido.StartsWith("{")) contenido = contenido.Substring(1);
                if (contenido.EndsWith("}")) contenido = contenido.Substring(0, contenido.Length - 1);

                // Divide en pares separados por comas que NO estén dentro de comillas
                var pares = SplitJson(contenido);

                foreach (string par in pares)
                {
                    string trimmed = par.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    int separador = trimmed.IndexOf(':');
                    if (separador < 0) continue;

                    string clave = LimpiarCadenaJson(trimmed.Substring(0, separador).Trim());
                    string valor = LimpiarCadenaJson(trimmed.Substring(separador + 1).Trim());

                    if (!string.IsNullOrEmpty(clave))
                        resultado[clave] = valor;
                }
            }
            catch
            {
                // Si el JSON está malformado, devolvemos lo que pudimos parsear
            }

            return resultado;
        }

        /// <summary>
        /// Divide el contenido JSON en pares clave:valor respetando comas dentro de strings.
        /// </summary>
        private static List<string> SplitJson(string contenido)
        {
            var pares = new List<string>();
            bool dentroString = false;
            bool escape = false;
            int inicio = 0;

            for (int i = 0; i < contenido.Length; i++)
            {
                char c = contenido[i];

                if (escape) { escape = false; continue; }
                if (c == '\\') { escape = true; continue; }
                if (c == '"') { dentroString = !dentroString; continue; }

                if (!dentroString && c == ',')
                {
                    pares.Add(contenido.Substring(inicio, i - inicio));
                    inicio = i + 1;
                }
            }

            // último par
            string ultimo = contenido.Substring(inicio).Trim();
            if (!string.IsNullOrEmpty(ultimo))
                pares.Add(ultimo);

            return pares;
        }

        /// <summary>
        /// Quita las comillas externas de un token JSON y decodifica secuencias de escape básicas.
        /// </summary>
        private static string LimpiarCadenaJson(string token)
        {
            if (token.Length >= 2 && token[0] == '"' && token[token.Length - 1] == '"')
                token = token.Substring(1, token.Length - 2);

            return token
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        #endregion

        #region Traducciones

        /// <summary>
        /// Retorna el texto traducido para la clave dada en el idioma activo.
        /// Si la clave no existe, retorna la clave misma como fallback.
        /// </summary>
        public string Obtener(string clave)
        {
            if (_traducciones.TryGetValue(_idiomaActual, out var dicc))
                if (dicc.TryGetValue(clave, out var texto))
                    return texto;
            return clave; 
        }

        /// <summary>
        /// Retorna el texto traducido con formato aplicado (string.Format).
        /// Útil para mensajes con parámetros como nombres, DNIs, etc.
        /// </summary>
        public string Obtener(string clave, params object[] args)
        {
            string plantilla = Obtener(clave);
            if (args == null || args.Length == 0) return plantilla;
            try { return string.Format(plantilla, args); }
            catch { return plantilla; }
        }

        #endregion
    }
}
