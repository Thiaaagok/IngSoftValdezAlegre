using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Esqueleto común de las pantallas "paso a paso", el mismo del asistente de compra y
    /// del pedido de cotización: título, cabecera de pasos, una línea de ayuda, una
    /// página por paso, un panel lateral con el resumen y una barra inferior con
    /// Atrás / Siguiente y los botones propios de cada pantalla.
    ///
    /// Todo crece con el texto (AutoSize) en vez de tener altos fijos: con la escala de
    /// Windows al 125% o 150% los altos fijos cortaban botones y sumaban barras de scroll.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal abstract class AsistenteBase06AV : UserControl, IIdiomaAplicable06AV
    {
        protected readonly Label LblTitulo, LblSubtitulo, LblAyuda;
        protected readonly PasosWizard06AV Pasos;
        protected readonly Panel Lateral;
        protected readonly Button BtnAtras, BtnSiguiente;

        private readonly Panel _cuerpo, _barra;
        private readonly FlowLayoutPanel _botones;
        private readonly List<Panel> _paginas = new List<Panel>();

        protected AsistenteBase06AV()
        {
            LblTitulo = new Label { AutoSize = true, Margin = new Padding(0) };
            LblSubtitulo = new Label { AutoSize = true, Margin = new Padding(2, 4, 0, 0) };
            var cabecera = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16, 12, 16, 4)
            };
            cabecera.Controls.Add(LblTitulo);
            cabecera.Controls.Add(LblSubtitulo);

            Pasos = new PasosWizard06AV { Dock = DockStyle.Top };
            Pasos.PasoElegido += (s, i) => IrA(i);

            LblAyuda = new Label
            {
                Dock = DockStyle.Top, AutoSize = true, MaximumSize = new Size(4000, 0),
                Padding = new Padding(16, 2, 16, 8)
            };

            _cuerpo = new Panel { Dock = DockStyle.Fill };

            BtnAtras = NuevoBoton(130);
            BtnSiguiente = NuevoBoton(160);
            BtnAtras.Click += (s, e) => IrA(PasoActual - 1);
            BtnSiguiente.Click += (s, e) => AlSiguiente();

            _botones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 10, 12, 10)
            };
            _botones.Controls.Add(BtnAtras);
            _botones.Controls.Add(BtnSiguiente);
            // Un Panel con AutoSize no mide a un hijo acoplado y quedaba con alto 0: la barra
            // (y con ella Atrás / Siguiente) no se veía. El alto se copia de los botones.
            _barra = new Panel { Dock = DockStyle.Bottom, Height = 56 };
            _barra.Controls.Add(_botones);
            _botones.SizeChanged += (s, e) => { if (_botones.Height > 0) _barra.Height = _botones.Height; };

            var izquierda = new Panel { Dock = DockStyle.Fill };
            izquierda.Controls.Add(_cuerpo);
            izquierda.Controls.Add(_barra);
            izquierda.Controls.Add(LblAyuda);
            izquierda.Controls.Add(Pasos);

            Lateral = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(16, 14, 16, 12) };

            Controls.Add(izquierda);
            Controls.Add(Lateral);
            Controls.Add(cabecera);
        }

        protected int PasoActual { get; private set; }
        protected int CantidadPasos => _paginas.Count;

        /// <summary>Agrega la página de un paso. Se llaman en orden: la primera es el paso 0.</summary>
        protected Panel NuevaPagina()
        {
            var p = new Panel { Dock = DockStyle.Fill, Visible = _paginas.Count == 0, Padding = new Padding(16, 4, 16, 8) };
            _paginas.Add(p);
            _cuerpo.Controls.Add(p);
            return p;
        }

        /// <summary>Agrega un botón propio de la pantalla a la barra, después de Siguiente.</summary>
        protected Button AgregarBotonBarra(int ancho, EventHandler click, bool alPrincipio = false)
        {
            Button b = NuevoBoton(ancho);
            b.Click += click;
            _botones.Controls.Add(b);
            if (alPrincipio) _botones.Controls.SetChildIndex(b, 0);
            return b;
        }

        /// <summary>El paso tiene lo necesario para pasar al siguiente.</summary>
        protected abstract bool PasoCompleto(int paso);

        /// <summary>Se llama cada vez que se muestra un paso, para cargar o refrescar su página.</summary>
        protected virtual void AlMostrarPaso(int paso) { }

        /// <summary>Qué hace "Siguiente". Por defecto avanza si el paso está completo.</summary>
        protected virtual void AlSiguiente()
        {
            if (PasoCompleto(PasoActual)) IrA(PasoActual + 1);
        }

        /// <summary>El botón Siguiente se ve en este paso (el último suele tener su propio botón).</summary>
        protected virtual bool MuestraSiguiente(int paso) => paso < CantidadPasos - 1;

        /// <summary>Visibilidad y estilo de los botones propios; se llama con cada cambio de estado.</summary>
        protected virtual void ActualizarBotonesPropios() { }

        protected void IrA(int paso)
        {
            if (CantidadPasos == 0) return;
            paso = Math.Max(0, Math.Min(CantidadPasos - 1, paso));
            // No se puede saltar a un paso si alguno anterior está incompleto.
            for (int i = 0; i < paso; i++)
                if (!PasoCompleto(i)) { paso = i; break; }

            PasoActual = paso;
            for (int i = 0; i < _paginas.Count; i++) _paginas[i].Visible = i == paso;
            _paginas[paso].BringToFront();
            AlMostrarPaso(paso);
            AplicarIdioma();
            ActualizarBarra();
        }

        /// <summary>Recalcula la cabecera de pasos y los botones de la barra.</summary>
        protected void ActualizarBarra()
        {
            Pasos.Actual = PasoActual;
            BtnAtras.Visible = PasoActual > 0;
            BtnSiguiente.Visible = MuestraSiguiente(PasoActual);
            Habilitar(BtnSiguiente, PasoCompleto(PasoActual), true);
            Tema.AplicarBotonSecundario(BtnAtras);
            ActualizarBotonesPropios();
        }

        /// <summary>Habilitado: primario o secundario según importancia; deshabilitado: gris.</summary>
        protected static void Habilitar(Button b, bool habilitado, bool primario)
        {
            b.Enabled = habilitado;
            if (!habilitado) Tema.AplicarBotonDeshabilitado(b);
            else if (primario) Tema.AplicarBotonPrimario(b);
            else Tema.AplicarBotonSecundario(b);
        }

        protected static Button NuevoBoton(int ancho) => new Button
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly, MinimumSize = new Size(ancho, 34),
            Margin = new Padding(8, 0, 0, 0), Padding = new Padding(10, 0, 10, 0),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        /// <summary>Título de una sección de la página.</summary>
        protected static Label Rotulo(string texto = "") => new Label
        {
            Text = texto, AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 12, 0, 6)
        };

        /// <summary>
        /// Apila los controles de arriba hacia abajo, cada uno con el ancho de la página, en un
        /// panel con scroll vertical. Las filas de tarjetas (<see cref="Fila"/>) pasan a la línea
        /// siguiente cuando no entran, así que nunca aparece scroll horizontal.
        /// </summary>
        protected static Panel Apilar(params Control[] controles)
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            // Con Dock = Top el último agregado queda arriba: se agregan al revés.
            for (int i = controles.Length - 1; i >= 0; i--)
            {
                controles[i].Dock = DockStyle.Top;
                p.Controls.Add(controles[i]);
            }
            return p;
        }

        /// <summary>
        /// Igual que <see cref="Apilar"/> pero sin scroll y con el alto justo de su contenido:
        /// va arriba de una grilla que ocupa el resto de la página.
        /// </summary>
        protected static Pila06AV Pila(params Control[] controles)
        {
            var p = new Pila06AV { Dock = DockStyle.Top };
            for (int i = controles.Length - 1; i >= 0; i--)
            {
                controles[i].Dock = DockStyle.Top;
                p.Controls.Add(controles[i]);
            }
            return p;
        }

        /// <summary>Fila que pasa a la línea siguiente cuando no entra; crece en alto con su contenido.</summary>
        protected static FlowLayoutPanel Fila(params Control[] controles)
        {
            var f = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, WrapContents = true,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0)
            };
            f.Controls.AddRange(controles);
            return f;
        }

        public abstract void AplicarIdioma();

        /// <summary>Colores y fuentes comunes; las pantallas agregan los suyos después.</summary>
        public virtual void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;
            foreach (Control c in Controls) PintarFondo(c, Tema.FondoApp);
            PintarFondo(Lateral, Tema.FondoPanel);

            Tema.AplicarTitulo(LblTitulo);
            LblSubtitulo.Font = Tema.FuenteRegular;
            LblSubtitulo.ForeColor = Tema.TextoSuave;
            LblAyuda.Font = Tema.FuenteRegular;
            LblAyuda.ForeColor = Tema.TextoSuave;
            ActualizarBarra();
            Pasos.Invalidate();
        }

        /// <summary>
        /// Fondo de un contenedor y de sus hijos que no son entradas ni botones: el tema
        /// base pinta los paneles de blanco y acá el fondo general es gris.
        /// </summary>
        protected static void PintarFondo(Control raiz, Color color)
        {
            raiz.BackColor = color;
            foreach (Control c in raiz.Controls)
            {
                if (c is Button || c is TextBox || c is ComboBox || c is DateTimePicker || c is NumericUpDown ||
                    c is DataGridView || c is FichaDatos06AV || c is TarjetaOpcion06AV || c.Tag as string == "propio")
                    continue;
                PintarFondo(c, color);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Lateral != null) Lateral.Width = Width < 1050 ? 290 : 340;
            if (LblAyuda != null) LblAyuda.MaximumSize = new Size(Math.Max(200, Width - Lateral.Width - 40), 0);
        }
    }

    /// <summary>
    /// Panel que apila sus hijos (Dock = Top) y toma el alto justo de la suma. Con AutoSize
    /// de WinForms un panel no mide bien a sus hijos acoplados; así, una fila de tarjetas que
    /// pasa a dos líneas empuja hacia abajo lo que tiene debajo.
    /// </summary>
    internal class Pila06AV : Panel
    {
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int alto = Padding.Vertical;
            foreach (Control c in Controls)
                if (c.Visible) alto += c.Height;
            if (Height != alto) Height = alto;
        }
    }
}
