using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Acceso a datos de la VENTA (RFN1 - CU01/CU03/CU07): la computadora solicitada,
    /// la venta en sí y los pagos (seña y saldo final).
    /// La orden de producción vive en <see cref="ProduccionDAL06AV"/>.
    /// </summary>
    public class VentasDAL06AV
    {
        // ── Computadora solicitada ───────────────────────────────
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

        public void AgregarComponenteAComputadora(int idComputadora, string codigoComponente, int cantidad)
        {
            EjecutarSPNonQuery("sp_Computadoras_AgregarComponente", new Dictionary<string, object>
            {
                { "@IdComputadora", idComputadora },
                { "@CodigoComponente", codigoComponente },
                { "@Cantidad", cantidad }
            });
        }

        public DataTable ObtenerComputadoraPorId(int id) =>
            EjecutarSP("sp_Computadoras_ObtenerPorId", new Dictionary<string, object> { { "@Id", id } });

        public DataTable ObtenerComponentesDeComputadora(int id) =>
            EjecutarSP("sp_Computadoras_ObtenerComponentes", new Dictionary<string, object> { { "@IdComputadora", id } });

        // ── Venta ────────────────────────────────────────────────
        public int AgregarVenta(string dniCliente, int idComputadora, DateTime fechaEntregaEstimada, string usuario)
        {
            object num = EjecutarSPEscalar("sp_Ventas_Agregar", new Dictionary<string, object>
            {
                { "@DniCliente", dniCliente },
                { "@IdComputadora", idComputadora },
                { "@FechaEntregaEstimada", fechaEntregaEstimada },
                { "@UsuarioRegistro", (object)usuario ?? DBNull.Value }
            });
            return num == null || num == DBNull.Value ? 0 : Convert.ToInt32(num);
        }

        public DataTable ObtenerVentas() => EjecutarSP("sp_Ventas_ObtenerTodas", null);

        public DataTable ObtenerVentaPorNumero(int numero) =>
            EjecutarSP("sp_Ventas_ObtenerPorNumero", new Dictionary<string, object> { { "@NumeroVenta", numero } });

        /// <summary>CU04: ventas señadas que todavía no tienen orden de producción.</summary>
        public DataTable ObtenerVentasParaProduccion() => EjecutarSP("sp_Ventas_ObtenerParaProduccion", null);

        public void CambiarEstadoVenta(int numero, int estado)
        {
            EjecutarSPNonQuery("sp_Ventas_CambiarEstado", new Dictionary<string, object>
            {
                { "@NumeroVenta", numero }, { "@Estado", estado }
            });
        }

        // ── Pagos ────────────────────────────────────────────────
        /// <summary>Registra el pago y devuelve la fila resultante (incluye el Nº de comprobante generado).</summary>
        public DataTable AgregarPago(int numeroVenta, int tipo, decimal monto, int formaPago,
                                     string referencia, string usuario)
        {
            return EjecutarSP("sp_Pagos_Agregar", new Dictionary<string, object>
            {
                { "@NumeroVenta", numeroVenta },
                { "@Tipo", tipo },
                { "@Monto", monto },
                { "@FormaPago", formaPago },
                { "@Referencia", (object)referencia ?? DBNull.Value },
                { "@Usuario", (object)usuario ?? DBNull.Value }
            });
        }

        public DataTable ObtenerPagosPorVenta(int numeroVenta) =>
            EjecutarSP("sp_Pagos_ObtenerPorVenta", new Dictionary<string, object> { { "@NumeroVenta", numeroVenta } });

        public DataTable ObtenerPagos() => EjecutarSP("sp_Pagos_ObtenerTodos", null);

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
