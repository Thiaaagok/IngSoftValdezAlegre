using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Mapeo del proceso de Compras (RFN2). Persiste y reconstruye órdenes de compra
    /// (con su detalle de insumos) y pedidos de cotización (con proveedor e insumos de la OC).
    /// </summary>
    public class ComprasMPP06AV
    {
        private readonly ComprasDAL06AV _dal = new ComprasDAL06AV();
        private readonly ProveedoresMPP06AV _proveedores = new ProveedoresMPP06AV();

        // ── Orden de compra ──────────────────────────────────────
        public void AgregarOrdenCompra(OrdenCompra06AV oc)
        {
            oc.NumeroCompra = _dal.AgregarOrdenCompra(oc.FechaLimite, oc.RepositorSolicitante);
            foreach (DetalleInsumo06AV d in oc.InsumosFaltantes)
                _dal.AgregarDetalle(oc.NumeroCompra, d.Insumo.Codigo, d.Cantidad);
        }

        public List<OrdenCompra06AV> ObtenerOrdenesCompra()
        {
            var lista = new List<OrdenCompra06AV>();
            foreach (DataRow row in _dal.ObtenerOrdenesCompra().Rows)
                lista.Add(MapearOrdenCompra(row));
            return lista;
        }

        public OrdenCompra06AV ObtenerOrdenCompraPorNumero(int numero)
        {
            DataTable t = _dal.ObtenerOrdenCompraPorNumero(numero);
            return t.Rows.Count == 0 ? null : MapearOrdenCompra(t.Rows[0]);
        }

        public List<DetalleInsumo06AV> ObtenerDetalle(int numeroCompra)
        {
            var lista = new List<DetalleInsumo06AV>();
            foreach (DataRow r in _dal.ObtenerDetalle(numeroCompra).Rows)
            {
                lista.Add(new DetalleInsumo06AV
                {
                    Cantidad = Convert.ToInt32(r["Cantidad"]),
                    Insumo = new Insumo06AV
                    {
                        Codigo = r["CodigoInsumo"].ToString(),
                        Descripcion = r["Descripcion"].ToString(),
                        Stock = Convert.ToInt32(r["Stock"]),
                        StockMinimo = Convert.ToInt32(r["StockMinimo"])
                    }
                });
            }
            return lista;
        }

        public void FinalizarOrdenCompra(int numero) => _dal.FinalizarOrdenCompra(numero);

        public void CambiarEstadoOrdenCompra(int numero, EstadoOrdenCompra06AV estado) =>
            _dal.CambiarEstadoOrdenCompra(numero, (int)estado);

        // ── Cotización ───────────────────────────────────────────
        public void AgregarCotizacion(PedidoCotizacion06AV cot)
        {
            cot.Numero = _dal.AgregarCotizacion(cot.NumeroCompra, cot.Proveedor.Id, cot.Costo, cot.Condiciones);
        }

        public List<PedidoCotizacion06AV> ObtenerCotizaciones()
        {
            var lista = new List<PedidoCotizacion06AV>();
            foreach (DataRow row in _dal.ObtenerCotizaciones().Rows)
                lista.Add(MapearCotizacion(row));
            return lista;
        }

        public PedidoCotizacion06AV ObtenerCotizacionPorNumero(int numero)
        {
            DataTable t = _dal.ObtenerCotizacionPorNumero(numero);
            return t.Rows.Count == 0 ? null : MapearCotizacion(t.Rows[0]);
        }

        public void CambiarEstadoCotizacion(int numero, EstadoCotizacion06AV estado) =>
            _dal.CambiarEstadoCotizacion(numero, (int)estado);

        // ── Stock ────────────────────────────────────────────────
        public void SumarStock(string codigoInsumo, int cantidad) => _dal.SumarStock(codigoInsumo, cantidad);

        // ── Helpers de mapeo ─────────────────────────────────────
        private OrdenCompra06AV MapearOrdenCompra(DataRow row)
        {
            int numero = Convert.ToInt32(row["NumeroCompra"]);
            var oc = new OrdenCompra06AV
            {
                NumeroCompra = numero,
                FechaLimite = Convert.ToDateTime(row["FechaLimite"]),
                RepositorSolicitante = row["RepositorSolicitante"] == DBNull.Value ? "" : row["RepositorSolicitante"].ToString(),
                Estado = (EstadoOrdenCompra06AV)Convert.ToInt32(row["Estado"]),
                InsumosFaltantes = ObtenerDetalle(numero)
            };
            if (row["FechaCierre"] != DBNull.Value)
                oc.FechaCierre = Convert.ToDateTime(row["FechaCierre"]);
            return oc;
        }

        private PedidoCotizacion06AV MapearCotizacion(DataRow row)
        {
            int numeroCompra = Convert.ToInt32(row["NumeroCompra"]);
            return new PedidoCotizacion06AV
            {
                Numero = Convert.ToInt32(row["Numero"]),
                NumeroCompra = numeroCompra,
                Proveedor = _proveedores.ObtenerPorId(Convert.ToInt32(row["IdProveedor"])),
                FechaEmision = Convert.ToDateTime(row["FechaEmision"]),
                Estado = (EstadoCotizacion06AV)Convert.ToInt32(row["Estado"]),
                Costo = row.Table.Columns.Contains("Costo") && row["Costo"] != DBNull.Value ? Convert.ToDecimal(row["Costo"]) : 0m,
                Condiciones = row.Table.Columns.Contains("Condiciones") && row["Condiciones"] != DBNull.Value ? row["Condiciones"].ToString() : "",
                InsumosPedidos = ObtenerDetalle(numeroCompra)
            };
        }
    }
}
