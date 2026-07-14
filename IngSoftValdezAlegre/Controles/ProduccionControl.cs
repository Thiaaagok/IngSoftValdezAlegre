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
    /// Producción (RFN1) rediseñado como una sola pantalla centrada en la orden.
    /// La lista muestra el estado de cada orden y, al seleccionarla, el panel de detalle
    /// muestra el PROGRESO (Pendiente → Planificada → En ensamblaje → Finalizada → Entregada)
    /// y el ÚNICO paso siguiente ("¿Qué sigue?"), además de "Registrar seña" y
    /// "Volver al paso anterior". Los componentes de una orden "Personalizada" se eligen con
    /// el asistente "Armá tu PC". La lógica de negocio no cambia (solo se agregó VolverAtras).
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ProduccionControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly OrdenProduccionBLL06AV _ordenesBLL = new OrdenProduccionBLL06AV();
        private readonly ClientesBLL06AV _clientesBLL = new ClientesBLL06AV();
        private readonly ComponentesBLL06AV _componentesBLL = new ComponentesBLL06AV();
        private readonly LineasEnsamblajeBLL06AV _lineasBLL = new LineasEnsamblajeBLL06AV();
        private readonly ModelosEstandarBLL06AV _modelosBLL = new ModelosEstandarBLL06AV();
        private bool _cargandoCombos;
        private List<Componente06AV> _componentesElegidos = new List<Componente06AV>();
        private OrdenProduccion06AV _ordenSel;

        // Vistas
        private Panel pnlGrilla, pnlFormOrden, pnlFormPlan;

        // Hub (lista + detalle)
        private Label lblTitulo;
        private DataGridView grilla;
        private Button btnNueva, btnRefrescar;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // Formulario "Nueva orden"
        private Label lblFormOrdenTit, lblCliente, lblTipo, lblModelo, lblComp, lblEntrega;
        private ResumenPcControl06AV _resumen;
        private ComboBox cboCliente, cboTipo, cboModelo;
        private Button btnArmar;
        private DateTimePicker dtpEntrega;
        private Button btnRegistrar, btnVolverOrden;

        // Formulario "Planificar"
        private Label lblFormPlanTit, lblLinea, lblInicio, lblResp;
        private ComboBox cboLinea;
        private DateTimePicker dtpInicio;
        private TextBox txtResp;
        private Button btnConfirmarPlan, btnVolverPlan;
        private OrdenProduccion06AV _ordenPlan;

        public ProduccionControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            CargarCombos();
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

            Controls.Add(pnlFormOrden);
            Controls.Add(pnlFormPlan);
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
            lblCliente = new Label(); lblTipo = new Label(); lblModelo = new Label();
            lblComp = new Label(); lblEntrega = new Label();
            _resumen = new ResumenPcControl06AV();
            cboCliente = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
            cboTipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
            cboTipo.DataSource = Enum.GetValues(typeof(TipoConfiguracion06AV));
            cboTipo.SelectedIndexChanged += (s, e) => ActualizarModeloSegunTipo();
            cboModelo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
            cboModelo.SelectedIndexChanged += (s, e) => AplicarModeloSeleccionado();
            btnArmar = new Button { Width = 190, Height = 32, AutoSize = false };
            btnArmar.Click += (s, e) => AbrirAsistenteComponentes();
            dtpEntrega = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7), Width = 360 };
            btnRegistrar = NuevoBoton(150);
            btnVolverOrden = NuevoBoton(120);
            btnRegistrar.Click += (s, e) => RegistrarOrden();
            btnVolverOrden.Click += (s, e) => MostrarGrilla();

            var pnlComp = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0)
            };
            pnlComp.Controls.Add(btnArmar);
            pnlComp.Controls.Add(_resumen);

            var tablaOrden = NuevaTabla();
            AgregarFila(tablaOrden, lblCliente, cboCliente);
            AgregarFila(tablaOrden, lblTipo, cboTipo);
            AgregarFila(tablaOrden, lblModelo, cboModelo);
            AgregarFila(tablaOrden, lblComp, pnlComp);
            AgregarFila(tablaOrden, lblEntrega, dtpEntrega);
            var contOrden = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            contOrden.Controls.Add(tablaOrden);
            var barraOrdenTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraOrdenTop.Controls.Add(lblFormOrdenTit);
            var barraOrdenBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraOrdenBot.Controls.Add(BarraBotones(btnVolverOrden, btnRegistrar));

            pnlFormOrden = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormOrden.Controls.Add(contOrden);
            pnlFormOrden.Controls.Add(barraOrdenTop);
            pnlFormOrden.Controls.Add(barraOrdenBot);
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
            btnConfirmarPlan.Click += (s, e) => Planificar();
            btnVolverPlan.Click += (s, e) => MostrarGrilla();

            var tablaPlan = NuevaTabla();
            AgregarFila(tablaPlan, lblLinea, cboLinea);
            AgregarFila(tablaPlan, lblInicio, dtpInicio);
            AgregarFila(tablaPlan, lblResp, txtResp);
            var contPlan = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), AutoScroll = true };
            contPlan.Controls.Add(tablaPlan);
            var barraPlanTop = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraPlanTop.Controls.Add(lblFormPlanTit);
            var barraPlanBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraPlanBot.Controls.Add(BarraBotones(btnVolverPlan, btnConfirmarPlan));

            pnlFormPlan = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormPlan.Controls.Add(contPlan);
            pnlFormPlan.Controls.Add(barraPlanTop);
            pnlFormPlan.Controls.Add(barraPlanBot);
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
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
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
            Tema.AplicarSubtitulo(lblDetTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnNueva);
            Tema.AplicarBotonSecundario(btnRefrescar);
            Tema.AplicarBotonAcento(btnArmar);
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarBotonSecundario(btnVolverOrden);
            Tema.AplicarBotonPrimario(btnConfirmarPlan);
            Tema.AplicarBotonSecundario(btnVolverPlan);

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            foreach (Control panel in new[] { pnlGrilla, pnlFormOrden, pnlFormPlan })
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
            lblCliente.Text = t.Obtener("pcf_cliente") + ":";
            lblTipo.Text = t.Obtener("tipo") + ":";
            lblModelo.Text = t.Obtener("pcf_modelo") + ":";
            lblComp.Text = t.Obtener("pcf_componentes") + ":";
            btnArmar.Text = t.Obtener("pcf_elegir_componentes");
            lblEntrega.Text = t.Obtener("pcf_f_entrega") + ":";
            btnRegistrar.Text = t.Obtener("pcf_registrar_orden");
            btnVolverOrden.Text = t.Obtener("volver");
            ActualizarResumenComp();

            lblFormPlanTit.Text = t.Obtener("pcf_planificar");
            lblLinea.Text = t.Obtener("pcf_linea") + ":";
            lblInicio.Text = t.Obtener("pcf_f_inicio") + ":";
            lblResp.Text = t.Obtener("pcf_responsable") + ":";
            btnConfirmarPlan.Text = t.Obtener("pcf_planificar");
            btnVolverPlan.Text = t.Obtener("volver");

            if (grilla.DataSource != null) CargarOrdenes();
            else ActualizarDetalle();
        }

        // ══════════════════════════════════════════════════════════════
        //  Datos
        // ══════════════════════════════════════════════════════════════
        private void CargarCombos()
        {
            try
            {
                _cargandoCombos = true;
                cboCliente.DataSource = _clientesBLL.ObtenerTodos();
                cboLinea.DataSource = _lineasBLL.ObtenerTodas();
                cboModelo.DataSource = _modelosBLL.ObtenerTodos();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
            finally { _cargandoCombos = false; }
        }

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
                        Cliente = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "",
                        Entrega = o.FechaEntrega.ToShortDateString(),
                        Estado = txt,
                        Saldo = o.SaldoPendiente.ToString("C0"),
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
            H("Cliente", "pcf_cliente");
            H("Entrega", "pcf_f_entrega");
            H("Estado", "pcf_col_estado");
            H("Saldo", "pcf_saldo");
            if (grilla.Columns["Numero"] != null) grilla.Columns["Numero"].FillWeight = 32;
            if (grilla.Columns["Saldo"] != null)
                grilla.Columns["Saldo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

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

            // Progreso (stepper vertical)
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_progreso"), fuerte: true));
            string[] pasos =
            {
                t.Obtener("pcf_est_op_pendiente"), t.Obtener("pcf_est_op_planificada"),
                t.Obtener("pcf_est_op_ensamblaje"), t.Obtener("pcf_est_op_finalizada"),
                t.Obtener("pcf_est_op_entregada")
            };
            int actual = (int)o.Estado;
            for (int i = 0; i < pasos.Length; i++)
            {
                string marca = i < actual ? "✔" : (i == actual ? "●" : "○");
                Color c = i < actual ? Tema.Exito : (i == actual ? estColor : Tema.TextoSuave);
                flpDetalle.Controls.Add(PasoLabel(marca + "   " + pasos[i], c, i == actual));
            }

            flpDetalle.Controls.Add(Separador());

            // Datos
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_f_entrega")));
            flpDetalle.Controls.Add(TextoSecundario(o.FechaEntrega.ToShortDateString()));

            if (o.LineaEnsamblaje != null || !string.IsNullOrWhiteSpace(o.ResponsableTecnico))
            {
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_linea") + " / " + t.Obtener("pcf_responsable")));
                string ln = o.LineaEnsamblaje != null ? o.LineaEnsamblaje.Nombre : "-";
                string rp = string.IsNullOrWhiteSpace(o.ResponsableTecnico) ? "-" : o.ResponsableTecnico;
                flpDetalle.Controls.Add(TextoSecundario(ln + "   ·   " + rp));
            }

            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_total") + " / " + t.Obtener("pcf_abonado") + " / " + t.Obtener("pcf_saldo")));
            flpDetalle.Controls.Add(TextoSecundario(
                o.PrecioTotal.ToString("C0") + "   ·   " + o.TotalAbonado.ToString("C0") + "   ·   " + o.SaldoPendiente.ToString("C0")));

            bool tieneSena = o.Pagos != null && o.Pagos.Any(p => p.Tipo == TipoPago06AV.Sena);
            flpDetalle.Controls.Add(TextoSecundario(
                (tieneSena ? "✔ " : "○ ") + (tieneSena ? t.Obtener("pcf_sena_registrada") : t.Obtener("pcf_sena_pendiente"))));

            // "¿Qué sigue?"
            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            switch (o.Estado)
            {
                case EstadoOrdenProduccion06AV.Pendiente:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_planificar")));
                    AddAccion(t.Obtener("pcf_planificar"), Tema.AplicarBotonPrimario, AbrirFormPlan);
                    break;
                case EstadoOrdenProduccion06AV.Planificada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_ensamblar")));
                    AddAccion(t.Obtener("pcf_iniciar_ensamblaje"), Tema.AplicarBotonPrimario, Ensamblar);
                    break;
                case EstadoOrdenProduccion06AV.EnEnsamblaje:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_finalizar")));
                    AddAccion(t.Obtener("pcf_marcar_finalizada"), Tema.AplicarBotonPrimario, FinalizarOrden);
                    break;
                case EstadoOrdenProduccion06AV.Finalizada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_op_entregar")));
                    AddAccion(t.Obtener("pcf_entregar"), Tema.AplicarBotonPrimario, Entregar);
                    break;
                case EstadoOrdenProduccion06AV.Entregada:
                    flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_op_entregada")));
                    break;
            }

            // Acciones secundarias
            if (o.Estado != EstadoOrdenProduccion06AV.Entregada && !tieneSena)
                AddAccion(t.Obtener("pcf_registrar_sena"), Tema.AplicarBotonAcento, Sena);

            if (o.Estado == EstadoOrdenProduccion06AV.Planificada ||
                o.Estado == EstadoOrdenProduccion06AV.EnEnsamblaje ||
                o.Estado == EstadoOrdenProduccion06AV.Finalizada)
                AddAccion("←  " + t.Obtener("pcf_volver_atras"), Tema.AplicarBotonSecundario, VolverAtras);
        }

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
            pnlGrilla.Visible = true;
            pnlGrilla.BringToFront();
            ActualizarDetalle();
        }

        private void AbrirFormOrden()
        {
            CargarCombos();
            if (cboTipo.Items.Count > 0) cboTipo.SelectedIndex = 0;
            if (cboModelo.Items.Count > 0) cboModelo.SelectedIndex = 0;
            _componentesElegidos = new List<Componente06AV>();
            dtpEntrega.Value = DateTime.Today.AddDays(7);
            ActualizarModeloSegunTipo();
            ActualizarResumenComp();
            pnlGrilla.Visible = false;
            pnlFormPlan.Visible = false;
            pnlFormOrden.Visible = true;
            pnlFormOrden.BringToFront();
        }

        private void AbrirFormPlan()
        {
            _ordenPlan = OrdenSeleccionada();
            if (_ordenPlan == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            CargarCombos();
            dtpInicio.Value = DateTime.Today;
            txtResp.Clear();
            lblFormPlanTit.Text = $"{GestorIdioma06AV.Instancia.Obtener("pcf_planificar")} — {GestorIdioma06AV.Instancia.Obtener("pcf_orden")} #{_ordenPlan.NumeroOrden}";
            pnlGrilla.Visible = false;
            pnlFormOrden.Visible = false;
            pnlFormPlan.Visible = true;
            pnlFormPlan.BringToFront();
        }

        // ── Componentes: modelo estándar o asistente "Armá tu PC" ────
        private void ActualizarModeloSegunTipo()
        {
            bool estandar = cboTipo.SelectedItem is TipoConfiguracion06AV tc && tc == TipoConfiguracion06AV.Estandar;
            lblModelo.Enabled = estandar;
            cboModelo.Enabled = estandar;
            btnArmar.Enabled = !estandar;
            if (estandar) AplicarModeloSeleccionado();
            else
            {
                _componentesElegidos = new List<Componente06AV>();
                ActualizarResumenComp();
            }
        }

        private void AplicarModeloSeleccionado()
        {
            if (_cargandoCombos) return;
            if (!(cboTipo.SelectedItem is TipoConfiguracion06AV tc) || tc != TipoConfiguracion06AV.Estandar) return;
            if (!(cboModelo.SelectedItem is ModeloEstandar06AV modelo)) return;
            _componentesElegidos = new List<Componente06AV>(modelo.Componentes ?? new List<Componente06AV>());
            ActualizarResumenComp();
        }

        private void AbrirAsistenteComponentes()
        {
            List<Componente06AV> todos;
            try { todos = _componentesBLL.ObtenerTodos() ?? new List<Componente06AV>(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }
            if (todos.Count == 0) { MostrarError("No hay componentes cargados."); return; }

            using (var dlg = new FRMArmarPc06AV(todos, _componentesElegidos))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _componentesElegidos = dlg.Seleccionados;
                    ActualizarResumenComp();
                }
            }
        }

        private void ActualizarResumenComp() => _resumen.Mostrar(_componentesElegidos);

        // ══════════════════════════════════════════════════════════════
        //  Acciones (delegan en la BLL)
        // ══════════════════════════════════════════════════════════════
        private void RegistrarOrden()
        {
            var cliente = cboCliente.SelectedItem as Cliente06AV;
            if (cliente == null) { MostrarError("Elegí un cliente."); return; }
            if (_componentesElegidos == null || _componentesElegidos.Count == 0)
            { MostrarError("Elegí al menos un componente (botón “Armá tu PC”) o un modelo estándar."); return; }

            var pc = new Computadora06AV
            {
                TipoConfiguracion = (TipoConfiguracion06AV)(cboTipo.SelectedItem ?? TipoConfiguracion06AV.Estandar),
                Nombre = cboModelo.Enabled && cboModelo.SelectedItem is ModeloEstandar06AV m
                    ? m.Nombre : cboTipo.SelectedItem?.ToString()
            };
            foreach (var c in _componentesElegidos) pc.Componentes.Add(c);

            try
            {
                var orden = _ordenesBLL.RegistrarOrden(cliente, pc, dtpEntrega.Value);
                MostrarGrilla();
                CargarOrdenes();
                SeleccionarOrden(orden.NumeroOrden);
                ConfirmacionForm.MostrarInfo(
                    $"Orden #{orden.NumeroOrden} registrada. Total: ${orden.PrecioTotal:0.00}.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Sena()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                decimal sena = _ordenesBLL.RegistrarSena(o.NumeroOrden);
                CargarOrdenes();
                string recibo = ComprobantePcFactory06AV.GenerarReciboSena(o, sena);
                ComprobantePcFactory06AV.Abrir(recibo);
                ConfirmacionForm.MostrarInfo($"Seña registrada: ${sena:0.00}.\nRecibo generado en:\n{recibo}",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
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
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void FinalizarOrden()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                _ordenesBLL.Finalizar(o.NumeroOrden);
                CargarOrdenes();
                ConfirmacionForm.MostrarInfo("Orden finalizada, lista para entregar.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Planificar()
        {
            if (_ordenPlan == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            var linea = cboLinea.SelectedItem as LineaEnsamblaje06AV;
            if (linea == null) { MostrarError("Elegí una línea de ensamblaje."); return; }
            try
            {
                _ordenesBLL.Planificar(_ordenPlan.NumeroOrden, linea.Id, dtpInicio.Value, txtResp.Text.Trim());
                MostrarGrilla();
                CargarOrdenes();
                CargarCombos();
                SeleccionarOrden(_ordenPlan.NumeroOrden);
                ConfirmacionForm.MostrarInfo("Orden planificada.",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Entregar()
        {
            var o = OrdenSeleccionada();
            if (o == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Entregar la orden #{o.NumeroOrden}? Se registrará el saldo pendiente (${o.SaldoPendiente:0.00}).",
                "Entregar", ConfirmacionForm.TipoConfirmacion.Advertencia, "Entregar", "Cancelar", FindForm());
            if (!ok) return;
            try
            {
                _ordenesBLL.Entregar(o.NumeroOrden);
                CargarOrdenes();
                CargarCombos();
                var entregada = _ordenesBLL.ObtenerPorNumero(o.NumeroOrden) ?? o;
                string factura = ComprobantePcFactory06AV.GenerarFactura(entregada);
                ComprobantePcFactory06AV.Abrir(factura);
                ConfirmacionForm.MostrarInfo($"Orden entregada y cerrada.\nFactura generada en:\n{factura}",
                    "Producción", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
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
                GestorIdioma06AV.Instancia.Obtener("volver"), "Cancelar", FindForm());
            if (!ok) return;
            try
            {
                _ordenesBLL.VolverAtras(o.NumeroOrden);
                CargarOrdenes();
                CargarCombos();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════
        //  View-model de la grilla
        // ══════════════════════════════════════════════════════════════
        private class OrdenVm
        {
            public int Numero { get; set; }
            public string Cliente { get; set; }
            public string Entrega { get; set; }
            public string Estado { get; set; }
            public string Saldo { get; set; }

            [Browsable(false)] public Color EstadoColor { get; set; }
            [Browsable(false)] public OrdenProduccion06AV Orden { get; set; }
        }
    }
}
