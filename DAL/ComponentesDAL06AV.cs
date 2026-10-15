using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DAL
{
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

        public void SumarStock(string codigo, int cantidad)
        {
            EjecutarSPNonQuery("sp_Componentes_SumarStock", new Dictionary<string, object>
            {
                { "@Codigo",   codigo   },
                { "@Cantidad", cantidad }
            });
        }

        public void BajaLogica(string codigo)
        {
            EjecutarSPNonQuery("sp_Componentes_BajaLogica", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        public void Reactivar(string codigo)
        {
            EjecutarSPNonQuery("sp_Componentes_Reactivar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        public void Eliminar(string codigo)
        {
            EjecutarSPNonQuery("sp_Componentes_Eliminar", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        #region Bitácora de cambios (Componentes_C)

        public DataTable ObtenerBitacora(string codigo, string descripcion,
                                         DateTime? fechaIni, DateTime? fechaFin)
        {
            return EjecutarSP("sp_ComponentesC_Listar", new Dictionary<string, object>
            {
                { "@Codigo",      string.IsNullOrWhiteSpace(codigo)      ? (object)DBNull.Value : codigo.Trim()      },
                { "@Descripcion", string.IsNullOrWhiteSpace(descripcion) ? (object)DBNull.Value : descripcion.Trim() },
                { "@FechaIni",    fechaIni.HasValue ? (object)fechaIni.Value.Date : DBNull.Value },
                { "@FechaFin",    fechaFin.HasValue ? (object)fechaFin.Value.Date : DBNull.Value }
            });
        }

        public void ActivarHistorico(int idHistorico)
        {
            EjecutarSPNonQuery("sp_ComponentesC_Activar", new Dictionary<string, object>
            {
                { "@IdHistorico", idHistorico }
            });
        }

        #endregion

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

        /// <summary>Devuelve unidades reservadas al stock libre (venta anulada o compensación de una venta que no se grabó).</summary>
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

        public DataTable ObtenerEspecificaciones()
        {
            return EjecutarSP("sp_Especificaciones_ObtenerTodas", null);
        }

        public DataTable ObtenerEspecificacion(string codigo)
        {
            return EjecutarSP("sp_Especificaciones_ObtenerPorCodigo", new Dictionary<string, object>
            {
                { "@Codigo", codigo }
            });
        }

        public void GuardarEspecificacion(string codigo, string plataforma, string socket, string tipoMemoria,
            int? frecuenciaMemoriaMhz, int? capacidadGb, int? slotsMemoria, int? memoriaMaximaGb, string formato,
            int? consumoW, int? potenciaW, int? tdpSoportadoW, bool? graficosIntegrados, bool? desbloqueado,
            bool? permiteOverclock)
        {
            EjecutarSPNonQuery("sp_Especificaciones_Guardar", new Dictionary<string, object>
            {
                { "@Codigo",               codigo                         },
                { "@Plataforma",           Nulo(plataforma)               },
                { "@Socket",               Nulo(socket)                   },
                { "@TipoMemoria",          Nulo(tipoMemoria)              },
                { "@FrecuenciaMemoriaMhz", Nulo(frecuenciaMemoriaMhz)     },
                { "@CapacidadGb",          Nulo(capacidadGb)              },
                { "@SlotsMemoria",         Nulo(slotsMemoria)             },
                { "@MemoriaMaximaGb",      Nulo(memoriaMaximaGb)          },
                { "@Formato",              Nulo(formato)                  },
                { "@ConsumoW",             Nulo(consumoW)                 },
                { "@PotenciaW",            Nulo(potenciaW)                },
                { "@TdpSoportadoW",        Nulo(tdpSoportadoW)            },
                { "@GraficosIntegrados",   Nulo(graficosIntegrados)       },
                { "@Desbloqueado",         Nulo(desbloqueado)             },
                { "@PermiteOverclock",     Nulo(permiteOverclock)         }
            });
        }

        private static object Nulo(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();

        private static object Nulo<T>(T? valor) where T : struct =>
            valor.HasValue ? (object)valor.Value : DBNull.Value;

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
