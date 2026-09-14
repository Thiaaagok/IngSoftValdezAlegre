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

        // ── Autorización ─────────────────────────────────────────
        /// <summary>
        /// Exige que el usuario de la sesión tenga la patente indicada.
        /// La autorización del sistema se maneja SIEMPRE por patentes (patrón
        /// Composite: Rol → Familias → Patentes), no por el nombre del rol.
        /// </summary>
        private static void ExigirPatente(PatenteEnum06AV patente, string accion)
        {
            var usuario = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuario == null)
                throw new ValidacionException06AV("usuario",
                    $"Se requiere un usuario autenticado para {accion}.");

            if (!UsuarioSesion06AV.Instancia().TienePermiso(patente))
                throw new ValidacionException06AV("patente",
                    $"El usuario '{usuario.Login}' no tiene el permiso '{patente}' requerido para {accion}.");
        }

        // ── Paso 1: registrar la orden de compra (patente RegistrarOrdenCompra) ──
        public OrdenCompra06AV RegistrarOrdenCompra(List<DetalleComponente06AV> faltantes,
                                                    DateTime fechaLimite, Usuario06AV repositor)
        {
            ExigirPatente(PatenteEnum06AV.RegistrarOrdenCompra, "registrar una orden de compra");

            if (repositor == null)
                throw new ValidacionException06AV("repositor",
                    "Se requiere un usuario autenticado para registrar una orden de compra.");

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
                                        .Where(o => o.Estado != EstadoOrdenCompra06AV.Finalizada))
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

        // ── Paso 4: aprobar / desaprobar (patente AprobarCotizacion) ─
        public void AprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            ExigirPatente(PatenteEnum06AV.AprobarCotizacion, "aprobar una cotización");
            var cot = BuscarCotizacion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede aprobar una cotización 'Por aprobar'.");

            try
            {
                _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Aprobado, gerente.Dni);

                // Se adjudica a un solo proveedor: el resto de las ofertas abiertas de la
                // misma orden quedan desaprobadas en el mismo acto, para que no queden
                // dos cotizaciones vigentes compitiendo por la misma compra.
                foreach (var otra in _mpp.ObtenerCotizacionesPorOrden(cot.NumeroCompra))
                    if (otra.Numero != numeroCotizacion && otra.Estado == EstadoCotizacion06AV.PorAprobar)
                        _mpp.CambiarEstadoCotizacion(otra.Numero, EstadoCotizacion06AV.Desaprobada, gerente.Dni);

                _mpp.CambiarEstadoOrdenCompra(cot.NumeroCompra, EstadoOrdenCompra06AV.Enviada);
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo aprobar la cotización.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cotización {numeroCotizacion} aprobada por {gerente.Login}", ModuloBitacora.Compras);
        }

        public void DesaprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            ExigirPatente(PatenteEnum06AV.AprobarCotizacion, "desaprobar una cotización");
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
        /// <summary>
        /// Lo que todavía falta recibir de una orden: lo pedido menos lo ya recibido en
        /// recepciones anteriores. Devuelve sólo los componentes con saldo pendiente.
        /// </summary>
        public List<DetalleComponente06AV> ObtenerPendienteDeRecibir(string idOrdenCompra)
        {
            var oc = BuscarOrden(idOrdenCompra);

            var recibido = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (FacturaCompra06AV f in _mpp.ObtenerFacturasPorOrden(oc.Id))
                    foreach (DetalleComponente06AV d in f.ComponentesRecibidos)
                    {
                        if (d.Componente == null) continue;
                        string cod = d.Componente.Codigo;
                        recibido[cod] = (recibido.ContainsKey(cod) ? recibido[cod] : 0) + d.Cantidad;
                    }
            }
            catch (Exception ex)
            {
                throw new AccesoDatosException06AV("No se pudieron leer las recepciones de la orden.", ex);
            }

            var pendiente = new List<DetalleComponente06AV>();
            foreach (DetalleComponente06AV d in oc.ComponentesFaltantes)
            {
                if (d.Componente == null) continue;
                int ya = recibido.ContainsKey(d.Componente.Codigo) ? recibido[d.Componente.Codigo] : 0;
                int falta = d.Cantidad - ya;
                if (falta > 0)
                    pendiente.Add(new DetalleComponente06AV { Componente = d.Componente, Cantidad = falta });
            }
            return pendiente;
        }

        /// <summary>
        /// Paso 5 del RFN2 — RECEPCIÓN. Antes se asumía que llegaba todo lo pedido; ahora
        /// se declara qué llegó realmente, como un control de recepción:
        ///
        ///   · suma al stock ÚNICAMENTE las unidades recibidas;
        ///   · si con esta entrega se completó todo lo pedido, la orden queda Finalizada;
        ///   · si quedó algo sin llegar, la orden pasa a Recibida parcial y el faltante
        ///     queda anotado en las observaciones de la factura, para poder reclamarlo y
        ///     recibirlo después contra la misma orden.
        ///
        /// <paramref name="recibidos"/> null significa "llegó todo lo pendiente".
        /// </summary>
        public FacturaCompra06AV RegistrarFacturaCompra(string idOrdenCompra, DateTime fechaEntrega,
                                                        string observaciones,
                                                        List<DetalleComponente06AV> recibidos = null)
        {
            var oc = BuscarOrden(idOrdenCompra);
            if (oc.Estado != EstadoOrdenCompra06AV.Enviada &&
                oc.Estado != EstadoOrdenCompra06AV.RecibidaParcial)
                throw new ValidacionException06AV("estado",
                    "Solo se puede recibir una orden Enviada o Recibida parcial.");

            var cotAprobada = _mpp.ObtenerCotizacionesPorOrden(oc.Id)
                                  .Where(c => c.Estado == EstadoCotizacion06AV.Aprobado)
                                  .OrderByDescending(c => c.FechaEmision)
                                  .FirstOrDefault();
            if (cotAprobada == null)
                throw new ValidacionException06AV("cotizacion", "La orden no tiene una cotización aprobada.");

            List<DetalleComponente06AV> pendiente = ObtenerPendienteDeRecibir(oc.Id);
            if (pendiente.Count == 0)
                throw new ValidacionException06AV("estado", "Esta orden ya recibió todo lo pedido.");

            // Sin detalle explícito: llegó todo lo que estaba pendiente.
            if (recibidos == null)
                recibidos = pendiente.Select(d => new DetalleComponente06AV
                {
                    Componente = d.Componente,
                    Cantidad = d.Cantidad
                }).ToList();

            recibidos = recibidos.Where(d => d.Componente != null && d.Cantidad > 0).ToList();
            if (recibidos.Count == 0)
                throw new ValidacionException06AV("recibidos",
                    "Marcá al menos un componente recibido para registrar la entrega.");

            // No se puede recibir más de lo que falta: el stock no se infla por un error de carga.
            foreach (DetalleComponente06AV d in recibidos)
            {
                DetalleComponente06AV esperado = pendiente
                    .FirstOrDefault(x => string.Equals(x.Componente.Codigo, d.Componente.Codigo,
                                                       StringComparison.OrdinalIgnoreCase));
                if (esperado == null)
                    throw new ValidacionException06AV("recibidos",
                        $"'{d.Componente.Codigo}' no está pendiente de recepción en esta orden.");
                if (d.Cantidad > esperado.Cantidad)
                    throw new ValidacionException06AV("cantidad",
                        $"De '{d.Componente.Codigo}' faltan {esperado.Cantidad} unidades y se están " +
                        $"declarando {d.Cantidad}.");
            }

            // Qué queda sin llegar DESPUÉS de esta recepción.
            var faltanteFinal = new List<DetalleComponente06AV>();
            foreach (DetalleComponente06AV p in pendiente)
            {
                DetalleComponente06AV llega = recibidos
                    .FirstOrDefault(x => string.Equals(x.Componente.Codigo, p.Componente.Codigo,
                                                       StringComparison.OrdinalIgnoreCase));
                int resto = p.Cantidad - (llega != null ? llega.Cantidad : 0);
                if (resto > 0)
                    faltanteFinal.Add(new DetalleComponente06AV { Componente = p.Componente, Cantidad = resto });
            }

            bool completa = faltanteFinal.Count == 0;

            // El costo cotizado cubre la orden entera: se prorratea por unidades recibidas
            // para que la suma de las recepciones parciales dé el total adjudicado.
            int unidadesOrden = oc.ComponentesFaltantes.Sum(d => d.Cantidad);
            int unidadesRecibidas = recibidos.Sum(d => d.Cantidad);
            decimal total = cotAprobada.Costo > 0 && unidadesOrden > 0
                ? Math.Round(cotAprobada.Costo * unidadesRecibidas / unidadesOrden, 2)
                : recibidos.Sum(d => d.Componente.PrecioUnitario * d.Cantidad);

            string detalleFaltante = completa
                ? string.Empty
                : "Pendiente de recibir: " +
                  string.Join(", ", faltanteFinal.Select(d => d.Componente.Codigo + " x" + d.Cantidad)) + ".";

            var factura = new FacturaCompra06AV
            {
                NumeroCompra = oc.Id,
                FechaEmision = DateTime.Now,
                FechaEntrega = fechaEntrega,
                ComponentesRecibidos = recibidos,
                Total = total,
                Observaciones = string.Join("  ", new[] { observaciones, detalleFaltante }
                                                 .Where(x => !string.IsNullOrWhiteSpace(x)))
            };

            var sumados = new List<DetalleComponente06AV>();
            try
            {
                _mpp.AgregarFacturaCompra(factura);

                foreach (DetalleComponente06AV d in recibidos)
                {
                    _componentes.SumarStock(d.Componente.Codigo, d.Cantidad);
                    sumados.Add(d);
                }

                if (completa) _mpp.CerrarOrdenCompra(oc.Id, fechaEntrega);
                else _mpp.CambiarEstadoOrdenCompra(oc.Id, EstadoOrdenCompra06AV.RecibidaParcial);
            }
            catch (Exception ex)
            {
                // Compensación: si algo falló después de sumar, se devuelve el stock sumado.
                foreach (DetalleComponente06AV d in sumados)
                    try { _componentes.SumarStock(d.Componente.Codigo, -d.Cantidad); } catch { }
                throw new AccesoDatosException06AV("No se pudo registrar la recepción de la orden.", ex);
            }

            AuditoriaPcFactory06AV.Alta(
                $"Recepción {factura.NumeroFactura} (OC #{oc.NumeroCompra}, {unidadesRecibidas} u." +
                (completa ? ", completa)" : ", parcial)"),
                ModuloBitacora.Compras);

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
