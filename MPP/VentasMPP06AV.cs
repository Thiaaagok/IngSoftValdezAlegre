using BE;
using DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MPP
{
    /// <summary>
    /// Mapeo de la VENTA (RFN1 - CU01/CU03/CU07). Persiste y reconstruye el grafo
    /// comercial: cliente, computadora + componentes y pagos (seña y saldo final).
    /// </summary>
    public class VentasMPP06AV
    {
        private readonly VentasDAL06AV _dal = new VentasDAL06AV();
        private readonly ClientesMPP06AV _clientes = new ClientesMPP06AV();

        // ── Computadora ──────────────────────────────────────────
        public void GuardarComputadora(Computadora06AV pc)
        {
            pc.Id = _dal.AgregarComputadora(pc.Nombre, (int)pc.TipoConfiguracion, pc.PrecioTotal);

            // La lista de componentes puede traer repetidos (2 módulos de RAM, 2 discos…):
            // se guarda una fila por código con su cantidad.
            foreach (var g in pc.Componentes.GroupBy(c => c.Codigo))
                _dal.AgregarComponenteAComputadora(pc.Id, g.Key, g.Count());
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

            // Se expande la cantidad para que PrecioTotal siga siendo la suma de la lista.
            foreach (DataRow rc in _dal.ObtenerComponentesDeComputadora(id).Rows)
            {
                int cantidad = rc.Table.Columns.Contains("Cantidad") && rc["Cantidad"] != DBNull.Value
                    ? Convert.ToInt32(rc["Cantidad"]) : 1;
                for (int i = 0; i < Math.Max(1, cantidad); i++)
                    pc.Componentes.Add(MapearComponente(rc));
            }

            return pc;
        }

        // ── Venta ────────────────────────────────────────────────
        public void AgregarVenta(Venta06AV venta)
        {
            GuardarComputadora(venta.Computadora);
            venta.NumeroVenta = _dal.AgregarVenta(
                venta.Cliente.Dni, venta.Computadora.Id, venta.FechaEntregaEstimada, venta.UsuarioRegistro);
            venta.Estado = EstadoVenta06AV.Pendiente;
        }

        public List<Venta06AV> ObtenerTodas()
        {
            var lista = new List<Venta06AV>();
            foreach (DataRow row in _dal.ObtenerVentas().Rows)
                lista.Add(MapearVenta(row));
            return lista;
        }

        public Venta06AV ObtenerPorNumero(int numero)
        {
            DataTable t = _dal.ObtenerVentaPorNumero(numero);
            return t.Rows.Count == 0 ? null : MapearVenta(t.Rows[0]);
        }

        /// <summary>CU04: ventas señadas que todavía no tienen orden de producción.</summary>
        public List<Venta06AV> ObtenerParaProduccion()
        {
            var lista = new List<Venta06AV>();
            foreach (DataRow row in _dal.ObtenerVentasParaProduccion().Rows)
                lista.Add(MapearVenta(row));
            return lista;
        }

        public void CambiarEstado(int numeroVenta, EstadoVenta06AV estado) =>
            _dal.CambiarEstadoVenta(numeroVenta, (int)estado);

        // ── Pagos ────────────────────────────────────────────────
        /// <summary>Registra el pago y devuelve el comprobante generado por la base.</summary>
        public Pago06AV AgregarPago(int numeroVenta, TipoPago06AV tipo, decimal monto,
                                    FormaPago06AV formaPago, string referencia, string usuario)
        {
            DataTable t = _dal.AgregarPago(numeroVenta, (int)tipo, monto, (int)formaPago, referencia, usuario);
            return t.Rows.Count == 0 ? null : MapearPago(t.Rows[0]);
        }

        public List<Pago06AV> ObtenerPagos(int numeroVenta)
        {
            var lista = new List<Pago06AV>();
            foreach (DataRow r in _dal.ObtenerPagosPorVenta(numeroVenta).Rows)
                lista.Add(MapearPago(r));
            return lista;
        }

        // ── Helpers de mapeo ─────────────────────────────────────
        private Venta06AV MapearVenta(DataRow row)
        {
            var venta = new Venta06AV
            {
                NumeroVenta = Convert.ToInt32(row["NumeroVenta"]),
                Cliente = _clientes.ObtenerPorDni(row["DniCliente"].ToString()),
                Computadora = ObtenerComputadora(Convert.ToInt32(row["IdComputadora"])),
                FechaVenta = Convert.ToDateTime(row["FechaVenta"]),
                FechaEntregaEstimada = Convert.ToDateTime(row["FechaEntregaEstimada"]),
                Estado = (EstadoVenta06AV)Convert.ToInt32(row["Estado"]),
                UsuarioRegistro = row["UsuarioRegistro"] == DBNull.Value ? "" : row["UsuarioRegistro"].ToString()
            };

            if (row.Table.Columns.Contains("NumeroOrdenProduccion") && row["NumeroOrdenProduccion"] != DBNull.Value)
                venta.NumeroOrdenProduccion = Convert.ToInt32(row["NumeroOrdenProduccion"]);

            venta.Pagos = ObtenerPagos(venta.NumeroVenta);
            return venta;
        }

        private Pago06AV MapearPago(DataRow r) => new Pago06AV
        {
            Id = Convert.ToInt32(r["Id"]),
            NumeroVenta = Convert.ToInt32(r["NumeroVenta"]),
            Tipo = (TipoPago06AV)Convert.ToInt32(r["Tipo"]),
            NumeroRecibo = r["NumeroRecibo"] == DBNull.Value ? "" : r["NumeroRecibo"].ToString(),
            Monto = Convert.ToDecimal(r["Monto"]),
            FormaPago = (FormaPago06AV)Convert.ToInt32(r["FormaPago"]),
            Referencia = r["Referencia"] == DBNull.Value ? "" : r["Referencia"].ToString(),
            Fecha = Convert.ToDateTime(r["Fecha"]),
            Usuario = r["Usuario"] == DBNull.Value ? "" : r["Usuario"].ToString()
        };

        private Componente06AV MapearComponente(DataRow row) => new Componente06AV
        {
            Codigo = row["Codigo"].ToString(),
            Descripcion = row["Descripcion"].ToString(),
            Tipo = (TipoComponente06AV)Convert.ToInt32(row["Tipo"]),
            Marca = row["Marca"] == DBNull.Value ? "" : row["Marca"].ToString(),
            Modelo = row["Modelo"] == DBNull.Value ? "" : row["Modelo"].ToString(),
            PrecioUnitario = Convert.ToDecimal(row["PrecioUnitario"]),
            StockDisponible = Convert.ToInt32(row["StockDisponible"]),
            StockReservado = row.Table.Columns.Contains("StockReservado") && row["StockReservado"] != DBNull.Value
                             ? Convert.ToInt32(row["StockReservado"]) : 0
        };
    }
}
