using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Instalador
{
    /// <summary>
    /// Deja la aplicación lista para conectarse a la instancia elegida durante la
    /// instalación: localiza el ejecutable del sistema y escribe la cadena de conexión
    /// (nombre "IngSoft") en su archivo de configuración (IngSoftValdezAlegre.exe.config),
    /// que es de donde la lee la clase Conexion.
    /// </summary>
    public static class ConfiguradorApp06AV
    {
        public const string NombreExeApp = "IngSoftValdezAlegre.exe";

        /// <summary>
        /// Busca el ejecutable del sistema. Primero junto al Instalador (caso distribuido),
        /// y si no, subiendo por el árbol de carpetas hacia las salidas bin\Debug|Release
        /// del proyecto principal (caso ejecución desde Visual Studio). Devuelve null si
        /// no lo encuentra.
        /// </summary>
        public static string LocalizarExeApp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1) Mismo directorio que el Instalador.
            string junto = Path.Combine(baseDir, NombreExeApp);
            if (File.Exists(junto)) return junto;

            // 2) Subir hasta la raíz del repo y buscar las salidas del proyecto principal.
            try
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    foreach (string cfg in new[] { "Debug", "Release" })
                    {
                        string candidato = Path.Combine(
                            dir.FullName, "IngSoftValdezAlegre", "bin", cfg, NombreExeApp);
                        if (File.Exists(candidato)) return candidato;
                    }
                    dir = dir.Parent;
                }
            }
            catch { /* si algo falla, se devuelve null */ }

            return null;
        }

        /// <summary>
        /// Escribe/actualiza la cadena de conexión "IngSoft" en el archivo de configuración
        /// del ejecutable indicado (exePath + ".config"). Devuelve true si pudo escribirla.
        /// </summary>
        public static bool EscribirCadenaConexion(string exePath, string connectionString)
        {
            if (string.IsNullOrEmpty(exePath)) return false;

            try
            {
                string configPath = exePath + ".config";

                XDocument doc = File.Exists(configPath)
                    ? XDocument.Load(configPath)
                    : new XDocument(new XElement("configuration"));

                XElement config = doc.Element("configuration");
                if (config == null)
                {
                    config = new XElement("configuration");
                    doc.Add(config);
                }

                XElement conns = config.Element("connectionStrings");
                if (conns == null)
                {
                    conns = new XElement("connectionStrings");
                    config.Add(conns);
                }

                XElement add = null;
                foreach (XElement e in conns.Elements("add"))
                {
                    var nombre = (string)e.Attribute("name");
                    if (string.Equals(nombre, "IngSoft", StringComparison.OrdinalIgnoreCase))
                    {
                        add = e;
                        break;
                    }
                }

                if (add == null)
                {
                    add = new XElement("add", new XAttribute("name", "IngSoft"));
                    conns.Add(add);
                }

                add.SetAttributeValue("connectionString", connectionString);
                add.SetAttributeValue("providerName", "System.Data.SqlClient");

                doc.Save(configPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Crea (o actualiza) un acceso directo al ejecutable del sistema en el Escritorio
        /// del usuario, para que pueda abrir el sistema con un doble clic sin tener que
        /// crearlo a mano. Usa WScript.Shell por COM tardío (reflexión) para no requerir
        /// referencias adicionales. Devuelve la ruta del acceso directo, o null si falla.
        /// </summary>
        public static string CrearAccesoDirectoEscritorio(
            string exePath, string nombreAcceso = "IngSoftValdezAlegre")
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    return null;

                string escritorio = Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);
                string rutaLnk = Path.Combine(escritorio, nombreAcceso + ".lnk");

                Type tipoShell = Type.GetTypeFromProgID("WScript.Shell");
                if (tipoShell == null) return null;

                object shell = Activator.CreateInstance(tipoShell);
                object acceso = tipoShell.InvokeMember("CreateShortcut",
                    BindingFlags.InvokeMethod, null, shell, new object[] { rutaLnk });

                Type tipoAcceso = acceso.GetType();
                void Set(string prop, object valor) => tipoAcceso.InvokeMember(
                    prop, BindingFlags.SetProperty, null, acceso, new object[] { valor });

                Set("TargetPath", exePath);
                Set("WorkingDirectory", Path.GetDirectoryName(exePath));
                Set("Description", "Sistema IngSoftValdezAlegre");
                Set("IconLocation", exePath + ", 0");

                tipoAcceso.InvokeMember("Save", BindingFlags.InvokeMethod, null, acceso, null);
                return rutaLnk;
            }
            catch
            {
                return null;
            }
        }
    }
}
