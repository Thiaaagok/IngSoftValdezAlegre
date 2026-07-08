using BE;
using BLL.Excepciones;
using MPP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica del proceso de Venta / Producción (RFN1):
    ///   RegistrarOrden → RegistrarSena (50%) → Planificar (línea/fecha/responsable)
    ///   → Entregar (saldo final + cierre). Controla estados y disponibilidad de línea.
    /// </summary>
    public class OrdenProduccionBLL06AV
    {
        private readonly ProduccionMPP06AV _mpp = new ProduccionMPP06AV();
        private readonly LineasEnsamblajeMPP06AV _lineasMpp = new LineasEnsamblajeMPP06AV();

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

            var orden = new OrdenProduccion06AV
            {
                Cliente = cliente,
                Computadora = computadora,
                FechaEntrega = fechaEntrega
            };

            try { _mpp.AgregarOrden(orden); return orden; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la orden.", ex); }
        }

        /// <summary>Paso 3: registra la seña (50% del total). No se permite dos veces.</summary>
        public decimal RegistrarSena(int numeroOrden)
        {
            var orden = ObtenerOrdenOExcepcion(numeroOrden);
            if (orden.Pagos.Any(p => p.Tipo == TipoPago06AV.Sena))
                throw new ValidacionException06AV("sena", "La seña de esta orden ya fue registrada.");

            decimal sena = Math.Round(orden.PrecioTotal * 0.5m, 2);
            try { _mpp.AgregarPago(numeroOrden, TipoPago06AV.Sena, sena); return sena; }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la seña.", ex); }
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
        }

        /// <summary>Cambia el estado de fabricación (p. ej. a EnEnsamblaje o Finalizada).</summary>
        public void CambiarEstado(int numeroOrden, EstadoOrdenProduccion06AV estado)
        {
            ObtenerOrdenOExcepcion(numeroOrden);
            try { _mpp.CambiarEstado(numeroOrden, estado); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cambiar el estado.", ex); }
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
                return saldo;
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cerrar la entrega.", ex); }
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
