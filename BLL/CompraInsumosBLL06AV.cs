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
        private readonly ComponentesMPP06AV _componentes = new ComponentesMPP06AV();

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
            foreach (var abierta in _mpp.ObtenerOrdenesCompra().Where(o => o.EnCurso))
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

        public const decimal PrecioUnitarioMaximo = 99999999.99m;

        public PedidoCotizacion06AV RegistrarCotizacion(string idOrdenCompra, int idProveedor,
                                                        IList<DetalleComponente06AV> precios, string condiciones)
        {
            ExigirPatente(PatenteEnum06AV.RegistrarOrdenCompra, "registrar una cotización");
            var oc = BuscarOrden(idOrdenCompra);
            var proveedor = _proveedores.ObtenerPorId(idProveedor);
            if (proveedor == null)
                throw new NoEncontradoException06AV($"No existe el proveedor #{idProveedor}.");
            if (!proveedor.Activo)
                throw new ValidacionException06AV("proveedor", $"{proveedor.Nombre} está dado de baja: reactivalo antes de pedirle una cotización.");
            if (oc.Estado != EstadoOrdenCompra06AV.Pendiente)
                throw new ValidacionException06AV("estado", $"La orden #{oc.NumeroCompra} ya está adjudicada: no recibe más ofertas.");
            // Mientras su oferta siga abierta, un proveedor no cotiza dos veces la misma orden.
            if (_mpp.ObtenerCotizacionesPorOrden(oc.Id).Any(c => c.Proveedor?.Id == idProveedor &&
                                                                 c.Estado == EstadoCotizacion06AV.PorAprobar))
                throw new ValidacionException06AV("proveedor", $"{proveedor.Nombre} ya tiene una oferta abierta para esta orden.");

            List<DetalleComponente06AV> lineas = ArmarLineasCotizacion(oc, precios);

            var cot = new PedidoCotizacion06AV
            {
                NumeroCompra = oc.Id,
                ComponentesPedidos = lineas,
                Proveedor = proveedor,
                Estado = EstadoCotizacion06AV.PorAprobar,
                Costo = lineas.Sum(d => d.Subtotal),
                Condiciones = condiciones
            };

            try { _mpp.AgregarCotizacion(cot); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar la cotización.", ex); }

            AuditoriaPcFactory06AV.Alta($"Cotización {cot.Numero} → proveedor {proveedor.Nombre} (OC #{oc.NumeroCompra})", ModuloBitacora.Compras);
            return cot;
        }

        public PedidoCotizacion06AV AprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            ExigirPatente(PatenteEnum06AV.AprobarCotizacion, "aprobar una cotización");
            if (gerente == null)
                throw new ValidacionException06AV("usuario", "Se requiere un usuario autenticado para aprobar una cotización.");
            var cot = BuscarCotizacion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede aprobar una cotización 'Por aprobar'.");
            var orden = BuscarOrden(cot.NumeroCompra);
            if (orden.Estado != EstadoOrdenCompra06AV.Pendiente)
                throw new ValidacionException06AV("estado", $"La orden #{orden.NumeroCompra} ya no está esperando cotizaciones.");

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
            cot.Estado = EstadoCotizacion06AV.Aprobado;
            cot.GerenteAprobador = gerente;
            return cot;
        }

        public void DesaprobarCotizacion(string numeroCotizacion, Usuario06AV gerente)
        {
            ExigirPatente(PatenteEnum06AV.AprobarCotizacion, "desaprobar una cotización");
            if (gerente == null)
                throw new ValidacionException06AV("usuario", "Se requiere un usuario autenticado para desaprobar una cotización.");
            var cot = BuscarCotizacion(numeroCotizacion);
            if (cot.Estado != EstadoCotizacion06AV.PorAprobar)
                throw new ValidacionException06AV("estado", "Solo se puede desaprobar una cotización 'Por aprobar'.");

            try { _mpp.CambiarEstadoCotizacion(numeroCotizacion, EstadoCotizacion06AV.Desaprobada, gerente.Dni); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo desaprobar la cotización.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Cotización {numeroCotizacion} desaprobada por {gerente.Login}", ModuloBitacora.Compras);
        }

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

        public const decimal TotalFacturadoMaximo = 9999999999.99m;

        public FacturaCompra06AV RegistrarFacturaCompra(string idOrdenCompra, DateTime fechaEntrega,
                                                        string observaciones,
                                                        List<DetalleComponente06AV> recibidos = null,
                                                        string numeroFacturaProveedor = null,
                                                        decimal? totalFacturado = null)
        {
            ExigirPatente(PatenteEnum06AV.RegistrarOrdenCompra, "registrar una recepción");
            var oc = BuscarOrden(idOrdenCompra);
            if (fechaEntrega.Date > DateTime.Today)
                throw new ValidacionException06AV("FechaEntrega", "La fecha de entrega no puede ser posterior a hoy.");
            if (numeroFacturaProveedor != null && numeroFacturaProveedor.Trim().Length > 50)
                throw new ValidacionException06AV("NumeroFacturaProveedor", "El número de factura del proveedor admite hasta 50 caracteres.");
            if (totalFacturado.HasValue && (totalFacturado.Value < 0 || totalFacturado.Value > TotalFacturadoMaximo ||
                                            decimal.Round(totalFacturado.Value, 2) != totalFacturado.Value))
                throw new ValidacionException06AV("TotalFacturado", "El total facturado debe ser un importe válido, con hasta 2 decimales.");
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
            if (fechaEntrega.Date < cotAprobada.FechaEmision.Date)
                throw new ValidacionException06AV("FechaEntrega",
                    $"La fecha de entrega no puede ser anterior a la cotización ({cotAprobada.FechaEmision:dd/MM/yyyy}).");

            List<DetalleComponente06AV> pendiente = ObtenerPendienteDeRecibir(oc.Id);
            if (pendiente.Count == 0)
                throw new ValidacionException06AV("estado", "Esta orden ya recibió todo lo pedido.");

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

            decimal total = CalcularTotalRecepcion(cotAprobada, oc, recibidos);

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
                NumeroFacturaProveedor = string.IsNullOrWhiteSpace(numeroFacturaProveedor) ? null : numeroFacturaProveedor.Trim(),
                TotalFacturado = totalFacturado,
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
                foreach (DetalleComponente06AV d in sumados)
                    try { _componentes.SumarStock(d.Componente.Codigo, -d.Cantidad); } catch { }
                throw new AccesoDatosException06AV("No se pudo registrar la recepción de la orden.", ex);
            }

            AuditoriaPcFactory06AV.Alta(
                $"Recepción {factura.NumeroFactura} (OC #{oc.NumeroCompra}, {recibidos.Sum(d => d.Cantidad)} u." +
                (completa ? ", completa)" : ", parcial)"),
                ModuloBitacora.Compras);

            return factura;
        }

        public void AnularOrdenCompra(string idOrdenCompra, string motivo, Usuario06AV usuario)
        {
            var oc = BuscarOrden(idOrdenCompra);
            if (oc.Estado == EstadoOrdenCompra06AV.Pendiente)
                ExigirPatente(PatenteEnum06AV.RegistrarOrdenCompra, "anular una orden de compra");
            else if (oc.Estado == EstadoOrdenCompra06AV.Enviada)
                ExigirPatente(PatenteEnum06AV.AprobarCotizacion, "anular una orden de compra ya enviada al proveedor");
            else if (oc.Estado == EstadoOrdenCompra06AV.RecibidaParcial)
                throw new ValidacionException06AV("estado",
                    $"La orden #{oc.NumeroCompra} ya recibió mercadería: cerrala con faltante en lugar de anularla.");
            else
                throw new ValidacionException06AV("estado", $"La orden #{oc.NumeroCompra} ya está cerrada.");
            ValidarMotivo(motivo);

            try
            {
                foreach (var cot in _mpp.ObtenerCotizacionesPorOrden(oc.Id).Where(c => c.Estado == EstadoCotizacion06AV.PorAprobar))
                    _mpp.CambiarEstadoCotizacion(cot.Numero, EstadoCotizacion06AV.Desaprobada, usuario?.Dni);
                _mpp.TerminarOrdenCompra(oc.Id, EstadoOrdenCompra06AV.Anulada, DateTime.Now, motivo.Trim());
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo anular la orden de compra.", ex); }

            AuditoriaPcFactory06AV.Baja($"Orden de compra #{oc.NumeroCompra} anulada: {motivo.Trim()}", ModuloBitacora.Compras);
        }

        public void CerrarConFaltante(string idOrdenCompra, string motivo)
        {
            ExigirPatente(PatenteEnum06AV.RegistrarOrdenCompra, "cerrar una orden de compra con faltante");
            var oc = BuscarOrden(idOrdenCompra);
            if (oc.Estado != EstadoOrdenCompra06AV.RecibidaParcial)
                throw new ValidacionException06AV("estado", "Sólo se puede cerrar con faltante una orden Recibida parcial.");
            ValidarMotivo(motivo);

            List<DetalleComponente06AV> pendiente = ObtenerPendienteDeRecibir(oc.Id);
            string detalle = string.Join(", ", pendiente.Select(d => d.Componente.Codigo + " x" + d.Cantidad));
            string texto = motivo.Trim() + (detalle.Length > 0 ? " · No llegó: " + detalle : "");
            if (texto.Length > 300) texto = texto.Substring(0, 297) + "...";

            try { _mpp.TerminarOrdenCompra(oc.Id, EstadoOrdenCompra06AV.CerradaConFaltante, DateTime.Now, texto); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo cerrar la orden de compra.", ex); }

            AuditoriaPcFactory06AV.Modificacion($"Orden de compra #{oc.NumeroCompra} cerrada con faltante: {texto}", ModuloBitacora.Compras);
        }

        private static void ValidarMotivo(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
                throw new ValidacionException06AV("Motivo", "Indicá el motivo (al menos 5 caracteres).");
            if (motivo.Trim().Length > 250)
                throw new ValidacionException06AV("Motivo", "El motivo admite hasta 250 caracteres.");
        }

        public List<FacturaCompra06AV> ObtenerRecepciones(string idOrdenCompra)
        {
            try { return _mpp.ObtenerFacturasPorOrden(idOrdenCompra); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener las recepciones de la orden.", ex); }
        }

        public List<PagoProveedor06AV> ObtenerPagos(string idOrdenCompra = null)
        {
            try
            {
                var pagos = _mpp.ObtenerPagosProveedor();
                return idOrdenCompra == null ? pagos : pagos.Where(p => p.IdOrdenCompra == idOrdenCompra).ToList();
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudieron obtener los pagos a proveedores.", ex); }
        }

        public ResumenPagoProveedor06AV ResumenPagos(OrdenCompra06AV oc, IEnumerable<PagoProveedor06AV> pagos = null)
        {
            if (oc == null) return new ResumenPagoProveedor06AV();
            List<FacturaCompra06AV> facturas = ObtenerRecepciones(oc.Id);
            var propios = (pagos ?? ObtenerPagos(oc.Id)).Where(p => p.IdOrdenCompra == oc.Id).ToList();
            return new ResumenPagoProveedor06AV
            {
                Recepciones = facturas.Count,
                Facturado = facturas.Sum(f => f.TotalAPagar),
                Pagado = propios.Sum(p => p.Monto),
                Pagos = propios
            };
        }

        public PagoProveedor06AV RegistrarPagoProveedor(string idOrdenCompra, decimal monto, DateTime fechaPago,
                                                        FormaPago06AV forma, string referencia, Usuario06AV usuario)
        {
            ExigirPatente(PatenteEnum06AV.PagarProveedores, "registrar un pago a un proveedor");
            var oc = BuscarOrden(idOrdenCompra);
            ResumenPagoProveedor06AV resumen = ResumenPagos(oc);

            if (resumen.Recepciones == 0)
                throw new ValidacionException06AV("estado", $"La orden #{oc.NumeroCompra} todavía no recibió mercadería: no hay nada para pagar.");
            if (monto <= 0 || decimal.Round(monto, 2) != monto)
                throw new ValidacionException06AV("Monto", "El monto debe ser mayor a cero y tener hasta 2 decimales.");
            if (monto > resumen.Saldo)
                throw new ValidacionException06AV("Monto", $"El monto supera el saldo a pagar ({resumen.Saldo:C2}).");
            if (fechaPago.Date > DateTime.Today)
                throw new ValidacionException06AV("FechaPago", "La fecha de pago no puede ser posterior a hoy.");
            if (referencia != null && referencia.Trim().Length > 100)
                throw new ValidacionException06AV("Referencia", "La referencia admite hasta 100 caracteres.");

            var pago = new PagoProveedor06AV
            {
                IdOrdenCompra = oc.Id,
                Monto = monto,
                FechaPago = fechaPago.Date,
                FormaPago = forma,
                Referencia = referencia?.Trim(),
                DniUsuario = usuario?.Dni
            };
            try { _mpp.AgregarPagoProveedor(pago); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo registrar el pago al proveedor.", ex); }

            AuditoriaPcFactory06AV.Alta($"Pago {pago.Numero} a proveedor por {monto:C2} (OC #{oc.NumeroCompra})", ModuloBitacora.Compras);
            return pago;
        }

        public PedidoCotizacion06AV CotizacionAdjudicada(string idOrdenCompra)
        {
            try
            {
                return _mpp.ObtenerCotizacionesPorOrden(idOrdenCompra)
                           .Where(c => c.Estado == EstadoCotizacion06AV.Aprobado)
                           .OrderByDescending(c => c.FechaEmision)
                           .FirstOrDefault();
            }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo obtener la cotización adjudicada.", ex); }
        }

        public OrdenCompra06AV ObtenerOrden(string idONumero) => BuscarOrden(idONumero);

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

        public static List<DetalleComponente06AV> ArmarLineasCotizacion(OrdenCompra06AV oc, IList<DetalleComponente06AV> precios)
        {
            if (precios == null || precios.Count == 0)
                throw new ValidacionException06AV("precios", "Cargá el precio unitario de cada componente de la orden.");

            var porCodigo = new Dictionary<string, DetalleComponente06AV>(StringComparer.OrdinalIgnoreCase);
            foreach (DetalleComponente06AV p in precios)
            {
                string codigo = p?.Componente?.Codigo;
                if (string.IsNullOrWhiteSpace(codigo))
                    throw new ValidacionException06AV("precios", "Hay un precio sin componente.");
                if (porCodigo.ContainsKey(codigo))
                    throw new ValidacionException06AV("precios", $"El componente '{codigo}' tiene dos precios.");
                if (!oc.ComponentesFaltantes.Any(d => string.Equals(d.Componente?.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
                    throw new ValidacionException06AV("precios", $"El componente '{codigo}' no está en la orden #{oc.NumeroCompra}.");
                porCodigo[codigo] = p;
            }

            var lineas = new List<DetalleComponente06AV>();
            foreach (DetalleComponente06AV item in oc.ComponentesFaltantes)
            {
                string nombre = item.Componente?.Descripcion ?? item.Componente?.Codigo;
                if (!porCodigo.TryGetValue(item.Componente.Codigo, out DetalleComponente06AV precio))
                    throw new ValidacionException06AV("precios", $"Falta el precio unitario de '{nombre}'.");
                if (precio.PrecioUnitario <= 0)
                    throw new ValidacionException06AV("precios", $"El precio unitario de '{nombre}' tiene que ser mayor a cero.");
                if (precio.PrecioUnitario > PrecioUnitarioMaximo)
                    throw new ValidacionException06AV("precios", $"El precio unitario de '{nombre}' es demasiado alto.");
                if (decimal.Round(precio.PrecioUnitario, 2) != precio.PrecioUnitario)
                    throw new ValidacionException06AV("precios", $"El precio unitario de '{nombre}' admite hasta 2 decimales.");

                lineas.Add(new DetalleComponente06AV
                {
                    Componente = item.Componente,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precio.PrecioUnitario
                });
            }
            return lineas;
        }

        public static decimal CalcularTotalRecepcion(PedidoCotizacion06AV cotizacion, OrdenCompra06AV oc,
                                                     IList<DetalleComponente06AV> recibidos)
        {
            if (cotizacion.TienePreciosPorItem)
                return recibidos.Sum(d => Math.Round(cotizacion.PrecioDe(d.Componente.Codigo) * d.Cantidad, 2));

            int unidadesOrden = oc.ComponentesFaltantes.Sum(d => d.Cantidad);
            int unidadesRecibidas = recibidos.Sum(d => d.Cantidad);
            return cotizacion.Costo > 0 && unidadesOrden > 0
                ? Math.Round(cotizacion.Costo * unidadesRecibidas / unidadesOrden, 2)
                : recibidos.Sum(d => d.Componente.PrecioUnitario * d.Cantidad);
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
