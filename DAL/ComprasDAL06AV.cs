using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class ComprasDAL06AV
    {
        public int AgregarOrdenCompra(string id, DateTime fechaLimite, string dniRepositor)
        {
            var salida = new SqlParameter("@NumeroCompra", SqlDbType.Int) { Direction = ParameterDirection.Output };
            EjecutarSPConSalida("sp_OrdenesCompra_Agregar", new Dictionary<string, object>
            {
                { "@Id", id },
                { "@FechaLimite", fechaLimite },
                { "@DniRepositor", (object)dniRepositor ?? DBNull.Value }
            }, salida);
            return salida.Value == null || salida.Value == DBNull.Value ? 0 : Convert.ToInt32(salida.Value);
        }

        public void AgregarDetalle(string idOrdenCompra, string codigoComponente, int cantidad)
        {
            EjecutarSPNonQuery("sp_OrdenCompra_AgregarDetalle", new Dictionary<string, object>
            {
                { "@IdOrdenCompra", idOrdenCompra },
                { "@CodigoComponente", codigoComponente },
                { "@Cantidad", cantidad }
            });
        }

        public DataTable ObtenerOrdenesCompra() => EjecutarSP("sp_OrdenesCompra_ObtenerTodas", null);

        public DataTable ObtenerDetalle(string idOrdenCompra) =>
            EjecutarSP("sp_OrdenCompra_ObtenerDetalle",
                new Dictionary<string, object> { { "@IdOrdenCompra", idOrdenCompra } });

        public void CambiarEstadoOrdenCompra(string id, int estado) =>
            EjecutarSPNonQuery("sp_OrdenesCompra_CambiarEstado", new Dictionary<string, object>
            { { "@Id", id }, { "@Estado", estado } });

        public void CerrarOrdenCompra(string id, DateTime fechaCierre) =>
            EjecutarSPNonQuery("sp_OrdenesCompra_Cerrar", new Dictionary<string, object>
            { { "@Id", id }, { "@FechaCierre", fechaCierre } });

        public void AgregarCotizacion(string numero, string idOrdenCompra, int idProveedor,
                                      decimal costo, string condiciones)
        {
            EjecutarSPNonQuery("sp_Cotizacion_Agregar", new Dictionary<string, object>
            {
                { "@Numero", numero },
                { "@IdOrdenCompra", idOrdenCompra },
                { "@IdProveedor", idProveedor },
                { "@Costo", costo },
                { "@Condiciones", (object)condiciones ?? "" }
            });
        }

        public void AgregarCotizacionConDetalle(string numero, string idOrdenCompra, int idProveedor,
                                               string condiciones, string detalleXml)
        {
            EjecutarSPNonQuery("sp_Cotizacion_AgregarConDetalle", new Dictionary<string, object>
            {
                { "@Numero", numero },
                { "@IdOrdenCompra", idOrdenCompra },
                { "@IdProveedor", idProveedor },
                { "@Condiciones", (object)condiciones ?? "" },
                { "@Detalle", detalleXml }
            });
        }

        public DataTable ObtenerDetalleCotizacion(string numero) =>
            EjecutarSP("sp_Cotizacion_ObtenerDetalle", new Dictionary<string, object> { { "@Numero", numero } });

        public DataTable ObtenerCotizaciones() => EjecutarSP("sp_Cotizacion_ObtenerTodas", null);

        public DataTable ObtenerCotizacionesPorOrden(string idOrdenCompra) =>
            EjecutarSP("sp_Cotizacion_ObtenerPorOrden",
                new Dictionary<string, object> { { "@IdOrdenCompra", idOrdenCompra } });

        public void CambiarEstadoCotizacion(string numero, int estado, string dniGerenteAprobador) =>
            EjecutarSPNonQuery("sp_Cotizacion_CambiarEstado", new Dictionary<string, object>
            {
                { "@Numero", numero },
                { "@Estado", estado },
                { "@DniGerenteAprobador", (object)dniGerenteAprobador ?? DBNull.Value }
            });

        public void AgregarFacturaCompra(string numeroFactura, string idOrdenCompra, DateTime fechaEmision,
                                         DateTime fechaEntrega, decimal total, string observaciones)
        {
            EjecutarSPNonQuery("sp_FacturaCompra_Agregar", new Dictionary<string, object>
            {
                { "@NumeroFactura", numeroFactura },
                { "@IdOrdenCompra", idOrdenCompra },
                { "@FechaEmision", fechaEmision },
                { "@FechaEntrega", fechaEntrega },
                { "@Total", total },
                { "@Observaciones", (object)observaciones ?? "" }
            });
        }

        public DataTable ObtenerFacturasPorOrden(string idOrdenCompra) =>
            EjecutarSP("sp_FacturaCompra_ObtenerPorOrden", new Dictionary<string, object>
            {
                { "@IdOrdenCompra", idOrdenCompra }
            });

        public DataTable ObtenerFacturaCompraDetalle(string numeroFactura) =>
            EjecutarSP("sp_FacturaCompra_ObtenerDetalle", new Dictionary<string, object>
            {
                { "@NumeroFactura", numeroFactura }
            });

        public void AgregarFacturaCompraDetalle(string numeroFactura, string codigoComponente, int cantidad)
        {
            EjecutarSPNonQuery("sp_FacturaCompra_AgregarDetalle", new Dictionary<string, object>
            {
                { "@NumeroFactura", numeroFactura },
                { "@CodigoComponente", codigoComponente },
                { "@Cantidad", cantidad }
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
