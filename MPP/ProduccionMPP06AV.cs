using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Mapeo de la ORDEN DE PRODUCCIÓN (RFN1 - CU04 a CU07). Reconstruye la orden y
    /// engancha la VENTA asociada (que aporta cliente, computadora y pagos).
    /// </summary>
    public class ProduccionMPP06AV
    {
        private readonly ProduccionDAL06AV _dal = new ProduccionDAL06AV();
        private readonly VentasMPP06AV _ventas = new VentasMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineas = new LineasEnsamblajeMPP06AV();

        // ── Alta (CU04) ──────────────────────────────────────────
        public void AgregarOrden(OrdenProduccion06AV orden)
        {
            orden.NumeroOrden = _dal.AgregarOrden(orden.NumeroVenta, orden.FechaEntregaEstimada);
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

        public OrdenProduccion06AV ObtenerPorNumero(int numero)
        {
            DataTable t = _dal.ObtenerOrdenPorNumero(numero);
            return t.Rows.Count == 0 ? null : MapearOrden(t.Rows[0]);
        }

        /// <summary>CU07: órdenes en un estado dado (Finalizadas / Entregadas).</summary>
        public List<OrdenProduccion06AV> ObtenerPorEstado(EstadoOrdenProduccion06AV estado)
        {
            var lista = new List<OrdenProduccion06AV>();
            foreach (DataRow row in _dal.ObtenerOrdenesPorEstado((int)estado).Rows)
                lista.Add(MapearOrden(row));
            return lista;
        }

        public OrdenProduccion06AV ObtenerPorVenta(int numeroVenta)
        {
            DataTable t = _dal.ObtenerOrdenPorVenta(numeroVenta);
            return t.Rows.Count == 0 ? null : MapearOrden(t.Rows[0]);
        }

        // ── Transiciones ─────────────────────────────────────────
        public void Planificar(int numero, int idLinea, DateTime fechaInicio, string responsable) =>
            _dal.PlanificarOrden(numero, idLinea, fechaInicio, responsable);

        public void Desplanificar(int numero) => _dal.DesplanificarOrden(numero);

        public void CambiarEstado(int numero, EstadoOrdenProduccion06AV estado) =>
            _dal.CambiarEstadoOrden(numero, (int)estado);

        // ── Cierre de producción (CU06) ──────────────────────────
        public void RegistrarControlCalidad(int numero, ControlCalidad06AV cc) =>
            _dal.RegistrarControlCalidad(numero, cc.Encendido, cc.Conexiones,
                                         cc.SistemaOperativo, cc.Drivers,
                                         cc.Observaciones, cc.Responsable);

        public void Cerrar(int numero, string numeroSerie) => _dal.CerrarOrden(numero, numeroSerie);

        // ── Helpers de mapeo ─────────────────────────────────────
        private OrdenProduccion06AV MapearOrden(DataRow row)
        {
            int numeroVenta = Convert.ToInt32(row["NumeroVenta"]);

            var orden = new OrdenProduccion06AV
            {
                NumeroOrden = Convert.ToInt32(row["NumeroOrden"]),
                NumeroVenta = numeroVenta,
                Venta = _ventas.ObtenerPorNumero(numeroVenta),
                FechaRegistro = Convert.ToDateTime(row["FechaRegistro"]),
                FechaEntregaEstimada = Convert.ToDateTime(row["FechaEntregaEstimada"]),
                Estado = (EstadoOrdenProduccion06AV)Convert.ToInt32(row["Estado"]),
                ResponsableTecnico = row["ResponsableTecnico"] == DBNull.Value ? "" : row["ResponsableTecnico"].ToString(),
                NumeroSerie = row["NumeroSerie"] == DBNull.Value ? "" : row["NumeroSerie"].ToString()
            };

            if (row["IdLinea"] != DBNull.Value)
                orden.LineaEnsamblaje = _lineas.ObtenerPorId(Convert.ToInt32(row["IdLinea"]));

            if (row["FechaInicioPrevista"] != DBNull.Value)
                orden.FechaInicioPrevista = Convert.ToDateTime(row["FechaInicioPrevista"]);

            if (row["FechaCierre"] != DBNull.Value)
                orden.FechaCierre = Convert.ToDateTime(row["FechaCierre"]);

            orden.ControlCalidad = new ControlCalidad06AV
            {
                Encendido = row["CcEncendido"] != DBNull.Value && Convert.ToBoolean(row["CcEncendido"]),
                Conexiones = row["CcConexiones"] != DBNull.Value && Convert.ToBoolean(row["CcConexiones"]),
                SistemaOperativo = row["CcSistemaOperativo"] != DBNull.Value && Convert.ToBoolean(row["CcSistemaOperativo"]),
                Drivers = row["CcDrivers"] != DBNull.Value && Convert.ToBoolean(row["CcDrivers"]),
                Observaciones = row["CcObservaciones"] == DBNull.Value ? "" : row["CcObservaciones"].ToString(),
                Responsable = row["CcResponsable"] == DBNull.Value ? "" : row["CcResponsable"].ToString(),
                Fecha = row["CcFecha"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["CcFecha"])
            };

            return orden;
        }
    }
}
