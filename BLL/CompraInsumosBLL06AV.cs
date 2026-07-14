using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class CompraInsumosBLL06AV
    {
        private readonly ComprasMPP06AV _mpp = new ComprasMPP06AV();
        private readonly ProveedoresMPP06AV _proveedores = new ProveedoresMPP06AV();
        private readonly InsumosMPP06AV _insumos = new InsumosMPP06AV();

        /// <summary>Insumos bajo stock, para que el repositor arme la orden de compra.</summary>
        public List<Insumo06AV> ObtenerFaltantes()
        {
            try { return _insumos.ObtenerBajoStock(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los faltantes.", ex); }
        }

        public List<OrdenCompra06AV> ObtenerOrdenesCompra()
        {
            try { return _mpp.ObtenerOrdenesCompra(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las órdenes de compra.", ex); }
        }

        public List<PedidoCotizacion06AV> ObtenerCotizaciones()
        {
            try { return _mpp.ObtenerCotizaciones(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las cotizaciones.", ex); }
        }

        /// <summary>Paso 1: registra la orden de compra de insumos faltantes (estado Pendiente).</summary>
        public OrdenCompra06AV RegistrarOrdenCompra(List<DetalleInsumo06AV> faltantes, DateTime fechaLimite, string repositor)
        {
            if (faltantes == null || faltantes.Count == 0)
                throw new ValidacionException06AV("faltantes", "La orden debe incluir al menos un insumo.");
            foreach (var d in faltantes)
            {
                if (d.Insumo == null || string.IsNullOrWhiteSpace(d.Insumo.Codigo))
                    throw new ValidacionException06AV("insumo", "Hay un insumo inválido en el detalle.");
                if (d.Cantidad <= 0)
                    throw new ValidacionException06AV("cantidad", $"La cantidad de '{d.Insumo.Codigo}' debe ser mayor a cero.");
            }
            if (fechaLimite.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaLimite", "La fecha límite no puede ser anterior a hoy.");

            // Regla: un insumo no puede estar en dos órdenes de compra en curso a la vez.
            // Se bloquea si ya figura en una orden que todavía no fue recibida (Pendiente o Enviada).
            var enTramite = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var abierta in _mpp.ObtenerOrdenesCompra()
                                        .Where(o => o.Estado != EstadoOrdenCompra06AV.Finalizada))
                foreach (var d in abierta.InsumosFaltantes)
                    if (d.Insumo != null && !enTramite.ContainsKey(d.Insumo.Codigo))
                        enTramite[d.Insumo.Codigo] = abierta.NumeroCompra;

            var conflictivos = faltantes
                .Where(d => d.Insumo != null && enTramite.ContainsKey(d.Insumo.Codigo))
                .Select(d => $"{d.Insumo.Codigo} (OC #{enTramite[d.Insumo.Codigo]})")
                .Distinct()
                .ToList();

            if (conflictivos.Count > 0)
                throw new ValidacionException06AV("insumo",
                    "Estos insumos ya están en una orden de compra en curso: " +
                    string.Join(", ", conflictivos) +
                    ". Recibí esa orden antes de volver a pedirlos.");

            var oc = new OrdenCompra06AV
            {
                InsumosFaltantes = faltantes,
                FechaLimite = fechaLimite,
                RepositorSolicitante = repositor,
                Estado = EstadoOrdenCompra06AV.Pendiente
            };

            try { _mpp.AgregarOrdenCompra(oc); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la orden de compra.", ex); }

            AuditoriaPcFactory06AV.Alta($"Orden de compra ({faltantes.Count} insumo/s)", ModuloBitacora.Compras);
            return oc;
        }

        /// <summary>Paso 3: registra el pedido de cotización a un proveedor (estado Por aprobar).</summary>
        public PedidoCotizacion06AV RegistrarCotizacion(int numeroCompra, int idProveedor, decimal costo, string condiciones)
        {
            var oc = _mpp.ObtenerOrdenCompraPorNumero(numeroCompra);
            if (oc == null)
                throw new NoEncontradoException06AV($"No existe la orden de compra #{numeroCompra}.");

            var proveedor = _proveedores.ObtenerPorId(idProveedor);
            if (proveedor == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{idProveedor}.");

            if (costo < 0)
                throw new ValidacionException06AV("costo", "El costo de la cotización no puede ser negativo.");

            var cot = new PedidoCotizacion06AV
            {
                NumeroCompra = numeroCompra,
                Proveedor = proveedor,
                Estado = EstadoCotizacion06AV.PorAprobar,
                Costo = costo,
                Condiciones = condiciones
            };

            try { _mpp.AgregarCotizacion(cot); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la cotización.", ex); }

            AuditoriaPcFactory06AV.Alta($"Cotización a proveedor #{idProveedor} (compra #{numeroCompra})", ModuloBitacora.Compras);
            return cot;
        }

        /// <summary>Paso 4: el gerente aprueba la cotización y la orden de compra queda Enviada.</summary>
        public void AprobarCotizacion(int numeroCotizacion)
        {
            var cot = ObtenerCotizacionOExcepcion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede aprobar una cotización 'Por aprobar'.");

            try
            {
                _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Aprobado);
                _mpp.CambiarEstadoOrdenCompra(cot.NumeroCompra, EstadoOrdenCompra06AV.Enviada);
                AuditoriaPcFactory06AV.Modificacion($"Cotización #{numeroCotizacion} aprobada", ModuloBitacora.Compras);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo aprobar la cotización.", ex); }
        }

        /// <summary>Paso 4 (alt.): el gerente desaprueba; el repositor deberá cotizar de nuevo.</summary>
        public void DesaprobarCotizacion(int numeroCotizacion)
        {
            var cot = ObtenerCotizacionOExcepcion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede desaprobar una cotización 'Por aprobar'.");

            try { _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Desaprobada); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo desaprobar la cotización.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cotización #{numeroCotizacion} desaprobada", ModuloBitacora.Compras);
        }

        /// <summary>Paso 5: al recibir los insumos se suma el stock y la orden queda Finalizada.</summary>
        public void RecibirInsumos(int numeroCompra)
        {
            var oc = _mpp.ObtenerOrdenCompraPorNumero(numeroCompra);
            if (oc == null)
                throw new NoEncontradoException06AV($"No existe la orden de compra #{numeroCompra}.");
            if (oc.Estado != EstadoOrdenCompra06AV.Enviada)
                throw new ValidacionException06AV("estado",
                    "Solo se pueden recibir insumos de una orden Enviada (con cotización aprobada).");

            try
            {
                foreach (DetalleInsumo06AV d in oc.InsumosFaltantes)
                    _mpp.SumarStock(d.Insumo.Codigo, d.Cantidad);

                _mpp.FinalizarOrdenCompra(numeroCompra);
                AuditoriaPcFactory06AV.Modificacion($"Recepción de insumos (orden de compra #{numeroCompra})", ModuloBitacora.Compras);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la recepción.", ex); }
        }

        private PedidoCotizacion06AV ObtenerCotizacionOExcepcion(int numero)
        {
            var cot = _mpp.ObtenerCotizacionPorNumero(numero);
            if (cot == null)
                throw new NoEncontradoException06AV($"No existe la cotización #{numero}.");
            return cot;
        }
    }
}
