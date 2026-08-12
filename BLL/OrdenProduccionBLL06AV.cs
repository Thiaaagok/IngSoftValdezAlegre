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
    /// Reglas de la ORDEN DE PRODUCCIÓN (RFN1). Es el circuito de FÁBRICA, posterior
    /// y separado de la venta:
    ///   CU04 Gestionar orden de producción → <see cref="CrearOrden"/>
    ///   CU05 Asignar línea de ensamblaje   → <see cref="AsignarLinea"/>
    ///        Iniciar ensamblaje            → <see cref="IniciarEnsamblaje"/>
    ///   CU06 Cerrar orden de producción    → <see cref="RegistrarControlCalidad"/>
    ///
    /// Al cerrarse, la orden pasa a la bandeja de <see cref="EntregasBLL06AV"/>: la
    /// entrega al cliente y el cobro del saldo (CU07) son un circuito comercial aparte.
    /// </summary>
    public class OrdenProduccionBLL06AV
    {
        private readonly ProduccionMPP06AV _mpp = new ProduccionMPP06AV();
        private readonly VentasMPP06AV _ventasMpp = new VentasMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineasMpp = new LineasEnsamblajeMPP06AV();
        private readonly ComponentesMPP06AV _componentesMpp = new ComponentesMPP06AV();

        // ══════════════════════════════════════════════════════════
        //  Lectura
        // ══════════════════════════════════════════════════════════
        public List<OrdenProduccion06AV> ObtenerTodas()
        {
            try { return _mpp.ObtenerTodas(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las órdenes.", ex); }
        }

        public OrdenProduccion06AV ObtenerPorNumero(int numero)
        {
            try { return _mpp.ObtenerPorNumero(numero); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la orden.", ex); }
        }

        /// <summary>CU04 (paso 2): ventas con seña registrada y sin orden asociada.</summary>
        public List<Venta06AV> ObtenerVentasDisponibles()
        {
            try { return _ventasMpp.ObtenerParaProduccion(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las ventas señadas.", ex); }
        }

        // ══════════════════════════════════════════════════════════
        //  CU04 · Gestionar orden de producción
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// El gerente genera la orden sobre una venta YA SEÑADA y fija la fecha de
        /// entrega comprometida. La orden nace en estado "Pendiente".
        /// </summary>
        public OrdenProduccion06AV CrearOrden(int numeroVenta, DateTime fechaEntregaEstimada)
        {
            var venta = _ventasMpp.ObtenerPorNumero(numeroVenta);
            if (venta == null)
                throw new NoEncontradoException06AV($"No existe la venta #{numeroVenta}.");
            if (!venta.TieneSena)
                throw new ValidacionException06AV("sena",
                    "La venta no tiene la seña registrada: no se puede generar la orden de producción.");
            if (venta.Estado == EstadoVenta06AV.Anulada)
                throw new ValidacionException06AV("estado", "La venta está anulada.");
            if (_mpp.ObtenerPorVenta(numeroVenta) != null)
                throw new ValidacionException06AV("orden", "Esa venta ya tiene una orden de producción.");
            if (fechaEntregaEstimada.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaEntregaEstimada",
                    "La fecha de entrega no puede ser anterior a hoy.");

            var orden = new OrdenProduccion06AV
            {
                NumeroVenta = numeroVenta,
                Venta = venta,
                FechaEntregaEstimada = fechaEntregaEstimada
            };

            try
            {
                _mpp.AgregarOrden(orden);
                _ventasMpp.CambiarEstado(numeroVenta, EstadoVenta06AV.EnProduccion);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la orden de producción.", ex); }

            AuditoriaPcFactory06AV.Alta(
                $"Orden de producción #{orden.NumeroOrden} (venta #{numeroVenta})", ModuloBitacora.Produccion);
            return orden;
        }

        // ══════════════════════════════════════════════════════════
        //  CU05 · Asignar línea de ensamblaje
        // ══════════════════════════════════════════════════════════
        public void AsignarLinea(int numeroOrden, int idLinea, DateTime fechaInicio, string responsable)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Pendiente)
                throw new ValidacionException06AV("estado", "Solo se puede planificar una orden en estado Pendiente.");
            if (string.IsNullOrWhiteSpace(responsable))
                throw new ValidacionException06AV("responsable", "El responsable técnico es obligatorio.");

            var linea = _lineasMpp.ObtenerPorId(idLinea);
            if (linea == null)
                throw new NoEncontradoException06AV($"No existe la línea #{idLinea}.");
            if (!linea.Disponible)
                throw new ValidacionException06AV("linea", "La línea elegida no está disponible.");

            try
            {
                _mpp.Planificar(numeroOrden, idLinea, fechaInicio, responsable);
                linea.Disponible = false;          // la línea queda ocupada
                _lineasMpp.Modificar(linea);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo planificar la orden.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{numeroOrden} planificada (línea #{idLinea})", ModuloBitacora.Produccion);
        }

        /// <summary>De Planificada a En ensamblaje.</summary>
        public void IniciarEnsamblaje(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Planificada)
                throw new ValidacionException06AV("estado", "Solo se puede iniciar el ensamblaje de una orden Planificada.");
            CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.EnEnsamblaje);
        }

        // ══════════════════════════════════════════════════════════
        //  CU06 · Cerrar orden de producción
        // ══════════════════════════════════════════════════════════
        /// <summary>
        /// Registra el control de calidad del equipo.
        ///  · Aprobado  → descuenta del stock los componentes efectivamente utilizados
        ///                (consumiendo la reserva de la venta), asigna número de serie
        ///                y deja la orden "Finalizada".
        ///  · Rechazado → la orden queda "En revisión" con las observaciones; el stock
        ///                sigue reservado hasta que se corrija.
        /// Devuelve el número de serie asignado, o null si el control no fue aprobado.
        /// </summary>
        public string RegistrarControlCalidad(int numeroOrden, ControlCalidad06AV control)
        {
            if (control == null)
                throw new ValidacionException06AV("control", "Debe completarse el control de calidad.");

            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.EnEnsamblaje &&
                orden.Estado != EstadoOrdenProduccion06AV.EnRevision)
                throw new ValidacionException06AV("estado",
                    "Solo se puede cerrar una orden En ensamblaje o En revisión.");

            if (string.IsNullOrWhiteSpace(control.Responsable))
                control.Responsable = orden.ResponsableTecnico;

            try { _mpp.RegistrarControlCalidad(numeroOrden, control); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar el control de calidad.", ex); }

            // Escenario alternativo 3.1: el equipo no pasa el control.
            if (!control.Aprobado)
            {
                CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.EnRevision);
                AuditoriaPcFactory06AV.Modificacion(
                    $"Orden #{numeroOrden}: control de calidad rechazado → En revisión", ModuloBitacora.Produccion);
                return null;
            }

            // Descuento real de stock: componentes efectivamente utilizados.
            var requeridos = VentasBLL06AV.Requerimientos(orden.Computadora);
            var consumidos = new List<KeyValuePair<string, int>>();
            try
            {
                foreach (var r in requeridos)
                {
                    _componentesMpp.ConsumirReserva(r.Key, r.Value);
                    consumidos.Add(r);
                }
            }
            catch (Exception ex)
            {
                RevertirConsumo(consumidos);
                throw new AccesoDatosException06AV(
                    "No se pudo descontar el stock de los componentes; la orden no se cerró. Detalle: " + ex.Message, ex);
            }

            string numeroSerie = GenerarNumeroSerie(orden);
            try { _mpp.Cerrar(numeroOrden, numeroSerie); }
            catch (Exception ex)
            {
                RevertirConsumo(consumidos);
                throw new AccesoDatosException06AV(
                    "No se pudo cerrar la orden (se restituyó el stock). Detalle: " + ex.Message, ex);
            }

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{numeroOrden} finalizada (N° de serie {numeroSerie})", ModuloBitacora.Produccion);
            return numeroSerie;
        }

        // ══════════════════════════════════════════════════════════
        //  Estados
        // ══════════════════════════════════════════════════════════
        public void CambiarEstado(int numeroOrden, EstadoOrdenProduccion06AV estado)
        {
            ObtenerOrdenOExcepcion(numeroOrden);
            try { _mpp.CambiarEstado(numeroOrden, estado); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cambiar el estado.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} → estado {estado}", ModuloBitacora.Produccion);
        }

        /// <summary>
        /// Vuelve la orden al paso anterior:
        ///   Planificada  → Pendiente (se suelta la línea)
        ///   EnEnsamblaje → Planificada
        ///   EnRevision   → EnEnsamblaje
        /// No se permite desde Finalizada (el stock ya se consumió y hay N° de serie
        /// asignado) ni desde Entregada (la factura ya fue emitida).
        /// </summary>
        public EstadoOrdenProduccion06AV VolverAtras(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            EstadoOrdenProduccion06AV anterior;

            switch (orden.Estado)
            {
                case EstadoOrdenProduccion06AV.Planificada:
                    anterior = EstadoOrdenProduccion06AV.Pendiente; break;
                case EstadoOrdenProduccion06AV.EnEnsamblaje:
                    anterior = EstadoOrdenProduccion06AV.Planificada; break;
                case EstadoOrdenProduccion06AV.EnRevision:
                    anterior = EstadoOrdenProduccion06AV.EnEnsamblaje; break;
                case EstadoOrdenProduccion06AV.Finalizada:
                    throw new ValidacionException06AV("estado",
                        "La orden ya está finalizada: el stock fue descontado y tiene N° de serie asignado.");
                case EstadoOrdenProduccion06AV.Entregada:
                    throw new ValidacionException06AV("estado",
                        "Una orden entregada ya tiene la factura emitida y no se puede volver atrás.");
                default:
                    throw new ValidacionException06AV("estado",
                        "La orden está en el primer paso; no hay un paso anterior.");
            }

            try
            {
                if (anterior == EstadoOrdenProduccion06AV.Pendiente)
                {
                    _mpp.Desplanificar(numeroOrden);
                    if (orden.LineaEnsamblaje != null)
                    {
                        orden.LineaEnsamblaje.Disponible = true;
                        _lineasMpp.Modificar(orden.LineaEnsamblaje);
                    }
                }
                else
                {
                    _mpp.CambiarEstado(numeroOrden, anterior);
                }
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo volver al paso anterior.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{numeroOrden} vuelta a estado {anterior}", ModuloBitacora.Produccion);
            return anterior;
        }

        // ══════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════
        /// <summary>N° de serie legible y único por orden: SN-AAAA-00042.</summary>
        private static string GenerarNumeroSerie(OrdenProduccion06AV orden) =>
            $"SN-{DateTime.Now:yyyy}-{orden.NumeroOrden:00000}";

        /// <summary>Compensación: devuelve el stock físico y la reserva de lo ya consumido.</summary>
        private void RevertirConsumo(IEnumerable<KeyValuePair<string, int>> consumidos)
        {
            foreach (var c in consumidos)
            {
                try
                {
                    var comp = _componentesMpp.ObtenerPorCodigo(c.Key);
                    if (comp == null) continue;
                    comp.StockDisponible += c.Value;
                    _componentesMpp.Modificar(comp);
                    _componentesMpp.ReservarStock(c.Key, c.Value);
                }
                catch { /* compensación best-effort */ }
            }
        }

        private OrdenProduccion06AV ObtenerOrdenOExcepcion(int numeroOrden)
        {
            var orden = _mpp.ObtenerPorNumero(numeroOrden);
            if (orden == null)
                throw new NoEncontradoException06AV($"No existe la orden de producción #{numeroOrden}.");
            return orden;
        }
    }
}
