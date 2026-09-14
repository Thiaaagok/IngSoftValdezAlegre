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
    /// control de lo que bajó del camión, con la misma lógica que el control de
    /// calidad de producción: una línea por componente pedido, se marca lo que llegó
    /// y se corrige la cantidad cuando vino incompleto.
    ///
    /// Abajo, un veredicto en vivo dice qué va a pasar al confirmar — orden finalizada
    /// o recibida parcial con el faltante anotado — ANTES de confirmar, para que nadie
    /// cierre una orden creyendo que llegó todo.
    /// </summary>
    internal class RecepcionControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly Label _lblTitulo, _lblAyuda, _lblListaTit, _lblVeredicto, _lblEntrega, _lblObs;
        private readonly FlowLayoutPanel _lista;
        private readonly DateTimePicker _dtpEntrega;
        private readonly TextBox _txtObs;
        private readonly Button _btnTodo, _btnNada, _btnConfirmar, _btnCancelar;
        private readonly Panel _barra, _pnlPie;

        private readonly List<FilaRecepcion06AV> _filas = new List<FilaRecepcion06AV>();
        private int _numeroOrden;

        public RecepcionControl06AV()
        {
            _lblTitulo = new Label { AutoSize = true, Location = new Point(16, 14) };
            _lblAyuda = new Label { AutoSize = true, Location = new Point(18, 40) };

            _lblListaTit = new Label
            {
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 4, 0, 0)
            };

            _lista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 6, 6)
            };

            _btnTodo = NuevoBoton(200);
            _btnNada = NuevoBoton(200);
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

            _lblVeredicto = new Label
            {
                Dock = DockStyle.Top, Height = 44, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 0, 0, 0)
            };

            _lblEntrega = new Label { AutoSize = true, Location = new Point(2, 8) };
            _dtpEntrega = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 150,
                Location = new Point(2, 28)
            };
            _lblObs = new Label { AutoSize = true, Location = new Point(180, 8) };
            _txtObs = new TextBox { Width = 380, Location = new Point(180, 28) };

            _pnlPie = new Panel { Dock = DockStyle.Bottom, Height = 68 };
            _pnlPie.Controls.AddRange(new Control[] { _lblEntrega, _dtpEntrega, _lblObs, _txtObs });

            _btnCancelar = NuevoBoton(130);
            _btnConfirmar = NuevoBoton(260);
            _btnCancelar.Click += (s, e) => Cancelado?.Invoke(this, EventArgs.Empty);
            _btnConfirmar.Click += (s, e) => Confirmar();

            var flpBarra = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 12, 12, 0)
            };
            flpBarra.Controls.Add(_btnCancelar);
            flpBarra.Controls.Add(_btnConfirmar);

            _barra = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            _barra.Controls.Add(flpBarra);

            var cabecera = new Panel { Dock = DockStyle.Top, Height = 70 };
            cabecera.Controls.Add(_lblAyuda);
            cabecera.Controls.Add(_lblTitulo);

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 0, 18, 4) };
            cuerpo.Controls.Add(_lista);
            cuerpo.Controls.Add(_lblVeredicto);
            cuerpo.Controls.Add(flpAtajos);
            cuerpo.Controls.Add(_lblListaTit);
            cuerpo.Controls.Add(_pnlPie);

            Controls.Add(cuerpo);
            Controls.Add(_barra);
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
        /// Dice qué va a pasar al confirmar: completa cierra la orden; incompleta la deja
        /// Recibida parcial con el faltante anotado y se puede recibir el resto después.
        /// </summary>
        private void ActualizarVeredicto()
        {
            var t = GestorIdioma06AV.Instancia;

            int unidades = _filas.Sum(f => f.CantidadRecibida);
            int esperadas = _filas.Sum(f => f.Pendiente.Cantidad);
            bool completa = _filas.Count > 0 && _filas.All(f => f.Completo);
            bool algo = unidades > 0;

            if (!algo)
            {
                _lblVeredicto.Text = "!  " + t.Obtener("pcf_rec_nada");
                _lblVeredicto.ForeColor = Tema.Peligro;
            }
            else if (completa)
            {
                _lblVeredicto.Text = "✓  " + t.Obtener("pcf_rec_ver_completa", unidades);
                _lblVeredicto.ForeColor = Tema.Exito;
            }
            else
            {
                _lblVeredicto.Text = "!  " + t.Obtener("pcf_rec_ver_parcial", unidades, esperadas - unidades);
                _lblVeredicto.ForeColor = Tema.Advertencia;
            }

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
            Width = ancho, Height = 32,
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
            _lblEntrega.Text = t.Obtener("pcf_rec_fecha");
            _lblObs.Text = t.Obtener("pcf_observaciones");
            ActualizarVeredicto();
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(_lblTitulo);
            _lblAyuda.Font = Tema.FuenteRegular;
            _lblAyuda.ForeColor = Tema.TextoSuave;
            _lblAyuda.BackColor = Tema.FondoApp;

            foreach (Control c in new Control[] { _lista, _barra, _pnlPie })
                c.BackColor = Tema.FondoApp;

            Tema.AplicarSubtitulo(_lblListaTit);
            _lblListaTit.BackColor = Tema.FondoApp;
            _lblVeredicto.Font = Tema.FuenteBold;
            _lblVeredicto.BackColor = Tema.FondoApp;

            foreach (Label l in new[] { _lblEntrega, _lblObs })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
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
