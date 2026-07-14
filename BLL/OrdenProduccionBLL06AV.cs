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

        /// <summary>Paso 4: registra la orden con estado inicial Pendiente.</summary>
        public OrdenProduccion06AV RegistrarOrden(Cliente06AV cliente, Computadora06AV computadora, DateTime fechaEntrega)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Dni))
                throw new ValidacionException06AV("cliente", "Debe indicarse un cliente válido.");
            if (computadora == null || computadora.Componentes == null || computadora.Componentes.Count == 0)
                throw new ValidacionException06AV("computadora", "La computadora debe tener al menos un componente.");
            if (fechaEntrega.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaEntrega", "La fecha de entrega no puede ser anterior a hoy.");

            // Cantidad requerida de cada componente (agrupado por código).
            var requeridos = computadora.Componentes
                .GroupBy(c => c.Codigo)
                .Select(g => new { Codigo = g.Key, Cantidad = g.Count(), Descripcion = g.First().Descripcion })
                .ToList();

            // RFN1: validar que haya stock suficiente ANTES de registrar la orden.
            foreach (var r in requeridos)
            {
                var comp = _componentesMpp.ObtenerPorCodigo(r.Codigo);
                if (comp == null)
                    throw new NoEncontradoException06AV($"El componente '{r.Codigo}' no existe.");
                if (comp.StockDisponible < r.Cantidad)
                    throw new ValidacionException06AV("stock",
                        $"No hay stock suficiente de '{comp.Descripcion}' (disponible {comp.StockDisponible}, requerido {r.Cantidad}).");
            }

            // RFN1: descontar el stock ANTES de registrar la orden. Si el descuento falla
            // (falta stock, falta/está desactualizado el SP sp_Componentes_DescontarStock, etc.)
            // se revierte lo ya descontado y NO se registra la orden, para no dejarla a medias.
            var descontados = new List<KeyValuePair<string, int>>();
            try
            {
                foreach (var r in requeridos)
                {
                    _componentesMpp.DescontarStock(r.Codigo, r.Cantidad);
                    descontados.Add(new KeyValuePair<string, int>(r.Codigo, r.Cantidad));
                }
            }
            catch (Exception ex)
            {
                RestaurarStock(descontados);
                throw new AccesoDatosException06AV(
                    "No se pudo descontar el stock de los componentes; la orden no se registró. Detalle: " + ex.Message, ex);
            }

            var orden = new OrdenProduccion06AV
            {
                Cliente = cliente,
                Computadora = computadora,
                FechaEntrega = fechaEntrega
            };

            try { _mpp.AgregarOrden(orden); }
            catch (Exception ex)
            {
                RestaurarStock(descontados);   // no se pudo guardar: se devuelve el stock descontado
                throw new AccesoDatosException06AV("No se pudo registrar la orden (se devolvió el stock). Detalle: " + ex.Message, ex);
            }

            AuditoriaPcFactory06AV.Alta($"Orden de producción (cliente {cliente.Dni})", ModuloBitacora.Produccion);
            return orden;
        }

        /// <summary>Paso 3: registra la seña (50% del total). No se permite dos veces.</summary>
        public decimal RegistrarSena(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Pagos.Any(p => p.Tipo == TipoPago06AV.Sena))
                throw new ValidacionException06AV("sena", "La seña de esta orden ya fue registrada.");

            decimal sena = Math.Round(orden.PrecioTotal * 0.5m, 2);
            try { _mpp.AgregarPago(numeroOrden, TipoPago06AV.Sena, sena); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la seña.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Seña registrada en orden #{numeroOrden}", ModuloBitacora.Produccion);
            return sena;
        }

        /// <summary>Paso 5: asigna línea, fecha de inicio y responsable; pasa a Planificada.</summary>
        public void Planificar(int numeroOrden, int idLinea, DateTime fechaInicio, string responsable)
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

            AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} planificada (línea #{idLinea})", ModuloBitacora.Produccion);
        }

        /// <summary>Cambia el estado de fabricación (p. ej. a EnEnsamblaje o Finalizada).</summary>
        public void CambiarEstado(int numeroOrden, EstadoOrdenProduccion06AV estado)
        {
            ObtenerOrdenOExcepcion(numeroOrden);
            try { _mpp.CambiarEstado(numeroOrden, estado); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cambiar el estado.", ex); }


            AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} → estado {estado}", ModuloBitacora.Produccion);
        }

        /// <summary>Paso intermedio: de Planificada a EnEnsamblaje.</summary>
        public void IniciarEnsamblaje(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Planificada)
                throw new ValidacionException06AV("estado", "Solo se puede iniciar el ensamblaje de una orden Planificada.");
            CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.EnEnsamblaje);
        }

        /// <summary>Paso intermedio: de EnEnsamblaje a Finalizada.</summary>
        public void Finalizar(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.EnEnsamblaje)
                throw new ValidacionException06AV("estado", "Solo se puede finalizar una orden En ensamblaje.");
            CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.Finalizada);
        }

        /// <summary>Paso 6: registra el saldo final, cierra la orden (Entregada) y libera la línea.</summary>
        public decimal Entregar(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Estado == EstadoOrdenProduccion06AV.Entregada)
                throw new ValidacionException06AV("estado", "La orden ya fue entregada.");

            decimal saldo = orden.SaldoPendiente;
            try
            {
                if (saldo > 0)
                    _mpp.AgregarPago(numeroOrden, TipoPago06AV.SaldoFinal, saldo);

                _mpp.CambiarEstado(numeroOrden, EstadoOrdenProduccion06AV.Entregada);

                if (orden.LineaEnsamblaje != null)   // se libera la línea
                {
                    orden.LineaEnsamblaje.Disponible = true;
                    _lineasMpp.Modificar(orden.LineaEnsamblaje);
                }

                AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} entregada", ModuloBitacora.Produccion);
                return saldo;
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cerrar la entrega.", ex); }
        }

        /// <summary>
        /// Vuelve la orden al paso inmediatamente anterior de fabricación:
        /// Planificada→Pendiente (libera la línea), EnEnsamblaje→Planificada, Finalizada→EnEnsamblaje.
        /// No se permite desde Pendiente (primer paso) ni desde Entregada (factura ya emitida).
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
                case EstadoOrdenProduccion06AV.Finalizada:
                    anterior = EstadoOrdenProduccion06AV.EnEnsamblaje; break;
                case EstadoOrdenProduccion06AV.Entregada:
                    throw new ValidacionException06AV("estado",
                        "Una orden entregada ya tiene la factura emitida y no se puede volver atrás.");
                default:
                    throw new ValidacionException06AV("estado",
                        "La orden está en el primer paso; no hay un paso anterior.");
            }

            try
            {
                _mpp.CambiarEstado(numeroOrden, anterior);

                // Al deshacer la planificación, la línea vuelve a quedar disponible.
                if (anterior == EstadoOrdenProduccion06AV.Pendiente && orden.LineaEnsamblaje != null)
                {
                    orden.LineaEnsamblaje.Disponible = true;
                    _lineasMpp.Modificar(orden.LineaEnsamblaje);
                }
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo volver al paso anterior.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{numeroOrden} vuelta a estado {anterior}", ModuloBitacora.Produccion);
            return anterior;
        }

        /// <summary>Best-effort: devuelve al stock las cantidades ya descontadas (compensación).</summary>
        private void RestaurarStock(IEnumerable<KeyValuePair<string, int>> descontados)
        {
            foreach (var d in descontados)
            {
                try
                {
                    var comp = _componentesMpp.ObtenerPorCodigo(d.Key);
                    if (comp != null)
                    {
                        comp.StockDisponible += d.Value;
                        _componentesMpp.Modificar(comp);
                    }
                }
                catch { /* compensación best-effort: no interrumpe el flujo */ }
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