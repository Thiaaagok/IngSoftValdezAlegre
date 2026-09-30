using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Instalador
{
    public static class ConfiguradorApp06AV
    {
        public const string NombreExeApp = "PCFORGE-ValdezThiago-96VA.exe";

        public const string ArchivoConexion = "conexion.config";

        public static string LocalizarExeApp()
        {
            var todos = LocalizarTodosExeApp();
            return todos.Count > 0 ? todos[0] : null;
        }

        public static System.Collections.Generic.List<string> LocalizarTodosExeApp()
        {
            var lista = new System.Collections.Generic.List<string>();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            void Agregar(string ruta)
            {
                if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta) &&
                    !lista.Exists(x => string.Equals(x, ruta, StringComparison.OrdinalIgnoreCase)))
                    lista.Add(ruta);
            }

            Agregar(Path.Combine(baseDir, NombreExeApp));

            try
            {
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    foreach (string cfg in new[] { "Debug", "Release" })
                        Agregar(Path.Combine(dir.FullName, "PCFORGE-ValdezThiago-96VA", "bin", cfg, NombreExeApp));
                    dir = dir.Parent;
                }
            }
            catch {  }

            return lista;
        }

        public static bool EscribirCadenaConexion(string exePath, string connectionString)
        {
            if (string.IsNullOrEmpty(exePath)) return false;

            bool ok = false;

            // (A) Archivo externo "conexion.config" junto al exe. Es la FUENTE DE VERDAD que
            //     lee DAL.Conexion: no forma parte del proyecto, así que un rebuild de Visual
            //     Studio NO lo pisa (a diferencia del .exe.config, que se regenera desde App.config).
            try
            {
                string dir = Path.GetDirectoryName(exePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    File.WriteAllText(Path.Combine(dir, ArchivoConexion), connectionString);
                    ok = true;
                }
            }
            catch {  }

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
                ok = true;
            }
            catch {  }

            return ok;
        }

        public static string CrearAccesoDirectoEscritorio(
            string exePath, string nombreAcceso = "PCFORGE-ValdezThiago-96VA")
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
                Set("Description", "Sistema PCFORGE-ValdezThiago-96VA");
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
