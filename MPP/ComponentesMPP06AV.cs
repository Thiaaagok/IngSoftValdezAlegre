using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    public class ComponentesMPP06AV
    {
        private readonly ComponentesDAL06AV _dal = new ComponentesDAL06AV();

        public List<Componente06AV> ObtenerTodos()
        {
            DataTable tabla = _dal.ObtenerTodos();
            var lista = new List<Componente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            AdjuntarEspecificaciones(lista);
            return lista;
        }

        public Componente06AV ObtenerPorCodigo(string codigo)
        {
            DataTable tabla = _dal.ObtenerPorCodigo(codigo);
            if (tabla.Rows.Count == 0) return null;
            Componente06AV c = Mapear(tabla.Rows[0]);
            DataTable esp = _dal.ObtenerEspecificacion(codigo);
            if (esp.Rows.Count > 0) c.Especificacion = MapearEspecificacion(esp.Rows[0]);
            return c;
        }

        public void GuardarEspecificacion(string codigo, EspecificacionComponente06AV e)
        {
            if (e == null) e = new EspecificacionComponente06AV();
            _dal.GuardarEspecificacion(codigo, e.Plataforma, e.Socket, e.TipoMemoria, e.FrecuenciaMemoriaMhz,
                                       e.CapacidadGb, e.SlotsMemoria, e.MemoriaMaximaGb, e.Formato, e.ConsumoW,
                                       e.PotenciaW, e.TdpSoportadoW, e.GraficosIntegrados, e.Desbloqueado,
                                       e.PermiteOverclock);
        }

        private void AdjuntarEspecificaciones(List<Componente06AV> lista)
        {
            if (lista.Count == 0) return;
            var porCodigo = new Dictionary<string, EspecificacionComponente06AV>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in _dal.ObtenerEspecificaciones().Rows)
                porCodigo[row["CodigoComponente"].ToString()] = MapearEspecificacion(row);
            foreach (Componente06AV c in lista)
                if (porCodigo.TryGetValue(c.Codigo, out EspecificacionComponente06AV e))
                    c.Especificacion = e.Copiar();
        }

        private static EspecificacionComponente06AV MapearEspecificacion(DataRow row)
        {
            return new EspecificacionComponente06AV
            {
                Plataforma           = Texto(row, "Plataforma"),
                Socket               = Texto(row, "Socket"),
                TipoMemoria          = Texto(row, "TipoMemoria"),
                FrecuenciaMemoriaMhz = Entero(row, "FrecuenciaMemoriaMhz"),
                CapacidadGb          = Entero(row, "CapacidadGb"),
                SlotsMemoria         = Entero(row, "SlotsMemoria"),
                MemoriaMaximaGb      = Entero(row, "MemoriaMaximaGb"),
                Formato              = Texto(row, "Formato"),
                ConsumoW             = Entero(row, "ConsumoW"),
                PotenciaW            = Entero(row, "PotenciaW"),
                TdpSoportadoW        = Entero(row, "TdpSoportadoW"),
                GraficosIntegrados   = Logico(row, "GraficosIntegrados"),
                Desbloqueado         = Logico(row, "Desbloqueado"),
                PermiteOverclock     = Logico(row, "PermiteOverclock")
            };
        }

        private static string Texto(DataRow row, string col) =>
            row[col] == DBNull.Value ? null : row[col].ToString();

        private static int? Entero(DataRow row, string col) =>
            row[col] == DBNull.Value ? (int?)null : Convert.ToInt32(row[col]);

        private static bool? Logico(DataRow row, string col) =>
            row[col] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row[col]);

        public List<Componente06AV> ObtenerBajoStock()
        {
            DataTable tabla = _dal.ObtenerBajoStock();
            var lista = new List<Componente06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(Mapear(row));
            return lista;
        }

        public void Agregar(Componente06AV c)
        {
            _dal.Agregar(c.Codigo, c.Descripcion, (int)c.Tipo, c.Marca, c.Modelo,
                         c.PrecioUnitario, c.Stock, c.StockMinimo);
        }

        public void Modificar(Componente06AV c)
        {
            _dal.Modificar(c.Codigo, c.Descripcion, (int)c.Tipo, c.Marca, c.Modelo,
                           c.PrecioUnitario, c.Stock, c.StockMinimo);
        }

        public void SumarStock(string codigo, int cantidad) => _dal.SumarStock(codigo, cantidad);

        public void BajaLogica(string codigo) => _dal.BajaLogica(codigo);

        public void Reactivar(string codigo) => _dal.Reactivar(codigo);

        public void Eliminar(string codigo)
        {
            _dal.Eliminar(codigo);
        }

        public List<ComponenteHistorico06AV> ObtenerBitacora(string codigo, string descripcion,
                                                             DateTime? fechaIni, DateTime? fechaFin)
        {
            DataTable tabla = _dal.ObtenerBitacora(codigo, descripcion, fechaIni, fechaFin);
            var lista = new List<ComponenteHistorico06AV>();
            foreach (DataRow row in tabla.Rows)
                lista.Add(MapearHistorico(row));
            return lista;
        }

        public void ActivarHistorico(int idHistorico) => _dal.ActivarHistorico(idHistorico);

        public void DescontarStock(string codigo, int cantidad)
        {
            _dal.DescontarStock(codigo, cantidad);
        }

        /// <summary>CU01: compromete unidades para una venta (no las saca del depósito).</summary>
        public void ReservarStock(string codigo, int cantidad) => _dal.ReservarStock(codigo, cantidad);

        /// <summary>Devuelve al stock libre unidades reservadas.</summary>
        public void LiberarReserva(string codigo, int cantidad) => _dal.LiberarReserva(codigo, cantidad);

        public void ConsumirReserva(string codigo, int cantidad) => _dal.ConsumirReserva(codigo, cantidad);

        private Componente06AV Mapear(DataRow row)
        {
            return new Componente06AV
            {
                Codigo          = row["Codigo"].ToString(),
                Descripcion     = row["Descripcion"].ToString(),
                Tipo            = (TipoComponente06AV)Convert.ToInt32(row["Tipo"]),
                Marca           = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo          = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario  = Convert.ToDecimal(row["PrecioUnitario"]),
                Stock           = Convert.ToInt32(row["Stock"]),
                StockMinimo     = row.Table.Columns.Contains("StockMinimo") && row["StockMinimo"] != DBNull.Value
                                  ? Convert.ToInt32(row["StockMinimo"]) : 0,
                StockReservado  = row.Table.Columns.Contains("StockReservado") && row["StockReservado"] != DBNull.Value
                                  ? Convert.ToInt32(row["StockReservado"]) : 0,
                BajaLogica      = row.Table.Columns.Contains("Bit_Lo_Bo") && row["Bit_Lo_Bo"] != DBNull.Value
                                  && Convert.ToBoolean(row["Bit_Lo_Bo"])
            };
        }

        private ComponenteHistorico06AV MapearHistorico(DataRow row)
        {
            return new ComponenteHistorico06AV
            {
                IdHistorico      = Convert.ToInt32(row["IdHistorico"]),
                CodigoComponente = row["CodigoComponente"].ToString(),
                Fecha            = Convert.ToDateTime(row["Fecha"]),
                Hora             = (TimeSpan)row["Hora"],
                Descripcion      = row["Descripcion"].ToString(),
                Tipo             = (TipoComponente06AV)Convert.ToInt32(row["Tipo"]),
                Marca            = row["Marca"]  == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo           = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario   = Convert.ToDecimal(row["PrecioUnitario"]),
                Stock            = Convert.ToInt32(row["Stock"]),
                StockMinimo      = Convert.ToInt32(row["StockMinimo"]),
                BajaLogica       = Convert.ToBoolean(row["Bit_Lo_Bo"]),
                Activo           = Convert.ToBoolean(row["Act"])
            };
        }
    }
}
