using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Conexion
    {
        private static Conexion _Instancia;
        public string connectionString = "Server=.;DataBase=IngSoftValdezAlegre;Integrated Security=true";

        private Conexion()
        {

        }

        public static Conexion Instancia()
        {
            if (_Instancia == null)
            {
                _Instancia = new Conexion();
            }
            return _Instancia;
        }

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(connectionString);
        }
    }
}
