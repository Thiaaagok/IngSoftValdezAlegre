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
    /// Mapeo del proceso de Producción (RFN1). Persiste y reconstruye el grafo completo
    /// de una orden: cliente, computadora + componentes, línea de ensamblaje y pagos.
    /// </summary>
    public class ProduccionMPP06AV
    {
        private readonly ProduccionDAL06AV _dal = new ProduccionDAL06AV();
        private readonly VentasMPP06AV _ventas = new VentasMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineas = new LineasEnsamblajeMPP06AV();
        private readonly ModelosEstandarMPP06AV _modelos = new ModelosEstandarMPP06AV();
        private readonly GeneradorCodigo06AV _gen = new GeneradorCodigo06AV();

        // ── Computadora ──────────────────────────────────────────
        public void GuardarComputadora(Computadora06AV pc)
        {
            pc.Id = _dal.AgregarComputadora(pc.Nombre, (int)pc.TipoConfiguracion, pc.PrecioTotal);
            foreach (Componente06AV c in pc.Componentes)
                _dal.AgregarComponenteAComputadora(pc.Id, c.Codigo);
        }

        public Computadora06AV ObtenerComputadora(int id)
        {
            DataTable cab = _dal.ObtenerComputadoraPorId(id);
            if (cab.Rows.Count == 0) return null;
            DataRow r = cab.Rows[0];

            var pc = new Computadora06AV
            {
                Id = Convert.ToInt32(r["Id"]),
                Nombre = r["Nombre"] == DBNull.Value ? "" : r["Nombre"].ToString(),
                TipoConfiguracion = (TipoConfiguracion06AV)Convert.ToInt32(r["TipoConfiguracion"])
            };

            foreach (DataRow rc in _dal.ObtenerComponentesDeComputadora(id).Rows)
                pc.Componentes.Add(MapearComponente(rc));

            return pc;
        }

        // ── Orden de producción ──────────────────────────────────
        public void AgregarOrden(OrdenProduccion06AV orden)
        {
            GuardarComputadora(orden.Computadora);
            orden.NumeroOrden = _dal.AgregarOrden(
                orden.Cliente.Dni, orden.Computadora.Id, orden.FechaEntrega);
            orden.Estado = EstadoOrdenProduccion06AV.Pendiente;
        }

        // ── Lectura ──────────────────────────────────────────────
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

        public void Planificar(int numero, int idLinea, DateTime fechaInicio, string responsable) =>
            _dal.PlanificarOrden(numero, idLinea, fechaInicio, responsable);

        public void CambiarEstado(int numero, EstadoOrdenProduccion06AV estado) =>
            _dal.CambiarEstadoOrden(numero, (int)estado);

        // ── Pagos ────────────────────────────────────────────────
        public void AgregarPago(int numeroOrden, TipoPago06AV tipo, decimal monto) =>
            _dal.AgregarPago(numeroOrden, (int)tipo, monto);

        public List<Pago06AV> ObtenerPagos(int numeroOrden)
        {
            var lista = new List<Pago06AV>();
            foreach (DataRow r in _dal.ObtenerPagosPorOrden(numeroOrden).Rows)
                lista.Add(new Pago06AV
                {
                    Id = Convert.ToInt32(r["Id"]),
                    NumeroOrden = Convert.ToInt32(r["NumeroOrden"]),
                    Tipo = (TipoPago06AV)Convert.ToInt32(r["Tipo"]),
                    Monto = Convert.ToDecimal(r["Monto"]),
                    Fecha = Convert.ToDateTime(r["Fecha"])
                });
            return lista;
        }

        // ── Helpers de mapeo ─────────────────────────────────────
        private OrdenProduccion06AV MapearOrden(DataRow row)
        {
            int numeroVenta = Convert.ToInt32(row["NumeroVenta"]);

            var orden = new OrdenProduccion06AV
            {
                Id = row["Id"].ToString(),
                NumeroOrden = Convert.ToInt32(row["NumeroOrden"]),
                Cliente = _clientes.ObtenerPorDni(row["DniCliente"].ToString()),
                Computadora = ObtenerComputadora(Convert.ToInt32(row["IdComputadora"])),
                FechaEntrega = Convert.ToDateTime(row["FechaEntrega"]),
                Estado = (EstadoOrdenProduccion06AV)Convert.ToInt32(row["Estado"]),
                ResponsableTecnico = row["ResponsableTecnico"] == DBNull.Value ? "" : row["ResponsableTecnico"].ToString(),
                NumeroSerie = row["NumeroSerie"] == DBNull.Value ? "" : row["NumeroSerie"].ToString()
            };
            if (row["IdLinea"] != DBNull.Value)
                orden.LineaEnsamblaje = _lineas.ObtenerPorId(Convert.ToInt32(row["IdLinea"]));
            if (row["FechaInicioPrevista"] != DBNull.Value)
                orden.FechaInicioPrevista = Convert.ToDateTime(row["FechaInicioPrevista"]);

            orden.Pagos = ObtenerPagos(orden.NumeroOrden);
            return orden;
        }

            orden.ControlCalidad = new ControlCalidad06AV
            {
                Codigo = row["Codigo"].ToString(),
                Descripcion = row["Descripcion"].ToString(),
                Tipo = (TipoComponente06AV)Convert.ToInt32(row["Tipo"]),
                Marca = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
                Modelo = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
                PrecioUnitario = Convert.ToDecimal(row["PrecioUnitario"]),
                StockDisponible = Convert.ToInt32(row["StockDisponible"])
            };

            return orden;
        }

        private ModeloEstandar06AV BuscarModelo(int idModelo) =>
            _modelos.ObtenerTodos().FirstOrDefault(m => m.Id == idModelo)
            ?? new ModeloEstandar06AV { Id = idModelo };
    }
}
