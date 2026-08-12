using BE;
using DAL;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MPP
{
    /// <summary>
    /// Mapeo del proceso de Producción (RFN1). Tras el refactor:
    /// la Computadora existe de forma independiente (Id string, asociada a un Cliente),
    /// los Pagos cuelgan de la Computadora, la Orden de Producción referencia una
    /// Computadora ya existente, y el cliente/pagos/precio se navegan (no se duplican).
    /// Incluye Recibo de seña y Factura de Venta.
    /// </summary>
    public class ProduccionMPP06AV
    {
        private readonly ProduccionDAL06AV _dal = new ProduccionDAL06AV();
        private readonly ClientesMPP06AV _clientes = new ClientesMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineas = new LineasEnsamblajeMPP06AV();
        private readonly ModelosEstandarMPP06AV _modelos = new ModelosEstandarMPP06AV();
        private readonly GeneradorCodigo06AV _gen = new GeneradorCodigo06AV();

        // ── Computadora ──────────────────────────────────────────
        public void GuardarComputadora(Computadora06AV pc)
        {
            if (string.IsNullOrEmpty(pc.Id)) pc.Id = _gen.Generar("PC");
            _dal.AgregarComputadora(pc.Id, pc.Cliente.Dni, pc.Nombre,
                                    (int)pc.TipoConfiguracion, pc.ModeloOrigen?.Id, pc.PrecioTotal);
            int orden = 0;
            foreach (Componente06AV c in pc.Componentes)
                _dal.AgregarComponenteAComputadora(pc.Id, c.Codigo, orden++);
        }

        public Computadora06AV ObtenerComputadora(string id)
        {
            DataSet ds = _dal.ObtenerComputadoraPorId(id);
            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return null;
            DataRow r = ds.Tables[0].Rows[0];

            var pc = new Computadora06AV
            {
                Id = r["Id"].ToString(),
                Nombre = r["Nombre"] == DBNull.Value ? "" : r["Nombre"].ToString(),
                TipoConfiguracion = (TipoConfiguracion06AV)Convert.ToInt32(r["TipoConfiguracion"]),
                Cliente = _clientes.ObtenerPorDni(r["DniCliente"].ToString())
            };
            if (r["IdModeloOrigen"] != DBNull.Value)
                pc.ModeloOrigen = BuscarModelo(Convert.ToInt32(r["IdModeloOrigen"]));

            if (ds.Tables.Count > 1)
                foreach (DataRow rc in ds.Tables[1].Rows)
                    pc.Componentes.Add(MapearComponente(rc));

            pc.Pagos = ObtenerPagos(id, pc);
            return pc;
        }

        /// <summary>True si la computadora ya está asociada a alguna orden de producción.</summary>
        public bool ExisteOrdenPara(string idComputadora) => _dal.ExisteOrdenPara(idComputadora);

        // ── Pagos ────────────────────────────────────────────────
        public void GuardarPago(Pago06AV p)
        {
            if (string.IsNullOrEmpty(p.Id)) p.Id = _gen.Generar("PG");
            _dal.AgregarPago(p.Id, p.Computadora.Id, (int)p.Tipo, p.Monto, p.Fecha);
        }

        /// <summary>Pagos de una computadora. <paramref name="pc"/> se asigna como FK navegable.</summary>
        public List<Pago06AV> ObtenerPagos(string idComputadora, Computadora06AV pc = null)
        {
            var lista = new List<Pago06AV>();
            foreach (DataRow r in _dal.ObtenerPagosPorComputadora(idComputadora).Rows)
                lista.Add(new Pago06AV
                {
                    Id = r["Id"].ToString(),
                    Computadora = pc,
                    Tipo = (TipoPago06AV)Convert.ToInt32(r["Tipo"]),
                    Monto = Convert.ToDecimal(r["Monto"]),
                    Fecha = Convert.ToDateTime(r["Fecha"])
                });
            return lista;
        }

        // ── Recibo ───────────────────────────────────────────────
        public void GuardarRecibo(Recibo06AV r)
        {
            if (string.IsNullOrEmpty(r.Id)) r.Id = _gen.Generar("RC");
            _dal.AgregarRecibo(r.Id, r.Pago.Id, r.FechaEmision, r.MontoAbonado,
                               r.SaldoPendiente, r.FechaEntregaEstimada);
        }

        public List<Recibo06AV> ObtenerRecibos()
        {
            var lista = new List<Recibo06AV>();
            foreach (DataRow r in _dal.ObtenerRecibos().Rows)
            {
                var pc = new Computadora06AV
                {
                    Id = r["IdComputadora"].ToString(),
                    Nombre = r["NombrePC"] == DBNull.Value ? "" : r["NombrePC"].ToString(),
                    Cliente = new Cliente06AV { Dni = r["DniCliente"].ToString() }
                };
                lista.Add(new Recibo06AV
                {
                    Id = r["Id"].ToString(),
                    Pago = new Pago06AV { Id = r["IdPago"].ToString(), Computadora = pc, Monto = Convert.ToDecimal(r["Monto"]) },
                    FechaEmision = Convert.ToDateTime(r["FechaEmision"]),
                    MontoAbonado = Convert.ToDecimal(r["MontoAbonado"]),
                    SaldoPendiente = Convert.ToDecimal(r["SaldoPendiente"]),
                    FechaEntregaEstimada = Convert.ToDateTime(r["FechaEntregaEstimada"])
                });
            }
            return lista;
        }

        // ── Orden de producción ──────────────────────────────────
        /// <summary>La computadora ya existe; solo se crea la orden que la referencia.</summary>
        public void AgregarOrden(OrdenProduccion06AV orden)
        {
            if (string.IsNullOrEmpty(orden.Id)) orden.Id = _gen.Generar("OP");
            orden.NumeroOrden = _dal.AgregarOrden(orden.Id, orden.Computadora.Id, orden.FechaEntrega);
            orden.Estado = EstadoOrdenProduccion06AV.Pendiente;
        }

        public List<OrdenProduccion06AV> ObtenerTodas()
        {
            var lista = new List<OrdenProduccion06AV>();
            foreach (DataRow row in _dal.ObtenerOrdenes().Rows)
                lista.Add(MapearOrden(row));
            return lista;
        }

        public OrdenProduccion06AV ObtenerPorId(string id)
        {
            DataTable t = _dal.ObtenerOrdenPorId(id);
            return t.Rows.Count == 0 ? null : MapearOrden(t.Rows[0]);
        }

        public void Planificar(string id, int idLinea, DateTime fechaInicio, string responsable) =>
            _dal.PlanificarOrden(id, idLinea, fechaInicio, responsable);

        public void CambiarEstado(string id, EstadoOrdenProduccion06AV estado) =>
            _dal.CambiarEstadoOrden(id, (int)estado);

        public void CerrarOrden(string id, DateTime fechaCierre) => _dal.CerrarOrden(id, fechaCierre);

        // ── Factura de venta ─────────────────────────────────────
        public void AgregarFacturaVenta(FacturaVenta06AV f)
        {
            if (string.IsNullOrEmpty(f.NumeroFactura)) f.NumeroFactura = _gen.Generar("FV");
            _dal.AgregarFacturaVenta(f.NumeroFactura, f.NumeroOrden, f.FechaEmision, f.Total);
        }

        public List<FacturaVenta06AV> ObtenerFacturasVenta()
        {
            var lista = new List<FacturaVenta06AV>();
            foreach (DataRow r in _dal.ObtenerFacturasVenta().Rows)
                lista.Add(new FacturaVenta06AV
                {
                    NumeroFactura = r["NumeroFactura"].ToString(),
                    NumeroOrden = r["IdOrdenProduccion"].ToString(),
                    FechaEmision = Convert.ToDateTime(r["FechaEmision"]),
                    Total = Convert.ToDecimal(r["Total"])
                });
            return lista;
        }

        // ── Helpers de mapeo ─────────────────────────────────────
        private OrdenProduccion06AV MapearOrden(DataRow row)
        {
            var orden = new OrdenProduccion06AV
            {
                Id = row["Id"].ToString(),
                NumeroOrden = Convert.ToInt32(row["NumeroOrden"]),
                Computadora = ObtenerComputadora(row["IdComputadora"].ToString()),
                FechaEntrega = Convert.ToDateTime(row["FechaEntrega"]),
                Estado = (EstadoOrdenProduccion06AV)Convert.ToInt32(row["Estado"]),
                ResponsableTecnico = row["ResponsableTecnico"] == DBNull.Value ? "" : row["ResponsableTecnico"].ToString()
            };
            if (row["IdLinea"] != DBNull.Value)
                orden.LineaEnsamblaje = _lineas.ObtenerPorId(Convert.ToInt32(row["IdLinea"]));
            if (row["FechaInicioPrevista"] != DBNull.Value)
                orden.FechaInicioPrevista = Convert.ToDateTime(row["FechaInicioPrevista"]);
            if (row["FechaCierre"] != DBNull.Value)
                orden.FechaCierre = Convert.ToDateTime(row["FechaCierre"]);
            return orden;
        }

        private Componente06AV MapearComponente(DataRow row)
        {
            return new Componente06AV
            {
                Codigo         = row["CodigoComponente"].ToString(),
                Descripcion    = row["Descripcion"].ToString(),
                Marca          = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo         = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario = Convert.ToDecimal(row["PrecioUnitario"]),
                Stock          = Convert.ToInt32(row["Stock"]),
                StockMinimo    = Convert.ToInt32(row["StockMinimo"]),
                Tipo           = (TipoComponente06AV)Convert.ToInt32(row["Tipo"])
            };
        }

        private ModeloEstandar06AV BuscarModelo(int idModelo) =>
            _modelos.ObtenerTodos().FirstOrDefault(m => m.Id == idModelo)
            ?? new ModeloEstandar06AV { Id = idModelo };
    }
}
