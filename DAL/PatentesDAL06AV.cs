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

        #endregion
    }
}