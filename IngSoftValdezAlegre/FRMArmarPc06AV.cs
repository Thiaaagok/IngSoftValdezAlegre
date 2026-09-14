using BE;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// ARMÁ TU PC — configurador de equipos a medida.
    ///
    /// Antes: un ListBox con una línea de texto por componente y un cuadro vacío
    /// llamado "Tu PC" que no decía nada hasta que elegías algo. El vendedor no veía
    /// cuántas piezas faltaban, ni podía comparar dos opciones sin leer dos renglones
    /// separados por puntos.
    ///
    /// Ahora la pantalla tiene dos mitades con roles distintos:
    ///   · IZQUIERDA, el CHASIS: las bahías del equipo están todas a la vista desde el
    ///     arranque, vacías y punteadas. Se van llenando a medida que elegís, y se puede
    ///     volver a cualquier bahía con un clic. El total acompaña abajo.
    ///   · DERECHA, el CATÁLOGO del paso actual: una ficha por componente con marca,
    ///     modelo, stock y precio, más una barra proporcional al más caro del paso para
    ///     ver el salto de precio de un vistazo. Un clic elige y avanza.
    ///
    /// Arriba, la tira de pasos ubica dónde estás dentro del armado.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class FRMArmarPc06AV : Form
    {
        private static readonly TipoComponente06AV[] OrdenTipos =
        {
            TipoComponente06AV.Procesador, TipoComponente06AV.PlacaMadre,
            TipoComponente06AV.MemoriaRAM, TipoComponente06AV.Disco,
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Fuente,
            TipoComponente06AV.Gabinete, TipoComponente06AV.Refrigeracion,
            TipoComponente06AV.Otro
        };

        // Opcionales: la PC funciona sin ellos. El resto es obligatorio.
        private static readonly TipoComponente06AV[] Salteables =
        {
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Refrigeracion, TipoComponente06AV.Otro
        };

        private static bool EsSalteable(TipoComponente06AV t) => Array.IndexOf(Salteables, t) >= 0;

        private readonly List<Componente06AV> _todos;
        private readonly List<TipoComponente06AV> _pasos;
        private int _paso;
        private readonly Dictionary<TipoComponente06AV, Componente06AV> _elegidos =
            new Dictionary<TipoComponente06AV, Componente06AV>();

        /// <summary>Componentes elegidos (uno por tipo). Válido tras cerrar con OK.</summary>
        public List<Componente06AV> Seleccionados => _elegidos.Values.Where(c => c != null).ToList();

        private ChasisPcControl06AV chasis;
        private PasosWizard06AV pasos;
        private Label lblChasisTit, lblPasoTit, lblPasoAyuda, lblSinOpciones;
        private FlowLayoutPanel flpCatalogo;
        private Button btnAtras, btnSaltear, btnSiguiente, btnCancelar;
        private Panel pnlChasis;

        public FRMArmarPc06AV(IEnumerable<Componente06AV> componentes,
                              IEnumerable<Componente06AV> preseleccion = null)
        {
            _todos = (componentes ?? Enumerable.Empty<Componente06AV>()).ToList();
            _pasos = OrdenTipos.Where(t => _todos.Any(c => c.Tipo == t)).ToList();
            if (preseleccion != null)
                foreach (Componente06AV c in preseleccion)
                    if (c != null) _elegidos[c.Tipo] = c;

            ConstruirUI();
            AplicarTema();
            Tema.TemaChanged += AplicarTema;
            FormClosed += (s, e) => Tema.TemaChanged -= AplicarTema;
            MostrarPaso();
        }

        #region Construcción

        private void ConstruirUI()
        {
            var t = GestorIdioma06AV.Instancia;

            Text = t.Obtener("pcf_armar_titulo");
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(860, 560);

            // ── Chasis (izquierda) ───────────────────────────────
            lblChasisTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false };
            chasis = new ChasisPcControl06AV { Dock = DockStyle.Fill };
            chasis.BahiaElegida += (s, i) => IrAlPaso(i);

            pnlChasis = new Panel { Dock = DockStyle.Left, Width = 330, Padding = new Padding(18, 14, 14, 14) };
            pnlChasis.Controls.Add(chasis);
            pnlChasis.Controls.Add(lblChasisTit);

            // ── Catálogo (derecha) ───────────────────────────────
            pasos = new PasosWizard06AV { Dock = DockStyle.Top, Height = 48 };
            pasos.PasoElegido += (s, i) => IrAlPaso(i);

            lblPasoTit = new Label { Dock = DockStyle.Top, Height = 30, AutoSize = false, Padding = new Padding(2, 4, 0, 0) };
            lblPasoAyuda = new Label { Dock = DockStyle.Top, Height = 22, AutoSize = false, Padding = new Padding(2, 0, 0, 0) };
            lblSinOpciones = new Label { Dock = DockStyle.Top, Height = 40, AutoSize = false, Padding = new Padding(4, 8, 0, 0), Visible = false };

            flpCatalogo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 6, 6, 6)
            };

            var pnlCatalogo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 6) };
            pnlCatalogo.Controls.Add(flpCatalogo);
            pnlCatalogo.Controls.Add(lblSinOpciones);
            pnlCatalogo.Controls.Add(lblPasoAyuda);
            pnlCatalogo.Controls.Add(lblPasoTit);
            pnlCatalogo.Controls.Add(pasos);

            // ── Barra inferior ───────────────────────────────────
            btnCancelar = Boton(120);
            btnAtras = Boton(140);
            btnSaltear = Boton(150);
            btnSiguiente = Boton(170);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnAtras.Click += (s, e) => Retroceder();
            btnSaltear.Click += (s, e) => Avanzar(true);
            btnSiguiente.Click += (s, e) => Avanzar(false);

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 13, 16, 0)
            };
            barra.Controls.Add(btnSiguiente);
            barra.Controls.Add(btnSaltear);
            barra.Controls.Add(btnAtras);
            barra.Controls.Add(btnCancelar);

            Controls.Add(pnlCatalogo);
            Controls.Add(pnlChasis);
            Controls.Add(barra);

            CancelButton = btnCancelar;
            ArmarChasis();
        }

        private static Button Boton(int ancho) => new Button
        {
            Width = ancho,
            Height = 34,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        private void ArmarChasis()
        {
            var bahias = _pasos.Select(tipo => new BahiaPc06AV
            {
                Tipo = tipo,
                Nombre = NombreTipo(tipo),
                Icono = IconoDe(tipo),
                Opcional = EsSalteable(tipo),
                Puesto = _elegidos.TryGetValue(tipo, out Componente06AV c) ? c : null
            }).ToList();

            chasis.DefinirBahias(bahias);
            pasos.DefinirPasos(_pasos.Select(NombreTipoCorto));
        }

        #endregion

        #region Navegación

        private void MostrarPaso()
        {
            var t = GestorIdioma06AV.Instancia;

            if (_pasos.Count == 0)
            {
                lblPasoTit.Text = t.Obtener("pcf_armar_sin_catalogo");
                lblPasoAyuda.Text = string.Empty;
                btnSaltear.Visible = btnAtras.Visible = false;
                btnSiguiente.Text = t.Obtener("cerrar");
                return;
            }

            TipoComponente06AV tipo = _pasos[_paso];
            bool salteable = EsSalteable(tipo);

            pasos.Actual = _paso;
            chasis.Actual = _paso;

            lblPasoTit.Text = t.Obtener("pcf_armar_elegi", NombreTipo(tipo));
            lblPasoAyuda.Text = salteable
                ? t.Obtener("pcf_armar_opcional")
                : t.Obtener("pcf_armar_obligatorio");

            CargarCatalogo(tipo);

            btnAtras.Visible = _paso > 0;
            btnSaltear.Visible = salteable;
            btnSiguiente.Text = _paso == _pasos.Count - 1
                ? t.Obtener("pcf_armar_finalizar")
                : t.Obtener("pcf_asis_siguiente") + "  →";
            btnAtras.Text = "←  " + t.Obtener("pcf_asis_atras");
            btnSaltear.Text = t.Obtener("pcf_armar_saltear");
            btnCancelar.Text = t.Obtener("cancelar");
            lblChasisTit.Text = t.Obtener("pcf_armar_tu_pc");
        }

        private void CargarCatalogo(TipoComponente06AV tipo)
        {
            var t = GestorIdioma06AV.Instancia;

            flpCatalogo.SuspendLayout();
            foreach (Control c in flpCatalogo.Controls.Cast<Control>().ToList()) c.Dispose();
            flpCatalogo.Controls.Clear();

            List<Componente06AV> opciones = _todos
                .Where(c => c.Tipo == tipo)
                .OrderBy(c => c.PrecioUnitario)
                .ToList();

            decimal tope = opciones.Count > 0 ? opciones.Max(c => c.PrecioUnitario) : 0m;
            _elegidos.TryGetValue(tipo, out Componente06AV elegido);

            foreach (Componente06AV c in opciones)
            {
                var ficha = new TarjetaComponente06AV
                {
                    Componente = c,
                    PrecioTope = tope,
                    Icono = IconoDe(tipo),
                    Seleccionada = elegido != null && elegido.Codigo == c.Codigo,
                    Width = AnchoFicha(),
                    TextoStock = t.Obtener("pcf_armar_stock"),
                    TextoSinStock = t.Obtener("pcf_armar_sin_stock")
                };
                Componente06AV cLocal = c;
                ficha.Elegida += (s, e) => Elegir(tipo, cLocal);
                flpCatalogo.Controls.Add(ficha);
            }

            lblSinOpciones.Text = t.Obtener("pcf_armar_sin_opciones");
            lblSinOpciones.Visible = opciones.Count == 0;
            flpCatalogo.ResumeLayout();
        }

        private int AnchoFicha() => Math.Max(280, flpCatalogo.ClientSize.Width - 24);

        /// <summary>Elegir una pieza la pone en el chasis y pasa sola al siguiente hueco.</summary>
        private void Elegir(TipoComponente06AV tipo, Componente06AV componente)
        {
            _elegidos[tipo] = componente;
            chasis.Poner(tipo, componente);

            foreach (Control c in flpCatalogo.Controls)
                if (c is TarjetaComponente06AV f)
                {
                    bool esta = f.Componente != null && f.Componente.Codigo == componente.Codigo;
                    if (f.Seleccionada != esta) f.Seleccionada = esta;
                }

            if (_paso < _pasos.Count - 1) { _paso++; MostrarPaso(); }
            else MostrarPaso();
        }

        private void IrAlPaso(int indice)
        {
            if (indice < 0 || indice >= _pasos.Count) return;
            _paso = indice;
            MostrarPaso();
        }

        private void Avanzar(bool saltear)
        {
            var t = GestorIdioma06AV.Instancia;
            if (_pasos.Count == 0) { DialogResult = DialogResult.Cancel; Close(); return; }

            TipoComponente06AV tipo = _pasos[_paso];

            if (saltear)
            {
                _elegidos.Remove(tipo);
                chasis.Poner(tipo, null);
            }
            else if (!EsSalteable(tipo) &&
                     !(_elegidos.TryGetValue(tipo, out Componente06AV ya) && ya != null))
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("pcf_armar_falta", NombreTipo(tipo)),
                    t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                return;
            }

            if (_paso == _pasos.Count - 1)
            {
                // Último paso: sólo se cierra si no quedó ningún obligatorio sin cubrir.
                List<TipoComponente06AV> faltantes = _pasos
                    .Where(x => !EsSalteable(x) && !(_elegidos.ContainsKey(x) && _elegidos[x] != null))
                    .ToList();

                if (faltantes.Count > 0)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("pcf_armar_faltan",
                                  string.Join(", ", faltantes.Select(NombreTipoCorto))),
                        t.Obtener("pcf_armar_titulo"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                    IrAlPaso(_pasos.IndexOf(faltantes[0]));
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _paso++;
            MostrarPaso();
        }

        private void Retroceder()
        {
            if (_paso > 0) { _paso--; MostrarPaso(); }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (flpCatalogo == null) return;
            int ancho = AnchoFicha();
            foreach (Control c in flpCatalogo.Controls) c.Width = ancho;
        }

        #endregion

        #region Tema

        private void AplicarTema()
        {
            var t = GestorIdioma06AV.Instancia;

            Tema.AplicarFormulario(this);
            BackColor = Tema.FondoApp;

            pnlChasis.BackColor = Tema.FondoPanel;
            chasis.BackColor = Tema.FondoPanel;
            lblChasisTit.Font = Tema.FuenteSubtit;
            lblChasisTit.ForeColor = Tema.TextoFuerte;
            lblChasisTit.BackColor = Tema.FondoPanel;

            chasis.TextoTotal = t.Obtener("pcf_armar_total");
            chasis.TextoOpcional = t.Obtener("pcf_armar_opcional_corto");
            chasis.TextoVacio = t.Obtener("pcf_armar_vacio");
            chasis.TextoPiezas = t.Obtener("pcf_armar_piezas");

            foreach (Control c in Controls)
                if (c is Panel || c is FlowLayoutPanel) c.BackColor = Tema.FondoApp;
            pnlChasis.BackColor = Tema.FondoPanel;
            flpCatalogo.BackColor = Tema.FondoApp;

            lblPasoTit.Font = Tema.FuenteSubtit;
            lblPasoTit.ForeColor = Tema.TextoFuerte;
            lblPasoTit.BackColor = Tema.FondoApp;
            lblPasoAyuda.Font = Tema.FuenteRegular;
            lblPasoAyuda.ForeColor = Tema.TextoSuave;
            lblPasoAyuda.BackColor = Tema.FondoApp;
            lblSinOpciones.Font = Tema.FuenteRegular;
            lblSinOpciones.ForeColor = Tema.TextoSuave;
            lblSinOpciones.BackColor = Tema.FondoApp;

            Tema.AplicarBotonPrimario(btnSiguiente);
            Tema.AplicarBotonSecundario(btnAtras);
            Tema.AplicarBotonSecundario(btnSaltear);
            Tema.AplicarBotonSecundario(btnCancelar);

            Invalidate(true);
        }

        #endregion

        #region Nombres e íconos de los tipos

        private static IconoPcf06AV IconoDe(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return IconoPcf06AV.Chip;
                case TipoComponente06AV.PlacaMadre: return IconoPcf06AV.Chip;
                case TipoComponente06AV.MemoriaRAM: return IconoPcf06AV.Chip;
                case TipoComponente06AV.Disco: return IconoPcf06AV.Caja;
                case TipoComponente06AV.PlacaDeVideo: return IconoPcf06AV.Chip;
                case TipoComponente06AV.Fuente: return IconoPcf06AV.Caja;
                case TipoComponente06AV.Gabinete: return IconoPcf06AV.Caja;
                case TipoComponente06AV.Refrigeracion: return IconoPcf06AV.Destornillador;
                default: return IconoPcf06AV.Caja;
            }
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            var i = GestorIdioma06AV.Instancia;
            switch (t)
            {
                case TipoComponente06AV.Procesador: return i.Obtener("pcf_tipo_procesador");
                case TipoComponente06AV.PlacaMadre: return i.Obtener("pcf_tipo_placa_madre");
                case TipoComponente06AV.MemoriaRAM: return i.Obtener("pcf_tipo_ram");
                case TipoComponente06AV.Disco: return i.Obtener("pcf_tipo_disco");
                case TipoComponente06AV.PlacaDeVideo: return i.Obtener("pcf_tipo_video");
                case TipoComponente06AV.Fuente: return i.Obtener("pcf_tipo_fuente");
                case TipoComponente06AV.Gabinete: return i.Obtener("pcf_tipo_gabinete");
                case TipoComponente06AV.Refrigeracion: return i.Obtener("pcf_tipo_refrigeracion");
                default: return i.Obtener("pcf_tipo_otro");
            }
        }

        private static string NombreTipoCorto(TipoComponente06AV t)
        {
            switch (t)
            {
                case TipoComponente06AV.Procesador: return "CPU";
                case TipoComponente06AV.PlacaMadre: return "Mother";
                case TipoComponente06AV.MemoriaRAM: return "RAM";
                case TipoComponente06AV.Disco: return "Disco";
                case TipoComponente06AV.PlacaDeVideo: return "GPU";
                case TipoComponente06AV.Fuente: return "Fuente";
                case TipoComponente06AV.Gabinete: return "Gabinete";
                case TipoComponente06AV.Refrigeracion: return "Cooler";
                default: return "Otro";
            }
        }

        #endregion
    }
}

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Resumen de una PC ya armada, para mostrar dentro de otra pantalla (p. ej. la
    /// venta). Agrupa por tipo de componente y cierra con el total.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class ResumenPcControl06AV : FlowLayoutPanel
    {
        private static readonly TipoComponente06AV[] Orden =
        {
            TipoComponente06AV.Procesador, TipoComponente06AV.PlacaMadre,
            TipoComponente06AV.MemoriaRAM, TipoComponente06AV.Disco,
            TipoComponente06AV.PlacaDeVideo, TipoComponente06AV.Fuente,
            TipoComponente06AV.Gabinete, TipoComponente06AV.Refrigeracion,
            TipoComponente06AV.Otro
        };

        public ResumenPcControl06AV()
        {
            FlowDirection = FlowDirection.TopDown;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = false;
            Margin = new Padding(0);
            Padding = new Padding(0, 2, 0, 2);
            Mostrar(null);
        }

        /// <summary>Reconstruye el resumen a partir de la lista de componentes.</summary>
        public void Mostrar(List<Componente06AV> componentes)
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) c.Dispose();
            Controls.Clear();

            if (componentes == null || componentes.Count == 0)
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    ForeColor = Tema.TextoSuave,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    Text = GestorIdioma06AV.Instancia.Obtener("pcf_sin_componentes"),
                    Margin = new Padding(0, 2, 0, 2)
                });
                ResumeLayout();
                return;
            }

            decimal total = 0;
            foreach (var grupo in componentes
                        .GroupBy(c => c.Tipo)
                        .OrderBy(g => Array.IndexOf(Orden, g.Key)))
            {
                Controls.Add(new Label
                {
                    AutoSize = true,
                    Font = Tema.FuenteMini,
                    ForeColor = Tema.Primario,
                    Text = NombreTipo(grupo.Key).ToUpperInvariant(),
                    Margin = new Padding(0, 8, 0, 1)
                });

                foreach (Componente06AV c in grupo)
                {
                    total += c.PrecioUnitario;
                    Controls.Add(new Label
                    {
                        AutoSize = true,
                        ForeColor = Tema.Texto,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                        Text = "   " + c.Descripcion + "   ·   " + (c.Marca + " " + c.Modelo).Trim() +
                               "   ·   " + c.PrecioUnitario.ToString("C0"),
                        Margin = new Padding(0, 0, 0, 1)
                    });
                }
            }

            Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Tema.TextoFuerte,
                Text = GestorIdioma06AV.Instancia.Obtener("pcf_armar_total") + ":  " +
                       total.ToString("C0") + "     ·     " +
                       GestorIdioma06AV.Instancia.Obtener("pcf_armar_n_componentes", componentes.Count),
                Margin = new Padding(0, 10, 0, 2)
            });

            ResumeLayout();
        }

        private static string NombreTipo(TipoComponente06AV t)
        {
            var i = GestorIdioma06AV.Instancia;
            switch (t)
            {
                case TipoComponente06AV.Procesador: return i.Obtener("pcf_tipo_procesador");
                case TipoComponente06AV.PlacaMadre: return i.Obtener("pcf_tipo_placa_madre");
                case TipoComponente06AV.MemoriaRAM: return i.Obtener("pcf_tipo_ram");
                case TipoComponente06AV.Disco: return i.Obtener("pcf_tipo_disco");
                case TipoComponente06AV.PlacaDeVideo: return i.Obtener("pcf_tipo_video");
                case TipoComponente06AV.Fuente: return i.Obtener("pcf_tipo_fuente");
                case TipoComponente06AV.Gabinete: return i.Obtener("pcf_tipo_gabinete");
                case TipoComponente06AV.Refrigeracion: return i.Obtener("pcf_tipo_refrigeracion");
                default: return i.Obtener("pcf_tipo_otro");
            }
        }
    }
}
