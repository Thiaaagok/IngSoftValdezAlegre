using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SER
{
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
        public const string PT = "PT";
        public const string IdiomaDefecto = ES;

        public static readonly string[] IdiomasDisponibles = { ES, EN, PT };

        private string CarpetaIdiomas;

        private static string ResolverCarpetaIdiomas()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string juntoExe = Path.Combine(baseDir, "Resources", "Idiomas");
            if (Directory.Exists(juntoExe)) return juntoExe;

            try
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 5 && dir != null; i++)
                {
                    string candidato = Path.Combine(dir.FullName, "Resources", "Idiomas");
                    if (Directory.Exists(candidato)) return candidato;
                    dir = dir.Parent;
                }
            }
            catch {  }

            return juntoExe;
        }

        #endregion

        #region Observer

        public event System.Action IdiomaChanged;

        private void NotificarCambio() => IdiomaChanged?.Invoke();

        #endregion

        #region Estado

        private string _idiomaActual;

        public string IdiomaActual => _idiomaActual;

        public void Cargar(string idioma)
        {
            _idiomaActual = EsValido(idioma) ? idioma.ToUpperInvariant() : IdiomaDefecto;
        }

        public void CambiarIdioma(string idioma)
        {
            if (!EsValido(idioma))
                throw new ArgumentException(
                    $"Idioma '{idioma}' no está disponible. Opciones: {string.Join(", ", IdiomasDisponibles)}");
            _idiomaActual = idioma.ToUpperInvariant();
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

        private Dictionary<string, Dictionary<string, string>> _traducciones
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private void CargarTraducciones()
        {
            CarpetaIdiomas = ResolverCarpetaIdiomas();
            foreach (string codigo in IdiomasDisponibles)
            {
                string ruta = Path.Combine(CarpetaIdiomas, codigo.ToLower() + ".json");
                _traducciones[codigo] = LeerJson(ruta);
            }
        }

        public void RecargarTraducciones()
        {
            CargarTraducciones();
            NotificarCambio();
        }

        private static Dictionary<string, string> LeerJson(string ruta)
        {
            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(ruta))
                return resultado;

            try
            {
                string contenido = File.ReadAllText(ruta, Encoding.UTF8);

                contenido = contenido.Trim();
                if (contenido.StartsWith("{")) contenido = contenido.Substring(1);
                if (contenido.EndsWith("}")) contenido = contenido.Substring(0, contenido.Length - 1);

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
            }

            return resultado;
        }

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

            string ultimo = contenido.Substring(inicio).Trim();
            if (!string.IsNullOrEmpty(ultimo))
                pares.Add(ultimo);

            return pares;
        }

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

        public string Obtener(string clave)
        {
            if (_traducciones.TryGetValue(_idiomaActual, out var dicc))
                if (dicc.TryGetValue(clave, out var texto))
                    return texto;
            return clave;
        }

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
