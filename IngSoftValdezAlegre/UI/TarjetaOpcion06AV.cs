using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Tarjeta de opción excluyente: reemplaza al ComboBox cuando las alternativas son
    /// pocas y la elección importa.
    ///
    /// Un combo esconde las opciones detrás de un clic y muestra una sola a la vez; para
    /// elegir una línea de ensamblaje el encargado necesita ver TODAS con su estado
    /// (libre / ocupada) al mismo tiempo. Acá cada opción es una tarjeta con su marca de
    /// selección, y las no disponibles se muestran apagadas en vez de desaparecer.
    /// </summary>
    internal class TarjetaOpcion06AV : Control
    {
        private bool _hot;
        private bool _seleccionada;

        public TarjetaOpcion06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 66;
            MinimumSize = new Size(200, 66);
            Margin = new Padding(0, 0, 10, 10);
        }

        /// <summary>Objeto de negocio que representa esta opción.</summary>
        public object Valor { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;

        /// <summary>Chip corto a la derecha: "Disponible", "Ocupada"…</summary>
        public string Etiqueta { get; set; }

        public IconoPcf06AV Icono { get; set; } = IconoPcf06AV.Destornillador;

        public bool Habilitada { get; set; } = true;

        public bool Seleccionada
        {
            get { return _seleccionada; }
            set { _seleccionada = value; Invalidate(); }
        }

        public event EventHandler Elegida;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hot = true;
            Cursor = Habilitada ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Habilitada) return;
            Focus();
            Elegida?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) && Habilitada)
            {
                Elegida?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(2, 1, Width - 5, Height - 4);
            if ((_hot && Habilitada) || Seleccionada)
                Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, 18);

            Color fondo = !Habilitada ? (Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50)
                        : Seleccionada ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.10f)
                        : _hot ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.04f)
                        : Tema.FondoPanel;
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                              Seleccionada ? Tema.Primario : (Focused && Habilitada ? Tema.Primario : Tema.Borde),
                              Seleccionada ? 2f : 1f);

            // Radio / check
            var marca = new Rectangle(14, 22, 22, 22);
            if (Seleccionada)
            {
                using (var b = new SolidBrush(Tema.Primario)) g.FillEllipse(b, marca);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                   new RectangleF(marca.X + 3, marca.Y + 3, 16, 16), Color.White, 2.2f);
            }
            else
            {
                using (var p = new Pen(Habilitada ? (Tema.EsOscuro ? Tema.Acero500 : Tema.Acero300) : Tema.Acero300, 1.8f))
                    g.DrawEllipse(p, marca);
                Iconos06AV.Dibujar(g, Icono, new RectangleF(marca.X + 3, marca.Y + 3, 16, 16),
                                   Habilitada ? Tema.TextoSuave : Tema.Acero300, 1.6f);
            }

            int x = 48;
            int anchoEtiqueta = string.IsNullOrEmpty(Etiqueta) ? 0 : 96;
            int ancho = Math.Max(30, caja.Right - x - anchoEtiqueta - 14);

            Pintura06AV.TextoIzquierda(g, Titulo, Tema.FuenteBold,
                                       Habilitada ? Tema.TextoFuerte : Tema.TextoSuave,
                                       new Rectangle(x, 14, ancho, 20));
            if (!string.IsNullOrWhiteSpace(Subtitulo))
                Pintura06AV.TextoIzquierda(g, Subtitulo, Tema.FuenteRegular, Tema.TextoSuave,
                                           new Rectangle(x, 34, ancho, 18));

            if (!string.IsNullOrEmpty(Etiqueta))
            {
                Color c = Habilitada ? Tema.Exito : Tema.TextoSuave;
                int w = Math.Min(Pintura06AV.AnchoChip(g, Etiqueta, Tema.FuenteMini, 20), anchoEtiqueta);
                var chip = new Rectangle(caja.Right - 14 - w, 23, w, 20);
                Pintura06AV.Chip(g, chip, Etiqueta, Tema.FuenteMini, Pintura06AV.Suave(c, 32), c);
            }
        }
    }
}
