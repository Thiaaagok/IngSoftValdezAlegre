using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Acceso a datos de la ORDEN DE PRODUCCIÓN (RFN1 - CU04 a CU07).
    /// La orden apunta a una venta ya señada; los datos comerciales
    /// (cliente, computadora, pagos) se leen desde <see cref="VentasDAL06AV"/>.
    /// </summary>
    public class ProduccionDAL06AV
    {
        // ── Alta (CU04) ──────────────────────────────────────────
        public int AgregarOrden(int numeroVenta, DateTime fechaEntregaEstimada)
        {
            object num = EjecutarSPEscalar("sp_OP_Agregar", new Dictionary<string, object>
            {
                { "@NumeroVenta", numeroVenta },
                { "@FechaEntregaEstimada", fechaEntregaEstimada }
            });
            return num == null || num == DBNull.Value ? 0 : Convert.ToInt32(num);
        }

        // ── Lectura ──────────────────────────────────────────────
        public DataTable ObtenerOrdenes() => EjecutarSP("sp_OP_ObtenerTodas", null);

        public DataTable ObtenerOrdenPorNumero(int numero) =>
            EjecutarSP("sp_OP_ObtenerPorNumero", new Dictionary<string, object> { { "@NumeroOrden", numero } });

        /// <summary>CU07: órdenes en un estado dado (Finalizadas / Entregadas).</summary>
        public DataTable ObtenerOrdenesPorEstado(int estado) =>
            EjecutarSP("sp_OP_ObtenerPorEstado", new Dictionary<string, object> { { "@Estado", estado } });

        public DataTable ObtenerOrdenPorVenta(int numeroVenta) =>
            EjecutarSP("sp_OP_ObtenerPorVenta", new Dictionary<string, object> { { "@NumeroVenta", numeroVenta } });

        // ── Planificación (CU05) ─────────────────────────────────
        public void PlanificarOrden(int numero, int idLinea, DateTime fechaInicio, string responsable)
        {
            EjecutarSPNonQuery("sp_OP_Planificar", new Dictionary<string, object>
            {
                { "@NumeroOrden", numero }, { "@IdLinea", idLinea },
                { "@FechaInicioPrevista", fechaInicio }, { "@ResponsableTecnico", (object)responsable ?? "" }
            });
        }

        public void DesplanificarOrden(int numero) =>
            EjecutarSPNonQuery("sp_OP_Desplanificar", new Dictionary<string, object> { { "@NumeroOrden", numero } });

        public void CambiarEstadoOrden(int numero, int estado)
        {
            EjecutarSPNonQuery("sp_OP_CambiarEstado", new Dictionary<string, object>
            {
                { "@NumeroOrden", numero }, { "@Estado", estado }
            });
        }

        // ── Cierre de producción (CU06) ──────────────────────────
        public void RegistrarControlCalidad(int numero, bool encendido, bool conexiones,
                                            bool sistemaOperativo, bool drivers,
                                            string observaciones, string responsable)
        {
            EjecutarSPNonQuery("sp_OP_RegistrarControlCalidad", new Dictionary<string, object>
            {
                { "@NumeroOrden", numero },
                { "@Encendido", encendido },
                { "@Conexiones", conexiones },
                { "@SistemaOperativo", sistemaOperativo },
                { "@Drivers", drivers },
                { "@Observaciones", (object)observaciones ?? DBNull.Value },
                { "@Responsable", (object)responsable ?? DBNull.Value }
            });
        }

        public void CerrarOrden(int numero, string numeroSerie)
        {
            EjecutarSPNonQuery("sp_OP_Cerrar", new Dictionary<string, object>
            {
                { "@NumeroOrden", numero }, { "@NumeroSerie", (object)numeroSerie ?? "" }
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
