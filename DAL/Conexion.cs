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

        private const string CadenaPorDefecto =
            "Server=.;DataBase=IngSoftValdezAlegre;Integrated Security=true";
        private const string ArchivoConexion = "conexion.config";

        public string connectionString;

        private Conexion()
        {
            connectionString = ResolverCadena();
        }

        private static string ResolverCadena()
        {
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
            catch { }

            try
            {
                ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings["IngSoft"];
                if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
                    return cs.ConnectionString;
            }
            catch { }

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
