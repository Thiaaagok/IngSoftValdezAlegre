using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de Líneas de Ensamblaje (PC Factory). Id autonumérico.</summary>
    public class LineasEnsamblajeDAL06AV
    {
        public DataTable ObtenerTodas()
        {
            return EjecutarSP("sp_Lineas_ObtenerTodas", null);
        }

        public DataTable ObtenerPorId(int id)
        {
            return EjecutarSP("sp_Lineas_ObtenerPorId", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        public int Agregar(string nombre, string descripcion, bool disponible)
        {
            object id = EjecutarSPEscalar("sp_Lineas_Agregar", new Dictionary<string, object>
            {
                { "@Nombre", nombre }, { "@Descripcion", (object)descripcion ?? "" },
                { "@Disponible", disponible }
            });
            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        public void Modificar(int id, string nombre, string descripcion, bool disponible)
        {
            EjecutarSPNonQuery("sp_Lineas_Modificar", new Dictionary<string, object>
            {
                { "@Id", id }, { "@Nombre", nombre },
                { "@Descripcion", (object)descripcion ?? "" }, { "@Disponible", disponible }
            });
        }

        public void Eliminar(int id)
        {
            EjecutarSPNonQuery("sp_Lineas_Eliminar", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        #region Helpers

        private DataTable EjecutarSP(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null)
                foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataTable tabla = new DataTable();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(tabla);
            conn.Close();
            return tabla;
        }

        private object EjecutarSPEscalar(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null)
                foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open();
            object r = cmd.ExecuteScalar();
            conn.Close();
            return r;
        }

        private void EjecutarSPNonQuery(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null)
                foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        #endregion
    }
}
