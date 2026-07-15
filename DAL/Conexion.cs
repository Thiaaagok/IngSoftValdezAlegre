using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Conexion
    {
        private static Conexion _Instancia;

        /// <summary>Cadena por defecto si nada más define la conexión.</summary>
        private const string CadenaPorDefecto =
            "Server=.;DataBase=IngSoftValdezAlegre;Integrated Security=true";

        /// <summary>Archivo externo (junto al ejecutable) con la cadena de conexión.</summary>
        private const string ArchivoConexion = "conexion.config";

        /// <summary>
        /// Cadena de conexión. Orden de prioridad:
        ///   1) Archivo externo "conexion.config" junto al .exe. Lo escribe el Instalador
        ///      con la instancia/base elegida y — a diferencia del .exe.config — NO lo
        ///      regenera Visual Studio al recompilar, así que la elección SIEMPRE persiste.
        ///   2) Sección &lt;connectionStrings&gt; del .config (nombre "IngSoft").
        ///   3) Valor por defecto (Server=.), preservando el comportamiento anterior.
        /// </summary>
        public string connectionString;

        private Conexion()
        {
            connectionString = ResolverCadena();
        }

        private static string ResolverCadena()
        {
            // 1) Archivo externo junto al ejecutable (fuente de verdad del Instalador).
            try
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ArchivoConexion);
                if (File.Exists(ruta))
                {
                    string txt = File.ReadAllText(ruta).Trim();
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
            }
            catch { /* si no se puede leer, se intenta la siguiente fuente */ }

            // 2) connectionStrings del .config (nombre "IngSoft").
            try
            {
                ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings["IngSoft"];
                if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
                    return cs.ConnectionString;
            }
            catch { /* idem */ }

            // 3) Valor por defecto.
            return CadenaPorDefecto;
        }

        public static Conexion Instancia
        {
            get
            {
                if (_Instancia == null)
                {
                    _Instancia = new Conexion();
                }
                return _Instancia;
            }
        }

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(connectionString);
        }
    }
}
