using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// ENTREGA DE COMPUTADORAS (RFN1 - CU07) — pantalla del RECEPCIONISTA.
    ///
    /// Es la bandeja de salida de la fábrica: recibe las órdenes de producción que
    /// ya quedaron "Finalizadas" (control de calidad aprobado y N° de serie asignado)
    /// y permite buscarlas por N° de orden o DNI del cliente, cobrar el saldo
    /// pendiente, emitir la factura y cerrar el circuito.
    ///
    /// Acá NO se opera la orden de producción: solo se recibe terminada y se entrega.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class EntregasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly EntregasBLL06AV _entregasBLL = new EntregasBLL06AV();

        private List<OrdenProduccion06AV> _origen = new List<OrdenProduccion06AV>();
        private OrdenProduccion06AV _ordenSel;
        private OrdenProduccion06AV _ordenCobro;

        // Vistas
        private Panel pnlGrilla, pnlFormCobro;

        // Hub (búsqueda + lista + detalle)
        private Label lblTitulo, lblBuscar;
        private TextBox txtBuscar;
        private ComboBox cboVista;
        private DataGridView grilla;
        private Button btnRefrescar;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // Formulario de cobro del saldo final
        private Label lblFormCobroTit, lblCobroDetalle, lblMonto, lblMontoValor, lblFormaPago, lblReferencia;
        private ComboBox cboFormaPago;
        private TextBox txtReferencia;
        private Button btnConfirmar, btnVolver;

        public EntregasControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarGrilla();
            Cargar();
        }

        // ══════════════════════════════════════════════════════════════
        //  Construcción
        // ══════════════════════════════════════════════════════════════
        private void ConstruirUI()
        {
            ConstruirHub();
            ConstruirFormCobro();

            Controls.Add(pnlFormCobro);
            Controls.Add(pnlGrilla);
        }

        private void ConstruirHub()
        {
            grilla = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grilla.DataBindingComplete += (s, e) => FormatearGrilla();
            grilla.SelectionChanged += (s, e) => ActualizarDetalle();

            lblTitulo = new Label { AutoSize = true, Location = new Point(4, 16) };
            btnRefrescar = NuevoBoton(120);
            btnRefrescar.Click += (s, e) => Cargar();

            cboVista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Margin = new Padding(6, 4, 0, 0) };
            cboVista.SelectedIndexChanged += (s, e) => Cargar();

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { cboVista, btnRefrescar });

            var barraSup = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraSup.Controls.Add(lblTitulo);
            barraSup.Controls.Add(flpAcciones);

            // Barra de búsqueda (CU07 paso 2: N° de orden o DNI del cliente)
            lblBuscar = new Label { AutoSize = true, Margin = new Padding(0, 8, 8, 0) };
            txtBuscar = new TextBox { Width = 320, Margin = new Padding(0, 4, 0, 0) };
            txtBuscar.TextChanged += (s, e) => AplicarFiltro();

            var flpBuscar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(6, 4, 6, 4)
            };
            flpBuscar.Controls.Add(lblBuscar);
            flpBuscar.Controls.Add(txtBuscar);

            lblDetTitulo = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 26, Padding = new Padding(0, 2, 0, 0) };
            lblDetEstado = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 22, Padding = new Padding(0, 0, 0, 4) };
            flpDetalle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true
            };
            pnlDetalle = new Panel { Dock = DockStyle.Right, Width = 400, Padding = new Padding(16, 14, 12, 12) };
            pnlDetalle.Controls.Add(flpDetalle);
            pnlDetalle.Controls.Add(lblDetEstado);
            pnlDetalle.Controls.Add(lblDetTitulo);

            pnlGrilla = new Panel { Dock = DockStyle.Fill };
            pnlGrilla.Controls.Add(grilla);
            pnlGrilla.Controls.Add(pnlDetalle);
            pnlGrilla.Controls.Add(flpBuscar);
            pnlGrilla.Controls.Add(barraSup);
        }

        private void ConstruirFormCobro()
        {
            lblFormCobroTit = new Label { AutoSize = true, Location = new Point(6, 16) };
            lblCobroDetalle = new Label { AutoSize = true, MaximumSize = new Size(620, 0), Location = new Point(8, 6) };
            lblMonto = new Label(); lblFormaPago = new Label(); lblReferencia = new Label();
            lblMontoValor = new Label { AutoSize = true };

            cboFormaPago = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
            txtReferencia = new TextBox { Width = 360 };

            btnConfirmar = NuevoBoton(190);
            btnVolver = NuevoBoton(120);
            btnConfirmar.Click += (s, e) => ConfirmarEntrega();
            btnVolver.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            AgregarFila(tabla, lblMonto, lblMontoValor);
            AgregarFila(tabla, lblFormaPago, cboFormaPago);
            AgregarFila(tabla, lblReferencia, txtReferencia);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            var cabecera = new Panel { Dock = DockStyle.Top, Height = 66, Padding = new Padding(8, 6, 8, 6) };
            cabecera.Controls.Add(lblCobroDetalle);
            cont.Controls.Add(cabecera);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraTop.Controls.Add(lblFormCobroTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolver, btnConfirmar));

            pnlFormCobro = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormCobro.Controls.Add(cont);
            pnlFormCobro.Controls.Add(barraTop);
            pnlFormCobro.Controls.Add(barraBot);
        }

        private static Button NuevoBoton(int width = 110) =>
            new Button { Width = width, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private static FlowLayoutPanel BarraBotones(params Button[] botones)
        {
            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Padding = new Padding(0, 12, 10, 0)
            };
            flp.Controls.AddRange(botones);
            return flp;
        }

        private static TableLayoutPanel NuevaTabla()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2, Padding = new Padding(8)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return t;
        }

        private void AgregarFila(TableLayoutPanel t, Label etiqueta, Control campo)
        {
            int fila = t.RowCount;
            t.RowCount = fila + 1;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            etiqueta.AutoSize = true;
            etiqueta.Anchor = AnchorStyles.Left;
            etiqueta.Margin = new Padding(3, 9, 6, 3);
            campo.Anchor = AnchorStyles.Left;
            campo.Margin = new Padding(3, 6, 3, 6);
            Tema.AplicarEntrada(campo);
            t.Controls.Add(etiqueta, 0, fila);
            t.Controls.Add(campo, 1, fila);
        }

        // ══════════════════════════════════════════════════════════════
        //  Tema e idioma
        // ══════════════════════════════════════════════════════════════
        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarSubtitulo(lblFormCobroTit);
            Tema.AplicarSubtitulo(lblDetTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonSecundario(btnRefrescar);
            Tema.AplicarBotonPrimario(btnConfirmar);
            Tema.AplicarBotonSecundario(btnVolver);
            Tema.AplicarEntrada(txtBuscar);
            Tema.AplicarEntrada(cboVista);

            // AgregarFila aplica el estilo de "entrada"; acá se restituye el look de etiqueta.
            lblMontoValor.ForeColor = Tema.Primario;
            lblMontoValor.BackColor = Tema.FondoApp;
            lblMontoValor.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            lblCobroDetalle.ForeColor = Tema.TextoSuave;
            lblCobroDetalle.BackColor = Tema.FondoApp;
            lblBuscar.ForeColor = Tema.TextoSuave;

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (Control panel in new Control[] { pnlGrilla, pnlFormCobro })
            {
                panel.BackColor = Tema.FondoApp;
                foreach (Control hijo in panel.Controls)
                    if (hijo != pnlDetalle && (hijo is Panel || hijo is FlowLayoutPanel))
                        hijo.BackColor = Tema.FondoApp;
            }

            ActualizarDetalle();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("pcf_entregas_titulo");
            btnRefrescar.Text = t.Obtener("pcf_refrescar");
            lblBuscar.Text = t.Obtener("pcf_buscar_orden_dni") + ":";

            lblMonto.Text = t.Obtener("pcf_saldo_a_cobrar") + ":";
            lblFormaPago.Text = t.Obtener("pcf_forma_pago") + ":";
            lblReferencia.Text = t.Obtener("pcf_referencia") + ":";
            btnConfirmar.Text = t.Obtener("pcf_confirmar_entrega");
            btnVolver.Text = t.Obtener("volver");

            CargarVistas();
            CargarFormasPago();

            if (grilla.DataSource != null) Cargar();
            else ActualizarDetalle();
        }

        // ══════════════════════════════════════════════════════════════
        //  Datos
        // ══════════════════════════════════════════════════════════════
        private void CargarVistas()
        {
            var t = GestorIdioma06AV.Instancia;
            int idx = cboVista.SelectedIndex < 0 ? 0 : cboVista.SelectedIndex;
            cboVista.DataSource = null;
            cboVista.DataSource = new List<string>
            {
                t.Obtener("pcf_vista_para_entregar"),
                t.Obtener("pcf_vista_entregadas")
            };
            cboVista.SelectedIndex = idx;
        }

        private bool VistaPendientes => cboVista.SelectedIndex <= 0;

        private void CargarFormasPago()
        {
            var t = GestorIdioma06AV.Instancia;
            var actual = cboFormaPago.SelectedItem as FormaPagoVm;
            cboFormaPago.DataSource = null;
            cboFormaPago.DisplayMember = "Texto";
            cboFormaPago.DataSource = new List<FormaPagoVm>
            {
                new FormaPagoVm { Valor = FormaPago06AV.Efectivo,      Texto = t.Obtener("pcf_fp_efectivo") },
                new FormaPagoVm { Valor = FormaPago06AV.Transferencia, Texto = t.Obtener("pcf_fp_transferencia") },
                new FormaPagoVm { Valor = FormaPago06AV.Tarjeta,       Texto = t.Obtener("pcf_fp_tarjeta") }
            };
            if (actual != null)
                foreach (FormaPagoVm vm in cboFormaPago.Items)
                    if (vm.Valor == actual.Valor) { cboFormaPago.SelectedItem = vm; break; }
        }

        private void Cargar()
        {
            try
            {
                _origen = VistaPendientes
                    ? _entregasBLL.ObtenerPendientesDeEntrega()
                    : _entregasBLL.ObtenerEntregadas();
                AplicarFiltro();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AplicarFiltro()
        {
            try
            {
                int? sel = (grilla.CurrentRow?.DataBoundItem as EntregaVm)?.Orden;

                var filtradas = _entregasBLL.Filtrar(_origen, txtBuscar.Text);
                var vm = filtradas.Select(o => new EntregaVm
                {
                    Orden = o.NumeroOrden,
                    Venta = o.NumeroVenta,
                    Cliente = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "",
                    Dni = o.Cliente != null ? o.Cliente.Dni : "",
                    Serie = string.IsNullOrWhiteSpace(o.NumeroSerie) ? "-" : o.NumeroSerie,
                    Listo = o.FechaCierre.HasValue ? o.FechaCierre.Value.ToShortDateString() : "-",
                    Saldo = o.SaldoPendiente.ToString("C0"),
                    OrdenObj = o
                }).ToList();

                grilla.DataSource = null;
                grilla.DataSource = vm;

                if (sel.HasValue) SeleccionarOrden(sel.Value);
                ActualizarDetalle();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k) { if (grilla.Columns[c] != null) grilla.Columns[c].HeaderText = t.Obtener(k); }
            H("Orden", "pcf_orden");
            H("Venta", "pcf_venta");
            H("Cliente", "pcf_cliente");
            H("Dni", "dni");
            H("Serie", "pcf_nro_serie");
            H("Listo", "pcf_listo_desde");
            H("Saldo", "pcf_saldo");
            if (grilla.Columns["OrdenObj"] != null) grilla.Columns["OrdenObj"].Visible = false;
            if (grilla.Columns["Orden"] != null) grilla.Columns["Orden"].FillWeight = 24;
            if (grilla.Columns["Venta"] != null) grilla.Columns["Venta"].FillWeight = 24;
            if (grilla.Columns["Saldo"] != null)
                grilla.Columns["Saldo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private OrdenProduccion06AV OrdenSeleccionada() =>
            (grilla.CurrentRow?.DataBoundItem as EntregaVm)?.OrdenObj;

        private void SeleccionarOrden(int numeroOrden)
        {
            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is EntregaVm vm && vm.Orden == numeroOrden)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grilla.CurrentCell = row.Cells[0];
                    return;
                }
        }

        // ══════════════════════════════════════════════════════════════
        //  Panel de detalle
        // ══════════════════════════════════════════════════════════════
        private void ActualizarDetalle()
        {
            flpDetalle.Controls.Clear();
            var t = GestorIdioma06AV.Instancia;
            _ordenSel = OrdenSeleccionada();

            if (_ordenSel == null)
            {
                lblDetTitulo.Text = string.Empty;
                lblDetEstado.Text = string.Empty;
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_sel_entrega")));
                return;
            }

            var o = _ordenSel;
            var v = o.Venta;
            bool entregada = o.Estado == EstadoOrdenProduccion06AV.Entregada;

            string cli = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "";
            lblDetTitulo.Text = t.Obtener("pcf_orden") + " #" + o.NumeroOrden + "  —  " + cli;
            lblDetEstado.Text = "● " + (entregada ? t.Obtener("pcf_est_op_entregada") : t.Obtener("pcf_lista_para_retirar"));
            lblDetEstado.ForeColor = entregada ? Tema.TextoSuave : Tema.Exito;
            lblDetEstado.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            // Cliente
            if (o.Cliente != null)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_cliente"), fuerte: true));
                flpDetalle.Controls.Add(TextoSecundario("DNI " + o.Cliente.Dni +
                    (string.IsNullOrWhiteSpace(o.Cliente.Telefono) ? "" : "   ·   " + o.Cliente.Telefono)));
            }

            // Equipo
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_computadora")));
            flpDetalle.Controls.Add(TextoSecundario(
                (o.Computadora != null ? o.Computadora.Nombre : "-") +
                "   ·   " + (o.Computadora != null ? o.Computadora.TipoConfiguracion.ToString() : "")));

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_nro_serie")));
            flpDetalle.Controls.Add(TextoSecundario(string.IsNullOrWhiteSpace(o.NumeroSerie) ? "-" : o.NumeroSerie));

            if (o.FechaCierre.HasValue)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_listo_desde")));
                flpDetalle.Controls.Add(TextoSecundario(o.FechaCierre.Value.ToString("dd/MM/yyyy HH:mm")));
            }

            // Importes
            if (v != null)
            {
                flpDetalle.Controls.Add(Separador());
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_venta") + " #" + v.NumeroVenta, fuerte: true));

                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_total")));
                flpDetalle.Controls.Add(TextoSecundario(v.PrecioTotal.ToString("C2")));

                var sena = v.Sena;
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_sena")));
                flpDetalle.Controls.Add(TextoSecundario(sena != null
                    ? sena.Monto.ToString("C2") + "   ·   " + ComprobantePcFactory06AV.TextoFormaPago(sena.FormaPago) +
                      "   ·   " + sena.NumeroRecibo
                    : "-"));

                var saldoFinal = v.Pagos.FirstOrDefault(p => p.Tipo == TipoPago06AV.SaldoFinal);
                flpDetalle.Controls.Add(Etiqueta(entregada ? t.Obtener("pcf_saldo_final") : t.Obtener("pcf_saldo_a_cobrar")));
                flpDetalle.Controls.Add(TextoSecundario(entregada && saldoFinal != null
                    ? saldoFinal.Monto.ToString("C2") + "   ·   " + ComprobantePcFactory06AV.TextoFormaPago(saldoFinal.FormaPago) +
                      "   ·   " + saldoFinal.NumeroRecibo
                    : v.SaldoPendiente.ToString("C2")));
            }

            // ── ¿Qué sigue? ──────────────────────────────────────
            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            if (entregada)
            {
                flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_ent_entregada")));
                AddAccion(t.Obtener("pcf_ver_factura"), Tema.AplicarBotonPrimario, VerFactura);
            }
            else
            {
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_ent_cobrar")));
                AddAccion(t.Obtener("pcf_registrar_entrega"), Tema.AplicarBotonPrimario, AbrirFormCobro);
                if (v != null && v.TieneSena)
                    AddAccion(t.Obtener("pcf_reimprimir_recibo"), Tema.AplicarBotonSecundario, ReimprimirRecibo);
            }
        }

        private void AddAccion(string texto, Action<Button> estilo, Action onClick)
        {
            var b = BotonDetalle(texto, estilo);
            b.Click += (s, e) => onClick();
            flpDetalle.Controls.Add(b);
        }

        private Label Etiqueta(string texto, bool fuerte = false) => new Label
        {
            Text = texto, AutoSize = true, Margin = new Padding(0, fuerte ? 4 : 8, 0, 2),
            ForeColor = fuerte ? Tema.Texto : Tema.TextoSuave,
            Font = new Font("Segoe UI Semibold", fuerte ? 10f : 8.5f, FontStyle.Bold)
        };

        private Label TextoSecundario(string texto) => new Label
        {
            Text = texto, AutoSize = true, MaximumSize = new Size(352, 0),
            Margin = new Padding(0, 0, 0, 4), ForeColor = Tema.Texto,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
        };

        private Panel Separador() => new Panel { Height = 1, Width = 352, BackColor = Tema.Borde, Margin = new Padding(0, 10, 0, 6) };

        private Button BotonDetalle(string texto, Action<Button> estilo)
        {
            var b = new Button { Text = texto, Width = 220, Height = 32, Margin = new Padding(0, 4, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            estilo(b);
            return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  Navegación
        // ══════════════════════════════════════════════════════════════
        private void MostrarGrilla()
        {
            pnlFormCobro.Visible = false;
            pnlGrilla.Visible = true;
            pnlGrilla.BringToFront();
            ActualizarDetalle();
        }

        private void AbrirFormCobro()
        {
            _ordenCobro = OrdenSeleccionada();
            if (_ordenCobro == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }

            var t = GestorIdioma06AV.Instancia;
            var v = _ordenCobro.Venta;

            lblFormCobroTit.Text = t.Obtener("pcf_registrar_entrega") + " — " +
                                   t.Obtener("pcf_orden") + " #" + _ordenCobro.NumeroOrden;

            string cli = _ordenCobro.Cliente != null ? _ordenCobro.Cliente.NombreCompleto : "";
            string dni = _ordenCobro.Cliente != null ? "DNI " + _ordenCobro.Cliente.Dni : "";
            lblCobroDetalle.Text =
                cli + "   ·   " + dni + "\r\n" +
                (_ordenCobro.Computadora != null ? _ordenCobro.Computadora.Nombre : "") +
                "   ·   " + t.Obtener("pcf_nro_serie") + " " + _ordenCobro.NumeroSerie + "\r\n" +
                t.Obtener("pcf_total") + " " + _ordenCobro.PrecioTotal.ToString("C2") +
                "   ·   " + t.Obtener("pcf_abonado") + " " + _ordenCobro.TotalAbonado.ToString("C2");

            lblMontoValor.Text = (v != null ? v.SaldoPendiente : _ordenCobro.SaldoPendiente).ToString("C2");
            txtReferencia.Clear();
            if (cboFormaPago.Items.Count > 0) cboFormaPago.SelectedIndex = 0;

            pnlGrilla.Visible = false;
            pnlFormCobro.Visible = true;
            pnlFormCobro.BringToFront();
        }

        // ══════════════════════════════════════════════════════════════
        //  Acciones
        // ══════════════════════════════════════════════════════════════
        private void ConfirmarEntrega()
        {
            if (_ordenCobro == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            var forma = (cboFormaPago.SelectedItem as FormaPagoVm)?.Valor ?? FormaPago06AV.Efectivo;

            try
            {
                _entregasBLL.RegistrarEntrega(_ordenCobro.NumeroOrden, forma, txtReferencia.Text.Trim());

                var actualizada = _entregasBLL.ObtenerOrden(_ordenCobro.NumeroOrden) ?? _ordenCobro;
                MostrarGrilla();
                Cargar();

                // La factura siempre queda guardada; solo se abre el PDF si el cliente lo pide.
                string factura = ComprobantePcFactory06AV.GenerarFactura(actualizada.Venta, actualizada);
                ComprobantePcFactory06AV.PreguntarEImprimir(factura, esFactura: true, owner: FindForm(),
                    encabezado: $"Entrega registrada. La orden #{actualizada.NumeroOrden} queda cerrada.");
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void VerFactura()
        {
            var o = OrdenSeleccionada();
            if (o == null || o.Venta == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                string ruta = ComprobantePcFactory06AV.GenerarFactura(o.Venta, o);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void ReimprimirRecibo()
        {
            var o = OrdenSeleccionada();
            if (o == null || o.Venta == null || !o.Venta.TieneSena)
            { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                string ruta = ComprobantePcFactory06AV.GenerarReciboSena(o.Venta, o.Venta.Sena);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════
        //  View-models
        // ══════════════════════════════════════════════════════════════
        private class EntregaVm
        {
            public int Orden { get; set; }
            public int Venta { get; set; }
            public string Cliente { get; set; }
            public string Dni { get; set; }
            public string Serie { get; set; }
            public string Listo { get; set; }
            public string Saldo { get; set; }

            [Browsable(false)] public OrdenProduccion06AV OrdenObj { get; set; }
        }

        private class FormaPagoVm
        {
            public FormaPago06AV Valor { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
        }
    }
}
