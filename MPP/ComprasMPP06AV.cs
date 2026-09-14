using BE;
using DAL;
using SER;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Data;

namespace MPP
{
    /// <summary>
    /// Mapeo del proceso de Compras (RFN2). Tras el refactor:
    /// la OrdenCompra tiene Id string (PK técnica, GeneradorCodigo06AV) y NumeroCompra
    /// de negocio (secuencia). El detalle usa DetalleComponente06AV. La Cotización
    /// referencia la OC por su Id. Se persiste además la Factura de Compra.
    /// </summary>
    public class ComprasMPP06AV
    {
        private readonly ComprasDAL06AV _dal = new ComprasDAL06AV();
        private readonly ProveedoresMPP06AV _proveedores = new ProveedoresMPP06AV();
        private readonly UsuariosMPP06AV _usuarios = new UsuariosMPP06AV();
        private readonly GeneradorCodigo06AV _gen = new GeneradorCodigo06AV();

        // ── Orden de compra ──────────────────────────────────────
        public void AgregarOrdenCompra(OrdenCompra06AV oc)
        {
            if (string.IsNullOrEmpty(oc.Id)) oc.Id = _gen.Generar("OC");
            oc.NumeroCompra = _dal.AgregarOrdenCompra(oc.Id, oc.FechaLimite, oc.RepositorSolicitante?.Dni);
            foreach (DetalleComponente06AV d in oc.ComponentesFaltantes)
                _dal.AgregarDetalle(oc.Id, d.Componente.Codigo, d.Cantidad);
        }

        public List<OrdenCompra06AV> ObtenerOrdenesCompra()
        {
            var lista = new List<OrdenCompra06AV>();
            foreach (DataRow row in _dal.ObtenerOrdenesCompra().Rows)
                lista.Add(MapearOrdenCompra(row));
            return lista;
        }

        public List<DetalleComponente06AV> ObtenerDetalle(string idOrdenCompra)
        {
            var lista = new List<DetalleComponente06AV>();
            foreach (DataRow r in _dal.ObtenerDetalle(idOrdenCompra).Rows)
                lista.Add(new DetalleComponente06AV
                {
                    Cantidad = Convert.ToInt32(r["Cantidad"]),
                    Componente = MapearComponente(r)
                });
            return lista;
        }

        public void CerrarOrdenCompra(string id, DateTime fechaCierre) => _dal.CerrarOrdenCompra(id, fechaCierre);

        public void CambiarEstadoOrdenCompra(string id, EstadoOrdenCompra06AV estado) =>
            _dal.CambiarEstadoOrdenCompra(id, (int)estado);

        // ── Cotización ───────────────────────────────────────────
        public void AgregarCotizacion(PedidoCotizacion06AV cot)
        {
            if (string.IsNullOrEmpty(cot.Numero)) cot.Numero = _gen.Generar("CO");
            _dal.AgregarCotizacion(cot.Numero, cot.NumeroCompra, cot.Proveedor.Id, cot.Costo, cot.Condiciones);
        }

        public List<PedidoCotizacion06AV> ObtenerCotizaciones()
        {
            var lista = new List<PedidoCotizacion06AV>();
            foreach (DataRow row in _dal.ObtenerCotizaciones().Rows)
                lista.Add(MapearCotizacion(row));
            return lista;
        }

        public List<PedidoCotizacion06AV> ObtenerCotizacionesPorOrden(string idOrdenCompra)
        {
            var lista = new List<PedidoCotizacion06AV>();
            foreach (DataRow row in _dal.ObtenerCotizacionesPorOrden(idOrdenCompra).Rows)
                lista.Add(MapearCotizacion(row));
            return lista;
        }

        public void CambiarEstadoCotizacion(string numero, EstadoCotizacion06AV estado, string dniGerenteAprobador) =>
            _dal.CambiarEstadoCotizacion(numero, (int)estado, dniGerenteAprobador);

        // ── Factura de compra ────────────────────────────────────
        public void AgregarFacturaCompra(FacturaCompra06AV f)
        {
            if (string.IsNullOrEmpty(f.NumeroFactura)) f.NumeroFactura = _gen.Generar("FC");
            _dal.AgregarFacturaCompra(f.NumeroFactura, f.NumeroCompra, f.FechaEmision,
                                      f.FechaEntrega, f.Total, f.Observaciones);
            foreach (DetalleComponente06AV d in f.ComponentesRecibidos)
                _dal.AgregarFacturaCompraDetalle(f.NumeroFactura, d.Componente.Codigo, d.Cantidad);
        }

