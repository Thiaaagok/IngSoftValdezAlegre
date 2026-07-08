using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Conexion
    {
        private static Conexion _Instancia;

        /// <summary>Cadena por defecto si el archivo de configuración no la define.</summary>
        private const string CadenaPorDefecto =
            "Server=.;DataBase=IngSoftValdezAlegre;Integrated Security=true";

        /// <summary>
        /// Cadena de conexión. Se toma de la sección &lt;connectionStrings&gt; del archivo
        /// de configuración (nombre "IngSoft") — que es la que escribe el Instalador con
        /// la instancia elegida. Si no está definida, usa el valor por defecto (Server=.),
        /// preservando el comportamiento anterior.
        /// </summary>
        public string connectionString;

        private Conexion()
        {
            try
            {
                ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings["IngSoft"];
                connectionString = (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
                    ? cs.ConnectionString
                    : CadenaPorDefecto;
            }
            catch
            {
                connectionString = CadenaPorDefecto;
            }
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
