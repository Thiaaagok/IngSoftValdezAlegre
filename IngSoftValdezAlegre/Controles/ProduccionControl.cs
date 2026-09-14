using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
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

        // Vista de proceso: tablero de estaciones (operar) + grilla clásica (consultar).
        private TableroEstacionesControl06AV tablero;
        private RielEnsamblajeControl06AV riel;
        private Button btnVista;
        private bool _modoTablero = true;
        private Button btnDetalle;
        private bool _detalleVisible = true;
        private OrdenProduccion06AV _ordenElegidaTablero;

        // Formulario "Nueva orden" (CU04)
        private Label lblFormOrdenTit, lblVenta, lblDetalleVenta, lblEntrega, lblOrdenAyuda;
        private FlowLayoutPanel flpVentas;
        private FichaDatos06AV fichaVenta;
        private ComboBox cboVenta;
        private DateTimePicker dtpEntrega;
        private Button btnRegistrar, btnVolverOrden;

        // Formulario "Asignar línea" (CU05)
        private Label lblFormPlanTit, lblLinea, lblInicio, lblResp, lblPlanAyuda, lblSinLineas;
        private FlowLayoutPanel flpLineas;
        private LineaEnsamblaje06AV _lineaElegida;
        private DateTimePicker dtpInicio;
        private TextBox txtResp;
        private Button btnConfirmarPlan, btnVolverPlan;
        private OrdenProduccion06AV _ordenPlan;

        // Formulario "Cerrar orden" (CU06)
        private Label lblFormCierreTit, lblChecklist, lblObs, lblRespCc, lblCierreAyuda, lblVeredicto;
        private ItemChequeo06AV chkEncendido, chkConexiones, chkSO, chkDrivers;
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

            var estaciones = EstacionesProceso();
            tablero.DefinirColumnas(estaciones);
            riel.DefinirEstaciones(estaciones);
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
            btnVista = NuevoBoton(120);
            btnDetalle = NuevoBoton(110);
            btnNueva.Click += (s, e) => AbrirFormOrden();
            btnRefrescar.Click += (s, e) => CargarOrdenes();
            btnVista.Click += (s, e) => AlternarVista();
            btnDetalle.Click += (s, e) => MostrarDetalle(!_detalleVisible);

            // Tablero de estaciones: una columna por etapa del proceso físico.
            tablero = new TableroEstacionesControl06AV { Dock = DockStyle.Fill };
            tablero.TarjetaElegida += (s, tarjeta) =>
            {
                _ordenElegidaTablero = tarjeta?.Etiqueta as OrdenProduccion06AV;
                ActualizarDetalle();
            };
            tablero.AccionPedida += (s, tarjeta) =>
            {
                _ordenElegidaTablero = tarjeta?.Etiqueta as OrdenProduccion06AV;
                ActualizarDetalle();
                EjecutarAccionPrincipal();
            };

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { btnDetalle, btnVista, btnRefrescar, btnNueva });

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

            // Riel de ensamblaje: el avance como recorrido físico, no como lista de estados.
            riel = new RielEnsamblajeControl06AV { Dock = DockStyle.Top };

            pnlDetalle = new Panel { Dock = DockStyle.Right, Width = 400, Padding = new Padding(16, 14, 12, 12) };
            pnlDetalle.Controls.Add(flpDetalle);
            pnlDetalle.Controls.Add(riel);
            pnlDetalle.Controls.Add(lblDetEstado);
            pnlDetalle.Controls.Add(lblDetTitulo);

            pnlGrilla = new Panel { Dock = DockStyle.Fill };
            pnlGrilla.Controls.Add(grilla);
            pnlGrilla.Controls.Add(tablero);
            pnlGrilla.Controls.Add(pnlDetalle);
            pnlGrilla.Controls.Add(barraSup);

            grilla.Visible = false;   // arranca en modo tablero
        }

        /// <summary>
        /// CU04 — Nueva orden de producción. Un combo con una línea de texto larga y un
        /// bloque gris debajo obligaban a abrir el desplegable para comparar ventas.
        /// Ahora cada venta señada lista para producir es una tarjeta con su cliente, su
        /// equipo y su total, y la ficha de abajo muestra la venta elegida en detalle.
        /// </summary>
        private void ConstruirFormOrden()
        {
            lblFormOrdenTit = new Label { AutoSize = true, Location = new Point(16, 14) };
            lblOrdenAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };
            lblVenta = TituloSeccionProd(38);
            lblEntrega = new Label();
            lblDetalleVenta = new Label { AutoSize = true, Visible = false };

            flpVentas = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 176,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 2, 0, 2)
            };

            cboVenta = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420, Visible = false };
            fichaVenta = new FichaDatos06AV { Dock = DockStyle.Top, Height = 130 };

            dtpEntrega = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(10), Width = 260 };
            btnRegistrar = NuevoBoton(190);
            btnVolverOrden = NuevoBoton(120);
            btnRegistrar.Click += (s, e) => RegistrarOrden();
            btnVolverOrden.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            tabla.Dock = DockStyle.Top;
            AgregarFila(tabla, lblEntrega, dtpEntrega);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            cont.Controls.Add(fichaVenta);
            cont.Controls.Add(flpVentas);
            cont.Controls.Add(lblVenta);
            cont.Controls.Add(cboVenta);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            barraTop.Controls.Add(lblOrdenAyuda);
            barraTop.Controls.Add(lblFormOrdenTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverOrden, btnRegistrar));

            pnlFormOrden = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormOrden.Controls.Add(cont);
            pnlFormOrden.Controls.Add(barraTop);
            pnlFormOrden.Controls.Add(barraBot);
        }

        /// <summary>Título de bloque con alto holgado, para que la fuente no se corte.</summary>
        private static Label TituloSeccionProd(int alto) => new Label
        {
            Dock = DockStyle.Top,
            Height = alto,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 6, 0, 0)
        };

        /// <summary>Las ventas señadas listas para producir, como tarjetas comparables.</summary>
        private void ArmarTarjetasVenta()
        {
            var t = GestorIdioma06AV.Instancia;

            flpVentas.SuspendLayout();
            foreach (Control c in flpVentas.Controls.Cast<Control>().ToList()) c.Dispose();
            flpVentas.Controls.Clear();

            foreach (object item in cboVenta.Items)
            {
                var vm = item as VentaVm;
                if (vm == null || vm.Venta == null) continue;
                Venta06AV v = vm.Venta;

                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = item,
                    Titulo = "#" + v.NumeroVenta + "  ·  " +
                             (v.Cliente != null ? v.Cliente.NombreCompleto : "-"),
                    Subtitulo = v.Computadora != null ? v.Computadora.Nombre : "-",
                    Etiqueta = v.PrecioTotal.ToString("C0"),
                    Icono = IconoPcf06AV.Bandeja,
                    Width = 330,
                    Seleccionada = ReferenceEquals(item, cboVenta.SelectedItem)
                };
                object itemLocal = item;
                tarjeta.Elegida += (s, e) => ElegirVenta(itemLocal);
                flpVentas.Controls.Add(tarjeta);
            }

            flpVentas.ResumeLayout();
        }

        private void ElegirVenta(object item)
        {
            cboVenta.SelectedItem = item;
            foreach (Control c in flpVentas.Controls)
                if (c is TarjetaOpcion06AV t) t.Seleccionada = ReferenceEquals(t.Valor, item);
            MostrarDetalleVenta();
        }

        /// <summary>
        /// CU05 — Asignar línea. Tres campos sueltos sobre un fondo blanco no decían
        /// qué se estaba decidiendo. Ahora la elección principal (a qué línea va el
        /// equipo) se hace sobre tarjetas que muestran todas las líneas con su estado,
        /// y la fecha y el responsable quedan debajo como datos de acompañamiento.
        /// </summary>
        private void ConstruirFormPlan()
        {
            lblFormPlanTit = new Label { AutoSize = true, Location = new Point(16, 14) };
            lblPlanAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };

            lblLinea = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false, Padding = new Padding(2, 4, 0, 0) };
            lblSinLineas = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(4, 6, 0, 0), Visible = false };

            flpLineas = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 160,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 4, 0, 4)
            };

            lblInicio = new Label();
            lblResp = new Label();
            dtpInicio = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 260 };
            txtResp = new TextBox { Width = 300 };

            btnConfirmarPlan = NuevoBoton(170);
            btnVolverPlan = NuevoBoton(120);
            btnConfirmarPlan.Click += (s, e) => AsignarLinea();
            btnVolverPlan.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            tabla.Dock = DockStyle.Top;
            AgregarFila(tabla, lblInicio, dtpInicio);
            AgregarFila(tabla, lblResp, txtResp);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            cont.Controls.Add(flpLineas);
            cont.Controls.Add(lblSinLineas);
            cont.Controls.Add(lblLinea);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            barraTop.Controls.Add(lblPlanAyuda);
            barraTop.Controls.Add(lblFormPlanTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverPlan, btnConfirmarPlan));

            pnlFormPlan = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormPlan.Controls.Add(cont);
            pnlFormPlan.Controls.Add(barraTop);
            pnlFormPlan.Controls.Add(barraBot);
        }

        /// <summary>Pinta las líneas disponibles como tarjetas y deja elegida la primera.</summary>
        private void CargarLineas(List<LineaEnsamblaje06AV> lineas)
        {
            var t = GestorIdioma06AV.Instancia;

            flpLineas.SuspendLayout();
            foreach (Control c in flpLineas.Controls.Cast<Control>().ToList()) c.Dispose();
            flpLineas.Controls.Clear();
            _lineaElegida = null;

            foreach (LineaEnsamblaje06AV l in lineas)
            {
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = l,
                    Titulo = l.Nombre,
                    Subtitulo = t.Obtener("pcf_linea"),
                    Etiqueta = t.Obtener("pcf_linea_disponible"),
                    Icono = IconoPcf06AV.Destornillador,
                    Habilitada = true,
                    Width = 250
                };
                LineaEnsamblaje06AV lLocal = l;
                tarjeta.Elegida += (s, e) => ElegirLinea(lLocal);
                flpLineas.Controls.Add(tarjeta);
            }

            lblSinLineas.Visible = lineas.Count == 0;
            flpLineas.ResumeLayout();

            if (lineas.Count > 0) ElegirLinea(lineas[0]);
        }

        private void ElegirLinea(LineaEnsamblaje06AV linea)
        {
            _lineaElegida = linea;
            foreach (Control c in flpLineas.Controls)
                if (c is TarjetaOpcion06AV t)
                    t.Seleccionada = ReferenceEquals(t.Valor, linea);
        }

        /// <summary>
        /// CU06 — Cerrar orden. El control de calidad es la decisión que define si el
        /// equipo sale o vuelve al banco, así que la checklist dejó de ser cuatro
        /// casillas de 13 px dentro de una tabla: son cuatro filas grandes, cada una
        /// con qué se verifica, que se tiñen de verde al marcarse. Debajo, un veredicto
        /// en vivo dice qué va a pasar al confirmar, ANTES de confirmar.
        /// </summary>
        private void ConstruirFormCierre()
        {
            lblFormCierreTit = new Label { AutoSize = true, Location = new Point(16, 14) };
            lblCierreAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };
            lblChecklist = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false, Padding = new Padding(2, 4, 0, 0) };
            lblObs = new Label();
            lblRespCc = new Label();

            chkEncendido = NuevoCheck();
            chkConexiones = NuevoCheck();
            chkSO = NuevoCheck();
            chkDrivers = NuevoCheck();

            var flpChecks = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 272,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 4, 0, 4)
            };
            flpChecks.Controls.AddRange(new Control[] { chkEncendido, chkConexiones, chkSO, chkDrivers });

            lblVeredicto = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(2, 8, 0, 0) };

            txtObs = new TextBox { Width = 420, Multiline = true, Height = 64, ScrollBars = ScrollBars.Vertical };
            txtRespCc = new TextBox { Width = 300 };

            btnConfirmarCierre = NuevoBoton(190);
            btnVolverCierre = NuevoBoton(120);
            btnConfirmarCierre.Click += (s, e) => CerrarOrden();
            btnVolverCierre.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            tabla.Dock = DockStyle.Top;
            AgregarFila(tabla, lblRespCc, txtRespCc);
            AgregarFila(tabla, lblObs, txtObs);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            cont.Controls.Add(lblVeredicto);
            cont.Controls.Add(flpChecks);
            cont.Controls.Add(lblChecklist);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            barraTop.Controls.Add(lblCierreAyuda);
            barraTop.Controls.Add(lblFormCierreTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverCierre, btnConfirmarCierre));

            pnlFormCierre = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormCierre.Controls.Add(cont);
            pnlFormCierre.Controls.Add(barraTop);
            pnlFormCierre.Controls.Add(barraBot);

            foreach (ItemChequeo06AV chk in new[] { chkEncendido, chkConexiones, chkSO, chkDrivers })
                chk.MarcadoCambiado += (s, e) => ActualizarVeredicto();
        }

        /// <summary>
        /// Dice en vivo qué va a pasar al confirmar: aprobado cierra y asigna serie;
        /// con alguna verificación fallada la orden vuelve a revisión y exige explicar
        /// por qué. Evita la sorpresa después del clic.
        /// </summary>
        private void ActualizarVeredicto()
        {
            var t = GestorIdioma06AV.Instancia;
            int ok = new[] { chkEncendido, chkConexiones, chkSO, chkDrivers }.Count(c => c.Marcado);
            bool aprueba = ok == 4;

            lblVeredicto.Text = aprueba
                ? "✓  " + t.Obtener("pcf_cc_veredicto_ok")
                : "!  " + t.Obtener("pcf_cc_veredicto_falla", 4 - ok);
            lblVeredicto.ForeColor = aprueba ? Tema.Exito : Tema.Advertencia;

            btnConfirmarCierre.Text = aprueba
                ? t.Obtener("pcf_cc_aprobar_cerrar")
                : t.Obtener("pcf_cc_mandar_revision");
            if (aprueba) Tema.AplicarBotonPrimario(btnConfirmarCierre);
            else Tema.AplicarBotonAcento(btnConfirmarCierre);
        }

        private static ItemChequeo06AV NuevoCheck() =>
            new ItemChequeo06AV { Width = 440, Margin = new Padding(0, 0, 0, 8) };

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
            Tema.AplicarBotonSecundario(btnVista);
            Tema.AplicarBotonSecundario(btnDetalle);
            tablero.AplicarTema();
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarBotonSecundario(btnVolverOrden);
            Tema.AplicarBotonPrimario(btnConfirmarPlan);
            Tema.AplicarBotonSecundario(btnVolverPlan);
            Tema.AplicarBotonPrimario(btnConfirmarCierre);
            Tema.AplicarBotonSecundario(btnVolverCierre);

            // AgregarFila aplica el estilo de "entrada"; acá se restituye el look de etiqueta.
            lblDetalleVenta.ForeColor = Tema.TextoSuave;
            lblDetalleVenta.BackColor = Tema.FondoApp;
            foreach (ItemChequeo06AV chk in new[] { chkEncendido, chkConexiones, chkSO, chkDrivers })
            {
                chk.ForeColor = Tema.Texto;
                chk.Invalidate();
            }
            if (chkEncendido.Parent != null) chkEncendido.Parent.BackColor = Tema.FondoApp;
            if (flpLineas != null) flpLineas.BackColor = Tema.FondoApp;
            if (flpVentas != null)
            {
                flpVentas.BackColor = Tema.FondoApp;
                fichaVenta.BackColor = Tema.FondoApp;
                fichaVenta.Invalidate();
                lblVenta.Font = Tema.FuenteSubtit;
                lblVenta.ForeColor = Tema.TextoFuerte;
                lblVenta.BackColor = Tema.FondoApp;
                lblOrdenAyuda.Font = Tema.FuenteRegular;
                lblOrdenAyuda.ForeColor = Tema.TextoSuave;
                lblOrdenAyuda.BackColor = Tema.FondoApp;
            }

            foreach (Label l in new[] { lblPlanAyuda, lblCierreAyuda, lblSinLineas })
            {
                l.Font = Tema.FuenteRegular;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
            }
            foreach (Label l in new[] { lblLinea, lblChecklist })
            {
                l.Font = Tema.FuenteSubtit;
                l.ForeColor = Tema.TextoFuerte;
                l.BackColor = Tema.FondoApp;
            }
            lblVeredicto.Font = Tema.FuenteBold;
            lblVeredicto.BackColor = Tema.FondoApp;
            ActualizarVeredicto();

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

            var estaciones = EstacionesProceso();
            riel.DefinirEstaciones(estaciones);
            tablero.RenombrarColumnas(estaciones);
            btnVista.Text = _modoTablero ? t.Obtener("pcf_ver_grilla") : t.Obtener("pcf_ver_tablero");
            btnDetalle.Text = (_detalleVisible ? "◂  " : "▸  ") + t.Obtener("pcf_ver_detalle");
            btnNueva.Text = "＋ " + t.Obtener("pcf_nueva_orden");
            btnRefrescar.Text = t.Obtener("pcf_refrescar");

            lblFormOrdenTit.Text = t.Obtener("pcf_nueva_orden");
            lblOrdenAyuda.Text = t.Obtener("pcf_orden_ayuda");
            lblVenta.Text = t.Obtener("pcf_orden_elegi_venta");
            lblEntrega.Text = t.Obtener("pcf_f_entrega") + ":";
            btnRegistrar.Text = t.Obtener("pcf_registrar_orden");
            btnVolverOrden.Text = t.Obtener("volver");

            lblFormPlanTit.Text = t.Obtener("pcf_asignar_linea");
            lblPlanAyuda.Text = t.Obtener("pcf_plan_ayuda");
            lblLinea.Text = t.Obtener("pcf_plan_elegi_linea");
            lblSinLineas.Text = t.Obtener("pcf_sin_lineas");
            lblInicio.Text = t.Obtener("pcf_f_inicio") + ":";
            lblResp.Text = t.Obtener("pcf_responsable") + ":";
            btnConfirmarPlan.Text = t.Obtener("pcf_asignar");
            btnVolverPlan.Text = t.Obtener("volver");

            lblFormCierreTit.Text = t.Obtener("pcf_cerrar_orden");
            lblChecklist.Text = t.Obtener("pcf_control_calidad") + ":";
            lblRespCc.Text = t.Obtener("pcf_responsable") + ":";
            lblObs.Text = t.Obtener("pcf_observaciones") + ":";
            chkEncendido.Titulo = t.Obtener("pcf_cc_encendido");
            chkEncendido.Detalle = t.Obtener("pcf_cc_encendido_det");
            chkConexiones.Titulo = t.Obtener("pcf_cc_conexiones");
            chkConexiones.Detalle = t.Obtener("pcf_cc_conexiones_det");
            chkSO.Titulo = t.Obtener("pcf_cc_so");
            chkSO.Detalle = t.Obtener("pcf_cc_so_det");
            chkDrivers.Titulo = t.Obtener("pcf_cc_drivers");
            chkDrivers.Detalle = t.Obtener("pcf_cc_drivers_det");
            lblCierreAyuda.Text = t.Obtener("pcf_cc_ayuda");
            ActualizarVeredicto();
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
                RefrescarTablero(todas);

                if (sel.HasValue) SeleccionarOrden(sel.Value);
                else if (_ordenElegidaTablero != null) SeleccionarOrden(_ordenElegidaTablero.NumeroOrden);
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
            _modoTablero
                ? _ordenElegidaTablero
                : (grilla.CurrentRow?.DataBoundItem as OrdenVm)?.Orden;

        private void SeleccionarOrden(int numero)
        {
            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is OrdenVm vm && vm.Numero == numero)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grilla.CurrentCell = row.Cells[0];
                    break;
                }

            tablero.SeleccionarPorEtiqueta(o => (o as OrdenProduccion06AV)?.NumeroOrden == numero);
        }

        // ══════════════════════════════════════════════════════════════
        //  Tablero de estaciones
        // ══════════════════════════════════════════════════════════════

        /// <summary>Las cinco estaciones del proceso, en el orden real del taller.</summary>
        private List<EstacionRiel06AV> EstacionesProceso()
        {
            var t = GestorIdioma06AV.Instancia;
            return new List<EstacionRiel06AV>
            {
                new EstacionRiel06AV(t.Obtener("pcf_est_op_pendiente"),   IconoPcf06AV.Bandeja),
                new EstacionRiel06AV(t.Obtener("pcf_est_op_planificada"), IconoPcf06AV.Calendario),
                new EstacionRiel06AV(t.Obtener("pcf_est_op_ensamblaje"),  IconoPcf06AV.Destornillador),
                new EstacionRiel06AV(t.Obtener("pcf_est_op_finalizada"),  IconoPcf06AV.Escudo),
                new EstacionRiel06AV(t.Obtener("pcf_est_op_entregada"),   IconoPcf06AV.Camion)
            };
        }

        /// <summary>Columna del tablero. "En revisión" no tiene columna propia: vuelve a ensamblaje.</summary>
        private static int ColumnaDe(EstadoOrdenProduccion06AV estado) =>
            estado == EstadoOrdenProduccion06AV.EnRevision ? 2 : (int)estado;

        /// <summary>
        /// Acción única posible en la estación actual. Es la misma que ofrece el panel
        /// de detalle: el tablero no agrega caminos nuevos, sólo los acerca.
        /// </summary>
        private void AccionPrincipal(EstadoOrdenProduccion06AV estado, out string texto, out Action accion)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (estado)
            {
                case EstadoOrdenProduccion06AV.Pendiente:
                    texto = t.Obtener("pcf_asignar_linea"); accion = AbrirFormPlan; return;
                case EstadoOrdenProduccion06AV.Planificada:
                    texto = t.Obtener("pcf_iniciar_ensamblaje"); accion = Ensamblar; return;
                case EstadoOrdenProduccion06AV.EnEnsamblaje:
                    texto = t.Obtener("pcf_cerrar_orden"); accion = AbrirFormCierre; return;
                case EstadoOrdenProduccion06AV.EnRevision:
                    texto = t.Obtener("pcf_reintentar_cc"); accion = AbrirFormCierre; return;
                default:
                    texto = null; accion = null; return;
            }
        }

        private void EjecutarAccionPrincipal()
        {
            var o = OrdenSeleccionada();
            if (o == null) return;
            AccionPrincipal(o.Estado, out string _, out Action accion);
            accion?.Invoke();
        }

        /// <summary>
        /// Muestra u oculta el panel de detalle. En el tablero el detalle compite por
        /// el ancho con las cinco columnas: si la ventana no da, se pliega solo y las
        /// acciones siguen disponibles en la píldora de cada tarjeta.
        /// </summary>
        private void MostrarDetalle(bool visible)
        {
            _detalleVisible = visible;
            pnlDetalle.Visible = visible;
            riel.Visible = visible && OrdenSeleccionada() != null;
            AplicarIdioma();
        }

        /// <summary>Ancho mínimo para que el tablero y el detalle conviban sin apretarse.</summary>
        private void AjustarDetalleAlAncho()
        {
            if (!_modoTablero || pnlGrilla == null) return;
            bool entra = pnlGrilla.ClientSize.Width >= 5 * 188 + pnlDetalle.Width;
            if (!entra && _detalleVisible) MostrarDetalle(false);
        }

        private void AlternarVista()
        {
            _modoTablero = !_modoTablero;
            tablero.Visible = _modoTablero;
            grilla.Visible = !_modoTablero;
            if (_modoTablero) tablero.BringToFront(); else grilla.BringToFront();
            if (!_modoTablero) MostrarDetalle(true);   // la grilla sin detalle no sirve de nada
            else AjustarDetalleAlAncho();
            AplicarIdioma();
            ActualizarDetalle();
        }

        /// <summary>Vuelca las órdenes al tablero: una tarjeta por orden, en su estación.</summary>
        private void RefrescarTablero(List<OrdenProduccion06AV> ordenes)
        {
            var t = GestorIdioma06AV.Instancia;
            int? seleccionada = _ordenElegidaTablero?.NumeroOrden;

            tablero.Limpiar();
            foreach (var o in ordenes.OrderBy(x => x.FechaEntregaEstimada))
            {
                AccionPrincipal(o.Estado, out string textoAccion, out Action _);
                int dias = (int)(o.FechaEntregaEstimada.Date - DateTime.Today).TotalDays;
                bool cerrada = o.Estado == EstadoOrdenProduccion06AV.Entregada;

                var tarjeta = new TarjetaOrden06AV
                {
                    Etiqueta = o,
                    Clave = "#" + o.NumeroOrden,
                    Titulo = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "-",
                    Subtitulo = o.Computadora != null ? o.Computadora.Nombre : "-",
                    Chips = new[]
                    {
                        o.LineaEnsamblaje != null ? o.LineaEnsamblaje.Nombre : null,
                        string.IsNullOrWhiteSpace(o.ResponsableTecnico) ? null : o.ResponsableTecnico
                    },
                    PieIzquierda = t.Obtener("pcf_f_entrega") + ": " + o.FechaEntregaEstimada.ToShortDateString(),
                    TextoPlazo = cerrada ? null : TextoPlazo(dias),
                    Urgencia = cerrada ? 0 : (dias < 0 ? 2 : (dias <= 2 ? 1 : 0)),
                    TextoAccion = textoAccion
                };

                tablero.Agregar(ColumnaDe(o.Estado), tarjeta);
            }

            tablero.Recalcular();
            if (seleccionada.HasValue)
                tablero.SeleccionarPorEtiqueta(x => (x as OrdenProduccion06AV)?.NumeroOrden == seleccionada.Value);
        }

        private string TextoPlazo(int dias)
        {
            var t = GestorIdioma06AV.Instancia;
            if (dias < 0) return t.Obtener("pcf_plazo_atraso", -dias);
            if (dias == 0) return t.Obtener("pcf_plazo_hoy");
            return t.Obtener("pcf_plazo_dias", dias);
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
                riel.Visible = false;
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

            // Progreso: riel de estaciones. "En revisión" se dibuja como desvío, no como paso.
            riel.Visible = _detalleVisible;
            riel.EstablecerDetalle(1, o.FechaInicioPrevista.HasValue
                ? o.FechaInicioPrevista.Value.ToShortDateString() : null);
            riel.EstablecerDetalle(3, o.FechaCierre.HasValue
                ? o.FechaCierre.Value.ToShortDateString() : null);
            riel.EstablecerDetalle(4, o.FechaEntregaEstimada.ToShortDateString());
            riel.Avanzar(ColumnaDe(o.Estado),
                         o.Estado == EstadoOrdenProduccion06AV.EnRevision,
                         t.Obtener("pcf_est_op_revision"));

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
            ArmarTarjetasVenta();
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

            fichaVenta.Titulo = t.Obtener("pcf_venta") + " #" + v.NumeroVenta;
            fichaVenta.RotuloDestacado = null;
            fichaVenta.ValorDestacado = null;
            fichaVenta.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("pcf_cliente"),
                                  v.Cliente != null ? v.Cliente.NombreCompleto : "-"),
                new DatoFicha06AV(t.Obtener("pcf_equipo"),
                                  v.Computadora != null ? v.Computadora.Nombre : "-"),
                new DatoFicha06AV(t.Obtener("pcf_total"), v.PrecioTotal.ToString("C0")),
                new DatoFicha06AV(t.Obtener("pcf_sena"),
                                  sena != null ? sena.Monto.ToString("C0") : "-"),
                new DatoFicha06AV(t.Obtener("pcf_saldo"), v.SaldoPendiente.ToString("C0")),
                new DatoFicha06AV(t.Obtener("pcf_f_entrega_estimada"),
                                  v.FechaEntregaEstimada.ToShortDateString())
            });
            fichaVenta.Height = fichaVenta.AltoNecesario;

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

            CargarLineas(lineas);
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
            chkEncendido.Marcado = cc.Encendido;
            chkConexiones.Marcado = cc.Conexiones;
            chkSO.Marcado = cc.SistemaOperativo;
            chkDrivers.Marcado = cc.Drivers;
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
            LineaEnsamblaje06AV linea = _lineaElegida;
            if (linea == null)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_elegi_linea"));
                return;
            }
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
                Encendido = chkEncendido.Marcado,
                Conexiones = chkConexiones.Marcado,
                SistemaOperativo = chkSO.Marcado,
                Drivers = chkDrivers.Marcado,
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AjustarDetalleAlAncho();
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