        /// <summary>
        /// Recepciones ya registradas contra una orden. Se usa para saber cuánto falta
        /// todavía cuando una orden quedó Recibida parcial.
        /// </summary>
        public List<FacturaCompra06AV> ObtenerFacturasPorOrden(string idOrdenCompra)
        {
            var lista = new List<FacturaCompra06AV>();
            foreach (DataRow row in _dal.ObtenerFacturasPorOrden(idOrdenCompra).Rows)
            {
                string numero = row["NumeroFactura"].ToString();
                var f = new FacturaCompra06AV
                {
                    NumeroFactura = numero,
                    NumeroCompra = row["IdOrdenCompra"].ToString(),
                    FechaEmision = Convert.ToDateTime(row["FechaEmision"]),
                    FechaEntrega = Convert.ToDateTime(row["FechaEntrega"]),
                    Total = row["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Total"]),
                    Observaciones = row["Observaciones"] == DBNull.Value ? "" : row["Observaciones"].ToString(),
                    ComponentesRecibidos = ObtenerDetalleFactura(numero)
                };
                lista.Add(f);
            }
            return lista;
        }

        private List<DetalleComponente06AV> ObtenerDetalleFactura(string numeroFactura)
        {
            var lista = new List<DetalleComponente06AV>();
            foreach (DataRow r in _dal.ObtenerFacturaCompraDetalle(numeroFactura).Rows)
                lista.Add(new DetalleComponente06AV
                {
                    Componente = MapearComponente(r),
                    Cantidad = Convert.ToInt32(r["Cantidad"])
                });
            return lista;
        }

        // ── Helpers de mapeo ─────────────────────────────────────
        private OrdenCompra06AV MapearOrdenCompra(DataRow row)
        {
            string id = row["Id"].ToString();
            var oc = new OrdenCompra06AV
            {
                Id = id,
                NumeroCompra = Convert.ToInt32(row["NumeroCompra"]),
                FechaLimite = Convert.ToDateTime(row["FechaLimite"]),
                Estado = (EstadoOrdenCompra06AV)Convert.ToInt32(row["Estado"]),
                ComponentesFaltantes = ObtenerDetalle(id)
            };
            if (row["DniRepositor"] != DBNull.Value)
                oc.RepositorSolicitante = _usuarios.ObtenerPorDni(row["DniRepositor"].ToString());
            if (row["FechaCierre"] != DBNull.Value)
                oc.FechaCierre = Convert.ToDateTime(row["FechaCierre"]);
            return oc;
        }

        private PedidoCotizacion06AV MapearCotizacion(DataRow row)
        {
            string idOc = row["IdOrdenCompra"].ToString();
            var cot = new PedidoCotizacion06AV
            {
                Numero = row["Numero"].ToString(),
                NumeroCompra = idOc,
                Proveedor = _proveedores.ObtenerPorId(Convert.ToInt32(row["IdProveedor"])),
                FechaEmision = Convert.ToDateTime(row["FechaEmision"]),
                Estado = (EstadoCotizacion06AV)Convert.ToInt32(row["Estado"]),
                Costo = row["Costo"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Costo"]),
                Condiciones = row["Condiciones"] == DBNull.Value ? "" : row["Condiciones"].ToString(),
                ComponentesPedidos = ObtenerDetalle(idOc)
            };
            if (row["DniGerenteAprobador"] != DBNull.Value)
                cot.GerenteAprobador = _usuarios.ObtenerPorDni(row["DniGerenteAprobador"].ToString());
            return cot;
        }

        private Componente06AV MapearComponente(DataRow r)
        {
            return new Componente06AV
            {
                Codigo         = r["CodigoComponente"].ToString(),
                Descripcion    = r["Descripcion"].ToString(),
                Marca          = r["Marca"] == DBNull.Value ? "" : r["Marca"].ToString(),
                Modelo         = r["Modelo"] == DBNull.Value ? "" : r["Modelo"].ToString(),
                PrecioUnitario = Convert.ToDecimal(r["PrecioUnitario"]),
                Stock          = Convert.ToInt32(r["Stock"]),
                StockMinimo    = Convert.ToInt32(r["StockMinimo"]),
                Tipo           = (TipoComponente06AV)Convert.ToInt32(r["Tipo"])
            };
        }
    }
}
