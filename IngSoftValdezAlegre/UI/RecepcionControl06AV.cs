using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Resultado del control de recepción: qué llegó y cuándo.</summary>
    internal class RecepcionArmada06AV : EventArgs
    {
        public RecepcionArmada06AV(List<DetalleComponente06AV> recibidos, DateTime fechaEntrega,
                                   string observaciones, bool completa)
        {
            Recibidos = recibidos;
            FechaEntrega = fechaEntrega;
            Observaciones = observaciones;
            Completa = completa;
        }

        public List<DetalleComponente06AV> Recibidos { get; }
        public DateTime FechaEntrega { get; }
        public string Observaciones { get; }
        public bool Completa { get; }
    }

    /// <summary>
    /// CONTROL DE RECEPCIÓN (RFN2, paso 5).
    ///
    /// Recibir una orden dejó de ser un botón que decía "sí, llegó todo". Es un
    /// control de lo que bajó del camión: una línea por componente pedido, se marca lo
    /// que llegó y se corrige la cantidad cuando vino incompleto.
    ///
    /// La pantalla usa la misma estructura que "Nueva venta", porque la tarea tiene la
    /// misma forma: a la izquierda se DECIDE (qué llegó, cuánto de cada cosa) y a la
    /// derecha un remito fijo CONFIRMA — unidades a recibir en grande, el desglose
    /// pedido / recibido / pendiente, la fecha y las observaciones, y abajo el veredicto
    /// en vivo: si al confirmar la orden queda finalizada o recibida parcial. El
    /// operador ve la consecuencia ANTES de apretar el botón, no después.
    /// </summary>
    internal class RecepcionControl06AV : UserControl, IIdiomaAplicable06AV
    {
        // Cabecera
        private readonly Label _lblTitulo, _lblAyuda;

        // Izquierda — lista
        private readonly Label _lblListaTit;
        private readonly FlowLayoutPanel _lista;
        private readonly Button _btnTodo, _btnNada;

        // Derecha — remito
        private readonly Panel _pnlTicket, _pnlDatos, _relleno;
        private readonly Label _lblTicketTit, _lblVeredicto, _lblEntrega, _lblObs;
        private readonly FichaDatos06AV _ficha;
        private readonly DateTimePicker _dtpEntrega;
        private readonly TextBox _txtObs;
        private readonly FlowLayoutPanel _flpAcciones;
        private readonly Button _btnConfirmar, _btnCancelar;

        private readonly List<FilaRecepcion06AV> _filas = new List<FilaRecepcion06AV>();
        private int _numeroOrden;

        public RecepcionControl06AV()
        {
            // ── Cabecera ─────────────────────────────────────────
            _lblTitulo = new Label { AutoSize = true, Location = new Point(16, 14) };
            _lblAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };

            var cabecera = new Panel { Dock = DockStyle.Top, Height = 70 };
            cabecera.Controls.Add(_lblAyuda);
            cabecera.Controls.Add(_lblTitulo);

            // ── Izquierda: qué bajó del camión ───────────────────
            _lblListaTit = new Label
            {
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 4, 0, 0)
            };

            _btnTodo = NuevoBoton(180);
            _btnNada = NuevoBoton(180);
            _btnTodo.Click += (s, e) => MarcarTodas(true);
            _btnNada.Click += (s, e) => MarcarTodas(false);

            var flpAtajos = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, Padding = new Padding(0, 4, 0, 4)
            };
            flpAtajos.Controls.Add(_btnTodo);
            flpAtajos.Controls.Add(_btnNada);

            _lista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 6, 6)
            };

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 0, 18, 10) };
            cuerpo.Controls.Add(_lista);
            cuerpo.Controls.Add(flpAtajos);
            cuerpo.Controls.Add(_lblListaTit);

            // ── Derecha: remito ──────────────────────────────────
            _lblTicketTit = new Label { Dock = DockStyle.Top, Height = 28, AutoSize = false };
            _ficha = new FichaDatos06AV { Dock = DockStyle.Top, Height = 190 };

            _lblEntrega = new Label { AutoSize = true, Location = new Point(2, 4) };
            _dtpEntrega = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 150,
                Location = new Point(2, 22)
            };
            _lblObs = new Label { AutoSize = true, Location = new Point(2, 58) };
            _txtObs = new TextBox
            {
                Location = new Point(2, 76), Width = 300, Height = 54,
                Multiline = true, ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _pnlDatos = new Panel { Dock = DockStyle.Bottom, Height = 140 };
            _pnlDatos.Controls.AddRange(new Control[] { _lblEntrega, _dtpEntrega, _lblObs, _txtObs });

            _lblVeredicto = new Label
            {
                Dock = DockStyle.Bottom, Height = 62, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 2, 0, 2)
            };

            _btnConfirmar = NuevoBoton(300);
            _btnCancelar = NuevoBoton(300);
            _btnConfirmar.Margin = new Padding(0, 6, 0, 4);
            _btnCancelar.Margin = new Padding(0, 0, 0, 4);
            _btnConfirmar.Click += (s, e) => Confirmar();
            _btnCancelar.Click += (s, e) => Cancelado?.Invoke(this, EventArgs.Empty);

            _flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            _flpAcciones.Controls.Add(_btnConfirmar);
            _flpAcciones.Controls.Add(_btnCancelar);

            _relleno = new Panel { Dock = DockStyle.Fill };

            _pnlTicket = new Panel { Dock = DockStyle.Right, Width = 350, Padding = new Padding(18, 14, 18, 12) };
            _pnlTicket.Controls.Add(_relleno);
            _pnlTicket.Controls.Add(_pnlDatos);
            _pnlTicket.Controls.Add(_lblVeredicto);
            _pnlTicket.Controls.Add(_flpAcciones);
            _pnlTicket.Controls.Add(_ficha);
            _pnlTicket.Controls.Add(_lblTicketTit);

            Controls.Add(cuerpo);
            Controls.Add(_pnlTicket);
            Controls.Add(cabecera);

            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
        }

        public event EventHandler<RecepcionArmada06AV> Confirmado;
        public event EventHandler Cancelado;

        /// <summary>Carga lo que falta recibir de una orden.</summary>
        public void Cargar(int numeroOrden, IEnumerable<DetalleComponente06AV> pendiente)
        {
            var t = GestorIdioma06AV.Instancia;
            _numeroOrden = numeroOrden;

            _lista.SuspendLayout();
            foreach (Control c in _lista.Controls.Cast<Control>().ToList()) c.Dispose();
            _lista.Controls.Clear();
            _filas.Clear();

            foreach (DetalleComponente06AV d in (pendiente ?? Enumerable.Empty<DetalleComponente06AV>()))
            {
                var fila = new FilaRecepcion06AV(d)
                {
                    Width = AnchoFila(),
                    TextoCompleto = t.Obtener("pcf_rec_completo"),
                    TextoFaltan = t.Obtener("pcf_rec_faltan"),
                    TextoNoLlego = t.Obtener("pcf_rec_no_llego"),
                    TextoPedido = t.Obtener("pcf_rec_pedido"),
                    TextoRecibido = t.Obtener("pcf_rec_recibido")
                };
                fila.CambioRecepcion += (s, e) => ActualizarVeredicto();
                _filas.Add(fila);
                _lista.Controls.Add(fila);
            }

            _lista.ResumeLayout();

            _dtpEntrega.Value = DateTime.Today;
            _txtObs.Clear();
            AplicarIdioma();
            ActualizarVeredicto();
        }

        private int AnchoFila() => Math.Max(320, _lista.ClientSize.Width - 24);

        private void MarcarTodas(bool llego)
        {
            foreach (FilaRecepcion06AV f in _filas) f.Llego = llego;
            ActualizarVeredicto();
        }

        /// <summary>
        /// Recalcula el remito de la derecha: unidades que se van a recibir, desglose
        /// contra lo pedido y qué va a pasar con la orden al confirmar.
        /// </summary>
        private void ActualizarVeredicto()
        {
            var t = GestorIdioma06AV.Instancia;

            int unidades = _filas.Sum(f => f.CantidadRecibida);
            int esperadas = _filas.Sum(f => f.Pendiente.Cantidad);
            int lineas = _filas.Count;
            int lineasCompletas = _filas.Count(f => f.Completo);
            bool completa = lineas > 0 && lineasCompletas == lineas;
            bool algo = unidades > 0;

            Color color = !algo ? Tema.Peligro : completa ? Tema.Exito : Tema.Advertencia;

            _ficha.RotuloDestacado = t.Obtener("pcf_rec_u_recibir");
            _ficha.ValorDestacado = unidades + " / " + esperadas;
            _ficha.ColorDestacado = color;
            _ficha.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("pcf_cotp_orden"), _numeroOrden > 0 ? "#" + _numeroOrden : "—"),
                new DatoFicha06AV(t.Obtener("pcf_rec_lineas"), lineasCompletas + " / " + lineas),
                new DatoFicha06AV(t.Obtener("pcf_rec_pedido_lbl"), esperadas.ToString()),
                new DatoFicha06AV(t.Obtener("pcf_rec_pendientes"), Math.Max(0, esperadas - unidades).ToString())
            });
            _ficha.Height = _ficha.AltoNecesario;

            if (!algo) _lblVeredicto.Text = "!  " + t.Obtener("pcf_rec_nada");
            else if (completa) _lblVeredicto.Text = "✓  " + t.Obtener("pcf_rec_ver_completa", unidades);
            else _lblVeredicto.Text = "!  " + t.Obtener("pcf_rec_ver_parcial", unidades, esperadas - unidades);
            _lblVeredicto.ForeColor = color;

            _btnConfirmar.Enabled = algo;
            _btnConfirmar.Text = completa
                ? t.Obtener("pcf_rec_confirmar_completa")
                : t.Obtener("pcf_rec_confirmar_parcial");

            if (!algo) Tema.AplicarBotonDeshabilitado(_btnConfirmar);
            else if (completa) Tema.AplicarBotonPrimario(_btnConfirmar);
            else Tema.AplicarBotonAcento(_btnConfirmar);
        }

        private void Confirmar()
        {
            var recibidos = _filas
                .Where(f => f.CantidadRecibida > 0)
                .Select(f => new DetalleComponente06AV
                {
                    Componente = f.Pendiente.Componente,
                    Cantidad = f.CantidadRecibida
                })
                .ToList();

            if (recibidos.Count == 0) return;

            bool completa = _filas.All(f => f.Completo);
            Confirmado?.Invoke(this, new RecepcionArmada06AV(
                recibidos, _dtpEntrega.Value, _txtObs.Text.Trim(), completa));
        }

        private static Button NuevoBoton(int ancho) => new Button
        {
            Width = ancho, Height = 34,
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            _lblTitulo.Text = t.Obtener("pcf_rec_titulo") +
                              (_numeroOrden > 0 ? " — " + t.Obtener("pcf_orden_num") + " #" + _numeroOrden : "");
            _lblAyuda.Text = t.Obtener("pcf_rec_ayuda");
            _lblListaTit.Text = t.Obtener("pcf_rec_lista");
            _btnTodo.Text = t.Obtener("pcf_rec_todo");
            _btnNada.Text = t.Obtener("pcf_rec_nada_boton");
            _btnCancelar.Text = t.Obtener("cancelar");
            _lblTicketTit.Text = t.Obtener("pcf_rec_remito");
            _lblEntrega.Text = t.Obtener("pcf_rec_fecha");
            _lblObs.Text = t.Obtener("pcf_observaciones");
            ActualizarVeredicto();
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(_lblTitulo);
            _lblTitulo.BackColor = Tema.FondoApp;
            _lblAyuda.Font = Tema.FuenteRegular;
            _lblAyuda.ForeColor = Tema.TextoSuave;
            _lblAyuda.BackColor = Tema.FondoApp;

            _lista.BackColor = Tema.FondoApp;
            Tema.AplicarSubtitulo(_lblListaTit);
            _lblListaTit.BackColor = Tema.FondoApp;

            // Remito
            foreach (Control c in new Control[] { _pnlTicket, _pnlDatos, _relleno, _flpAcciones })
                c.BackColor = Tema.FondoPanel;

            Tema.AplicarSubtitulo(_lblTicketTit);
            _lblTicketTit.BackColor = Tema.FondoPanel;
            _lblVeredicto.Font = Tema.FuenteBold;
            _lblVeredicto.BackColor = Tema.FondoPanel;

            foreach (Label l in new[] { _lblEntrega, _lblObs })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoPanel;
            }

            Tema.AplicarEntrada(_dtpEntrega);
            Tema.AplicarEntrada(_txtObs);
            Tema.AplicarBotonSecundario(_btnTodo);
            Tema.AplicarBotonSecundario(_btnNada);
            Tema.AplicarBotonSecundario(_btnCancelar);

            foreach (FilaRecepcion06AV f in _filas) f.Invalidate();
            ActualizarVeredicto();
            Invalidate(true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_lista == null) return;
            int ancho = AnchoFila();
            foreach (Control c in _lista.Controls) c.Width = ancho;
        }
    }
}
