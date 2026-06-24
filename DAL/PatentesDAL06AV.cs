using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class PatentesDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Patentes_ObtenerTodos", null);
        }

        public DataTable ObtenerPorId(string id)
        {
            return EjecutarSP("sp_Patentes_ObtenerPorId", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        public DataTable ObtenerPatentesPorRol(string idRol)
        {
            return EjecutarSP("sp_ObtenerPatentesUsuario", new Dictionary<string, object>
            {
                { "@IdRol", idRol }
            });
        }

        public void Agregar(string id, string descripcion)
        {
            EjecutarSPNonQuery("sp_Patentes_Agregar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion }
            });
        }

        public void Modificar(string id, string descripcion)
        {
            EjecutarSPNonQuery("sp_Patentes_Modificar", new Dictionary<string, object>
            {
                { "@Id",          id          },
                { "@Descripcion", descripcion }
            });
        }

        public void Eliminar(string id)
        {
            EjecutarSPNonQuery("sp_Patentes_Eliminar", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        #region Helpers

        private DataTable EjecutarSP(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parametros != null)
                foreach (var p in parametros)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataTable tabla = new DataTable();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(tabla);
            conn.Close();
            return tabla;
        }

        private void EjecutarSPNonQuery(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parametros != null)
                foreach (var p in parametros)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        #endregion
    }
}