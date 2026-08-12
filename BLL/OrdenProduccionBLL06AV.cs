using BE;
using BLL.Excepciones;
using MPP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Proceso de Producción/Venta (RFN1). Tras el refactor el circuito es:
    /// 1) se registra la Computadora (existe por sí misma, asociada al cliente),
    /// 2) se cobra la seña y se emite el Recibo ANTES de la orden,
    /// 3) el gerente crea la Orden de Producción sobre una computadora ya señada,
    /// 4) planificación / ensamblaje / finalización,
    /// 5) entrega (saldo final) y emisión de la Factura de Venta.
    /// El cliente, los pagos y el precio se navegan desde la computadora.
    /// </summary>
    public class OrdenProduccionBLL06AV
    {
        private readonly ProduccionMPP06AV _mpp = new ProduccionMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineasMpp = new LineasEnsamblajeMPP06AV();
        private readonly ComponentesMPP06AV _componentesMpp = new ComponentesMPP06AV();

        // ── Consultas ────────────────────────────────────────────
        public List<OrdenProduccion06AV> ObtenerTodas()
        {
            try { return _mpp.ObtenerTodas(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las órdenes.", ex); }
        }

        public OrdenProduccion06AV ObtenerPorId(string id)
        {
            try { return _mpp.ObtenerPorId(id); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la orden.", ex); }
        }

        public Computadora06AV ObtenerComputadora(string idComputadora)
        {
            try { return _mpp.ObtenerComputadora(idComputadora); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la computadora.", ex); }
        }

        public List<Recibo06AV> ObtenerRecibos()
        {
            try { return _mpp.ObtenerRecibos(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los recibos.", ex); }
        }

        public List<FacturaVenta06AV> ObtenerFacturasVenta()
        {
            try { return _mpp.ObtenerFacturasVenta(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las facturas de venta.", ex); }
        }

        // ── Paso 1: registrar la computadora ─────────────────────
        /// <summary>
        /// Registra una computadora para un cliente. Sus componentes y su precio quedan como
        /// SNAPSHOT (copia) del momento de armarla. Todavía no consume stock (eso ocurre al
        /// crear la orden de producción).
        /// </summary>
        public Computadora06AV RegistrarComputadora(Cliente06AV cliente, Computadora06AV computadora)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Dni))
                throw new ValidacionException06AV("cliente", "Debe indicarse un cliente válido.");
            if (computadora == null || computadora.Componentes == null || computadora.Componentes.Count == 0)
                throw new ValidacionException06AV("computadora", "La computadora debe tener al menos un componente.");

            computadora.Cliente = cliente;

            try { _mpp.GuardarComputadora(computadora); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la computadora.", ex); }
          
            AuditoriaPcFactory06AV.Alta($"Computadora {computadora.Id} (cliente {cliente.Dni})", SER.ModuloBitacora.Computadoras);
            return computadora;
        }

        // ── Paso 2: cobrar seña + emitir recibo ──────────────────
        /// <summary>
        /// Cobra la seña (50% del total) de una computadora ya registrada y emite el recibo.
        /// Debe hacerse ANTES de crear la orden de producción. No se permite dos veces.
        /// </summary>
        public Recibo06AV CobrarSena(string idComputadora, DateTime fechaEntregaEstimada)
        {
            var pc = ObtenerComputadoraOExcepcion(idComputadora);
            if (pc.Pagos.Any(p => p.Tipo == TipoPago06AV.Sena))
                throw new ValidacionException06AV("sena", "La seña de esta computadora ya fue cobrada.");

            decimal sena = Math.Round(pc.PrecioTotal * 0.5m, 2);
            var pago = new Pago06AV { Computadora = pc, Tipo = TipoPago06AV.Sena, Monto = sena, Fecha = DateTime.Now };

            var recibo = new Recibo06AV
            {
                Pago = pago,
                FechaEmision = DateTime.Now,
                MontoAbonado = sena,
                SaldoPendiente = pc.PrecioTotal - sena,
                FechaEntregaEstimada = fechaEntregaEstimada
            };

            try
            {
                _mpp.GuardarPago(pago);
                _mpp.GuardarRecibo(recibo);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cobrar la seña.", ex); }

            AuditoriaPcFactory06AV.Alta($"Seña ${sena:0.00} + recibo {recibo.Id} (PC {pc.Id})", SER.ModuloBitacora.Ventas);
            return recibo;
        }

        // ── Paso 3: crear la orden de producción ─────────────────
        /// <summary>
        /// Crea la orden sobre una computadora YA registrada y señada. Valida que tenga seña,
        /// que no esté ya en otra orden, y descuenta el stock de sus componentes (RFN1).
        /// </summary>
        public OrdenProduccion06AV RegistrarOrden(string idComputadora, DateTime fechaEntrega)
        {
            var pc = ObtenerComputadoraOExcepcion(idComputadora);

            if (!pc.Pagos.Any(p => p.Tipo == TipoPago06AV.Sena))
                throw new ValidacionException06AV("sena", "La computadora no tiene seña cobrada; cobrá la seña antes de crear la orden.");
            if (_mpp.ExisteOrdenPara(pc.Id))
                throw new ValidacionException06AV("computadora", "Esta computadora ya está asociada a una orden de producción.");
            if (fechaEntrega.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaEntrega", "La fecha de entrega no puede ser anterior a hoy.");

            // Cantidad requerida de cada componente (agrupado por código).
            var requeridos = pc.Componentes
                .GroupBy(c => c.Codigo)
                .Select(g => new { Codigo = g.Key, Cantidad = g.Count(), Descripcion = g.First().Descripcion })
                .ToList();

            // RFN1: validar stock suficiente ANTES de comprometer la orden.
            foreach (var r in requeridos)
            {
                var comp = _componentesMpp.ObtenerPorCodigo(r.Codigo);
                if (comp == null)
                    throw new NoEncontradoException06AV($"El componente '{r.Codigo}' no existe.");
                if (comp.Stock < r.Cantidad)
                    throw new ValidacionException06AV("stock",
                        $"No hay stock suficiente de '{comp.Descripcion}' (disponible {comp.Stock}, requerido {r.Cantidad}).");
            }

            // RFN1: descontar stock ANTES de registrar; si algo falla, se restaura y no se crea la orden.
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

            var orden = new OrdenProduccion06AV { Computadora = pc, FechaEntrega = fechaEntrega };

            try { _mpp.AgregarOrden(orden); }
            catch (Exception ex)
            {
                RestaurarStock(descontados);
                throw new AccesoDatosException06AV("No se pudo registrar la orden (se devolvió el stock). Detalle: " + ex.Message, ex);
            }

            AuditoriaPcFactory06AV.Alta($"Orden de producción #{orden.NumeroOrden} (PC {pc.Id})", SER.ModuloBitacora.Produccion);
            return orden;
        }

        // ── Paso 4: planificación / ensamblaje ───────────────────
        public void Planificar(string idOrden, int idLinea, DateTime fechaInicio, string responsable)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
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
                _mpp.Planificar(idOrden, idLinea, fechaInicio, responsable);
                linea.Disponible = false;
                _lineasMpp.Modificar(linea);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo planificar la orden.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{orden.NumeroOrden} planificada (línea #{idLinea})", SER.ModuloBitacora.Produccion);
        }

        public void CambiarEstado(string idOrden, EstadoOrdenProduccion06AV estado)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
            try { _mpp.CambiarEstado(idOrden, estado); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cambiar el estado.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{orden.NumeroOrden} → estado {estado}", SER.ModuloBitacora.Produccion);
        }

        public void IniciarEnsamblaje(string idOrden)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Planificada)
                throw new ValidacionException06AV("estado", "Solo se puede iniciar el ensamblaje de una orden Planificada.");
            CambiarEstado(idOrden, EstadoOrdenProduccion06AV.EnEnsamblaje);
        }

        public void Finalizar(string idOrden)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.EnEnsamblaje)
                throw new ValidacionException06AV("estado", "Solo se puede finalizar una orden En ensamblaje.");
            CambiarEstado(idOrden, EstadoOrdenProduccion06AV.Finalizada);
        }

        // ── Paso 5: entregar (saldo final) ───────────────────────
        /// <summary>
        /// Registra el saldo final sobre la computadora, marca la orden Entregada y libera la línea.
        /// La factura de venta se emite luego con <see cref="EmitirFacturaVenta"/>.
        /// </summary>
        public decimal Entregar(string idOrden)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
            if (orden.Estado == EstadoOrdenProduccion06AV.Entregada)
                throw new ValidacionException06AV("estado", "La orden ya fue entregada.");
            if (orden.Estado != EstadoOrdenProduccion06AV.Finalizada)
                throw new ValidacionException06AV("estado", "Solo se puede entregar una orden Finalizada.");

            decimal saldo = orden.SaldoPendiente;
            try
            {
                if (saldo > 0)
                {
                    var pago = new Pago06AV
                    {
                        Computadora = orden.Computadora,
                        Tipo = TipoPago06AV.SaldoFinal,
                        Monto = saldo,
                        Fecha = DateTime.Now
                    };
                    _mpp.GuardarPago(pago);
                }

                _mpp.CambiarEstado(idOrden, EstadoOrdenProduccion06AV.Entregada);

                if (orden.LineaEnsamblaje != null)
                {
                    orden.LineaEnsamblaje.Disponible = true;
                    _lineasMpp.Modificar(orden.LineaEnsamblaje);
                }
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cerrar la entrega.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{orden.NumeroOrden} entregada (saldo ${saldo:0.00})", SER.ModuloBitacora.Produccion);
            return saldo;
        }

        // ── Paso 6: emitir factura de venta ──────────────────────
        /// <summary>
        /// Emite la factura de venta. Solo si la orden está Entregada y el saldo es 0.
        /// El total es un SNAPSHOT del precio de la orden al momento de emitir.
        /// </summary>
        public FacturaVenta06AV EmitirFacturaVenta(string idOrden)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
            if (orden.Estado != EstadoOrdenProduccion06AV.Entregada)
                throw new ValidacionException06AV("estado", "La factura de venta solo se emite sobre una orden Entregada.");
            if (orden.SaldoPendiente != 0)
                throw new ValidacionException06AV("saldo", "No se puede facturar: la orden tiene saldo pendiente.");
            if (_mpp.ObtenerFacturasVenta().Any(f => f.NumeroOrden == orden.Id))
                throw new ValidacionException06AV("factura", "La factura de venta de esta orden ya fue emitida.");

            var factura = new FacturaVenta06AV
            {
                NumeroOrden = orden.Id,
                FechaEmision = DateTime.Now,
                Total = orden.PrecioTotal   // snapshot
            };

            try { _mpp.AgregarFacturaVenta(factura); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo emitir la factura de venta.", ex); }

            AuditoriaPcFactory06AV.Alta($"Factura de venta {factura.NumeroFactura} (OP #{orden.NumeroOrden})", SER.ModuloBitacora.Ventas);
            return factura;
        }

        // ── Retroceso de estado ──────────────────────────────────
        public EstadoOrdenProduccion06AV VolverAtras(string idOrden)
        {
            var orden = ObtenerOrdenOExcepcion(idOrden);
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
                _mpp.CambiarEstado(idOrden, anterior);
                if (anterior == EstadoOrdenProduccion06AV.Pendiente && orden.LineaEnsamblaje != null)
                {
                    orden.LineaEnsamblaje.Disponible = true;
                    _lineasMpp.Modificar(orden.LineaEnsamblaje);
                }
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo volver al paso anterior.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden #{orden.NumeroOrden} vuelta a estado {anterior}", SER.ModuloBitacora.Produccion);
            return anterior;
        }

        // ── Helpers ──────────────────────────────────────────────
        private void RestaurarStock(IEnumerable<KeyValuePair<string, int>> descontados)
        {
            foreach (var d in descontados)
            {
                try { _componentesMpp.SumarStock(d.Key, d.Value); }
                catch { }
            }
        }

        private OrdenProduccion06AV ObtenerOrdenOExcepcion(string idOrden)
        {
            var orden = _mpp.ObtenerPorId(idOrden);
            if (orden == null)
                throw new NoEncontradoException06AV($"No existe la orden de producción '{idOrden}'.");
            return orden;
        }

        private Computadora06AV ObtenerComputadoraOExcepcion(string idComputadora)
        {
            var pc = _mpp.ObtenerComputadora(idComputadora);
            if (pc == null)
                throw new NoEncontradoException06AV($"No existe la computadora '{idComputadora}'.");
            return pc;
        }
    }
}
