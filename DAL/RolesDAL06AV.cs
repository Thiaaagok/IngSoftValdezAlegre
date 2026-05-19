using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class RolesDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            string query = "SELECT * FROM Roles";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPorId(Dictionary<string, object> parametros)
        {
            string query = "SELECT * FROM Roles WHERE Id = @id";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();

            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
            {
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            }

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }

        public DataTable ObtenerPermisosPorRol(Dictionary<string, object> parametros)
        {
            string query = "SELECT Permiso FROM RolesPermisos WHERE IdRol = @id";

            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(query, conn);

            foreach (var p in parametros)
                cmd.Parameters.AddWithValue(p.Key, p.Value);

            DataTable tabla = new DataTable();

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(tabla);
            conn.Close();

            return tabla;
        }
    }
}
