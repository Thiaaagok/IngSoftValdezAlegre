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
    /// Reglas de la VENTA (RFN1). Es el mostrador del RECEPCIONISTA:
    ///   CU01 Registrar venta        → <see cref="RegistrarVenta"/>
    ///   CU03 Registrar seña         → <see cref="RegistrarSena"/>
    ///
    /// La venta NO fabrica nada: solo compromete (reserva) los componentes.
    /// El descuento real de stock ocurre al cerrar la orden de producción (CU06),
    /// y la entrega al cliente (CU07) es un circuito aparte: <see cref="EntregasBLL06AV"/>.
    /// </summary>
    public class VentasBLL06AV
    {
        private readonly VentasMPP06AV _mpp = new VentasMPP06AV();
        private readonly ProduccionMPP06AV _produccionMpp = new ProduccionMPP06AV();
        private readonly ComponentesMPP06AV _componentesMpp = new ComponentesMPP06AV();

        // ══════════════════════════════════════════════════════════
        //  Lectura
        // ══════════════════════════════════════════════════════════
        public List<Venta06AV> ObtenerTodas()
        {
            try { return _mpp.ObtenerTodas(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las ventas.", ex); }
        }

        public Venta06AV ObtenerPorNumero(int numeroVenta)
        {
            try { return _mpp.ObtenerPorNumero(numeroVenta); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la venta.", ex); }
        }

        /// <summary>
        /// Recibos de seña emitidos (CU03). El recibo no es una tabla aparte: es la
        /// proyección del pago de seña de cada venta, con los importes congelados al
        /// momento de cobrarla.
        /// </summary>
        public List<Recibo06AV> ObtenerRecibos()
        {
            try
            {
                return (_mpp.ObtenerTodas() ?? new List<Venta06AV>())
                    .Where(v => v.TieneSena)
                    .OrderByDescending(v => v.Sena.Fecha)
                    .Select(v => new Recibo06AV
                    {
                        Id = v.Sena.NumeroRecibo,
                        Pago = v.Sena,
                        FechaEmision = v.Sena.Fecha,
                        MontoAbonado = v.Sena.Monto,
                        SaldoPendiente = v.PrecioTotal - v.Sena.Monto,
                        Venta = v,
                        FechaEntregaEstimada = v.FechaEntregaEstimada
                    })
                    .ToList();
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los recibos.", ex); }
        }

        /// <summary>Orden de producción asociada a la venta, o null si todavía no se generó.</summary>
        public OrdenProduccion06AV ObtenerOrdenDeVenta(int numeroVenta)
        {
            try { return _produccionMpp.ObtenerPorVenta(numeroVenta); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la orden de la venta.", ex); }
        }

        /// <summary>CU04 (paso 2): ventas con seña registrada y todavía sin orden de producción.</summary>
        public List<Venta06AV> ObtenerListasParaProduccion()
        {
            try { return _mpp.ObtenerParaProduccion(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las ventas señadas.", ex); }
        }

        // ══════════════════════════════════════════════════════════
        //  CU01 · Registrar venta
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// Registra la venta de una computadora (estándar o armada con el cliente) y
        /// RESERVA los componentes necesarios. La reserva impide que otra venta
        /// comprometa las mismas unidades, pero no las saca todavía del depósito.
        /// </summary>
        public Venta06AV RegistrarVenta(Cliente06AV cliente, Computadora06AV computadora, DateTime fechaEntregaEstimada)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Dni))
                throw new ValidacionException06AV("cliente", "Debe indicarse un cliente válido.");
            if (computadora == null || computadora.Componentes == null || computadora.Componentes.Count == 0)
                throw new ValidacionException06AV("computadora", "La computadora debe tener al menos un componente.");
            if (fechaEntregaEstimada.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaEntregaEstimada", "La fecha estimada de entrega no puede ser anterior a hoy.");

            var requeridos = Requerimientos(computadora);

            // Escenario alternativo 4.1 del CU01: componente sin stock disponible.
            foreach (var r in requeridos)
            {
                var comp = _componentesMpp.ObtenerPorCodigo(r.Key);
                if (comp == null)
                    throw new NoEncontradoException06AV($"El componente '{r.Key}' no existe.");
                if (comp.StockLibre < r.Value)
                    throw new ValidacionException06AV("stock",
                        $"Componente sin stock: '{comp.Descripcion}' (libre {comp.StockLibre}, requerido {r.Value}).");
            }

            // Reserva atómica por componente, con compensación si alguna falla.
            var reservados = new List<KeyValuePair<string, int>>();
            try
            {
                foreach (var r in requeridos)
                {
                    _componentesMpp.ReservarStock(r.Key, r.Value);
                    reservados.Add(r);
                }
            }
            catch (Exception ex)
            {
                LiberarReservas(reservados);
                throw new AccesoDatosException06AV(
                    "No se pudieron reservar los componentes; la venta no se registró. Detalle: " + ex.Message, ex);
            }

            var venta = new Venta06AV
            {
                Cliente = cliente,
                Computadora = computadora,
                FechaEntregaEstimada = fechaEntregaEstimada,
                UsuarioRegistro = UsuarioActual()
            };

            try { _mpp.AgregarVenta(venta); }
            catch (Exception ex)
            {
                LiberarReservas(reservados);
                throw new AccesoDatosException06AV(
                    "No se pudo registrar la venta (se liberó la reserva). Detalle: " + ex.Message, ex);
            }

            AuditoriaPcFactory06AV.Alta($"Venta #{venta.NumeroVenta} (cliente {cliente.Dni})", ModuloBitacora.Ventas);
            return venta;
        }

        // ══════════════════════════════════════════════════════════
        //  CU03 · Registrar seña y emitir recibo
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// Registra la seña del 50% del total con su forma de pago y devuelve el pago
        /// generado (incluye el número de recibo). Una venta admite una sola seña.
        /// </summary>
        public Pago06AV RegistrarSena(int numeroVenta, FormaPago06AV formaPago, string referencia = null)
        {
            var venta = ObtenerVentaOExcepcion(numeroVenta);

            if (venta.Estado == EstadoVenta06AV.Anulada)
                throw new ValidacionException06AV("estado", "La venta está anulada.");
            if (venta.TieneSena)
                throw new ValidacionException06AV("sena", "La seña de esta venta ya fue registrada.");
            if (venta.PrecioTotal <= 0)
                throw new ValidacionException06AV("total", "La venta no tiene un importe válido.");

            decimal monto = venta.MontoSenaRequerido;

            Pago06AV pago;
            try
            {
                pago = _mpp.AgregarPago(numeroVenta, TipoPago06AV.Sena, monto, formaPago, referencia, UsuarioActual());
                _mpp.CambiarEstado(numeroVenta, EstadoVenta06AV.Senada);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la seña.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Seña {pago?.NumeroRecibo} registrada en venta #{numeroVenta}", ModuloBitacora.Ventas);
            return pago;
        }

        // La entrega de la computadora y el cobro del saldo final (CU07) NO viven acá:
        // son un circuito propio, con su pantalla y sus reglas, en EntregasBLL06AV.

        // ══════════════════════════════════════════════════════════
        //  Anulación
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// Anula una venta que todavía no pasó a producción y libera los componentes
        /// reservados. No se permite si ya existe una orden de producción asociada.
        /// </summary>
        public void AnularVenta(int numeroVenta)
        {
            var venta = ObtenerVentaOExcepcion(numeroVenta);

            if (venta.Estado == EstadoVenta06AV.Anulada)
                throw new ValidacionException06AV("estado", "La venta ya está anulada.");
            if (venta.NumeroOrdenProduccion != null || venta.Estado == EstadoVenta06AV.EnProduccion)
                throw new ValidacionException06AV("estado",
                    "La venta ya tiene una orden de producción: no se puede anular desde acá.");
            if (venta.Estado == EstadoVenta06AV.Entregada)
                throw new ValidacionException06AV("estado", "La venta ya fue entregada.");

            try
            {
                _mpp.CambiarEstado(numeroVenta, EstadoVenta06AV.Anulada);
                LiberarReservas(Requerimientos(venta.Computadora));
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo anular la venta.", ex); }

            AuditoriaPcFactory06AV.Baja($"Venta #{numeroVenta} anulada", ModuloBitacora.Ventas);
        }

        // ══════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════
        /// <summary>Cantidad requerida de cada componente de la computadora, agrupada por código.</summary>
        internal static List<KeyValuePair<string, int>> Requerimientos(Computadora06AV pc)
        {
            if (pc == null || pc.Componentes == null) return new List<KeyValuePair<string, int>>();
            return pc.Componentes
                     .GroupBy(c => c.Codigo)
                     .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                     .ToList();
        }

        /// <summary>Compensación best-effort: devuelve al stock libre lo que se había reservado.</summary>
        private void LiberarReservas(IEnumerable<KeyValuePair<string, int>> reservados)
        {
            foreach (var r in reservados)
            {
                try { _componentesMpp.LiberarReserva(r.Key, r.Value); }
                catch { /* no interrumpe el flujo */ }
            }
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

        private Venta06AV ObtenerVentaOExcepcion(int numeroVenta)
        {
            var venta = _mpp.ObtenerPorNumero(numeroVenta);
            if (venta == null)
                throw new NoEncontradoException06AV($"No existe la venta #{numeroVenta}.");
            return venta;
        }
    }
}
