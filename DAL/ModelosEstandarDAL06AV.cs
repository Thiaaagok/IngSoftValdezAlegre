using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos del catálogo de modelos estándar (PC Factory / RFN1).</summary>
    public class ModelosEstandarDAL06AV
    {
        public DataTable ObtenerTodos() => EjecutarSP("sp_ModeloEstandar_ObtenerTodos", null);

        public DataTable ObtenerComponentes(int idModelo) =>
            EjecutarSP("sp_ModeloEstandar_ObtenerComponentes",
                new Dictionary<string, object> { { "@IdModelo", idModelo } });

        public int Agregar(string nombre, string descripcion)
        {
            object id = EjecutarSPEscalar("sp_ModeloEstandar_Agregar", new Dictionary<string, object>
            {
                { "@Nombre", nombre }, { "@Descripcion", (object)descripcion ?? "" }
            });
            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        public void Modificar(int id, string nombre, string descripcion) =>
            EjecutarSPNonQuery("sp_ModeloEstandar_Modificar", new Dictionary<string, object>
            {
                { "@Id", id }, { "@Nombre", nombre }, { "@Descripcion", (object)descripcion ?? "" }
            });

        public void Eliminar(int id) =>
            EjecutarSPNonQuery("sp_ModeloEstandar_Eliminar",
                new Dictionary<string, object> { { "@Id", id } });

        public void AgregarComponente(int idModelo, string codigo) =>
            EjecutarSPNonQuery("sp_ModeloEstandar_AgregarComponente", new Dictionary<string, object>
            {
                { "@IdModelo", idModelo }, { "@CodigoComponente", codigo }
            });

        public void QuitarComponentes(int idModelo) =>
            EjecutarSPNonQuery("sp_ModeloEstandar_QuitarComponentes",
                new Dictionary<string, object> { { "@IdModelo", idModelo } });

        #region Helpers

        private DataTable EjecutarSP(string sp, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataTable t = new DataTable();
            conn.Open(); new SqlDataAdapter(cmd).Fill(t); conn.Close();
            return t;
        }

        private void EjecutarSPNonQuery(string sp, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open(); cmd.ExecuteNonQuery(); conn.Close();
        }

        private object EjecutarSPEscalar(string sp, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null) foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            conn.Open(); object r = cmd.ExecuteScalar(); conn.Close();
            return r;
        }

        #endregion
    }
}
