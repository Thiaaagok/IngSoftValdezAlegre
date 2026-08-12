using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
    /// <summary>
    /// Acceso a datos de Componentes (PC Forge). Tras el refactor, Componente absorbe
    /// a Insumo: es la pieza comprable y stockeable (Stock / StockMinimo).
    /// </summary>
    public class ComponentesDAL06AV
    {
        public DataTable ObtenerTodos() => EjecutarSP("sp_Componentes_ObtenerTodos", null);

        public DataTable ObtenerBajoStock() => EjecutarSP("sp_Componentes_ObtenerBajoStock", null);

        public DataTable ObtenerPorCodigo(string codigo)
        {
            return EjecutarSP("sp_Componentes_ObtenerPorCodigo", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        public void Agregar(string codigo, string descripcion, string marca, string modelo,
                            decimal precioUnitario, int stock, int stockMinimo, int tipo)
        {
            EjecutarSPNonQuery("sp_Componentes_Agregar", Parametros(
                codigo, descripcion, marca, modelo, precioUnitario, stock, stockMinimo, tipo));
        }

        public void Modificar(string codigo, string descripcion, string marca, string modelo,
                              decimal precioUnitario, int stock, int stockMinimo, int tipo)
        {
            EjecutarSPNonQuery("sp_Componentes_Modificar", Parametros(
                codigo, descripcion, marca, modelo, precioUnitario, stock, stockMinimo, tipo));
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

        private static Dictionary<string, object> Parametros(string codigo, string descripcion,
            string marca, string modelo, decimal precioUnitario, int stock, int stockMinimo, int tipo)
        {
            return new Dictionary<string, object>
            {
                { "@Codigo",         codigo               },
                { "@Descripcion",    descripcion          },
                { "@Marca",          (object)marca  ?? "" },
                { "@Modelo",         (object)modelo ?? "" },
                { "@PrecioUnitario", precioUnitario       },
                { "@Stock",          stock                },
                { "@StockMinimo",    stockMinimo          },
                { "@Tipo",           tipo                 }
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
