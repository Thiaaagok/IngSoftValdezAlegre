using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ComprasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly ProveedoresBLL06AV _proveedoresBLL = new ProveedoresBLL06AV();

        private List<OrdenCompra06AV> _ordenes = new List<OrdenCompra06AV>();
        private List<PedidoCotizacion06AV> _cotizaciones = new List<PedidoCotizacion06AV>();
        private List<PagoProveedor06AV> _pagos = new List<PagoProveedor06AV>();

        private OrdenCompra06AV _ocSel;

        private enum Paso { Cotizar, Aprobar, Recibir, Finalizada, Cerrada }

        private Panel pnlLista;
        private Label lblTitulo, lblBuscar;
        private TextBox txtBuscar;
        private ComboBox cboEstado;
        private Button btnActualizar, btnNueva;
        private DataGridView grOC;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        private Panel pnlForm;
        private AsistenteCompraControl06AV asistente;

        private Panel pnlCotizacion;
        private PedidoCotizacionControl06AV cotizador;

        private Panel pnlRecepcion;
        private RecepcionControl06AV recepcion;

        public ComprasControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarLista();
            CargarOrdenes();
        }

        private static bool Puede(PatenteEnum06AV patente)
        {
            try { return UsuarioSesion06AV.Instancia().TienePermiso(patente); }
            catch { return false; }
        }

        private void ConstruirUI()
        {
            ConstruirVistaLista();
            ConstruirVistaFormulario();

            Controls.Add(pnlLista);
            Controls.Add(pnlForm);
            Controls.Add(pnlCotizacion);
            Controls.Add(pnlRecepcion);
        }

        private void ConstruirVistaLista()
        {
            lblTitulo = new Label { AutoSize = true, Location = new Point(4, 14) };

            lblBuscar = new Label { AutoSize = true, Margin = new Padding(0, 8, 4, 0) };
            txtBuscar = new TextBox { Width = 170, Margin = new Padding(0, 4, 8, 0) };
            txtBuscar.TextChanged += (s, e) => RefrescarGrilla();
            cboEstado = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 4, 0) };
            cboEstado.SelectedIndexChanged += (s, e) => RefrescarGrilla();

            btnActualizar = NuevoBoton(120);
            btnNueva = NuevoBoton(210);
            btnActualizar.Click += (s, e) => CargarOrdenes();
            btnNueva.Click += (s, e) => AbrirFormularioNueva();
            btnNueva.Visible = Puede(PatenteEnum06AV.RegistrarOrdenCompra);

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { lblBuscar, txtBuscar, cboEstado, btnActualizar, btnNueva });

            var barraSup = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraSup.Controls.Add(lblTitulo);
            barraSup.Controls.Add(flpAcciones);

            grOC = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grOC.SelectionChanged += (s, e) => ActualizarDetalle();
            grOC.DataBindingComplete += (s, e) => FormatearGrillaOrdenes();

            lblDetTitulo = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 26, Padding = new Padding(0, 2, 0, 0) };
            lblDetEstado = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 24, Padding = new Padding(0, 0, 0, 6) };
            flpDetalle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true
            };
            pnlDetalle = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(16, 14, 12, 12) };
            pnlDetalle.Controls.Add(flpDetalle);
            pnlDetalle.Controls.Add(lblDetEstado);
            pnlDetalle.Controls.Add(lblDetTitulo);

            pnlLista = new Panel { Dock = DockStyle.Fill };
            pnlLista.Controls.Add(grOC);
            pnlLista.Controls.Add(pnlDetalle);
            pnlLista.Controls.Add(barraSup);
        }

        private void ConstruirVistaFormulario()
        {
            asistente = new AsistenteCompraControl06AV { Dock = DockStyle.Fill };
            asistente.Cancelado += (s, e) => MostrarLista();
            asistente.Confirmado += (s, orden) => CrearOrden(orden);

            pnlForm = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlForm.Controls.Add(asistente);

            cotizador = new PedidoCotizacionControl06AV { Dock = DockStyle.Fill };
            cotizador.Cancelado += (s, e) => MostrarLista();
            cotizador.Confirmado += (s, c) => RegistrarCotizacion(c);
            cotizador.NuevoProveedor += (s, e) => AltaProveedorDesdeCotizador();

            pnlCotizacion = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlCotizacion.Controls.Add(cotizador);

            recepcion = new RecepcionControl06AV { Dock = DockStyle.Fill };
            recepcion.Cancelado += (s, e) => MostrarLista();
            recepcion.Confirmado += (s, r) => ConfirmarRecepcion(r);

            pnlRecepcion = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlRecepcion.Controls.Add(recepcion);
        }

        private static Button NuevoBoton(int width) =>
            new Button { Width = width, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private void MostrarLista()
        {
            pnlForm.Visible = false;
            pnlCotizacion.Visible = false;
            pnlRecepcion.Visible = false;
            pnlLista.Visible = true;
            pnlLista.BringToFront();
        }

        private void AbrirFormularioNueva()
        {
            CargarFaltantes();
            pnlRecepcion.Visible = false;
            pnlCotizacion.Visible = false;
            pnlLista.Visible = false;
            pnlForm.Visible = true;
            pnlForm.BringToFront();
        }

        #region Datos y grilla

        private void CargarOrdenes()
        {
            try
            {
                _ordenes = _bll.ObtenerOrdenesCompra() ?? new List<OrdenCompra06AV>();
                _cotizaciones = _bll.ObtenerCotizaciones() ?? new List<PedidoCotizacion06AV>();
                try { _pagos = _bll.ObtenerPagos() ?? new List<PagoProveedor06AV>(); }
                catch { _pagos = new List<PagoProveedor06AV>(); }
                RefrescarGrilla();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void RefrescarGrilla()
        {
            if (grOC == null || cboEstado == null) return;
            int? seleccion = (grOC.CurrentRow?.DataBoundItem as OrdenVm)?.Numero;
            string filtro = (txtBuscar.Text ?? "").Trim();

            var vm = _ordenes
                .OrderByDescending(o => o.NumeroCompra)
                .Select(CrearVm)
                .Where(v => PasaEstado(v) && PasaBusqueda(v, filtro))
                .ToList();

            grOC.DataSource = null;
            grOC.DataSource = vm;

            if (seleccion.HasValue) SeleccionarOrden(seleccion.Value);
            ActualizarDetalle();
        }

        private bool PasaEstado(OrdenVm v)
        {
            switch (cboEstado.SelectedIndex)
            {
                case 1: return v.Orden.EnCurso;
                case 2: return v.Paso == Paso.Cotizar;
                case 3: return v.Paso == Paso.Aprobar;
                case 4: return v.Paso == Paso.Recibir;
                case 5: return v.Atrasada;
                case 6: return v.Orden.Cerrada;
                default: return true;
            }
        }

        private static bool PasaBusqueda(OrdenVm v, string filtro)
        {
            if (filtro.Length == 0) return true;
            string f = filtro.TrimStart('#');
            return v.Numero.ToString().Contains(f)
                || (v.Proveedor ?? "").IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0
                || (v.Repositor ?? "").IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private OrdenVm CrearVm(OrdenCompra06AV oc)
        {
            var t = GestorIdioma06AV.Instancia;
            var cot = CotizacionDe(oc);
            EstadoDe(oc, cot, out string texto, out Color color, out Paso paso);
            int atraso = oc.EnCurso ? (DateTime.Today - oc.FechaLimite.Date).Days : 0;
            bool adjudicada = cot != null && cot.Estado == EstadoCotizacion06AV.Aprobado;
            return new OrdenVm
            {
                Numero = oc.NumeroCompra,
                Proveedor = adjudicada ? cot.Proveedor?.Nombre ?? "" : "",
                Repositor = oc.RepositorSolicitante?.Login ?? "",
                Limite = oc.FechaLimite.ToShortDateString(),
                Estado = texto,
                Costo = cot != null && cot.Estado != EstadoCotizacion06AV.Desaprobada ? cot.Costo.ToString("C0") : "—",
                Atraso = atraso > 0 ? t.Obtener("pcf_oc_atraso_dias", atraso) : "",
                EstadoColor = color,
                Atrasada = atraso > 0,
                Paso = paso,
                Orden = oc,
                Cotizacion = cot
            };
        }

        private PedidoCotizacion06AV CotizacionDe(OrdenCompra06AV oc)
        {
            var propias = _cotizaciones.Where(c => c.NumeroCompra == oc.Id).ToList();
            return propias.Where(c => c.Estado == EstadoCotizacion06AV.Aprobado).OrderByDescending(c => c.FechaEmision).FirstOrDefault()
                ?? propias.Where(c => c.Estado == EstadoCotizacion06AV.PorAprobar).OrderBy(c => c.Costo).FirstOrDefault()
                ?? propias.OrderByDescending(c => c.FechaEmision).FirstOrDefault();
        }

        private int OfertasAbiertas(OrdenCompra06AV oc) =>
            _cotizaciones.Count(c => c.NumeroCompra == oc.Id && c.Estado == EstadoCotizacion06AV.PorAprobar);

        private void EstadoDe(OrdenCompra06AV oc, PedidoCotizacion06AV cot,
                              out string texto, out Color color, out Paso paso)
        {
            var t = GestorIdioma06AV.Instancia;

            switch (oc.Estado)
            {
                case EstadoOrdenCompra06AV.Finalizada:
                    texto = t.Obtener("pcf_est_finalizada"); color = Tema.Exito; paso = Paso.Finalizada; return;
                case EstadoOrdenCompra06AV.Anulada:
                    texto = t.Obtener("pcf_oc_est_Anulada"); color = Tema.TextoSuave; paso = Paso.Cerrada; return;
                case EstadoOrdenCompra06AV.CerradaConFaltante:
                    texto = t.Obtener("pcf_oc_est_CerradaConFaltante"); color = Tema.Peligro; paso = Paso.Cerrada; return;
                case EstadoOrdenCompra06AV.RecibidaParcial:
                    texto = t.Obtener("pcf_est_parcial"); color = Tema.Advertencia; paso = Paso.Recibir; return;
                case EstadoOrdenCompra06AV.Enviada:
                    texto = t.Obtener("pcf_est_enviada"); color = Tema.Acento; paso = Paso.Recibir; return;
            }
            if (cot != null && cot.Estado == EstadoCotizacion06AV.PorAprobar)
            {
                texto = t.Obtener("pcf_est_por_aprobar"); color = Tema.Primario; paso = Paso.Aprobar; return;
            }
            if (cot != null && cot.Estado == EstadoCotizacion06AV.Desaprobada)
            {
                texto = t.Obtener("pcf_est_rechazada"); color = Tema.Peligro; paso = Paso.Cotizar; return;
            }
            texto = t.Obtener("pcf_est_pendiente"); color = Tema.Advertencia; paso = Paso.Cotizar;
        }

        private void CargarFaltantes()
        {
            try
            {
                try { _ordenes = _bll.ObtenerOrdenesCompra() ?? _ordenes; } catch { }
                var enTramite = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var o in _ordenes.Where(o => o.EnCurso))
                    foreach (var d in o.ComponentesFaltantes)
                        if (d.Componente != null && !enTramite.ContainsKey(d.Componente.Codigo))
                            enTramite[d.Componente.Codigo] = o.NumeroCompra;

                var items = new List<FaltanteItem06AV>();
                foreach (var i in _bll.ObtenerFaltantes())
                {
                    bool bloq = enTramite.TryGetValue(i.Codigo, out int ocNum);
                    items.Add(new FaltanteItem06AV
                    {
                        Codigo = i.Codigo,
                        Descripcion = i.Descripcion,
                        Stock = i.Stock,
                        StockMinimo = i.StockMinimo,
                        Bloqueado = bloq,
                        OrdenBloqueo = ocNum
                    });
                }

                var actual = UsuarioSesion06AV.Instancia().UsuarioActual;
                asistente.Cargar(items, actual?.Login);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void SeleccionarOrden(int numero)
        {
            foreach (DataGridViewRow row in grOC.Rows)
                if (row.DataBoundItem is OrdenVm vm && vm.Numero == numero)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grOC.CurrentCell = row.Cells[0];
                    return;
                }
        }

        private void FormatearGrillaOrdenes()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key) { if (grOC.Columns[col] != null) grOC.Columns[col].HeaderText = t.Obtener(key); }
            H("Numero", "pcf_col_numero");
            H("Proveedor", "pcf_proveedor");
            H("Repositor", "pcf_repositor");
            H("Limite", "pcf_f_limite");
            H("Estado", "pcf_col_estado");
            H("Costo", "pcf_costo");
            H("Atraso", "pcf_oc_col_atraso");
            if (grOC.Columns["Numero"] != null) grOC.Columns["Numero"].FillWeight = 40;
            if (grOC.Columns["Atraso"] != null) grOC.Columns["Atraso"].FillWeight = 50;
            if (grOC.Columns["Costo"] != null)
                grOC.Columns["Costo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewRow row in grOC.Rows)
                if (row.DataBoundItem is OrdenVm vm && grOC.Columns["Estado"] != null)
                {
                    row.Cells["Estado"].Style.ForeColor = vm.EstadoColor;
                    row.Cells["Estado"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    if (vm.Atrasada && grOC.Columns["Atraso"] != null)
                    {
                        row.Cells["Atraso"].Style.ForeColor = Tema.Peligro;
                        row.Cells["Atraso"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                        row.Cells["Limite"].Style.ForeColor = Tema.Peligro;
                    }
                }
        }

        #endregion

        #region Detalle

        private void ActualizarDetalle()
        {
            flpDetalle.SuspendLayout();
            foreach (Control c in flpDetalle.Controls.Cast<Control>().ToList()) c.Dispose();
            flpDetalle.Controls.Clear();
            try { ArmarDetalle(); }
            finally { flpDetalle.ResumeLayout(); }
        }

        private void ArmarDetalle()
        {
            var t = GestorIdioma06AV.Instancia;

            _ocSel = (grOC.CurrentRow?.DataBoundItem as OrdenVm)?.Orden;

            if (_ocSel == null)
            {
                lblDetTitulo.Text = string.Empty;
                lblDetEstado.Text = string.Empty;
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_sel_orden")));
                return;
            }

            var cot = CotizacionDe(_ocSel);
            EstadoDe(_ocSel, cot, out string estadoTxt, out Color estadoColor, out Paso paso);

            lblDetTitulo.Text = t.Obtener("pcf_orden_num") + " #" + _ocSel.NumeroCompra;
            lblDetEstado.Text = "● " + estadoTxt;
            lblDetEstado.ForeColor = estadoColor;
            lblDetEstado.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            int atraso = _ocSel.EnCurso ? (DateTime.Today - _ocSel.FechaLimite.Date).Days : 0;
            string limite = t.Obtener("pcf_f_limite") + ": " + _ocSel.FechaLimite.ToShortDateString();
            var lblLimite = TextoSecundario(atraso > 0 ? "!  " + limite + "  ·  " + t.Obtener("pcf_plazo_atraso", atraso) : limite);
            if (atraso > 0) lblLimite.ForeColor = Tema.Peligro;
            flpDetalle.Controls.Add(lblLimite);

            bool conPrecios = cot != null && cot.Estado != EstadoCotizacion06AV.Desaprobada && cot.TienePreciosPorItem;
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_insumos")));
            var lineas = _ocSel.ComponentesFaltantes
                .Select(d => "•  " + (d.Componente != null ? d.Componente.Descripcion : "") + "   ×" + d.Cantidad +
                             (conPrecios && d.Componente != null
                                 ? "   " + t.Obtener("pcf_oc_precio_unit", cot.PrecioDe(d.Componente.Codigo).ToString("C0"))
                                 : ""));
            flpDetalle.Controls.Add(TextoSecundario(string.Join(Environment.NewLine, lineas)));

            if (cot != null)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_cotizacion_lbl")));
                string prov = cot.Proveedor != null ? cot.Proveedor.Nombre : "-";
                string linea = prov + "  —  " + cot.Costo.ToString("C0");
                if (!string.IsNullOrWhiteSpace(cot.Condiciones))
                    linea += Environment.NewLine + cot.Condiciones;
                if (cot.Estado == EstadoCotizacion06AV.Aprobado && cot.GerenteAprobador != null)
                    linea += Environment.NewLine + t.Obtener("pcf_oc_aprobo", cot.GerenteAprobador.Login);
                flpDetalle.Controls.Add(TextoSecundario(linea));
            }

            if (_ocSel.Estado != EstadoOrdenCompra06AV.Pendiente && _ocSel.Estado != EstadoOrdenCompra06AV.Enviada &&
                _ocSel.Estado != EstadoOrdenCompra06AV.Anulada)
                SeccionRecepcionesYPagos(cot);

            if (_ocSel.Cerrada && _ocSel.Estado != EstadoOrdenCompra06AV.Finalizada &&
                !string.IsNullOrWhiteSpace(_ocSel.MotivoCierre))
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_oc_motivo_cierre")));
                var lblMotivo = TextoSecundario(_ocSel.MotivoCierre);
                lblMotivo.ForeColor = Tema.Peligro;
                flpDetalle.Controls.Add(lblMotivo);
            }

            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            switch (paso)
            {
                case Paso.Cotizar: SeccionCotizar(); break;
                case Paso.Aprobar: SeccionAprobar(cot); break;
                case Paso.Recibir: SeccionRecibir(cot); break;
                case Paso.Finalizada: SeccionFinalizada(cot); break;
                case Paso.Cerrada: SeccionCerrada(cot); break;
            }
        }

        private void SeccionRecepcionesYPagos(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            List<FacturaCompra06AV> recepciones;
            try { recepciones = _bll.ObtenerRecepciones(_ocSel.Id) ?? new List<FacturaCompra06AV>(); }
            catch { return; }
            if (recepciones.Count == 0) return;

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_oc_recepciones") + "  (" + recepciones.Count + ")"));
            foreach (FacturaCompra06AV f in recepciones.OrderBy(f => f.FechaEntrega))
            {
                int unidades = f.ComponentesRecibidos.Sum(d => d.Cantidad);
                string linea = f.FechaEntrega.ToString("dd/MM/yyyy") + "  ·  " + t.Obtener("pcf_cot_unidades", unidades) +
                               "  ·  " + f.Total.ToString("C0");
                if (!string.IsNullOrWhiteSpace(f.NumeroFacturaProveedor))
                    linea += Environment.NewLine + "    " + t.Obtener("pcf_oc_fact_prov", f.NumeroFacturaProveedor);
                var lbl = TextoSecundario(linea);
                if (f.TotalFacturado.HasValue && Math.Abs(f.Diferencia) >= 0.01m)
                {
                    lbl.Text += Environment.NewLine + "    !  " + t.Obtener("pcf_oc_facturo",
                        f.TotalFacturado.Value.ToString("C0"), (f.Diferencia > 0 ? "+" : "") + f.Diferencia.ToString("C0"));
                    lbl.ForeColor = Tema.Advertencia;
                }
                flpDetalle.Controls.Add(lbl);
            }

            ResumenPagoProveedor06AV resumen;
            try { resumen = _bll.ResumenPagos(_ocSel, _pagos); }
            catch { return; }

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_oc_pagos")));
            flpDetalle.Controls.Add(TextoSecundario(
                t.Obtener("pcf_pago_facturado") + ": " + resumen.Facturado.ToString("C0") + "   ·   " +
                t.Obtener("pcf_pago_pagado") + ": " + resumen.Pagado.ToString("C0")));

            Color color = resumen.Estado == EstadoPagoProveedor06AV.Pagada ? Tema.Exito
                        : resumen.Estado == EstadoPagoProveedor06AV.Parcial ? Tema.Advertencia : Tema.Peligro;
            var lblSaldo = new Label
            {
                AutoSize = true, Margin = new Padding(0, 0, 0, 4), ForeColor = color,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Text = "● " + t.Obtener("pcf_pago_est_" + resumen.Estado) +
                       (resumen.Saldo > 0 ? "   ·   " + t.Obtener("pcf_pago_saldo") + ": " + resumen.Saldo.ToString("C2") : "")
            };
            flpDetalle.Controls.Add(lblSaldo);

            if (resumen.Pagos.Count > 0)
                flpDetalle.Controls.Add(TextoSecundario(string.Join(Environment.NewLine, resumen.Pagos
                    .OrderBy(p => p.FechaPago)
                    .Select(p => "•  " + p.FechaPago.ToString("dd/MM/yyyy") + "  ·  " + TextoForma(p.FormaPago) + "  ·  " +
                                 p.Monto.ToString("C0") + (string.IsNullOrWhiteSpace(p.Referencia) ? "" : "  ·  " + p.Referencia)))));

            if (resumen.Saldo > 0 && Puede(PatenteEnum06AV.PagarProveedores))
            {
                var btnPagar = BotonDetalle(t.Obtener("pcf_pago_registrar"), Tema.AplicarBotonAcento);
                btnPagar.Click += (s, e) => RegistrarPago(cot, resumen);
                flpDetalle.Controls.Add(btnPagar);
            }
        }

        private static string TextoForma(FormaPago06AV f)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (f)
            {
                case FormaPago06AV.Efectivo: return t.Obtener("pcf_fp_efectivo");
                case FormaPago06AV.Tarjeta: return t.Obtener("pcf_fp_tarjeta");
                case FormaPago06AV.TarjetaDebito: return t.Obtener("pcf_fp_tarjeta_debito");
                default: return t.Obtener("pcf_fp_transferencia");
            }
        }

        private void SeccionCotizar()
        {
            var t = GestorIdioma06AV.Instancia;
            if (Puede(PatenteEnum06AV.RegistrarOrdenCompra))
            {
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_cotizar")));
                var btnPedir = BotonDetalle(t.Obtener("pcf_pedir_cotizacion"), Tema.AplicarBotonAcento);
                btnPedir.Click += (s, e) => AbrirPedidoCotizacion();
                flpDetalle.Controls.Add(btnPedir);
                AgregarBotonAnular();
            }
            else flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_oc_espera_cotizacion")));
        }

        private void SeccionAprobar(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            int abiertas = OfertasAbiertas(_ocSel);

            if (Puede(PatenteEnum06AV.AprobarCotizacion))
            {
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_aprobar")));
                if (abiertas > 1)
                {
                    var lbl = TextoSecundario(t.Obtener("pcf_oc_varias_ofertas", abiertas));
                    lbl.ForeColor = Tema.Primario;
                    flpDetalle.Controls.Add(lbl);
                }

                var btnAprobar = BotonDetalle(t.Obtener("pcf_aprobar"), Tema.AplicarBotonPrimario);
                btnAprobar.Click += (s, e) => AprobarDesaprobar(cot, true);
                flpDetalle.Controls.Add(btnAprobar);

                var btnRechazar = BotonDetalle(t.Obtener("pcf_rechazar"), Tema.AplicarBotonPeligro);
                btnRechazar.Click += (s, e) => AprobarDesaprobar(cot, false);
                flpDetalle.Controls.Add(btnRechazar);
            }
            else flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_oc_espera_aprobacion")));

            if (Puede(PatenteEnum06AV.RegistrarOrdenCompra))
            {
                if (abiertas > 0 && !Puede(PatenteEnum06AV.AprobarCotizacion))
                {
                    var btnOtra = BotonDetalle(t.Obtener("pcf_pedir_cotizacion"), Tema.AplicarBotonSecundario);
                    btnOtra.Click += (s, e) => AbrirPedidoCotizacion();
                    flpDetalle.Controls.Add(btnOtra);
                }
                AgregarBotonAnular();
            }
        }

        private void SeccionRecibir(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            bool parcial = _ocSel.Estado == EstadoOrdenCompra06AV.RecibidaParcial;

            if (Puede(PatenteEnum06AV.RegistrarOrdenCompra))
            {
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener(parcial ? "pcf_oc_hint_parcial" : "pcf_hint_recibir")));
                var btnRecibir = BotonDetalle(t.Obtener("pcf_recibir_insumos"), Tema.AplicarBotonPrimario);
                btnRecibir.Click += (s, e) => Recibir(cot);
                flpDetalle.Controls.Add(btnRecibir);
            }
            else flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_oc_espera_recepcion")));

            AgregarBotonOrdenCompra(cot);

            if (parcial && Puede(PatenteEnum06AV.RegistrarOrdenCompra))
            {
                var btnCerrar = BotonDetalle(t.Obtener("pcf_oc_cerrar_faltante"), Tema.AplicarBotonPeligro);
                btnCerrar.Click += (s, e) => CerrarConFaltante();
                flpDetalle.Controls.Add(btnCerrar);
            }
            else if (!parcial && Puede(PatenteEnum06AV.AprobarCotizacion))
                AgregarBotonAnular();
        }

        private void SeccionFinalizada(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_finalizada")));
            AgregarBotonOrdenCompra(cot);
        }

        private void SeccionCerrada(PedidoCotizacion06AV cot)
        {
            var t = GestorIdioma06AV.Instancia;
            bool anulada = _ocSel.Estado == EstadoOrdenCompra06AV.Anulada;
            flpDetalle.Controls.Add(TextoSecundario(t.Obtener(anulada ? "pcf_oc_hint_anulada" : "pcf_oc_hint_cerrada")));
            if (cot != null && cot.Estado == EstadoCotizacion06AV.Aprobado) AgregarBotonOrdenCompra(cot);
        }

        private void AgregarBotonAnular()
        {
            var btn = BotonDetalle(t("pcf_oc_anular"), Tema.AplicarBotonSecundario);
            btn.ForeColor = Tema.Peligro;
            btn.Click += (s, e) => AnularOrden();
            flpDetalle.Controls.Add(btn);
        }

        private void AgregarBotonOrdenCompra(PedidoCotizacion06AV cot)
        {
            if (cot == null || cot.Estado != EstadoCotizacion06AV.Aprobado) return;
            var btn = BotonDetalle(t("pcf_oc_ver_enviar"), Tema.AplicarBotonSecundario);
            btn.Click += (s, e) => ComprobanteCompras06AV.Ofrecer(_ocSel, cot, false, FindForm());
            flpDetalle.Controls.Add(btn);
        }

        private static string t(string clave, params object[] args) => GestorIdioma06AV.Instancia.Obtener(clave, args);

        #endregion

        #region Acciones

        private void AbrirPedidoCotizacion()
        {
            if (_ocSel == null)
            {
                MostrarError(t("pcf_sel_orden"));
                return;
            }

            List<Proveedor06AV> proveedores;
            try { proveedores = _proveedoresBLL.ObtenerActivos(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            cotizador.Cargar(_ocSel, proveedores, ProveedoresConOfertaAbierta(_ocSel));

            pnlLista.Visible = false;
            pnlForm.Visible = false;
            pnlRecepcion.Visible = false;
            pnlCotizacion.Visible = true;
            pnlCotizacion.BringToFront();
        }

        private List<int> ProveedoresConOfertaAbierta(OrdenCompra06AV oc)
        {
            return _cotizaciones
                .Where(c => c.NumeroCompra == oc.Id
                            && c.Estado == EstadoCotizacion06AV.PorAprobar
                            && c.Proveedor != null)
                .Select(c => c.Proveedor.Id)
                .Distinct()
                .ToList();
        }

        private void AltaProveedorDesdeCotizador()
        {
            using (var dlg = new FRMNuevoProveedor06AV())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK || dlg.ProveedorCreado == null) return;
                try
                {
                    var proveedores = _proveedoresBLL.ObtenerActivos();
                    var creado = proveedores.FirstOrDefault(p => p.Cuit == dlg.ProveedorCreado.Cuit);
                    cotizador.RecargarProveedores(proveedores, creado != null ? creado.Id : (int?)null);
                }
                catch (Exception ex) { MostrarError(ex.Message); }
            }
        }

        private void RegistrarCotizacion(CotizacionArmada06AV armada)
        {
            if (_ocSel == null || armada == null || armada.Proveedor == null) return;

            try
            {
                var cot = _bll.RegistrarCotizacion(_ocSel.Id, armada.Proveedor.Id,
                                                   armada.Precios, armada.Condiciones);
                ConfirmacionForm.MostrarInfo(
                    t("pcf_cotp_ok", cot.Numero, armada.Proveedor.Nombre, armada.Costo.ToString("C0")),
                    t("pcf_compras_titulo"), ConfirmacionForm.TipoConfirmacion.Info, FindForm());

                int numero = _ocSel.NumeroCompra;
                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(numero);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AprobarDesaprobar(PedidoCotizacion06AV cot, bool aprobar)
        {
            if (cot == null || _ocSel == null) { MostrarError(t("pcf_oc_sin_cotizacion")); return; }
            var oc = _ocSel;
            var gerente = UsuarioSesion06AV.Instancia().UsuarioActual;
            string proveedor = cot.Proveedor != null ? cot.Proveedor.Nombre : "-";

            bool ok = ConfirmacionForm.Mostrar(
                aprobar ? t("pcf_cot_confirmar_adj", proveedor, cot.Costo.ToString("C0"))
                        : t("pcf_cot_confirmar_desc", proveedor),
                t("pcf_compras_titulo"),
                aprobar ? ConfirmacionForm.TipoConfirmacion.Info : ConfirmacionForm.TipoConfirmacion.Advertencia,
                aprobar ? t("pcf_aprobar") : t("pcf_rechazar"), t("cancelar"), FindForm());
            if (!ok) return;

            if (aprobar && EsAutoaprobacion(oc, gerente) &&
                !ConfirmacionForm.Mostrar(t("pcf_autoaprobacion", oc.NumeroCompra), t("pcf_compras_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, t("pcf_aprobar_igual"), t("cancelar"), FindForm()))
                return;

            try
            {
                if (aprobar)
                {
                    var aprobada = _bll.AprobarCotizacion(cot.Numero, gerente);
                    CargarOrdenes();
                    SeleccionarOrden(oc.NumeroCompra);
                    OrdenCompra06AV actual = _ordenes.FirstOrDefault(o => o.Id == oc.Id) ?? oc;
                    ComprobanteCompras06AV.Ofrecer(actual, aprobada ?? cot, true, FindForm());
                }
                else
                {
                    _bll.DesaprobarCotizacion(cot.Numero, gerente);
                    CargarOrdenes();
                    SeleccionarOrden(oc.NumeroCompra);
                }
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        internal static bool EsAutoaprobacion(OrdenCompra06AV oc, Usuario06AV gerente) =>
            oc?.RepositorSolicitante != null && gerente != null &&
            !string.IsNullOrWhiteSpace(gerente.Dni) &&
            string.Equals(oc.RepositorSolicitante.Dni, gerente.Dni, StringComparison.OrdinalIgnoreCase);

        private void AnularOrden()
        {
            if (_ocSel == null) return;
            var oc = _ocSel;
            string motivo = FRMMotivo06AV.Pedir(FindForm(), t("pcf_oc_anular_titulo", oc.NumeroCompra),
                                               t("pcf_oc_anular_msg", oc.NumeroCompra), t("pcf_oc_anular"));
            if (motivo == null) return;
            try
            {
                _bll.AnularOrdenCompra(oc.Id, motivo, UsuarioSesion06AV.Instancia().UsuarioActual);
                ConfirmacionForm.MostrarInfo(t("pcf_oc_anulada_ok", oc.NumeroCompra), t("pcf_compras_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                CargarOrdenes();
                SeleccionarOrden(oc.NumeroCompra);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CerrarConFaltante()
        {
            if (_ocSel == null) return;
            var oc = _ocSel;
            string pendiente = "";
            try
            {
                pendiente = string.Join(", ", _bll.ObtenerPendienteDeRecibir(oc.Id)
                    .Select(d => (d.Componente?.Descripcion ?? d.Componente?.Codigo) + " ×" + d.Cantidad));
            }
            catch { }
            string motivo = FRMMotivo06AV.Pedir(FindForm(), t("pcf_oc_cerrar_titulo", oc.NumeroCompra),
                                               t("pcf_oc_cerrar_msg", pendiente), t("pcf_oc_cerrar_faltante"));
            if (motivo == null) return;
            try
            {
                _bll.CerrarConFaltante(oc.Id, motivo);
                ConfirmacionForm.MostrarInfo(t("pcf_oc_cerrada_ok", oc.NumeroCompra), t("pcf_compras_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                CargarOrdenes();
                SeleccionarOrden(oc.NumeroCompra);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void RegistrarPago(PedidoCotizacion06AV cot, ResumenPagoProveedor06AV resumen)
        {
            if (_ocSel == null) return;
            var oc = _ocSel;
            using (var f = new FRMPagoProveedor06AV(oc, cot?.Proveedor?.Nombre, resumen))
            {
                if (f.ShowDialog(FindForm()) != DialogResult.OK || f.PagoRegistrado == null) return;
                ConfirmacionForm.MostrarInfo(
                    t("pcf_pago_ok", f.PagoRegistrado.Monto.ToString("C2"), oc.NumeroCompra, f.PagoRegistrado.Numero),
                    t("pcf_pago_titulo"), ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            CargarOrdenes();
            SeleccionarOrden(oc.NumeroCompra);
        }

        private void Recibir(PedidoCotizacion06AV cot)
        {
            if (_ocSel == null) return;

            List<DetalleComponente06AV> pendiente;
            try { pendiente = _bll.ObtenerPendienteDeRecibir(_ocSel.Id); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            if (pendiente == null || pendiente.Count == 0)
            {
                MostrarError(t("pcf_rec_ya_completa"));
                return;
            }

            PedidoCotizacion06AV adjudicada = cot != null && cot.Estado == EstadoCotizacion06AV.Aprobado ? cot : null;
            recepcion.Cargar(_ocSel, adjudicada, pendiente);

            pnlLista.Visible = false;
            pnlForm.Visible = false;
            pnlCotizacion.Visible = false;
            pnlRecepcion.Visible = true;
            pnlRecepcion.BringToFront();
        }

        private void ConfirmarRecepcion(RecepcionArmada06AV r)
        {
            if (_ocSel == null || r == null) return;
            int numero = _ocSel.NumeroCompra;

            try
            {
                _bll.RegistrarFacturaCompra(_ocSel.Id, r.FechaEntrega, r.Observaciones, r.Recibidos,
                                            r.NumeroFacturaProveedor, r.TotalFacturado);

                ConfirmacionForm.MostrarInfo(
                    r.Completa ? t("pcf_rec_ok_completa", numero) : t("pcf_rec_ok_parcial", numero),
                    t("pcf_rec_titulo"),
                    r.Completa ? ConfirmacionForm.TipoConfirmacion.Info
                               : ConfirmacionForm.TipoConfirmacion.Advertencia,
                    FindForm());

                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(numero);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CrearOrden(OrdenArmada06AV orden)
        {
            if (orden == null || orden.Detalles == null || orden.Detalles.Count == 0) return;

            try
            {
                var repositor = UsuarioSesion06AV.Instancia().UsuarioActual;
                var oc = _bll.RegistrarOrdenCompra(orden.Detalles, orden.FechaLimite, repositor);
                ConfirmacionForm.MostrarInfo(t("pcf_oc_creada", oc.NumeroCompra),
                    t("pcf_compras_titulo"), ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                MostrarLista();
                CargarOrdenes();
                SeleccionarOrden(oc.NumeroCompra);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        #endregion

        #region Helpers visuales

        private Label Etiqueta(string texto, bool fuerte = false)
        {
            return new Label
            {
                Text = texto, AutoSize = true, Margin = new Padding(0, fuerte ? 4 : 8, 0, 2),
                ForeColor = fuerte ? Tema.Texto : Tema.TextoSuave,
                Font = new Font("Segoe UI Semibold", fuerte ? 10f : 8.5f, FontStyle.Bold)
            };
        }

        private Label TextoSecundario(string texto)
        {
            return new Label
            {
                Text = texto, AutoSize = true, MaximumSize = new Size(336, 0),
                Margin = new Padding(0, 0, 0, 4), ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
        }

        private Panel Separador() => new Panel { Height = 1, Width = 336, BackColor = Tema.Borde, Margin = new Padding(0, 10, 0, 6) };

        private Button BotonDetalle(string texto, Action<Button> estilo)
        {
            var b = new Button { Text = texto, Width = 240, Height = 32, Margin = new Padding(0, 4, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            estilo(b);
            return b;
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grOC);
            Tema.AplicarBotonSecundario(btnActualizar);
            Tema.AplicarBotonPrimario(btnNueva);
            Tema.AplicarEntrada(txtBuscar);
            Tema.AplicarEntrada(cboEstado);
            lblBuscar.Font = Tema.FuenteMini;
            lblBuscar.ForeColor = Tema.TextoSuave;
            asistente.AplicarTema();
            cotizador.AplicarTema();
            Tema.AplicarSubtitulo(lblDetTitulo);

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (var p in new[] { pnlLista, pnlForm, pnlCotizacion, pnlRecepcion }) p.BackColor = Tema.FondoApp;
            recepcion.AplicarTema();

            if (grOC.DataSource != null) RefrescarGrilla();
            else ActualizarDetalle();
        }

        public void AplicarIdioma()
        {
            lblTitulo.Text = t("pcf_compras_titulo");
            lblBuscar.Text = t("buscar");
            btnActualizar.Text = t("pcf_actualizar");
            btnNueva.Text = "＋ " + t("pcf_nueva_oc");

            int filtro = cboEstado.SelectedIndex;
            cboEstado.Items.Clear();
            cboEstado.Items.AddRange(new object[]
            {
                t("pcf_oc_f_todas"), t("pcf_oc_f_en_curso"), t("pcf_oc_f_cotizar"), t("pcf_oc_f_aprobar"),
                t("pcf_oc_f_recibir"), t("pcf_oc_f_atrasadas"), t("pcf_oc_f_cerradas")
            });
            cboEstado.SelectedIndex = filtro >= 0 ? filtro : 0;

            asistente.AplicarIdioma();
            cotizador.AplicarIdioma();
            recepcion.AplicarIdioma();

            if (grOC.DataSource != null) RefrescarGrilla();
            else ActualizarDetalle();
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, t("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        #endregion

        private class OrdenVm
        {
            public int Numero { get; set; }
            public string Proveedor { get; set; }
            public string Repositor { get; set; }
            public string Limite { get; set; }
            public string Estado { get; set; }
            public string Costo { get; set; }
            public string Atraso { get; set; }

            [Browsable(false)] public Color EstadoColor { get; set; }
            [Browsable(false)] public bool Atrasada { get; set; }
            [Browsable(false)] public Paso Paso { get; set; }
            [Browsable(false)] public OrdenCompra06AV Orden { get; set; }
            [Browsable(false)] public PedidoCotizacion06AV Cotizacion { get; set; }
        }
    }
}
