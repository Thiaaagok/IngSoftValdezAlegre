using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>Acceso a datos de Componentes (PC Factory).</summary>
    public class ComponentesDAL06AV
    {
        public DataTable ObtenerTodos()
        {
            return EjecutarSP("sp_Componentes_ObtenerTodos", null);
        }

        public DataTable ObtenerPorCodigo(string codigo)
        {
            return EjecutarSP("sp_Componentes_ObtenerPorCodigo", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        /// <summary>RFN2: componentes que llegaron al mínimo y hay que reponer.</summary>
        public DataTable ObtenerBajoStock()
        {
            return EjecutarSP("sp_Componentes_ObtenerBajoStock", null);
        }

        public void Agregar(string codigo, string descripcion, int tipo, string marca,
                            string modelo, decimal precioUnitario, int stock, int stockMinimo)
        {
            EjecutarSPNonQuery("sp_Componentes_Agregar", Parametros(
                codigo, descripcion, tipo, marca, modelo, precioUnitario, stock, stockMinimo));
        }

        public void Modificar(string codigo, string descripcion, int tipo, string marca,
                              string modelo, decimal precioUnitario, int stock, int stockMinimo)
        {
            EjecutarSPNonQuery("sp_Componentes_Modificar", Parametros(
                codigo, descripcion, tipo, marca, modelo, precioUnitario, stock, stockMinimo));
        }

        /// <summary>RFN2: suma al stock lo efectivamente recibido del proveedor.</summary>
        public void SumarStock(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_SumarStock", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        public void Eliminar(string codigo)
        {
            EjecutarSPNonQuery("sp_Componentes_Eliminar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        /// <summary>Descuenta stock de un componente (al usarlo en una orden). Atómico en el SP.</summary>
        public void DescontarStock(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_DescontarStock", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        /// <summary>CU01: reserva unidades al registrar la venta. Falla si no hay stock libre.</summary>
        public void ReservarStock(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_ReservarStock", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        /// <summary>Devuelve unidades reservadas al stock libre (venta anulada, orden vuelta atrás).</summary>
        public void LiberarReserva(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_LiberarReserva", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        /// <summary>CU06: descuenta el stock físico y libera la reserva al cerrar la orden.</summary>
        public void ConsumirReserva(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_ConsumirReserva", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        private static Dictionary<string, object> Parametros(string codigo, string descripcion,
            int tipo, string marca, string modelo, decimal precioUnitario, int stock, int stockMinimo)
        {
            return new Dictionary<string, object>
            {
                { "@StockMinimo",     stockMinimo          },
                { "@Codigo",          codigo               },
                { "@Descripcion",     descripcion          },
                { "@Tipo",            tipo                 },
                { "@Marca",           (object)marca  ?? "" },
                { "@Modelo",          (object)modelo ?? "" },
                { "@PrecioUnitario",  precioUnitario       },
                { "@Stock", stock      }
            };
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
