using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de Proveedores (PC Factory). Id autonumérico.</summary>
    public class ProveedoresDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Proveedores_ObtenerTodos", null);
        }

        public DataTable ObtenerPorId(int id)
        {
            return EjecutarSP("sp_Proveedores_ObtenerPorId", new Dictionary<string, object>
            {
                { "@Id", id }
            });
        }

        public DataTable ObtenerPorCuit(string cuit)
        {
            return EjecutarSP("sp_Proveedores_ObtenerPorCuit", new Dictionary<string, object>
            {
                { "@Cuit", cuit }
            });
        }

        /// <summary>Inserta y devuelve el Id generado.</summary>
        public int Agregar(string nombre, string cuit, string email, string telefono, string direccion)
        {
            object id = EjecutarSPEscalar("sp_Proveedores_Agregar", new Dictionary<string, object>
            {
                { "@Nombre", nombre }, { "@Cuit", cuit },
                { "@Email", (object)email ?? "" }, { "@Telefono", (object)telefono ?? "" },
                { "@Direccion", (object)direccion ?? "" }
            });
            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        public void Modificar(int id, string nombre, string cuit, string email, string telefono, string direccion)
        {
            EjecutarSPNonQuery("sp_Proveedores_Modificar", new Dictionary<string, object>
            {
                { "@Id", id }, { "@Nombre", nombre }, { "@Cuit", cuit },
                { "@Email", (object)email ?? "" }, { "@Telefono", (object)telefono ?? "" },
                { "@Direccion", (object)direccion ?? "" }
            });
        }

        public void Eliminar(int id)
        {
            EjecutarSPNonQuery("sp_Proveedores_Eliminar", new Dictionary<string, object>
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
