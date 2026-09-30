using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    public class ReportesDAL06AV
    {
        public DataTable ObtenerVentasProduccion(DateTime desde, DateTime hasta,
                                                 int? estado, int? tipoConfiguracion, string cliente)
        {
            return EjecutarSP("sp_Reporte_VentasProduccion", new Dictionary<string, object>
            {
                { "@Desde", desde.Date },
                { "@Hasta", hasta.Date },
                { "@Estado", estado.HasValue ? (object)estado.Value : DBNull.Value },
                { "@TipoConfiguracion", tipoConfiguracion.HasValue ? (object)tipoConfiguracion.Value : DBNull.Value },
                { "@Cliente", string.IsNullOrWhiteSpace(cliente) ? (object)DBNull.Value : cliente.Trim() }
            });
        }

        #region Helpers

        private DataTable EjecutarSP(string nombreSP, Dictionary<string, object> parametros)
        {
            using (SqlConnection conn = Conexion.Instancia.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(nombreSP, conn) { CommandType = CommandType.StoredProcedure })
            {
                if (parametros != null)
                    foreach (var p in parametros) cmd.Parameters.AddWithValue(p.Key, p.Value);
                DataTable tabla = new DataTable();
                conn.Open();
                using (var da = new SqlDataAdapter(cmd)) da.Fill(tabla);
                return tabla;
            }
        }

        #endregion
    }
}
