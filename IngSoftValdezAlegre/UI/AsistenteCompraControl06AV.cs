using BE;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// ASISTENTE DE COMPRA (RFN2) — reemplaza la tabla de checkboxes de "Nueva orden".
    ///
    /// Problema del diseño anterior: una grilla plana con una casilla por fila trata
    /// igual a un componente con 1 unidad y a uno con 9. No hay jerarquía, no hay
    /// urgencia y no hay noción de "estoy armando un pedido".
    ///
    /// Diseño nuevo, en tres pasos que siguen la decisión real del repositor:
    ///   1. QUÉ FALTA  — tarjetas agrupadas por criticidad (crítico / bajo / cubierto),
    ///      cada una con su medidor de reposición y la cantidad sugerida ya cargada.
    ///      Un panel lateral acumula el pedido en vivo, como un carrito.
    ///   2. PLAZO      — cuándo se necesita el material y quién lo pide.
    ///   3. CONFIRMAR  — repaso completo antes de impactar.
    ///
    /// No pierde ninguna capacidad de la pantalla anterior: se puede elegir cualquier
    /// componente, editar cantidades una por una y fijar la fecha límite.
    /// </summary>
    internal class AsistenteCompraControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly PasosWizard06AV _pasos;
        private readonly Label _lblTitulo, _lblAyuda;

        private readonly Panel _pagina1, _pagina2, _pagina3;
        private readonly FlowLayoutPanel _lista;
        private readonly Panel _carrito;
        private readonly Label _lblCarritoTit, _lblCarritoDet, _lblCarritoVacio;
        private readonly Button _btnCriticos, _btnLimpiar;

        private readonly Label _lblLimite, _lblRepositor, _lblRepositorValor;
        private readonly DateTimePicker _dtpLimite;
        private readonly FlowLayoutPanel _resumen2;

        private readonly FlowLayoutPanel _resumen3;
        private readonly Label _lblResumenTit;

        private readonly Button _btnAtras, _btnSiguiente, _btnRegistrar, _btnCancelar;
        private readonly Panel _barra;

        private readonly List<TarjetaFaltante06AV> _tarjetas = new List<TarjetaFaltante06AV>();
        private readonly List<Label> _titulosGrupo = new List<Label>();
        private int _paso;

        public AsistenteCompraControl06AV()
        {
            _lblTitulo = new Label { AutoSize = true, Location = new Point(14, 12) };
            _lblAyuda = new Label { AutoSize = false, Dock = DockStyle.Top, Height = 22, Padding = new Padding(14, 0, 14, 0) };

            _pasos = new PasosWizard06AV { Dock = DockStyle.Top };
            _pasos.PasoElegido += (s, i) => IrA(i);

            // ── Paso 1 ────────────────────────────────────────────
            _lista = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10, 6, 10, 10)
            };

            _lblCarritoTit = new Label { Dock = DockStyle.Top, Height = 26, AutoSize = false };
            _lblCarritoDet = new Label { Dock = DockStyle.Top, Height = 46, AutoSize = false };
            _lblCarritoVacio = new Label { Dock = DockStyle.Top, Height = 60, AutoSize = false };
            _btnCriticos = NuevoBoton();
            _btnLimpiar = NuevoBoton();
            _btnCriticos.Click += (s, e) => SeleccionarCriticos();
            _btnLimpiar.Click += (s, e) => LimpiarSeleccion();

            var flpCarrito = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 8, 0, 0)
            };
            flpCarrito.Controls.Add(_btnCriticos);
            flpCarrito.Controls.Add(_btnLimpiar);

            _carrito = new Panel { Dock = DockStyle.Right, Width = 250, Padding = new Padding(14, 12, 14, 12) };
            _carrito.Controls.Add(flpCarrito);
            _carrito.Controls.Add(_lblCarritoVacio);
            _carrito.Controls.Add(_lblCarritoDet);
            _carrito.Controls.Add(_lblCarritoTit);

            _pagina1 = new Panel { Dock = DockStyle.Fill };
            _pagina1.Controls.Add(_lista);
            _pagina1.Controls.Add(_carrito);

            // ── Paso 2 ────────────────────────────────────────────
            _lblLimite = new Label { AutoSize = true, Location = new Point(16, 18) };
            _dtpLimite = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(5),
                Width = 150,
                Location = new Point(16, 40)
            };
            _lblRepositor = new Label { AutoSize = true, Location = new Point(200, 18) };
            _lblRepositorValor = new Label { AutoSize = true, Location = new Point(200, 42) };

            _resumen2 = new FlowLayoutPanel
            {
                Location = new Point(16, 84),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _pagina2 = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(10) };
            _pagina2.Controls.Add(_lblLimite);
            _pagina2.Controls.Add(_dtpLimite);
            _pagina2.Controls.Add(_lblRepositor);
            _pagina2.Controls.Add(_lblRepositorValor);
            _pagina2.Controls.Add(_resumen2);

            // ── Paso 3 ────────────────────────────────────────────
            _lblResumenTit = new Label { Dock = DockStyle.Top, Height = 28, AutoSize = false, Padding = new Padding(6, 4, 0, 0) };
            _resumen3 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(6)
            };
            _pagina3 = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(10) };
            _pagina3.Controls.Add(_resumen3);
            _pagina3.Controls.Add(_lblResumenTit);

            // ── Barra inferior ───────────────────────────────────
            _btnCancelar = NuevoBoton(120);
            _btnAtras = NuevoBoton(110);
            _btnSiguiente = NuevoBoton(140);
            _btnRegistrar = NuevoBoton(170);
            _btnCancelar.Click += (s, e) => Cancelado?.Invoke(this, EventArgs.Empty);
            _btnAtras.Click += (s, e) => IrA(_paso - 1);
            _btnSiguiente.Click += (s, e) => Siguiente();
            _btnRegistrar.Click += (s, e) => Registrar();

            var flpBarra = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 10, 0)
            };
            flpBarra.Controls.AddRange(new Control[] { _btnCancelar, _btnAtras, _btnSiguiente, _btnRegistrar });

            _barra = new Panel { Dock = DockStyle.Bottom, Height = 58 };
            _barra.Controls.Add(flpBarra);

            var cabecera = new Panel { Dock = DockStyle.Top, Height = 46 };
            cabecera.Controls.Add(_lblTitulo);

            var cuerpo = new Panel { Dock = DockStyle.Fill };
            cuerpo.Controls.Add(_pagina3);
            cuerpo.Controls.Add(_pagina2);
            cuerpo.Controls.Add(_pagina1);

            Controls.Add(cuerpo);
            Controls.Add(_barra);
            Controls.Add(_lblAyuda);
            Controls.Add(_pasos);
            Controls.Add(cabecera);

            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
        }

        #region API pública

        /// <summary>Confirmación final: detalles elegidos y fecha límite.</summary>
        public event EventHandler<OrdenArmada06AV> Confirmado;

        /// <summary>El usuario salió del asistente.</summary>
        public event EventHandler Cancelado;

        /// <summary>Carga los candidatos y vuelve al paso 1.</summary>
        public void Cargar(IEnumerable<FaltanteItem06AV> items, string loginRepositor)
        {
            var t = GestorIdioma06AV.Instancia;

            _lista.SuspendLayout();
            foreach (Control c in _lista.Controls.Cast<Control>().ToList()) c.Dispose();
            _lista.Controls.Clear();
            _tarjetas.Clear();
            _titulosGrupo.Clear();

            var lista = (items ?? Enumerable.Empty<FaltanteItem06AV>()).ToList();

            // Agrupación por criticidad: primero lo que puede frenar la producción.
            AgregarGrupo(t.Obtener("pcf_asis_criticos"), lista.Where(EsCritico));
            AgregarGrupo(t.Obtener("pcf_asis_bajos"), lista.Where(i => !EsCritico(i) && i.Stock < i.StockMinimo));
            AgregarGrupo(t.Obtener("pcf_asis_cubiertos"), lista.Where(i => i.Stock >= i.StockMinimo));

            if (_tarjetas.Count == 0)
            {
                var vacio = new Label
                {
                    Text = t.Obtener("pcf_asis_sin_faltantes"),
                    AutoSize = true,
                    Margin = new Padding(6, 16, 6, 6),
                    ForeColor = Tema.TextoSuave,
                    Font = Tema.FuenteRegular
                };
                _lista.Controls.Add(vacio);
            }

            _lista.ResumeLayout();

            _lblRepositorValor.Text = string.IsNullOrWhiteSpace(loginRepositor) ? "-" : loginRepositor;
            _dtpLimite.Value = DateTime.Today.AddDays(5);
            _dtpLimite.MinDate = DateTime.Today;

            IrA(0);
            ActualizarCarrito();
        }

        #endregion

        private static bool EsCritico(FaltanteItem06AV i) =>
            i.Stock < Math.Max(1, i.StockMinimo) * 0.5f;

        private void AgregarGrupo(string titulo, IEnumerable<FaltanteItem06AV> items)
        {
            var t = GestorIdioma06AV.Instancia;
            List<FaltanteItem06AV> lista = items.OrderBy(i => i.Stock / (float)Math.Max(1, i.StockMinimo))
                                                .ThenBy(i => i.Codigo)
                                                .ToList();
            if (lista.Count == 0) return;

            var lbl = new Label
            {
                Text = titulo + "  (" + lista.Count + ")",
                AutoSize = true,
                Margin = new Padding(6, 12, 6, 4),
                Font = Tema.FuenteSubtit,
                ForeColor = Tema.Texto
            };
            _titulosGrupo.Add(lbl);
            _lista.Controls.Add(lbl);

            foreach (FaltanteItem06AV item in lista)
            {
                var tarjeta = new TarjetaFaltante06AV(item, t.Obtener("pcf_asis_reponer"),
                                                      t.Obtener("pcf_asis_bloqueado"))
                {
                    Width = Math.Max(320, _lista.ClientSize.Width - 34)
                };
                tarjeta.IncluidoCambiado += (s, e) => ActualizarCarrito();
                tarjeta.CantidadCambiada += (s, e) => ActualizarCarrito();
                _tarjetas.Add(tarjeta);
                _lista.Controls.Add(tarjeta);
            }
        }

        private IEnumerable<TarjetaFaltante06AV> Elegidas() => _tarjetas.Where(t => t.Incluido);

        private void SeleccionarCriticos()
        {
            foreach (TarjetaFaltante06AV t in _tarjetas)
                if (!t.Item.Bloqueado && EsCritico(t.Item)) t.Incluido = true;
            ActualizarCarrito();
        }

        private void LimpiarSeleccion()
        {
            foreach (TarjetaFaltante06AV t in _tarjetas) t.Incluido = false;
            ActualizarCarrito();
        }

        private void ActualizarCarrito()
        {
            var t = GestorIdioma06AV.Instancia;
            List<TarjetaFaltante06AV> elegidas = Elegidas().ToList();
            int unidades = elegidas.Sum(x => x.Cantidad);

            _lblCarritoDet.Text = t.Obtener("pcf_asis_seleccionados", elegidas.Count, unidades);
            _lblCarritoDet.Visible = elegidas.Count > 0;
            _lblCarritoVacio.Visible = elegidas.Count == 0;

            _btnSiguiente.Enabled = elegidas.Count > 0;
            if (_btnSiguiente.Enabled) Tema.AplicarBotonPrimario(_btnSiguiente);
            else Tema.AplicarBotonDeshabilitado(_btnSiguiente);
        }

        private void Siguiente()
        {
            if (_paso == 0 && !Elegidas().Any()) return;
            IrA(_paso + 1);
        }

        private void IrA(int paso)
        {
            _paso = Math.Max(0, Math.Min(2, paso));
            _pasos.Actual = _paso;

            _pagina1.Visible = _paso == 0;
            _pagina2.Visible = _paso == 1;
            _pagina3.Visible = _paso == 2;
            if (_pagina1.Visible) _pagina1.BringToFront();
            if (_pagina2.Visible) { ArmarResumen(_resumen2); _pagina2.BringToFront(); }
            if (_pagina3.Visible) { ArmarResumen(_resumen3); _pagina3.BringToFront(); }

            _btnAtras.Visible = _paso > 0;
            _btnSiguiente.Visible = _paso < 2;
            _btnRegistrar.Visible = _paso == 2;

            AplicarIdioma();
            ActualizarCarrito();
        }

        private void ArmarResumen(FlowLayoutPanel destino)
        {
            var t = GestorIdioma06AV.Instancia;
            destino.SuspendLayout();
            foreach (Control c in destino.Controls.Cast<Control>().ToList()) c.Dispose();
            destino.Controls.Clear();

            foreach (TarjetaFaltante06AV tarjeta in Elegidas())
            {
                var fila = new Label
                {
                    AutoSize = true,
                    Margin = new Padding(2, 2, 2, 2),
                    Font = Tema.FuenteRegular,
                    ForeColor = Tema.Texto,
                    Text = tarjeta.Item.Codigo + "   ·   " + tarjeta.Item.Descripcion +
                           "   ·   " + t.Obtener("pcf_col_cantidad") + ": " + tarjeta.Cantidad
                };
                destino.Controls.Add(fila);
            }

            var total = new Label
            {
                AutoSize = true,
                Margin = new Padding(2, 10, 2, 2),
                Font = Tema.FuenteBold,
                ForeColor = Tema.TextoFuerte,
                Text = t.Obtener("pcf_asis_seleccionados",
                                 Elegidas().Count(), Elegidas().Sum(x => x.Cantidad)) +
                       "   ·   " + t.Obtener("pcf_f_limite") + ": " + _dtpLimite.Value.ToShortDateString()
            };
            destino.Controls.Add(total);
            destino.ResumeLayout();
        }

        private void Registrar()
        {
            var detalles = new List<DetalleComponente06AV>();
            foreach (TarjetaFaltante06AV tarjeta in Elegidas())
                detalles.Add(new DetalleComponente06AV
                {
                    Cantidad = tarjeta.Cantidad,
                    Componente = new Componente06AV
                    {
                        Codigo = tarjeta.Item.Codigo,
                        Descripcion = tarjeta.Item.Descripcion,
                        Stock = tarjeta.Item.Stock,
                        StockMinimo = tarjeta.Item.StockMinimo
                    }
                });

            if (detalles.Count == 0) return;
            Confirmado?.Invoke(this, new OrdenArmada06AV(detalles, _dtpLimite.Value));
        }

        private static Button NuevoBoton(int ancho = 150) => new Button
        {
            Width = ancho,
            Height = 32,
            Margin = new Padding(6, 0, 0, 6),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            _lblTitulo.Text = t.Obtener("pcf_asis_titulo");
            _pasos.DefinirPasos(new[]
            {
                t.Obtener("pcf_asis_paso1"), t.Obtener("pcf_asis_paso2"), t.Obtener("pcf_asis_paso3")
            });

            _lblAyuda.Text = _paso == 0 ? t.Obtener("pcf_asis_hint1")
                           : _paso == 1 ? t.Obtener("pcf_asis_hint2")
                                        : t.Obtener("pcf_asis_hint3");

            _lblCarritoTit.Text = t.Obtener("pcf_asis_carrito");
            _lblCarritoVacio.Text = t.Obtener("pcf_asis_elegir_uno");
            _btnCriticos.Text = t.Obtener("pcf_asis_sel_criticos");
            _btnLimpiar.Text = t.Obtener("pcf_asis_limpiar");

            _lblLimite.Text = t.Obtener("pcf_f_limite");
            _lblRepositor.Text = t.Obtener("pcf_repositor");
            _lblResumenTit.Text = t.Obtener("pcf_asis_resumen");

            _btnCancelar.Text = t.Obtener("cancelar");
            _btnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            _btnSiguiente.Text = t.Obtener("pcf_asis_siguiente") + "  →";
            _btnRegistrar.Text = t.Obtener("pcf_asis_confirmar");
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;

            Tema.AplicarTitulo(_lblTitulo);
            _lblAyuda.Font = Tema.FuenteRegular;
            _lblAyuda.ForeColor = Tema.TextoSuave;
            _lblAyuda.BackColor = Tema.FondoApp;

            foreach (Control p in new Control[] { _pagina1, _pagina2, _pagina3, _barra, _lista, _resumen2, _resumen3 })
                p.BackColor = Tema.FondoApp;

            _carrito.BackColor = Tema.FondoPanel;
            _lblCarritoTit.Font = Tema.FuenteSubtit;
            _lblCarritoTit.ForeColor = Tema.TextoFuerte;
            _lblCarritoTit.BackColor = Tema.FondoPanel;
            _lblCarritoDet.Font = Tema.FuenteBold;
            _lblCarritoDet.ForeColor = Tema.Primario;
            _lblCarritoDet.BackColor = Tema.FondoPanel;
            _lblCarritoVacio.Font = Tema.FuenteRegular;
            _lblCarritoVacio.ForeColor = Tema.TextoSuave;
            _lblCarritoVacio.BackColor = Tema.FondoPanel;

            Tema.AplicarBotonSecundario(_btnCriticos);
            Tema.AplicarBotonSecundario(_btnLimpiar);
            Tema.AplicarBotonSecundario(_btnCancelar);
            Tema.AplicarBotonSecundario(_btnAtras);
            Tema.AplicarBotonPrimario(_btnSiguiente);
            Tema.AplicarBotonAcento(_btnRegistrar);

            Tema.AplicarEntrada(_dtpLimite);
            _lblLimite.ForeColor = Tema.TextoSuave;
            _lblRepositor.ForeColor = Tema.TextoSuave;
            _lblRepositorValor.Font = Tema.FuenteBold;
            _lblRepositorValor.ForeColor = Tema.Texto;
            Tema.AplicarSubtitulo(_lblResumenTit);
            _lblResumenTit.BackColor = Tema.FondoApp;

            foreach (Label l in _titulosGrupo)
            {
                l.Font = Tema.FuenteSubtit;
                l.ForeColor = Tema.Texto;
                l.BackColor = Tema.FondoApp;
            }
            foreach (TarjetaFaltante06AV tarjeta in _tarjetas) tarjeta.AplicarTema();

            Invalidate(true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (TarjetaFaltante06AV tarjeta in _tarjetas)
                tarjeta.Width = Math.Max(320, _lista.ClientSize.Width - 34);
            _resumen2.Size = new Size(Math.Max(200, _pagina2.ClientSize.Width - 32),
                                      Math.Max(80, _pagina2.ClientSize.Height - 100));
        }
    }

    /// <summary>Resultado del asistente: qué se pide y para cuándo.</summary>
    internal class OrdenArmada06AV : EventArgs
    {
        public OrdenArmada06AV(List<DetalleComponente06AV> detalles, DateTime fechaLimite)
        {
            Detalles = detalles;
            FechaLimite = fechaLimite;
        }

        public List<DetalleComponente06AV> Detalles { get; }
        public DateTime FechaLimite { get; }
    }
}
