using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class OrdenProduccionBLL06AV
    {
        private readonly ProduccionMPP06AV _mpp = new ProduccionMPP06AV();
        private readonly VentasMPP06AV _ventasMpp = new VentasMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineasMpp = new LineasEnsamblajeMPP06AV();
        private readonly ComponentesMPP06AV _componentesMpp = new ComponentesMPP06AV();

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

        public List<Venta06AV> ObtenerVentasDisponibles()
        {
            try { return _ventasMpp.ObtenerParaProduccion(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las ventas señadas.", ex); }
        }

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
                linea.Disponible = false;
                _lineasMpp.Modificar(linea);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo planificar la orden.", ex); }

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{numeroOrden} planificada (línea #{idLinea})", ModuloBitacora.Produccion);
        }

        public void IniciarEnsamblaje(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Planificada)
                throw new ValidacionException06AV("estado", "Solo se puede iniciar el ensamblaje de una orden Planificada.");
            CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.EnEnsamblaje);
        }

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

            LiberarLinea(orden);

            AuditoriaPcFactory06AV.Modificacion(
                $"Orden #{numeroOrden} finalizada (N° de serie {numeroSerie})", ModuloBitacora.Produccion);
            return numeroSerie;
        }

        public void CambiarEstado(int numeroOrden, EstadoOrdenProduccion06AV estado)
        {
            ObtenerOrdenOExcepcion(numeroOrden);
            try { _mpp.CambiarEstado(numeroOrden, estado); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cambiar el estado.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} → estado {estado}", ModuloBitacora.Produccion);
        }

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
                    LiberarLinea(orden);
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

        private void LiberarLinea(OrdenProduccion06AV orden)
        {
            if (orden == null || orden.LineaEnsamblaje == null) return;
            try
            {
                orden.LineaEnsamblaje.Disponible = true;
                _lineasMpp.Modificar(orden.LineaEnsamblaje);
            }
            catch { }
        }

        private static string GenerarNumeroSerie(OrdenProduccion06AV orden) =>
            $"SN-{DateTime.Now:yyyy}-{orden.NumeroOrden:00000}";

        private void RevertirConsumo(IEnumerable<KeyValuePair<string, int>> consumidos)
        {
            foreach (var c in consumidos)
            {
                try
                {
                    var comp = _componentesMpp.ObtenerPorCodigo(c.Key);
                    if (comp == null) continue;
                    comp.Stock += c.Value;
                    _componentesMpp.Modificar(comp);
                    _componentesMpp.ReservarStock(c.Key, c.Value);
                }
                catch {  }
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
