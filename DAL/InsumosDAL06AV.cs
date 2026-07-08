using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de Insumos (PC Factory).</summary>
    public class InsumosDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Insumos_ObtenerTodos", null);
        }

        public DataTable ObtenerBajoStock()
        {
            return EjecutarSP("sp_Insumos_ObtenerBajoStock", null);
        }

        public DataTable ObtenerPorCodigo(string codigo)
        {
            return EjecutarSP("sp_Insumos_ObtenerPorCodigo", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        public void Agregar(string codigo, string descripcion, int stock, int stockMinimo)
        {
            EjecutarSPNonQuery("sp_Insumos_Agregar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }, { "@Descripcion", descripcion },
                { "@Stock", stock }, { "@StockMinimo", stockMinimo }
            });
        }

        public void Modificar(string codigo, string descripcion, int stock, int stockMinimo)
        {
            EjecutarSPNonQuery("sp_Insumos_Modificar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }, { "@Descripcion", descripcion },
                { "@Stock", stock }, { "@StockMinimo", stockMinimo }
            });
        }

        public void Eliminar(string codigo)
        {
            EjecutarSPNonQuery("sp_Insumos_Eliminar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
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
