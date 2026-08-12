using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// ENTREGA DE COMPUTADORAS (RFN1 - CU07). Circuito propio del RECEPCIONISTA,
    /// separado tanto de la venta como de la fábrica.
    ///
    /// Recibe las órdenes de producción que ya salieron de fábrica en estado
    /// "Finalizada" (control de calidad aprobado y N° de serie asignado), cobra el
    /// saldo pendiente de la venta, emite la factura, marca la orden como Entregada
    /// y libera la línea de ensamblaje.
    ///
    /// El recepcionista NO opera la orden de producción: solo la recibe terminada y
    /// cierra el circuito comercial.
    /// </summary>
    public class EntregasBLL06AV
    {
        private readonly ProduccionMPP06AV _produccionMpp = new ProduccionMPP06AV();
        private readonly VentasMPP06AV _ventasMpp = new VentasMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineasMpp = new LineasEnsamblajeMPP06AV();

        // ══════════════════════════════════════════════════════════
        //  Lectura
        // ══════════════════════════════════════════════════════════
        /// <summary>Órdenes terminadas en fábrica y todavía sin retirar.</summary>
        public List<OrdenProduccion06AV> ObtenerPendientesDeEntrega()
        {
            try { return _produccionMpp.ObtenerPorEstado(EstadoOrdenProduccion06AV.Finalizada); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las computadoras listas para entregar.", ex); }
        }

        /// <summary>Histórico de órdenes ya retiradas por el cliente.</summary>
        public List<OrdenProduccion06AV> ObtenerEntregadas()
        {
            try { return _produccionMpp.ObtenerPorEstado(EstadoOrdenProduccion06AV.Entregada); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las entregas realizadas.", ex); }
        }

        /// <summary>
        /// CU07 (paso 2): busca la orden por número de orden o por DNI del cliente
        /// dentro del conjunto indicado. Devuelve la lista filtrada.
        /// </summary>
        public List<OrdenProduccion06AV> Filtrar(IEnumerable<OrdenProduccion06AV> ordenes, string criterio)
        {
            if (ordenes == null) return new List<OrdenProduccion06AV>();
            if (string.IsNullOrWhiteSpace(criterio)) return ordenes.ToList();

            string c = criterio.Trim().TrimStart('#');
            return ordenes.Where(o =>
                    o.NumeroOrden.ToString().Contains(c) ||
                    o.NumeroVenta.ToString().Contains(c) ||
                    (o.Cliente != null && (
                        (o.Cliente.Dni ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (o.Cliente.NombreCompleto ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (!string.IsNullOrEmpty(o.NumeroSerie) &&
                        o.NumeroSerie.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        public OrdenProduccion06AV ObtenerOrden(int numeroOrden)
        {
            try { return _produccionMpp.ObtenerPorNumero(numeroOrden); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la orden.", ex); }
        }

        // ══════════════════════════════════════════════════════════
        //  CU07 · Entregar computadora
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// Registra el pago del saldo final, cierra la orden como "Entregada",
        /// deja la venta en estado Entregada y libera la línea de ensamblaje.
        /// Devuelve el pago generado (trae el número de factura), o null si no
        /// quedaba saldo por cobrar.
        /// </summary>
        public Pago06AV RegistrarEntrega(int numeroOrden, FormaPago06AV formaPago, string referencia = null)
        {
            var orden = _produccionMpp.ObtenerPorNumero(numeroOrden);
            if (orden == null)
                throw new NoEncontradoException06AV($"No existe la orden de producción #{numeroOrden}.");

            if (orden.Estado == EstadoOrdenProduccion06AV.Entregada)
                throw new ValidacionException06AV("estado", "Esa orden ya fue entregada.");
            if (orden.Estado != EstadoOrdenProduccion06AV.Finalizada)
                throw new ValidacionException06AV("estado",
                    "La computadora todavía no está terminada: la orden debe estar Finalizada para poder entregarla.");

            var venta = orden.Venta ?? _ventasMpp.ObtenerPorNumero(orden.NumeroVenta);
            if (venta == null)
                throw new NoEncontradoException06AV($"No existe la venta #{orden.NumeroVenta}.");
            if (venta.Estado == EstadoVenta06AV.Anulada)
                throw new ValidacionException06AV("estado", "La venta asociada está anulada.");

            decimal saldo = venta.SaldoPendiente;
            Pago06AV pago = null;
            try
            {
                if (saldo > 0)
                    pago = _ventasMpp.AgregarPago(venta.NumeroVenta, TipoPago06AV.SaldoFinal,
                                                  saldo, formaPago, referencia, UsuarioActual());

                _produccionMpp.CambiarEstado(orden.NumeroOrden, EstadoOrdenProduccion06AV.Entregada);
                _ventasMpp.CambiarEstado(venta.NumeroVenta, EstadoVenta06AV.Entregada);

                if (orden.LineaEnsamblaje != null && !orden.LineaEnsamblaje.Disponible)
                {
                    orden.LineaEnsamblaje.Disponible = true;
                    _lineasMpp.Modificar(orden.LineaEnsamblaje);
                }
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la entrega.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{orden.NumeroOrden} entregada (venta #{venta.NumeroVenta})", ModuloBitacora.Ventas);
            return pago;
        }

        private static string UsuarioActual()
        {
            try
            {
                var sesion = UsuarioSesion06AV.Instancia();
                string nombre = sesion.NombreCompleto();
                return string.IsNullOrWhiteSpace(nombre) ? sesion.UsuarioActual?.Dni : nombre;
            }
            catch { return null; }
        }
    }
}
