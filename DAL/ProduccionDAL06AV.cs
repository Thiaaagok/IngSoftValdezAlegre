using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Acceso a datos del proceso de Producción (RFN1). Tras el refactor:
    /// la Computadora existe de forma independiente (Id string generado por la app,
    /// asociada a un Cliente), los Pagos cuelgan de la Computadora (no de la orden),
    /// y la Orden de Producción referencia una Computadora existente. Incluye Recibo
    /// y Factura de Venta.
    /// </summary>
    public class ProduccionDAL06AV
    {
        // ── Computadora ──────────────────────────────────────────
        public void AgregarComputadora(string id, string dniCliente, string nombre,
                                       int tipoConfiguracion, int? idModeloOrigen, decimal precioTotal)
        {
            EjecutarSPNonQuery("sp_Computadoras_Agregar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@DniCliente", dniCliente },
                { "@Nombre", (object)nombre ?? "" },
                { "@TipoConfiguracion", tipoConfiguracion },
                { "@IdModeloOrigen", idModeloOrigen.HasValue ? (object)idModeloOrigen.Value : DBNull.Value },
                { "@PrecioTotal", precioTotal }
            });
        }

        public void AgregarComponenteAComputadora(string idComputadora, string codigoComponente, int orden)
        {
            EjecutarSPNonQuery("sp_Computadora_AgregarComponente", new Dictionary<string, object>
            {
                { "@IdComputadora", idComputadora },
                { "@CodigoComponente", codigoComponente },
                { "@Orden", orden }
            });
        }

        /// <summary>Devuelve un DataSet con dos tablas: [0] cabecera de la computadora, [1] sus componentes.</summary>
        public DataSet ObtenerComputadoraPorId(string id) =>
            EjecutarSPDataSet("sp_Computadoras_ObtenerPorId", new Dictionary<string, object> { { "@Id", id } });

        /// <summary>True si la computadora ya está asociada a alguna orden de producción.</summary>
        public bool ExisteOrdenPara(string idComputadora)
        {
            object r = EjecutarSPEscalar("sp_Computadora_ExisteOrden",
                new Dictionary<string, object> { { "@IdComputadora", idComputadora } });
            return r != null && r != DBNull.Value && Convert.ToBoolean(r);
        }

        // ── Pagos ────────────────────────────────────────────────
        public void AgregarPago(string id, string idComputadora, int tipo, decimal monto, DateTime fecha)
        {
            EjecutarSPNonQuery("sp_Pagos_Agregar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@IdComputadora", idComputadora },
                { "@Tipo", tipo },
                { "@Monto", monto },
                { "@Fecha", fecha }
            });
        }

        public DataTable ObtenerPagosPorComputadora(string idComputadora) =>
            EjecutarSP("sp_Pagos_ObtenerPorComputadora",
                new Dictionary<string, object> { { "@IdComputadora", idComputadora } });

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

        public DataTable ObtenerRecibos() => EjecutarSP("sp_Recibo_ObtenerTodos", null);

        // ── Orden de producción ──────────────────────────────────
        /// <summary>Inserta la OP con Id generado y devuelve el NumeroOrden de negocio (secuencia).</summary>
        public int AgregarOrden(string id, string idComputadora, DateTime fechaEntrega)
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

        public DataTable ObtenerOrdenes() => EjecutarSP("sp_OrdenesProduccion_ObtenerTodas", null);

        public DataTable ObtenerOrdenPorId(string id) =>
            EjecutarSP("sp_OrdenesProduccion_ObtenerPorId", new Dictionary<string, object> { { "@Id", id } });

        public void PlanificarOrden(string id, int idLinea, DateTime fechaInicio, string responsable)
        {
            EjecutarSPNonQuery("sp_OrdenesProduccion_Planificar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@IdLinea", idLinea },
                { "@FechaInicio", fechaInicio },
                { "@Responsable", (object)responsable ?? "" }
            });
        }

        public void CambiarEstadoOrden(string id, int estado) =>
            EjecutarSPNonQuery("sp_OrdenesProduccion_CambiarEstado", new Dictionary<string, object>
            { { "@Id", id }, { "@Estado", estado } });

        public void CerrarOrden(string id, DateTime fechaCierre) =>
            EjecutarSPNonQuery("sp_OrdenesProduccion_Cerrar", new Dictionary<string, object>
            { { "@Id", id }, { "@FechaCierre", fechaCierre } });

        // ── Factura de venta ─────────────────────────────────────
        public void AgregarFacturaVenta(string numeroFactura, string idOrdenProduccion,
                                        DateTime fechaEmision, decimal total)
        {
            EjecutarSPNonQuery("sp_FacturaVenta_Agregar", new Dictionary<string, object>
            {
                { "@NumeroFactura", numeroFactura },
                { "@IdOrdenProduccion", idOrdenProduccion },
                { "@FechaEmision", fechaEmision },
                { "@Total", total }
            });
        }

        public DataTable ObtenerFacturasVenta() => EjecutarSP("sp_FacturaVenta_ObtenerTodas", null);

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
