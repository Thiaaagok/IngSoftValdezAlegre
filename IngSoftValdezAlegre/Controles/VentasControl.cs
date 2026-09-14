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
    /// VENTAS (RFN1) — pantalla del RECEPCIONISTA. Es el circuito comercial, separado
    /// de la fábrica:
    ///   CU01 Registrar venta            → "＋ Nueva venta"
    ///   CU02 Registrar cliente          → botón "＋" junto al combo de clientes
    ///   CU03 Registrar seña y recibo    → acción "Registrar seña" del panel de detalle
    ///
    /// Acá termina el mostrador. La orden de producción la genera el gerente desde
    /// Producción una vez que la venta quedó señada, y el retiro del equipo con el
    /// cobro del saldo (CU07) se hace en "Entrega de computadoras".
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class VentasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly VentasBLL06AV _ventasBLL = new VentasBLL06AV();
        private readonly ClientesBLL06AV _clientesBLL = new ClientesBLL06AV();
        private readonly ComponentesBLL06AV _componentesBLL = new ComponentesBLL06AV();
        private readonly ModelosEstandarBLL06AV _modelosBLL = new ModelosEstandarBLL06AV();

        private bool _cargandoCombos;
        private List<Componente06AV> _componentesElegidos = new List<Componente06AV>();
        private Venta06AV _ventaSel;
        private OrdenProduccion06AV _ordenSel;

        // Vistas
        private Panel pnlGrilla, pnlFormVenta, pnlFormPago;

        // Hub (lista + detalle)
        private Label lblTitulo;
        private DataGridView grilla;
        private Button btnNueva, btnRefrescar;
        private Panel pnlDetalle;
        private Label lblDetTitulo, lblDetEstado;
        private FlowLayoutPanel flpDetalle;

        // Formulario "Nueva venta"
        private Label lblFormVentaTit, lblCliente, lblTipo, lblModelo, lblComp, lblEntrega;
        private Label lblResumenTit, lblResumenVacio, lblTotalRotulo, lblTotalValor, lblVentaAyuda;
        private ResumenPcControl06AV _resumen;
        private ComboBox cboCliente;
        private FlowLayoutPanel flpTipo, flpModelos;
        private Panel pnlTicket, pnlArmar;
        private TipoConfiguracion06AV _tipoElegido = TipoConfiguracion06AV.Estandar;
        private ModeloEstandar06AV _modeloElegido;
        private List<ModeloEstandar06AV> _modelos = new List<ModeloEstandar06AV>();
        private Button btnNuevoCliente, btnArmar, btnRegistrar, btnVolverVenta;
        private DateTimePicker dtpEntrega;

        // Formulario "Registrar seña" (CU03)
        private Label lblFormPagoTit, lblPagoDetalle, lblMonto, lblFormaPago, lblReferencia;
        private FichaDatos06AV fichaPago;
        private FlowLayoutPanel flpFormaPago;
        private Label lblMontoValor;
        private ComboBox cboFormaPago;
        private TextBox txtReferencia;
        private Button btnConfirmarPago, btnVolverPago;
        private Venta06AV _ventaPago;

        public VentasControl()
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
            CargarVentas();
        }

        // ══════════════════════════════════════════════════════════════
        //  Construcción
        // ══════════════════════════════════════════════════════════════
        private void ConstruirUI()
        {
            ConstruirHub();
            ConstruirFormVenta();
            ConstruirFormPago();

            Controls.Add(pnlFormVenta);
            Controls.Add(pnlFormPago);
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
            btnNueva.Click += (s, e) => AbrirFormVenta();
            btnRefrescar.Click += (s, e) => CargarVentas();

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

        /// <summary>
        /// NUEVA VENTA — armado del pedido a la izquierda, ticket a la derecha.
        ///
        /// El formulario anterior era una tabla de etiqueta+control donde todo pesaba
        /// lo mismo: el combo de tipo, el de modelo y la lista de ocho componentes
        /// compartían jerarquía con la fecha de entrega, y el total aparecía perdido
        /// al final de la lista.
        ///
        /// La venta tiene en realidad dos mitades distintas: DECIDIR (quién compra,
        /// qué equipo, para cuándo) y CONFIRMAR (qué lleva y cuánto sale). Por eso la
        /// pantalla se parte: a la izquierda los pasos de la decisión, cada uno en su
        /// bloque; a la derecha un ticket fijo con el equipo y el total en grande, que
        /// se actualiza con cada cambio y tiene el botón de registrar al pie.
        ///
        /// El tipo de configuración dejó de ser un combo: son dos tarjetas, porque la
        /// elección cambia toda la pantalla (modelos vs. configurador) y merece verse.
        /// </summary>
        private void ConstruirFormVenta()
        {
            lblFormVentaTit = new Label { AutoSize = true, Location = new Point(16, 14) };
            lblVentaAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };
            // TextAlign centrado en vertical + alto generoso: con Padding arriba y alto
            // justo, la fuente de subtítulo se cortaba por abajo.
            lblCliente = TituloSeccion(32);
            lblTipo = TituloSeccion(40);
            lblModelo = TituloSeccion(40);
            lblComp = new Label();
            lblEntrega = new Label();
            _resumen = new ResumenPcControl06AV();

            // ── Cliente ──────────────────────────────────────────
            cboCliente = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
            btnNuevoCliente = new Button { Width = 44, Height = 28, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0) };
            btnNuevoCliente.Click += (s, e) => AbrirNuevoCliente();

            var pnlCliente = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Margin = new Padding(0)
            };
            pnlCliente.Controls.Add(cboCliente);
            pnlCliente.Controls.Add(btnNuevoCliente);

            // ── Tipo de equipo (dos tarjetas) ────────────────────
            flpTipo = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 80,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(0, 2, 0, 2)
            };

            // ── Modelos estándar / configurador ─────────────────
            flpModelos = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 156,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, AutoScroll = true,
                Padding = new Padding(0, 2, 0, 2)
            };

            btnArmar = new Button
            {
                Width = 230, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Location = new Point(0, 6)
            };
            btnArmar.Click += (s, e) => AbrirAsistenteComponentes();

            // El botón va dentro de un panel acoplado: un Button suelto en un panel con
            // hijos Dock.Top se queda en (0,0) y se monta encima del primer bloque.
            pnlArmar = new Panel { Dock = DockStyle.Top, Height = 52 };
            pnlArmar.Controls.Add(btnArmar);

            // ── Entrega ──────────────────────────────────────────
            dtpEntrega = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(15), Width = 220 };
            var tablaEntrega = NuevaTabla();
            tablaEntrega.Dock = DockStyle.Top;
            AgregarFila(tablaEntrega, lblEntrega, dtpEntrega);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 8), AutoScroll = true };
            cont.Controls.Add(tablaEntrega);
            cont.Controls.Add(pnlArmar);
            cont.Controls.Add(flpModelos);
            cont.Controls.Add(lblModelo);
            cont.Controls.Add(flpTipo);
            cont.Controls.Add(lblTipo);
            cont.Controls.Add(pnlCliente);
            cont.Controls.Add(lblCliente);

            // ── Ticket (derecha) ─────────────────────────────────
            lblResumenTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false };
            lblResumenVacio = new Label { Dock = DockStyle.Top, Height = 52, AutoSize = false, Padding = new Padding(0, 4, 0, 0) };

            var flpResumen = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true,
                Padding = new Padding(0, 2, 0, 4)
            };
            flpResumen.Controls.Add(_resumen);

            lblTotalRotulo = new Label { Dock = DockStyle.Top, Height = 20, AutoSize = false };
            lblTotalValor = new Label { Dock = DockStyle.Top, Height = 44, AutoSize = false };

            btnRegistrar = NuevoBoton(240);
            btnVolverVenta = NuevoBoton(240);
            btnRegistrar.Click += (s, e) => RegistrarVenta();
            btnVolverVenta.Click += (s, e) => MostrarGrilla();
            btnRegistrar.Margin = new Padding(0, 6, 0, 4);
            btnVolverVenta.Margin = new Padding(0, 0, 0, 4);

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            flpAcciones.Controls.Add(btnRegistrar);
            flpAcciones.Controls.Add(btnVolverVenta);

            var pnlTotal = new Panel { Dock = DockStyle.Bottom, Height = 70 };
            pnlTotal.Controls.Add(lblTotalValor);
            pnlTotal.Controls.Add(lblTotalRotulo);

            pnlTicket = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(18, 14, 18, 12) };
            pnlTicket.Controls.Add(flpResumen);
            pnlTicket.Controls.Add(lblResumenVacio);
            pnlTicket.Controls.Add(lblResumenTit);
            pnlTicket.Controls.Add(pnlTotal);
            pnlTicket.Controls.Add(flpAcciones);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            barraTop.Controls.Add(lblVentaAyuda);
            barraTop.Controls.Add(lblFormVentaTit);

            pnlFormVenta = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormVenta.Controls.Add(cont);
            pnlFormVenta.Controls.Add(pnlTicket);
            pnlFormVenta.Controls.Add(barraTop);

            ArmarTarjetasTipo();
        }

        /// <summary>Las dos formas de comprar un equipo, como tarjetas excluyentes.</summary>
        private void ArmarTarjetasTipo()
        {
            var t = GestorIdioma06AV.Instancia;

            flpTipo.SuspendLayout();
            foreach (Control c in flpTipo.Controls.Cast<Control>().ToList()) c.Dispose();
            flpTipo.Controls.Clear();

            foreach (TipoConfiguracion06AV tipo in new[]
                     { TipoConfiguracion06AV.Estandar, TipoConfiguracion06AV.Configurable })
            {
                bool estandar = tipo == TipoConfiguracion06AV.Estandar;
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = tipo,
                    Titulo = t.Obtener(estandar ? "pcf_venta_tipo_estandar" : "pcf_venta_tipo_config"),
                    Subtitulo = t.Obtener(estandar ? "pcf_venta_tipo_estandar_det" : "pcf_venta_tipo_config_det"),
                    Icono = estandar ? IconoPcf06AV.Caja : IconoPcf06AV.Destornillador,
                    Width = 300,
                    Seleccionada = tipo == _tipoElegido
                };
                TipoConfiguracion06AV tipoLocal = tipo;
                tarjeta.Elegida += (s, e) => ElegirTipo(tipoLocal);
                flpTipo.Controls.Add(tarjeta);
            }

            flpTipo.ResumeLayout();
        }

        private void ElegirTipo(TipoConfiguracion06AV tipo)
        {
            _tipoElegido = tipo;
            foreach (Control c in flpTipo.Controls)
                if (c is TarjetaOpcion06AV t)
                    t.Seleccionada = t.Valor is TipoConfiguracion06AV v && v == tipo;
            ActualizarModeloSegunTipo();
        }

        /// <summary>Cada modelo estándar como tarjeta con su cantidad de piezas y precio.</summary>
        private void ArmarTarjetasModelo()
        {
            var t = GestorIdioma06AV.Instancia;

            flpModelos.SuspendLayout();
            foreach (Control c in flpModelos.Controls.Cast<Control>().ToList()) c.Dispose();
            flpModelos.Controls.Clear();

            foreach (ModeloEstandar06AV m in _modelos)
            {
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = m,
                    Titulo = m.Nombre,
                    Subtitulo = t.Obtener("pcf_armar_n_componentes", m.Componentes != null ? m.Componentes.Count : 0),
                    Etiqueta = m.PrecioTotal.ToString("C0"),
                    Icono = IconoPcf06AV.Caja,
                    Width = 300,
                    Seleccionada = _modeloElegido != null && _modeloElegido.Id == m.Id
                };
                ModeloEstandar06AV mLocal = m;
                tarjeta.Elegida += (s, e) => ElegirModelo(mLocal);
                flpModelos.Controls.Add(tarjeta);
            }

            flpModelos.ResumeLayout();
        }

        private void ElegirModelo(ModeloEstandar06AV modelo)
        {
            _modeloElegido = modelo;
            foreach (Control c in flpModelos.Controls)
                if (c is TarjetaOpcion06AV t)
                    t.Seleccionada = t.Valor is ModeloEstandar06AV m && modelo != null && m.Id == modelo.Id;
            AplicarModeloSeleccionado();
        }

        /// <summary>
        /// REGISTRAR SEÑA — el monto es fijo (50% del total) y no se discute: lo único
        /// que decide el cajero es CÓMO cobra. Por eso el importe pasó a ser el número
        /// grande de una ficha de contexto, junto con quién compra y qué lleva, y la
        /// forma de pago dejó de ser un combo para ser tres tarjetas visibles.
        /// </summary>
        private void ConstruirFormPago()
        {
            lblFormPagoTit = new Label { AutoSize = true, Location = new Point(16, 14) };
            lblPagoDetalle = new Label { AutoSize = true, Location = new Point(18, 40) };
            lblMonto = new Label();
            lblFormaPago = TituloSeccion(38);
            lblReferencia = new Label();
            lblMontoValor = new Label { AutoSize = true };

            fichaPago = new FichaDatos06AV { Dock = DockStyle.Top, Height = 150 };

            flpFormaPago = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 84,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, Padding = new Padding(0, 2, 0, 2)
            };

            cboFormaPago = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240, Visible = false };
            txtReferencia = new TextBox { Width = 320 };

            btnConfirmarPago = NuevoBoton(210);
            btnVolverPago = NuevoBoton(120);
            btnConfirmarPago.Click += (s, e) => ConfirmarPago();
            btnVolverPago.Click += (s, e) => MostrarGrilla();

            var tabla = NuevaTabla();
            tabla.Dock = DockStyle.Top;
            AgregarFila(tabla, lblReferencia, txtReferencia);

            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 8), AutoScroll = true };
            cont.Controls.Add(tabla);
            cont.Controls.Add(flpFormaPago);
            cont.Controls.Add(lblFormaPago);
            cont.Controls.Add(fichaPago);
            cont.Controls.Add(cboFormaPago);

            var barraTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            barraTop.Controls.Add(lblPagoDetalle);
            barraTop.Controls.Add(lblFormPagoTit);
            var barraBot = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraBot.Controls.Add(BarraBotones(btnVolverPago, btnConfirmarPago));

            pnlFormPago = new Panel { Dock = DockStyle.Fill, Visible = false };
            pnlFormPago.Controls.Add(cont);
            pnlFormPago.Controls.Add(barraTop);
            pnlFormPago.Controls.Add(barraBot);
        }

        /// <summary>Las formas de pago como tarjetas: son tres, no tiene sentido esconderlas.</summary>
        private void ArmarTarjetasFormaPago()
        {
            flpFormaPago.SuspendLayout();
            foreach (Control c in flpFormaPago.Controls.Cast<Control>().ToList()) c.Dispose();
            flpFormaPago.Controls.Clear();

            foreach (object item in cboFormaPago.Items)
            {
                var tarjeta = new TarjetaOpcion06AV
                {
                    Valor = item,
                    Titulo = item.ToString(),
                    Icono = IconoPcf06AV.Caja,
                    Width = 210,
                    Height = 58,
                    Seleccionada = ReferenceEquals(item, cboFormaPago.SelectedItem)
                };
                object itemLocal = item;
                tarjeta.Elegida += (s, e) => ElegirFormaPago(itemLocal);
                flpFormaPago.Controls.Add(tarjeta);
            }

            flpFormaPago.ResumeLayout();
        }

        private void ElegirFormaPago(object item)
        {
            cboFormaPago.SelectedItem = item;
            foreach (Control c in flpFormaPago.Controls)
                if (c is TarjetaOpcion06AV t) t.Seleccionada = ReferenceEquals(t.Valor, item);
        }

        /// <summary>Título de bloque: alto holgado y texto centrado, para que nunca se corte.</summary>
        private static Label TituloSeccion(int alto) => new Label
        {
            Dock = DockStyle.Top,
            Height = alto,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 6, 0, 0)
        };

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
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
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
            Tema.AplicarSubtitulo(lblFormVentaTit);
            Tema.AplicarSubtitulo(lblFormPagoTit);
            Tema.AplicarSubtitulo(lblDetTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnNueva);
            Tema.AplicarBotonSecundario(btnRefrescar);
            Tema.AplicarBotonAcento(btnArmar);
            Tema.AplicarBotonSecundario(btnNuevoCliente);
            Tema.AplicarBotonPrimario(btnRegistrar);
            Tema.AplicarBotonSecundario(btnVolverVenta);
            Tema.AplicarBotonPrimario(btnConfirmarPago);
            Tema.AplicarBotonSecundario(btnVolverPago);

            // AgregarFila aplica el estilo de "entrada"; acá se restituye el look de etiqueta.
            lblMontoValor.ForeColor = Tema.Primario;
            lblMontoValor.BackColor = Tema.FondoApp;
            lblMontoValor.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            lblPagoDetalle.ForeColor = Tema.TextoSuave;
            lblPagoDetalle.BackColor = Tema.FondoApp;

            pnlDetalle.BackColor = Tema.FondoPanel;
            flpDetalle.BackColor = Tema.FondoPanel;
            lblDetTitulo.BackColor = Tema.FondoPanel;
            lblDetEstado.BackColor = Tema.FondoPanel;
            if (fichaPago != null)
            {
                fichaPago.BackColor = Tema.FondoApp;
                flpFormaPago.BackColor = Tema.FondoApp;
                lblFormaPago.Font = Tema.FuenteSubtit;
                lblFormaPago.ForeColor = Tema.TextoFuerte;
                lblFormaPago.BackColor = Tema.FondoApp;
                lblPagoDetalle.Font = Tema.FuenteRegular;
                lblPagoDetalle.ForeColor = Tema.TextoSuave;
                lblPagoDetalle.BackColor = Tema.FondoApp;
                fichaPago.Invalidate();
            }

            if (pnlTicket != null)
            {
                pnlTicket.BackColor = Tema.FondoPanel;
                foreach (Control c in pnlTicket.Controls) c.BackColor = Tema.FondoPanel;
                Tema.AplicarSubtitulo(lblResumenTit);
                lblResumenTit.BackColor = Tema.FondoPanel;
                lblResumenVacio.Font = Tema.FuenteRegular;
                lblResumenVacio.ForeColor = Tema.TextoSuave;
                lblResumenVacio.BackColor = Tema.FondoPanel;
                lblTotalRotulo.Font = Tema.FuenteMini;
                lblTotalRotulo.ForeColor = Tema.TextoSuave;
                lblTotalRotulo.BackColor = Tema.FondoPanel;
                lblTotalValor.Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold);
                lblTotalValor.ForeColor = Tema.TextoFuerte;
                lblTotalValor.BackColor = Tema.FondoPanel;
                _resumen.BackColor = Tema.FondoPanel;
                Tema.AplicarBotonSecundario(btnVolverVenta);
                Tema.AplicarBotonAcento(btnArmar);

                foreach (Label l in new[] { lblCliente, lblTipo, lblModelo })
                {
                    l.Font = Tema.FuenteSubtit;
                    l.ForeColor = Tema.TextoFuerte;
                    l.BackColor = Tema.FondoApp;
                }
                lblVentaAyuda.Font = Tema.FuenteRegular;
                lblVentaAyuda.ForeColor = Tema.TextoSuave;
                lblVentaAyuda.BackColor = Tema.FondoApp;
                foreach (Control c in new Control[] { flpTipo, flpModelos, pnlArmar })
                    c.BackColor = Tema.FondoApp;
                ActualizarResumenComp();
            }

            foreach (Control panel in new[] { pnlGrilla, pnlFormVenta, pnlFormPago })
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
            lblTitulo.Text = t.Obtener("pcf_ventas_titulo");
            btnNueva.Text = "＋ " + t.Obtener("pcf_nueva_venta");
            btnRefrescar.Text = t.Obtener("pcf_refrescar");

            lblFormVentaTit.Text = t.Obtener("pcf_nueva_venta");
            lblVentaAyuda.Text = t.Obtener("pcf_venta_ayuda");
            lblCliente.Text = t.Obtener("pcf_venta_paso_cliente");
            lblResumenTit.Text = t.Obtener("pcf_venta_ticket");
            lblResumenVacio.Text = t.Obtener("pcf_venta_ticket_vacio");
            lblTotalRotulo.Text = t.Obtener("pcf_armar_total");
            btnNuevoCliente.Text = "＋";
            lblTipo.Text = t.Obtener("pcf_venta_paso_tipo");
            lblModelo.Text = t.Obtener("pcf_venta_paso_modelo");
            ArmarTarjetasTipo();
            ArmarTarjetasModelo();
            lblComp.Text = t.Obtener("pcf_componentes") + ":";
            btnArmar.Text = t.Obtener("pcf_elegir_componentes");
            lblEntrega.Text = t.Obtener("pcf_f_entrega_estimada") + ":";
            btnVolverVenta.Text = t.Obtener("volver");
            btnRegistrar.Text = t.Obtener("pcf_registrar_venta");
            btnVolverVenta.Text = t.Obtener("volver");
            ActualizarResumenComp();

            lblMonto.Text = t.Obtener("pcf_monto") + ":";
            lblFormaPago.Text = t.Obtener("pcf_sena_forma");
            lblReferencia.Text = t.Obtener("pcf_referencia") + ":";
            btnVolverPago.Text = t.Obtener("volver");
            CargarFormasPago();

            if (grilla.DataSource != null) CargarVentas();
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
                _modelos = _modelosBLL.ObtenerTodos() ?? new List<ModeloEstandar06AV>();
                ArmarTarjetasModelo();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
            finally { _cargandoCombos = false; }
        }

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

        private void CargarVentas()
        {
            try
            {
                int? sel = (grilla.CurrentRow?.DataBoundItem as VentaVm)?.Numero;

                var todas = _ventasBLL.ObtenerTodas() ?? new List<Venta06AV>();
                var vm = todas.OrderByDescending(v => v.NumeroVenta).Select(v =>
                {
                    EstadoInfo(v.Estado, out string txt, out Color col);
                    return new VentaVm
                    {
                        Numero = v.NumeroVenta,
                        Cliente = v.Cliente != null ? v.Cliente.Apellido + ", " + v.Cliente.Nombre : "",
                        Fecha = v.FechaVenta.ToShortDateString(),
                        Total = v.PrecioTotal.ToString("C0"),
                        Saldo = v.SaldoPendiente.ToString("C0"),
                        Estado = txt,
                        EstadoColor = col,
                        Venta = v
                    };
                }).ToList();

                grilla.DataSource = null;
                grilla.DataSource = vm;

                if (sel.HasValue) SeleccionarVenta(sel.Value);
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
            H("Fecha", "pcf_fecha");
            H("Total", "pcf_total");
            H("Saldo", "pcf_saldo");
            H("Estado", "pcf_col_estado");
            if (grilla.Columns["Numero"] != null) grilla.Columns["Numero"].FillWeight = 30;
            foreach (string c in new[] { "Total", "Saldo" })
                if (grilla.Columns[c] != null)
                    grilla.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is VentaVm vm && grilla.Columns["Estado"] != null)
                {
                    row.Cells["Estado"].Style.ForeColor = vm.EstadoColor;
                    row.Cells["Estado"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
        }

        private Venta06AV VentaSeleccionada() => (grilla.CurrentRow?.DataBoundItem as VentaVm)?.Venta;

        private void SeleccionarVenta(int numero)
        {
            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is VentaVm vm && vm.Numero == numero)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0) grilla.CurrentCell = row.Cells[0];
                    return;
                }
        }

        private void EstadoInfo(EstadoVenta06AV e, out string texto, out Color color)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoVenta06AV.Pendiente: texto = t.Obtener("pcf_est_vta_pendiente"); color = Tema.Advertencia; break;
                case EstadoVenta06AV.Senada: texto = t.Obtener("pcf_est_vta_senada"); color = Tema.Primario; break;
                case EstadoVenta06AV.EnProduccion: texto = t.Obtener("pcf_est_vta_produccion"); color = Tema.Acento; break;
                case EstadoVenta06AV.Entregada: texto = t.Obtener("pcf_est_vta_entregada"); color = Tema.Exito; break;
                default: texto = t.Obtener("pcf_est_vta_anulada"); color = Tema.Peligro; break;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Panel de detalle: "¿Qué sigue?"
        // ══════════════════════════════════════════════════════════════
        private void ActualizarDetalle()
        {
            flpDetalle.Controls.Clear();
            var t = GestorIdioma06AV.Instancia;
            _ventaSel = VentaSeleccionada();
            _ordenSel = null;

            if (_ventaSel == null)
            {
                lblDetTitulo.Text = string.Empty;
                lblDetEstado.Text = string.Empty;
                flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_sel_venta")));
                return;
            }

            var v = _ventaSel;
            string cli = v.Cliente != null ? v.Cliente.Apellido + ", " + v.Cliente.Nombre : "";
            lblDetTitulo.Text = t.Obtener("pcf_venta") + " #" + v.NumeroVenta + "  —  " + cli;
            EstadoInfo(v.Estado, out string estTxt, out Color estColor);
            lblDetEstado.Text = "● " + estTxt;
            lblDetEstado.ForeColor = estColor;
            lblDetEstado.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

            // Equipo
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_computadora"), fuerte: true));
            flpDetalle.Controls.Add(TextoSecundario(
                (v.Computadora != null ? v.Computadora.Nombre : "-") +
                "   ·   " + (v.Computadora != null ? v.Computadora.TipoConfiguracion.ToString() : "")));

            // Importes
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_total") + " / " + t.Obtener("pcf_abonado") + " / " + t.Obtener("pcf_saldo")));
            flpDetalle.Controls.Add(TextoSecundario(
                v.PrecioTotal.ToString("C0") + "   ·   " + v.TotalAbonado.ToString("C0") + "   ·   " + v.SaldoPendiente.ToString("C0")));

            // Seña
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_sena")));
            if (v.TieneSena)
            {
                var s = v.Sena;
                flpDetalle.Controls.Add(TextoSecundario(
                    "✔ " + s.Monto.ToString("C0") + "   ·   " + ComprobantePcFactory06AV.TextoFormaPago(s.FormaPago)));
                flpDetalle.Controls.Add(TextoSecundario(
                    s.NumeroRecibo + "   ·   " + s.Fecha.ToString("dd/MM/yyyy HH:mm") +
                    (string.IsNullOrWhiteSpace(s.Usuario) ? "" : "   ·   " + s.Usuario)));
            }
            else
            {
                flpDetalle.Controls.Add(TextoSecundario("○ " + t.Obtener("pcf_sena_pendiente") +
                    "   (" + v.MontoSenaRequerido.ToString("C0") + ")"));
            }

            // Entrega estimada
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_f_entrega_estimada")));
            flpDetalle.Controls.Add(TextoSecundario(v.FechaEntregaEstimada.ToShortDateString()));

            // Orden de producción asociada
            if (v.NumeroOrdenProduccion.HasValue)
            {
                try { _ordenSel = _ventasBLL.ObtenerOrdenDeVenta(v.NumeroVenta); } catch { _ordenSel = null; }
                flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_orden")));
                string estOrden = _ordenSel != null ? TextoEstadoOrden(_ordenSel.Estado) : "-";
                flpDetalle.Controls.Add(TextoSecundario("#" + v.NumeroOrdenProduccion.Value + "   ·   " + estOrden));
                if (_ordenSel != null && !string.IsNullOrWhiteSpace(_ordenSel.NumeroSerie))
                {
                    flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_nro_serie")));
                    flpDetalle.Controls.Add(TextoSecundario(_ordenSel.NumeroSerie));
                }
            }

            // ── ¿Qué sigue? ──────────────────────────────────────
            flpDetalle.Controls.Add(Separador());
            flpDetalle.Controls.Add(Etiqueta(t.Obtener("pcf_que_sigue"), fuerte: true));

            switch (v.Estado)
            {
                case EstadoVenta06AV.Pendiente:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_vta_senar")));
                    AddAccion(t.Obtener("pcf_registrar_sena"), Tema.AplicarBotonPrimario, AbrirFormPago);
                    AddAccion(t.Obtener("pcf_anular_venta"), Tema.AplicarBotonPeligro, AnularVenta);
                    break;

                case EstadoVenta06AV.Senada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_vta_esperando_orden")));
                    AddAccion(t.Obtener("pcf_reimprimir_recibo"), Tema.AplicarBotonSecundario, ReimprimirRecibo);
                    break;

                case EstadoVenta06AV.EnProduccion:
                    // El retiro y el cobro del saldo se hacen en "Entrega de computadoras".
                    flpDetalle.Controls.Add(TextoSecundario(
                        _ordenSel != null && _ordenSel.Estado == EstadoOrdenProduccion06AV.Finalizada
                            ? t.Obtener("pcf_hint_vta_retiro")
                            : t.Obtener("pcf_hint_vta_en_fabrica")));
                    AddAccion(t.Obtener("pcf_reimprimir_recibo"), Tema.AplicarBotonSecundario, ReimprimirRecibo);
                    break;

                case EstadoVenta06AV.Entregada:
                    flpDetalle.Controls.Add(TextoSecundario("✓  " + t.Obtener("pcf_hint_vta_entregada")));
                    AddAccion(t.Obtener("pcf_ver_factura"), Tema.AplicarBotonPrimario, VerFactura);
                    break;

                case EstadoVenta06AV.Anulada:
                    flpDetalle.Controls.Add(TextoSecundario(t.Obtener("pcf_hint_vta_anulada")));
                    break;
            }
        }

        private string TextoEstadoOrden(EstadoOrdenProduccion06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoOrdenProduccion06AV.Pendiente: return t.Obtener("pcf_est_op_pendiente");
                case EstadoOrdenProduccion06AV.Planificada: return t.Obtener("pcf_est_op_planificada");
                case EstadoOrdenProduccion06AV.EnEnsamblaje: return t.Obtener("pcf_est_op_ensamblaje");
                case EstadoOrdenProduccion06AV.Finalizada: return t.Obtener("pcf_est_op_finalizada");
                case EstadoOrdenProduccion06AV.Entregada: return t.Obtener("pcf_est_op_entregada");
                default: return t.Obtener("pcf_est_op_revision");
            }
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
            pnlFormVenta.Visible = false;
            pnlFormPago.Visible = false;
            pnlGrilla.Visible = true;
            pnlGrilla.BringToFront();
            ActualizarDetalle();
        }

        private void AbrirFormVenta()
        {
            CargarCombos();
            _tipoElegido = TipoConfiguracion06AV.Estandar;
            _modeloElegido = _modelos.FirstOrDefault();
            ArmarTarjetasTipo();
            ArmarTarjetasModelo();
            _componentesElegidos = new List<Componente06AV>();
            dtpEntrega.Value = DateTime.Today.AddDays(15);
            ActualizarModeloSegunTipo();
            ActualizarResumenComp();
            pnlGrilla.Visible = false;
            pnlFormPago.Visible = false;
            pnlFormVenta.Visible = true;
            pnlFormVenta.BringToFront();
        }

        /// <summary>CU03: cobro de la seña del 50% con su forma de pago.</summary>
        private void AbrirFormPago()
        {
            _ventaPago = VentaSeleccionada();
            if (_ventaPago == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }

            var t = GestorIdioma06AV.Instancia;

            lblFormPagoTit.Text = t.Obtener("pcf_registrar_sena") + " — " +
                                  t.Obtener("pcf_venta") + " #" + _ventaPago.NumeroVenta;

            string cli = _ventaPago.Cliente != null ? _ventaPago.Cliente.NombreCompleto : "";
            lblPagoDetalle.Text = t.Obtener("pcf_sena_ayuda");

            fichaPago.Titulo = t.Obtener("pcf_venta") + " #" + _ventaPago.NumeroVenta;
            fichaPago.RotuloDestacado = t.Obtener("pcf_sena_a_cobrar");
            fichaPago.ValorDestacado = _ventaPago.MontoSenaRequerido.ToString("C2");
            fichaPago.ColorDestacado = Tema.Primario;
            fichaPago.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("pcf_cliente"), cli),
                new DatoFicha06AV(t.Obtener("pcf_total"), _ventaPago.PrecioTotal.ToString("C0")),
                new DatoFicha06AV(t.Obtener("pcf_equipo"),
                                  _ventaPago.Computadora != null ? _ventaPago.Computadora.Nombre : "-", true)
            });
            fichaPago.Height = fichaPago.AltoNecesario;

            lblMontoValor.Text = _ventaPago.MontoSenaRequerido.ToString("C2");
            btnConfirmarPago.Text = t.Obtener("pcf_confirmar_sena");
            txtReferencia.Clear();
            if (cboFormaPago.Items.Count > 0) cboFormaPago.SelectedIndex = 0;
            ArmarTarjetasFormaPago();

            pnlGrilla.Visible = false;
            pnlFormVenta.Visible = false;
            pnlFormPago.Visible = true;
            pnlFormPago.BringToFront();
        }

        // ── Componentes: modelo estándar o asistente "Armá tu PC" ────
        /// <summary>
        /// El tipo elegido decide qué mitad de la pantalla tiene sentido: con Estándar
        /// se muestran los modelos y se esconde el configurador; con Configurable, al revés.
        /// Mostrar las dos cosas a la vez era lo que hacía el formulario viejo, con el
        /// combo de modelo grisado pero igual presente.
        /// </summary>
        private void ActualizarModeloSegunTipo()
        {
            bool estandar = _tipoElegido == TipoConfiguracion06AV.Estandar;

            lblModelo.Visible = estandar;
            flpModelos.Visible = estandar;
            pnlArmar.Visible = !estandar;

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
            if (_tipoElegido != TipoConfiguracion06AV.Estandar) return;
            if (_modeloElegido == null) { ActualizarResumenComp(); return; }
            _componentesElegidos = new List<Componente06AV>(_modeloElegido.Componentes ?? new List<Componente06AV>());
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

        private void ActualizarResumenComp()
        {
            _resumen.Mostrar(_componentesElegidos);

            bool hay = _componentesElegidos != null && _componentesElegidos.Count > 0;
            decimal total = hay ? _componentesElegidos.Sum(c => c.PrecioUnitario) : 0m;

            lblResumenVacio.Visible = !hay;
            lblTotalValor.Text = total.ToString("C0");
            btnRegistrar.Enabled = hay;
            if (hay) Tema.AplicarBotonPrimario(btnRegistrar);
            else Tema.AplicarBotonDeshabilitado(btnRegistrar);
        }

        /// <summary>CU02: alta rápida de cliente sin abandonar la venta.</summary>
        private void AbrirNuevoCliente()
        {
            using (var dlg = new FRMNuevoCliente06AV())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK || dlg.ClienteCreado == null) return;
                CargarCombos();
                foreach (var item in cboCliente.Items)
                    if (item is Cliente06AV c && c.Dni == dlg.ClienteCreado.Dni)
                    { cboCliente.SelectedItem = item; break; }
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Acciones (delegan en la BLL)
        // ══════════════════════════════════════════════════════════════
        private void RegistrarVenta()
        {
            var cliente = cboCliente.SelectedItem as Cliente06AV;
            if (cliente == null) { MostrarError("Elegí un cliente."); return; }
            if (_componentesElegidos == null || _componentesElegidos.Count == 0)
            { MostrarError("Elegí al menos un componente (botón “Armá tu PC”) o un modelo estándar."); return; }

            var pc = new Computadora06AV
            {
                TipoConfiguracion = _tipoElegido,
                Nombre = _tipoElegido == TipoConfiguracion06AV.Estandar && _modeloElegido != null
                    ? _modeloElegido.Nombre
                    : _tipoElegido.ToString()
            };
            foreach (var c in _componentesElegidos) pc.Componentes.Add(c);

            try
            {
                var venta = _ventasBLL.RegistrarVenta(cliente, pc, dtpEntrega.Value);
                MostrarGrilla();
                CargarVentas();
                SeleccionarVenta(venta.NumeroVenta);
                ConfirmacionForm.MostrarInfo(
                    $"Venta #{venta.NumeroVenta} registrada. Total: ${venta.PrecioTotal:0.00}.\n" +
                    $"Seña a cobrar (50%): ${venta.MontoSenaRequerido:0.00}.",
                    GestorIdioma06AV.Instancia.Obtener("pcf_ventas_titulo"),
                    ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void ConfirmarPago()
        {
            if (_ventaPago == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            var forma = (cboFormaPago.SelectedItem as FormaPagoVm)?.Valor ?? FormaPago06AV.Efectivo;
            string referencia = txtReferencia.Text.Trim();

            try
            {
                var pago = _ventasBLL.RegistrarSena(_ventaPago.NumeroVenta, forma, referencia);
                var actualizada = _ventasBLL.ObtenerPorNumero(_ventaPago.NumeroVenta) ?? _ventaPago;
                MostrarGrilla();
                CargarVentas();
                SeleccionarVenta(_ventaPago.NumeroVenta);

                // El recibo siempre queda guardado; solo se abre el PDF si el cliente lo pide.
                string recibo = ComprobantePcFactory06AV.GenerarReciboSena(actualizada, pago);
                ComprobantePcFactory06AV.PreguntarEImprimir(recibo, esFactura: false, owner: FindForm(),
                    encabezado: $"Seña registrada: ${pago.Monto:0.00} ({pago.NumeroRecibo}).\n" +
                                "La venta queda lista para que el gerente genere la orden de producción.");
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void ReimprimirRecibo()
        {
            var v = VentaSeleccionada();
            if (v == null || !v.TieneSena) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                string ruta = ComprobantePcFactory06AV.GenerarReciboSena(v, v.Sena);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void VerFactura()
        {
            var v = VentaSeleccionada();
            if (v == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                var orden = v.NumeroOrdenProduccion.HasValue ? _ventasBLL.ObtenerOrdenDeVenta(v.NumeroVenta) : null;
                string ruta = ComprobantePcFactory06AV.GenerarFactura(v, orden);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AnularVenta()
        {
            var v = VentaSeleccionada();
            if (v == null) return;
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Anular la venta #{v.NumeroVenta}? Se liberarán los componentes reservados.",
                GestorIdioma06AV.Instancia.Obtener("pcf_anular_venta"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                GestorIdioma06AV.Instancia.Obtener("pcf_anular_venta"), GestorIdioma06AV.Instancia.Obtener("cancelar"),
                FindForm());
            if (!ok) return;
            try
            {
                _ventasBLL.AnularVenta(v.NumeroVenta);
                CargarVentas();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════
        //  View-models
        // ══════════════════════════════════════════════════════════════
        private class VentaVm
        {
            public int Numero { get; set; }
            public string Cliente { get; set; }
            public string Fecha { get; set; }
            public string Total { get; set; }
            public string Saldo { get; set; }
            public string Estado { get; set; }

            [Browsable(false)] public Color EstadoColor { get; set; }
            [Browsable(false)] public Venta06AV Venta { get; set; }
        }

        private class FormaPagoVm
        {
            public FormaPago06AV Valor { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
        }
    }
}
