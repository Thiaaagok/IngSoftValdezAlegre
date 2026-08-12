using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos del proceso de Producción (RFN1): computadoras, órdenes y pagos.</summary>
    public class ProduccionDAL06AV
    {
        // ── Computadora ──────────────────────────────────────────
        public int AgregarComputadora(string nombre, int tipoConfiguracion, decimal precioTotal)
        {
            object id = EjecutarSPEscalar("sp_Computadoras_Agregar", new Dictionary<string, object>
            {
                { "@Nombre", (object)nombre ?? "" },
                { "@TipoConfiguracion", tipoConfiguracion },
                { "@PrecioTotal", precioTotal }
            });
            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        public void AgregarComponenteAComputadora(int idComputadora, string codigoComponente)
        {
            EjecutarSPNonQuery("sp_Computadoras_AgregarComponente", new Dictionary<string, object>
            {
                { "@IdComputadora", idComputadora }, { "@CodigoComponente", codigoComponente }
            });
        }

        public DataTable ObtenerComputadoraPorId(int id) =>
            EjecutarSP("sp_Computadoras_ObtenerPorId", new Dictionary<string, object> { { "@Id", id } });

        public DataTable ObtenerComponentesDeComputadora(int id) =>
            EjecutarSP("sp_Computadoras_ObtenerComponentes", new Dictionary<string, object> { { "@IdComputadora", id } });

        // ── Orden de producción ──────────────────────────────────
        public int AgregarOrden(string dniCliente, int idComputadora, DateTime fechaEntrega)
        {
            object num = EjecutarSPEscalar("sp_OP_Agregar", new Dictionary<string, object>
            {
                { "@DniCliente", dniCliente }, { "@IdComputadora", idComputadora },
                { "@FechaEntrega", fechaEntrega }
            });
        }

        public DataTable ObtenerOrdenes() => EjecutarSP("sp_OP_ObtenerTodas", null);

        // ── Recibo ───────────────────────────────────────────────
        public void AgregarRecibo(string id, string idPago, DateTime fechaEmision, decimal montoAbonado,
                                  decimal saldoPendiente, DateTime fechaEntregaEstimada)
        {
            EjecutarSPNonQuery("sp_Recibo_Agregar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@IdPago", idPago },
                { "@FechaEmision", fechaEmision },
                { "@MontoAbonado", montoAbonado },
                { "@SaldoPendiente", saldoPendiente },
                { "@FechaEntregaEstimada", fechaEntregaEstimada }
            });
        }

        public void PlanificarOrden(int numero, int idLinea, DateTime fechaInicio, string responsable)
        {
            var salida = new SqlParameter("@NumeroOrden", SqlDbType.Int) { Direction = ParameterDirection.Output };
            EjecutarSPConSalida("sp_OrdenesProduccion_Agregar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@IdComputadora", idComputadora },
                { "@FechaEntrega", fechaEntrega }
            }, salida);
            return salida.Value == null || salida.Value == DBNull.Value ? 0 : Convert.ToInt32(salida.Value);
        }

        public void CambiarEstadoOrden(int numero, int estado)
        {
            EjecutarSPNonQuery("sp_OrdenesProduccion_Planificar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@IdLinea", idLinea },
                { "@FechaInicio", fechaInicio },
                { "@Responsable", (object)responsable ?? "" }
            });
        }

        // ── Pagos ────────────────────────────────────────────────
        public void AgregarPago(int numeroOrden, int tipo, decimal monto)
        {
            EjecutarSPNonQuery("sp_Pagos_Agregar", new Dictionary<string, object>
            {
                { "@NumeroOrden", numeroOrden }, { "@Tipo", tipo }, { "@Monto", monto }
            });
        }

        public DataTable ObtenerPagosPorOrden(int numeroOrden) =>
            EjecutarSP("sp_Pagos_ObtenerPorOrden", new Dictionary<string, object> { { "@NumeroOrden", numeroOrden } });

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

        private DataSet EjecutarSPDataSet(string nombreSP, Dictionary<string, object> parametros)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null)
                foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            DataSet ds = new DataSet();
            conn.Open();
            new SqlDataAdapter(cmd).Fill(ds);
            conn.Close();
            return ds;
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

        private void EjecutarSPConSalida(string nombreSP, Dictionary<string, object> parametros, SqlParameter salida)
        {
            SqlConnection conn = Conexion.Instancia.ObtenerConexion();
            SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure };
            if (parametros != null)
                foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
            cmd.Parameters.Add(salida);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        #endregion
    }
}
