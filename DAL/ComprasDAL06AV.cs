using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos del proceso de Compras (RFN2): órdenes de compra y cotizaciones.</summary>
    public class ComprasDAL06AV
    {
        // ── Orden de compra ──────────────────────────────────────
        public int AgregarOrdenCompra(DateTime fechaLimite, string repositor)
        {
            object num = EjecutarSPEscalar("sp_OC_Agregar", new Dictionary<string, object>
            {
                { "@FechaLimite", fechaLimite }, { "@RepositorSolicitante", (object)repositor ?? "" }
            });
            return num == null || num == DBNull.Value ? 0 : Convert.ToInt32(num);
        }

        public void AgregarDetalle(int numeroCompra, string codigoInsumo, int cantidad)
        {
            EjecutarSPNonQuery("sp_OC_AgregarDetalle", new Dictionary<string, object>
            {
                { "@NumeroCompra", numeroCompra }, { "@CodigoInsumo", codigoInsumo }, { "@Cantidad", cantidad }
            });
        }

        public DataTable ObtenerOrdenesCompra() => EjecutarSP("sp_OC_ObtenerTodas", null);

        public DataTable ObtenerOrdenCompraPorNumero(int numero) =>
            EjecutarSP("sp_OC_ObtenerPorNumero", new Dictionary<string, object> { { "@NumeroCompra", numero } });

        public DataTable ObtenerDetalle(int numeroCompra) =>
            EjecutarSP("sp_OC_ObtenerDetalle", new Dictionary<string, object> { { "@NumeroCompra", numeroCompra } });

        public void CambiarEstadoOrdenCompra(int numero, int estado) =>
            EjecutarSPNonQuery("sp_OC_CambiarEstado", new Dictionary<string, object>
            { { "@NumeroCompra", numero }, { "@Estado", estado } });

        public void FinalizarOrdenCompra(int numero) =>
            EjecutarSPNonQuery("sp_OC_Finalizar", new Dictionary<string, object> { { "@NumeroCompra", numero } });

        // ── Cotización ───────────────────────────────────────────
        public int AgregarCotizacion(int numeroCompra, int idProveedor, decimal costo, string condiciones)
        {
            object num = EjecutarSPEscalar("sp_Cotizacion_Agregar", new Dictionary<string, object>
            {
                { "@NumeroCompra", numeroCompra }, { "@IdProveedor", idProveedor },
                { "@Costo", costo }, { "@Condiciones", (object)condiciones ?? "" }
            });
            return num == null || num == DBNull.Value ? 0 : Convert.ToInt32(num);
        }

        public DataTable ObtenerCotizaciones() => EjecutarSP("sp_Cotizacion_ObtenerTodas", null);

        public DataTable ObtenerCotizacionPorNumero(int numero) =>
            EjecutarSP("sp_Cotizacion_ObtenerPorNumero", new Dictionary<string, object> { { "@Numero", numero } });

        public void CambiarEstadoCotizacion(int numero, int estado) =>
            EjecutarSPNonQuery("sp_Cotizacion_CambiarEstado", new Dictionary<string, object>
            { { "@Numero", numero }, { "@Estado", estado } });

        // ── Stock ────────────────────────────────────────────────
        public void SumarStock(string codigoInsumo, int cantidad) =>
            EjecutarSPNonQuery("sp_Insumos_SumarStock", new Dictionary<string, object>
            { { "@Codigo", codigoInsumo }, { "@Cantidad", cantidad } });

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
