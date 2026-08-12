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
    /// Proceso de Compras de componentes (RFN2). Tras el refactor:
    /// el faltante es un Componente06AV (Insumo se fusionó), la orden de compra la
    /// registra un Repositor, la aprueba un Gerente de Compras, y la recepción se
    /// documenta con una Factura de Compra que suma stock y cierra la orden.
    /// </summary>
    public class CompraInsumosBLL06AV
    {
        private readonly ComprasMPP06AV _mpp = new ComprasMPP06AV();
        private readonly ProveedoresMPP06AV _proveedores = new ProveedoresMPP06AV();
        private readonly ComponentesMPP06AV _componentes = new ComponentesMPP06AV();

        // ── Consultas ────────────────────────────────────────────
        public List<Componente06AV> ObtenerFaltantes()
        {
            try { return _componentes.ObtenerBajoStock(); }
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

        public List<PedidoCotizacion06AV> ObtenerCotizacionesPorOrden(string idOrdenCompra)
        {
            try { return _mpp.ObtenerCotizacionesPorOrden(idOrdenCompra); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las cotizaciones de la orden.", ex); }
        }

        // ── Paso 1: registrar la orden de compra (rol Repositor) ──
        public OrdenCompra06AV RegistrarOrdenCompra(List<DetalleComponente06AV> faltantes,
                                                    DateTime fechaLimite, Usuario06AV repositor)
        {
            RolNegocio06AV.Exigir(repositor, RolUsuario06AV.Repositor, "registrar una orden de compra");

            if (faltantes == null || faltantes.Count == 0)
                throw new ValidacionException06AV("faltantes", "La orden debe incluir al menos un componente.");
            foreach (var d in faltantes)
            {
                if (d.Componente == null || string.IsNullOrWhiteSpace(d.Componente.Codigo))
                    throw new ValidacionException06AV("componente", "Hay un componente inválido en el detalle.");
                if (d.Cantidad <= 0)
                    throw new ValidacionException06AV("cantidad", $"La cantidad de '{d.Componente.Codigo}' debe ser mayor a cero.");
            }
            if (fechaLimite.Date < DateTime.Today)
                throw new ValidacionException06AV("FechaLimite", "La fecha límite no puede ser anterior a hoy.");

            // RFN2: un componente no puede estar en dos órdenes de compra EN CURSO a la vez.
            // "En curso" = todavía no recibida: se lista de forma EXPLÍCITA por estado
            // (Pendiente o Enviada), no por negación de Finalizada.
            var enTramite = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var abierta in _mpp.ObtenerOrdenesCompra()
                                        .Where(o => o.Estado == EstadoOrdenCompra06AV.Pendiente
                                                 || o.Estado == EstadoOrdenCompra06AV.Enviada))
                foreach (var d in abierta.ComponentesFaltantes)
                    if (d.Componente != null && !enTramite.ContainsKey(d.Componente.Codigo))
                        enTramite[d.Componente.Codigo] = abierta.NumeroCompra;

            var conflictivos = faltantes
                .Where(d => d.Componente != null && enTramite.ContainsKey(d.Componente.Codigo))
                .Select(d => $"{d.Componente.Codigo} (OC #{enTramite[d.Componente.Codigo]})")
                .Distinct()
                .ToList();

            if (conflictivos.Count > 0)
                throw new ValidacionException06AV("componente",
                    "Estos componentes ya están en una orden de compra en curso: " +
                    string.Join(", ", conflictivos) +
                    ". Recibí esa orden antes de volver a pedirlos.");

            var oc = new OrdenCompra06AV
            {
                ComponentesFaltantes = faltantes,
                FechaLimite = fechaLimite,
                RepositorSolicitante = repositor,
                Estado = EstadoOrdenCompra06AV.Pendiente
            };

            try { _mpp.AgregarOrdenCompra(oc); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la orden de compra.", ex); }

            AuditoriaPcFactory06AV.Alta($"Orden de compra #{oc.NumeroCompra} ({faltantes.Count} comp.)", ModuloBitacora.Compras);
            return oc;
        }

        // ── Paso 3: registrar cotización a un proveedor ──────────
        public PedidoCotizacion06AV RegistrarCotizacion(string idOrdenCompra, int idProveedor,
                                                        decimal costo, string condiciones)
        {
            var oc = BuscarOrden(idOrdenCompra);
            var proveedor = _proveedores.ObtenerPorId(idProveedor);
            if (proveedor == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{idProveedor}.");
            if (costo < 0)
                throw new ValidacionException06AV("costo", "El costo de la cotización no puede ser negativo.");

            var cot = new PedidoCotizacion06AV
            {
                NumeroCompra = oc.Id,
                ComponentesPedidos = oc.ComponentesFaltantes,
                Proveedor = proveedor,
                Estado = EstadoCotizacion06AV.PorAprobar,
                Costo = costo,
                Condiciones = condiciones
            };

            try { _mpp.AgregarCotizacion(cot); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la cotización.", ex); }

            AuditoriaPcFactory06AV.Alta($"Cotización {cot.Numero} → proveedor {proveedor.Nombre} (OC #{oc.NumeroCompra})", ModuloBitacora.Compras);
            return cot;
        }

        // ── Paso 4: aprobar / desaprobar (rol Gerente de Compras) ─
        public void AprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            RolNegocio06AV.Exigir(gerente, RolUsuario06AV.GerenteCompras, "aprobar una cotización");
            var cot = BuscarCotizacion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede aprobar una cotización 'Por aprobar'.");

            try
            {
                _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Aprobado, gerente.Dni);
                _mpp.CambiarEstadoOrdenCompra(cot.NumeroCompra, EstadoOrdenCompra06AV.Enviada);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo aprobar la cotización.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cotización {numeroCotizacion} aprobada por {gerente.Login}", ModuloBitacora.Compras);
        }

        public void DesaprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            RolNegocio06AV.Exigir(gerente, RolUsuario06AV.GerenteCompras, "desaprobar una cotización");
            var cot = BuscarCotizacion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede desaprobar una cotización 'Por aprobar'.");

            try { _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Desaprobada, gerente.Dni); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo desaprobar la cotización.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cotización {numeroCotizacion} desaprobada por {gerente.Login}", ModuloBitacora.Compras);
        }

        // ── Paso 5: recibir mercadería = Factura de Compra ───────
        /// <summary>
        /// Registra la factura de compra (recepción real): suma el stock de lo recibido,
        /// deja la orden Finalizada y guarda la fecha de cierre = fecha de entrega.
        /// El proveedor y el total se navegan desde la cotización aprobada de la OC.
        /// </summary>
        public FacturaCompra06AV RegistrarFacturaCompra(string idOrdenCompra, DateTime fechaEntrega, string observaciones)
        {
            var oc = BuscarOrden(idOrdenCompra);
            if (oc.Estado != EstadoOrdenCompra06AV.Enviada)
                throw new ValidacionException06AV("estado",
                    "Solo se puede facturar/recibir una orden Enviada (con cotización aprobada).");

            var cotAprobada = _mpp.ObtenerCotizacionesPorOrden(oc.Id)
                                  .Where(c => c.Estado == EstadoCotizacion06AV.Aprobado)
                                  .OrderByDescending(c => c.FechaEmision)
                                  .FirstOrDefault();
            if (cotAprobada == null)
                throw new ValidacionException06AV("cotizacion", "La orden no tiene una cotización aprobada.");

            var recibidos = oc.ComponentesFaltantes;
            decimal total = cotAprobada.Costo > 0
                ? cotAprobada.Costo
                : recibidos.Sum(d => d.Componente.PrecioUnitario * d.Cantidad);

            var factura = new FacturaCompra06AV
            {
                NumeroCompra = oc.Id,
                FechaEmision = DateTime.Now,
                FechaEntrega = fechaEntrega,
                ComponentesRecibidos = recibidos,
                Total = total,
                Observaciones = observaciones
            };

            try
            {
                _mpp.AgregarFacturaCompra(factura);
                foreach (DetalleComponente06AV d in recibidos)
                    _componentes.SumarStock(d.Componente.Codigo, d.Cantidad);
                _mpp.CerrarOrdenCompra(oc.Id, fechaEntrega);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la factura de compra.", ex); }

            AuditoriaPcFactory06AV.Alta($"Factura de compra {factura.NumeroFactura} (OC #{oc.NumeroCompra})", ModuloBitacora.Compras);
            return factura;
        }

        // ── Helpers ──────────────────────────────────────────────
        /// <summary>Busca una OC por su Id técnico o por su NumeroCompra de negocio.</summary>
        private OrdenCompra06AV BuscarOrden(string idONumero)
        {
            if (string.IsNullOrWhiteSpace(idONumero))
                throw new ValidacionException06AV("orden", "Debe indicarse la orden de compra.");
            var todas = _mpp.ObtenerOrdenesCompra();
            var oc = todas.FirstOrDefault(o => o.Id == idONumero)
                  ?? todas.FirstOrDefault(o => o.NumeroCompra.ToString() == idONumero);
            if (oc == null)
                throw new NoEncontradoException06AV($"No existe la orden de compra '{idONumero}'.");
            return oc;
        }

        private PedidoCotizacion06AV BuscarCotizacion(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ValidacionException06AV("cotizacion", "Debe indicarse la cotización.");
            var cot = _mpp.ObtenerCotizaciones().FirstOrDefault(c => c.Numero == numero);
            if (cot == null)
                throw new NoEncontradoException06AV($"No existe la cotización '{numero}'.");
            return cot;
        }
    }
}
