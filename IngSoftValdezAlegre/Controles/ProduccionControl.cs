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
    /// PRODUCCIÓN (RFN1) — pantalla del GERENTE y del RESPONSABLE TÉCNICO. Arranca
    /// donde termina la venta: la orden se genera sobre una venta YA SEÑADA.
    ///   CU04 Gestionar orden de producción → "＋ Nueva orden" (elige la venta señada)
    ///   CU05 Asignar línea de ensamblaje   → acción "Asignar línea"
    ///        Iniciar ensamblaje            → acción "Iniciar ensamblaje"
    ///   CU06 Cerrar orden de producción    → acción "Cerrar orden" (checklist + N° serie)
    ///
    /// La entrega al cliente y el cobro del saldo (CU07) se hacen desde Ventas.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ProduccionControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly OrdenProduccionBLL06AV _ordenesBLL = new OrdenProduccionBLL06AV();
        private readonly LineasEnsamblajeBLL06AV _lineasBLL = new LineasEnsamblajeBLL06AV();

        private OrdenProduccion06AV _ordenSel;

        // Vistas
        private Panel pnlGrilla, pnlFormOrden, pnlFormPlan, pnlFormCierre;

        // Hub (lista + detalle)
        private Label lblTitulo;
        private DataGridView grilla;
        private Button btnNueva, btnRefrescar;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // Formulario "Nueva orden" (CU04)
        private Label lblFormOrdenTit, lblVenta, lblDetalleVenta, lblEntrega;
        private ComboBox cboVenta;
        private DateTimePicker dtpEntrega;
        private Button btnRegistrar, btnVolverOrden;

        // Formulario "Asignar línea" (CU05)
        private Label lblFormPlanTit, lblLinea, lblInicio, lblResp;
        private ComboBox cboLinea;
        private DateTimePicker dtpInicio;
        private TextBox txtResp;
        private Button btnConfirmarPlan, btnVolverPlan;
        private OrdenProduccion06AV _ordenPlan;

        // Formulario "Cerrar orden" (CU06)
        private Label lblFormCierreTit, lblChecklist, lblObs, lblRespCc;
        private CheckBox chkEncendido, chkConexiones, chkSO, chkDrivers;
        private TextBox txtObs, txtRespCc;
        private Button btnConfirmarCierre, btnVolverCierre;
        private OrdenProduccion06AV _ordenCierre;

        public ProduccionControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarGrilla();
            CargarOrdenes();
        }

        // ══════════════════════════════════════════════════════════════
        //  Construcción
        // ══════════════════════════════════════════════════════════════
        private void ConstruirUI()
        {
            ConstruirHub();
            ConstruirFormOrden();
            ConstruirFormPlan();
            ConstruirFormCierre();

            Controls.Add(pnlFormOrden);
            Controls.Add(pnlFormPlan);
            Controls.Add(pnlFormCierre);
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
            btnNueva = NuevoBoton(150);
            btnRefrescar = NuevoBoton(120);
            btnNueva.Click += (s, e) => AbrirFormOrden();
            btnRefrescar.Click += (s, e) => CargarOrdenes();

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { btnRefrescar, btnNueva });

            var barraSup = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraSup.Controls.Add(lblTitulo);
            barraSup.Controls.Add(flpAcciones);

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
            pnlGrilla.Controls.Add(barraSup);
        }

        private void ConstruirFormOrden()
        {
            lblFormOrdenTit = new Label { AutoSize = true, Location = new Point(6, 16) };
            lblVenta = new Label(); lblEntrega = new Label();
            lblDetalleVenta = new Label
            {
                AutoSize = true, MaximumSize = new Size(560, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };

            cboVenta = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
            cboVenta.SelectedIndexChanged += (s, e) => MostrarDetalleVenta();

            dtpEntrega = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(15), Width = 360 };
            btnRegistrar = NuevoBoton(150);
            btnVolverOrden = NuevoBoton(120);
            btnRegistrar.Click += (s, e) => RegistrarOrden();
            btnVolverOrden.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            AgregarFila(tabla, lblVenta, cboVenta);
            AgregarFila(tabla, new Label(), lblDetalleVenta);
            AgregarFila(tabla, lblEntrega, dtpEntrega);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            var barraTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraTop.Controls.Add(lblFormOrdenTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverOrden, btnRegistrar));

            pnlFormOrden = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormOrden.Controls.Add(cont);
            pnlFormOrden.Controls.Add(barraTop);
            pnlFormOrden.Controls.Add(barraBot);
        }

        private void ConstruirFormPlan()
        {
            lblFormPlanTit = new Label { AutoSize = true, Location = new Point(6, 16) };
            lblLinea = new Label(); lblInicio = new Label(); lblResp = new Label();
            cboLinea = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
            dtpInicio = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 360 };
            txtResp = new TextBox { Width = 360 };
            btnConfirmarPlan = NuevoBoton(130);
            btnVolverPlan = NuevoBoton(120);
            btnConfirmarPlan.Click += (s, e) => AsignarLinea();
            btnVolverPlan.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            AgregarFila(tabla, lblLinea, cboLinea);
            AgregarFila(tabla, lblInicio, dtpInicio);
            AgregarFila(tabla, lblResp, txtResp);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            var barraTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraTop.Controls.Add(lblFormPlanTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverPlan, btnConfirmarPlan));

            pnlFormPlan = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormPlan.Controls.Add(cont);
            pnlFormPlan.Controls.Add(barraTop);
            pnlFormPlan.Controls.Add(barraBot);
        }

        private void ConstruirFormCierre()
        {
            lblFormCierreTit = new Label { AutoSize = true, Location = new Point(6, 16) };
            lblChecklist = new Label(); lblObs = new Label(); lblRespCc = new Label();

            chkEncendido = NuevoCheck();
            chkConexiones = NuevoCheck();
            chkSO = NuevoCheck();
            chkDrivers = NuevoCheck();

            var flpChecks = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0)
            };
            flpChecks.Controls.AddRange(new Control[] { chkEncendido, chkConexiones, chkSO, chkDrivers });

            txtObs = new TextBox { Width = 400, Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };
            txtRespCc = new TextBox { Width = 360 };

            btnConfirmarCierre = NuevoBoton(160);
            btnVolverCierre = NuevoBoton(120);
            btnConfirmarCierre.Click += (s, e) => CerrarOrden();
            btnVolverCierre.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            AgregarFila(tabla, lblChecklist, flpChecks);
            AgregarFila(tabla, lblRespCc, txtRespCc);
            AgregarFila(tabla, lblObs, txtObs);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            var barraTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraTop.Controls.Add(lblFormCierreTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverCierre, btnConfirmarCierre));

            pnlFormCierre = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormCierre.Controls.Add(cont);
            pnlFormCierre.Controls.Add(barraTop);
            pnlFormCierre.Controls.Add(barraBot);
        }

        private static CheckBox NuevoCheck() =>
            new CheckBox { AutoSize = true, Margin = new Padding(0, 3, 0, 3), Cursor = Cursors.Hand };

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
            Tema.AplicarSubtitulo(lblFormOrdenTit);
            Tema.AplicarSubtitulo(lblFormPlanTit);
            Tema.AplicarSubtitulo(lblFormCierreTit);
            Tema.AplicarSubtitulo(lblDetTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnNueva);
            Tema.AplicarBotonSecundario(btnRefrescar);
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarBotonSecundario(btnVolverOrden);
            Tema.AplicarBotonPrimario(btnConfirmarPlan);
            Tema.AplicarBotonSecundario(btnVolverPlan);
            Tema.AplicarBotonPrimario(btnConfirmarCierre);
            Tema.AplicarBotonSecundario(btnVolverCierre);

            // AgregarFila aplica el estilo de "entrada"; acá se restituye el look de etiqueta.
            lblDetalleVenta.ForeColor = Tema.TextoSuave;
            lblDetalleVenta.BackColor = Tema.FondoApp;
            foreach (var chk in new[] { chkEncendido, chkConexiones, chkSO, chkDrivers })
            {
                chk.ForeColor = Tema.Texto;
                chk.BackColor = Color.Transparent;
            }
            if (chkEncendido.Parent != null) chkEncendido.Parent.BackColor = Tema.FondoApp;

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (Control panel in new[] { pnlGrilla, pnlFormOrden, pnlFormPlan, pnlFormCierre })
            {
                panel.BackColor = Tema.FondoApp;
                foreach (Control hijo in panel.Controls)
                    if (hijo is Panel && hijo != pnlDetalle) hijo.BackColor = Tema.FondoApp;
            }

            ActualizarDetalle();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("pcf_produccion_titulo");
            btnNueva.Text = "＋ " + t.Obtener("pcf_nueva_orden");
            btnRefrescar.Text = t.Obtener("pcf_refrescar");

            lblFormOrdenTit.Text = t.Obtener("pcf_nueva_orden");
            lblVenta.Text = t.Obtener("pcf_venta_senada") + ":";
            lblEntrega.Text = t.Obtener("pcf_f_entrega") + ":";
            btnRegistrar.Text = t.Obtener("pcf_registrar_orden");
            btnVolverOrden.Text = t.Obtener("volver");

            lblFormPlanTit.Text = t.Obtener("pcf_asignar_linea");
            lblLinea.Text = t.Obtener("pcf_linea") + ":";
            lblInicio.Text = t.Obtener("pcf_f_inicio") + ":";
            lblResp.Text = t.Obtener("pcf_responsable") + ":";
            btnConfirmarPlan.Text = t.Obtener("pcf_asignar");
            btnVolverPlan.Text = t.Obtener("volver");

            lblFormCierreTit.Text = t.Obtener("pcf_cerrar_orden");
            lblChecklist.Text = t.Obtener("pcf_control_calidad") + ":";
            lblRespCc.Text = t.Obtener("pcf_responsable") + ":";
            lblObs.Text = t.Obtener("pcf_observaciones") + ":";
            chkEncendido.Text = t.Obtener("pcf_cc_encendido");
            chkConexiones.Text = t.Obtener("pcf_cc_conexiones");
            chkSO.Text = t.Obtener("pcf_cc_so");
            chkDrivers.Text = t.Obtener("pcf_cc_drivers");
            btnConfirmarCierre.Text = t.Obtener("pcf_confirmar_cierre");
            btnVolverCierre.Text = t.Obtener("volver");

            if (grilla.DataSource != null) CargarOrdenes();
            else ActualizarDetalle();
        }

        // ══════════════════════════════════════════════════════════════
        //  Datos
        // ══════════════════════════════════════════════════════════════
        private void CargarOrdenes()
        {
            try
            {
                int? sel = (grilla.CurrentRow?.DataBoundItem as OrdenVm)?.Numero;

                var todas = _ordenesBLL.ObtenerTodas() ?? new List<OrdenProduccion06AV>();
                var vm = todas.OrderByDescending(o => o.NumeroOrden).Select(o =>
                {
                    EstadoInfo(o.Estado, out string txt, out Color col);
                    return new OrdenVm
                    {
                        Numero = o.NumeroOrden,
                        Venta = o.NumeroVenta,
                        Cliente = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "",
                        Entrega = o.FechaEntregaEstimada.ToShortDateString(),
                        Linea = o.LineaEnsamblaje != null ? o.LineaEnsamblaje.Nombre : "-",
                        Estado = txt,
                        Serie = string.IsNullOrWhiteSpace(o.NumeroSerie) ? "-" : o.NumeroSerie,
                        EstadoColor = col,
                        Orden = o
                    };
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
            H("Numero", "pcf_col_numero");
            H("Venta", "pcf_venta");
            H("Cliente", "pcf_cliente");
            H("Entrega", "pcf_f_entrega");
            H("Linea", "pcf_linea");
            H("Estado", "pcf_col_estado");
            H("Serie", "pcf_nro_serie");
            if (grilla.Columns["Numero"] != null) grilla.Columns["Numero"].FillWeight = 26;
            if (grilla.Columns["Venta"] != null) grilla.Columns["Venta"].FillWeight = 26;

            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is OrdenVm vm && grilla.Columns["Estado"] != null)
                {
                    row.Cells["Estado"].Style.ForeColor = vm.EstadoColor;
                    row.Cells["Estado"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
        }

        private OrdenProduccion06AV OrdenSeleccionada() =>
            (grilla.CurrentRow?.DataBoundItem as OrdenVm)?.Orden;

        private void SeleccionarOrden(int numero)
        {
            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is OrdenVm vm && vm.Numero == numero)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grilla.CurrentCell = row.Cells[0];
                    return;
                }
        }

        private void EstadoInfo(EstadoOrdenProduccion06AV e, out string texto, out Color color)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoOrdenProduccion06AV.Pendiente: texto = t.Obtener("pcf_est_op_pendiente"); color = Tema.Advertencia; break;
                case EstadoOrdenProduccion06AV.Planificada: texto = t.Obtener("pcf_est_op_planificada"); color = Tema.Primario; break;
                case EstadoOrdenProduccion06AV.EnEnsamblaje: texto = t.Obtener("pcf_est_op_ensamblaje"); color = Tema.Acento; break;
                case EstadoOrdenProduccion06AV.Finalizada: texto = t.Obtener("pcf_est_op_finalizada"); color = Tema.Exito; break;
                case EstadoOrdenProduccion06AV.EnRevision: texto = t.Obtener("pcf_est_op_revision"); color = Tema.Peligro; break;
                default: texto = t.Obtener("pcf_est_op_entregada"); color = Tema.TextoSuave; break;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Panel de detalle: progreso + "¿Qué sigue?"
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
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_sel_orden_prod")));
                return;
            }

            var o = _ordenSel;
            string cli = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "";
            lblDetTitulo.Text = t.Obtener("pcf_orden") + " #" + o.NumeroOrden + "  —  " + cli;
            EstadoInfo(o.Estado, out string estTxt, out Color estColor);
            lblDetEstado.Text = "● " + estTxt;
            lblDetEstado.ForeColor = estColor;
            lblDetEstado.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            // Progreso (stepper vertical). "En revisión" se dibuja sobre el paso de ensamblaje.
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_progreso"), fuerte: true));
            string[] pasos =
            {
                t.Obtener("pcf_est_op_pendiente"), t.Obtener("pcf_est_op_planificada"),
                t.Obtener("pcf_est_op_ensamblaje"), t.Obtener("pcf_est_op_finalizada"),
                t.Obtener("pcf_est_op_entregada")
            };
            int actual = o.Estado == EstadoOrdenProduccion06AV.EnRevision ? 2 : (int)o.Estado;
            for (int i = 0; i < pasos.Length; i++)
            {
                string marca = i < actual ? "✔" : (i == actual ? "●" : "○");
                Color c = i < actual ? Tema.Exito : (i == actual ? estColor : Tema.TextoSuave);
                flpDetalle.Controls.Add(PasoLabel(marca + "   " + pasos[i], c, i == actual));
            }

            flpDetalle.Controls.Add(Separador());

            // Venta de origen
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_venta")));
            flpDetalle.Controls.Add(TextoSecundario(
                "#" + o.NumeroVenta + "   ·   " +
                (o.Computadora != null ? o.Computadora.Nombre : "-") + "   ·   " + o.PrecioTotal.ToString("C0")));

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_f_entrega")));
            flpDetalle.Controls.Add(TextoSecundario(o.FechaEntregaEstimada.ToShortDateString()));

            if (o.LineaEnsamblaje != null || !string.IsNullOrWhiteSpace(o.ResponsableTecnico))
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_linea") + " / " + t.Obtener("pcf_responsable")));
                string ln = o.LineaEnsamblaje != null ? o.LineaEnsamblaje.Nombre : "-";
                string rp = string.IsNullOrWhiteSpace(o.ResponsableTecnico) ? "-" : o.ResponsableTecnico;
                flpDetalle.Controls.Add(TextoSecundario(ln + "   ·   " + rp));
            }

            // Control de calidad y N° de serie
            if (o.ControlCalidad != null && o.ControlCalidad.Registrado)
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_control_calidad")));
                var cc = o.ControlCalidad;
                flpDetalle.Controls.Add(TextoSecundario(
                    Tick(cc.Encendido) + " " + t.Obtener("pcf_cc_encendido") + "    " +
                    Tick(cc.Conexiones) + " " + t.Obtener("pcf_cc_conexiones")));
                flpDetalle.Controls.Add(TextoSecundario(
                    Tick(cc.SistemaOperativo) + " " + t.Obtener("pcf_cc_so") + "    " +
                    Tick(cc.Drivers) + " " + t.Obtener("pcf_cc_drivers")));
                if (!string.IsNullOrWhiteSpace(cc.Observaciones))
                    flpDetalle.Controls.Add(TextoSecundario(cc.Observaciones));
            }

            if (!string.IsNullOrWhiteSpace(o.NumeroSerie))
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_nro_serie")));
                flpDetalle.Controls.Add(TextoSecundario(o.NumeroSerie));
            }

            // ── ¿Qué sigue? ──────────────────────────────────────
            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            switch (o.Estado)
            {
                case EstadoOrdenProduccion06AV.Pendiente:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_planificar")));
                    AddAccion(t.Obtener("pcf_asignar_linea"), Tema.AplicarBotonPrimario, AbrirFormPlan);
                    break;
                case EstadoOrdenProduccion06AV.Planificada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_ensamblar")));
                    AddAccion(t.Obtener("pcf_iniciar_ensamblaje"), Tema.AplicarBotonPrimario, Ensamblar);
                    break;
                case EstadoOrdenProduccion06AV.EnEnsamblaje:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_cerrar")));
                    AddAccion(t.Obtener("pcf_cerrar_orden"), Tema.AplicarBotonPrimario, AbrirFormCierre);
                    break;
                case EstadoOrdenProduccion06AV.EnRevision:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_revision")));
                    AddAccion(t.Obtener("pcf_reintentar_cc"), Tema.AplicarBotonPrimario, AbrirFormCierre);
                    break;
                case EstadoOrdenProduccion06AV.Finalizada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_entregar")));
                    break;
                case EstadoOrdenProduccion06AV.Entregada:
                    flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_op_entregada")));
                    break;
            }

            if (o.Estado == EstadoOrdenProduccion06AV.Planificada ||
                o.Estado == EstadoOrdenProduccion06AV.EnEnsamblaje ||
                o.Estado == EstadoOrdenProduccion06AV.EnRevision)
                AddAccion("←  " + t.Obtener("pcf_volver_atras"), Tema.AplicarBotonSecundario, VolverAtras);
        }

        private static string Tick(bool ok) => ok ? "✔" : "✘";

        private void AddAccion(string texto, Action<Button> estilo, Action onClick)
        {
            var b = BotonDetalle(texto, estilo);
            b.Click += (s, e) => onClick();
            flpDetalle.Controls.Add(b);
        }

        // ── Helpers de detalle ───────────────────────────────────────
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

        private Label PasoLabel(string texto, Color color, bool bold) => new Label
        {
            Text = texto, AutoSize = true, Margin = new Padding(2, 1, 0, 1), ForeColor = color,
            Font = new Font("Segoe UI", 9.5f, bold ? FontStyle.Bold : FontStyle.Regular)
        };

        private Panel Separador() => new Panel { Height = 1, Width = 352, BackColor = Tema.Borde, Margin = new Padding(0, 10, 0, 6) };

        private Button BotonDetalle(string texto, Action<Button> estilo)
        {
            var b = new Button { Text = texto, Width = 220, Height = 32, Margin = new Padding(0, 4, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            estilo(b);
            return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  Navegación entre vistas
        // ══════════════════════════════════════════════════════════════
        private void MostrarGrilla()
        {
            pnlFormOrden.Visible = false;
            pnlFormPlan.Visible = false;
            pnlFormCierre.Visible = false;
            pnlGrilla.Visible = true;
            pnlGrilla.BringToFront();
            ActualizarDetalle();
        }

        /// <summary>CU04: el gerente elige una venta señada sin orden y fija la fecha de entrega.</summary>
        private void AbrirFormOrden()
        {
            List<Venta06AV> disponibles;
            try { disponibles = _ordenesBLL.ObtenerVentasDisponibles() ?? new List<Venta06AV>(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            // Escenario alternativo 3.1 del CU04.
            if (disponibles.Count == 0)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_sin_ventas_pendientes"));
                return;
            }

            cboVenta.DataSource = null;
            cboVenta.DisplayMember = "Texto";
            cboVenta.DataSource = disponibles.Select(v => new VentaVm
            {
                Venta = v,
                Texto = $"#{v.NumeroVenta} — {(v.Cliente != null ? v.Cliente.NombreCompleto : "")} — " +
                        $"{(v.Computadora != null ? v.Computadora.Nombre : "")} — {v.PrecioTotal:C0}"
            }).ToList();

            dtpEntrega.Value = disponibles[0].FechaEntregaEstimada < DateTime.Today
                ? DateTime.Today.AddDays(15) : disponibles[0].FechaEntregaEstimada;
            MostrarDetalleVenta();

            pnlGrilla.Visible = false;
            pnlFormPlan.Visible = false;
            pnlFormCierre.Visible = false;
            pnlFormOrden.Visible = true;
            pnlFormOrden.BringToFront();
        }

        private void MostrarDetalleVenta()
        {
            var t = GestorIdioma06AV.Instancia;
            var v = (cboVenta.SelectedItem as VentaVm)?.Venta;
            if (v == null) { lblDetalleVenta.Text = string.Empty; return; }

            var sena = v.Sena;
            lblDetalleVenta.Text =
                $"{t.Obtener("pcf_total")}: {v.PrecioTotal:C0}   ·   " +
                $"{t.Obtener("pcf_sena")}: {(sena != null ? sena.Monto.ToString("C0") : "-")}   ·   " +
                $"{t.Obtener("pcf_saldo")}: {v.SaldoPendiente:C0}\r\n" +
                $"{t.Obtener("pcf_f_entrega_estimada")}: {v.FechaEntregaEstimada:dd/MM/yyyy}";

            if (v.FechaEntregaEstimada >= DateTime.Today) dtpEntrega.Value = v.FechaEntregaEstimada;
        }

        private void AbrirFormPlan()
        {
            _ordenPlan = OrdenSeleccionada();
            if (_ordenPlan == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }

            List<LineaEnsamblaje06AV> lineas;
            try { lineas = (_lineasBLL.ObtenerTodas() ?? new List<LineaEnsamblaje06AV>()).Where(l => l.Disponible).ToList(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }

            // Escenario alternativo 1.1 del CU05.
            if (lineas.Count == 0)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_sin_lineas"));
                return;
            }

            cboLinea.DataSource = null;
            cboLinea.DataSource = lineas;
            dtpInicio.Value = DateTime.Today;
            txtResp.Clear();
            lblFormPlanTit.Text = $"{GestorIdioma06AV.Instancia.Obtener("pcf_asignar_linea")} — " +
                                  $"{GestorIdioma06AV.Instancia.Obtener("pcf_orden")} #{_ordenPlan.NumeroOrden}";

            pnlGrilla.Visible = false;
            pnlFormOrden.Visible = false;
            pnlFormCierre.Visible = false;
            pnlFormPlan.Visible = true;
            pnlFormPlan.BringToFront();
        }

        private void AbrirFormCierre()
        {
            _ordenCierre = OrdenSeleccionada();
            if (_ordenCierre == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }

            var cc = _ordenCierre.ControlCalidad ?? new ControlCalidad06AV();
            chkEncendido.Checked = cc.Encendido;
            chkConexiones.Checked = cc.Conexiones;
            chkSO.Checked = cc.SistemaOperativo;
            chkDrivers.Checked = cc.Drivers;
            txtObs.Text = cc.Observaciones ?? "";
            txtRespCc.Text = string.IsNullOrWhiteSpace(cc.Responsable) ? _ordenCierre.ResponsableTecnico : cc.Responsable;

            lblFormCierreTit.Text = $"{GestorIdioma06AV.Instancia.Obtener("pcf_cerrar_orden")} — " +
                                    $"{GestorIdioma06AV.Instancia.Obtener("pcf_orden")} #{_ordenCierre.NumeroOrden}";

            pnlGrilla.Visible = false;
            pnlFormOrden.Visible = false;
            pnlFormPlan.Visible = false;
            pnlFormCierre.Visible = true;
            pnlFormCierre.BringToFront();
        }

        // ══════════════════════════════════════════════════════════════
        //  Acciones (delegan en la BLL)
        // ══════════════════════════════════════════════════════════════
        private void RegistrarOrden()
        {
            var v = (cboVenta.SelectedItem as VentaVm)?.Venta;
            if (v == null) { MostrarError("Elegí una venta señada."); return; }

            try
            {
                var orden = _ordenesBLL.CrearOrden(v.NumeroVenta, dtpEntrega.Value);
                MostrarGrilla();
                CargarOrdenes();
                SeleccionarOrden(orden.NumeroOrden);
                ConfirmacionForm.MostrarInfo(
                    $"Orden #{orden.NumeroOrden} registrada sobre la venta #{v.NumeroVenta}.",
                    GestorIdioma06AV.Instancia.Obtener("pcf_produccion_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AsignarLinea()
        {
            if (_ordenPlan == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            var linea = cboLinea.SelectedItem as LineaEnsamblaje06AV;
            if (linea == null) { MostrarError("Elegí una línea de ensamblaje."); return; }
            try
            {
                _ordenesBLL.AsignarLinea(_ordenPlan.NumeroOrden, linea.Id, dtpInicio.Value, txtResp.Text.Trim());
                MostrarGrilla();
                CargarOrdenes();
                SeleccionarOrden(_ordenPlan.NumeroOrden);
                ConfirmacionForm.MostrarInfo("Orden planificada.",
                    GestorIdioma06AV.Instancia.Obtener("pcf_produccion_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Ensamblar()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                _ordenesBLL.IniciarEnsamblaje(o.NumeroOrden);
                CargarOrdenes();
                ConfirmacionForm.MostrarInfo("Orden en ensamblaje.",
                    GestorIdioma06AV.Instancia.Obtener("pcf_produccion_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        /// <summary>CU06: registra el control de calidad; si aprueba, descuenta stock y cierra.</summary>
        private void CerrarOrden()
        {
            if (_ordenCierre == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }

            var cc = new ControlCalidad06AV
            {
                Encendido = chkEncendido.Checked,
                Conexiones = chkConexiones.Checked,
                SistemaOperativo = chkSO.Checked,
                Drivers = chkDrivers.Checked,
                Observaciones = txtObs.Text.Trim(),
                Responsable = txtRespCc.Text.Trim()
            };

            if (!cc.Aprobado && string.IsNullOrWhiteSpace(cc.Observaciones))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_cc_obs_requerida"));
                return;
            }

            try
            {
                string serie = _ordenesBLL.RegistrarControlCalidad(_ordenCierre.NumeroOrden, cc);
                MostrarGrilla();
                CargarOrdenes();
                SeleccionarOrden(_ordenCierre.NumeroOrden);

                if (serie != null)
                    ConfirmacionForm.MostrarInfo(
                        $"Control de calidad aprobado.\nStock descontado y N° de serie asignado: {serie}.\n" +
                        "La computadora queda lista para su retiro desde Ventas.",
                        GestorIdioma06AV.Instancia.Obtener("pcf_produccion_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                else
                    ConfirmacionForm.MostrarInfo(
                        "El equipo no pasó el control de calidad: la orden queda En revisión.",
                        GestorIdioma06AV.Instancia.Obtener("pcf_produccion_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void VolverAtras()
        {
            var o = OrdenSeleccionada();
            if (o == null) return;
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Volver la orden #{o.NumeroOrden} al paso anterior?",
                "Volver atrás", ConfirmacionForm.TipoConfirmacion.Advertencia,
                GestorIdioma06AV.Instancia.Obtener("volver"), GestorIdioma06AV.Instancia.Obtener("cancelar"),
                FindForm());
            if (!ok) return;
            try
            {
                _ordenesBLL.VolverAtras(o.NumeroOrden);
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════
        //  View-models
        // ══════════════════════════════════════════════════════════════
        private class OrdenVm
        {
            public int Numero { get; set; }
            public int Venta { get; set; }
            public string Cliente { get; set; }
            public string Entrega { get; set; }
            public string Linea { get; set; }
            public string Estado { get; set; }
            public string Serie { get; set; }

            [Browsable(false)] public Color EstadoColor { get; set; }
            [Browsable(false)] public OrdenProduccion06AV Orden { get; set; }
        }

        private class VentaVm
        {
            public Venta06AV Venta { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
        }
    }
}
