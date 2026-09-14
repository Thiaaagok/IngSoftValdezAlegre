using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Ítem de checklist de control de calidad.
    ///
    /// El CheckBox nativo es una casilla de 13 px con una etiqueta al lado: en una
    /// pantalla de taller, con el equipo en la mano, es un blanco chico y su estado
    /// se lee mal de reojo. Acá cada verificación es una fila alta y clickeable entera,
    /// que cambia de color al marcarse — verificado en verde, pendiente en gris — y
    /// aguanta el espacio para explicar QUÉ hay que verificar, no sólo nombrarlo.
    /// </summary>
    internal class ItemChequeo06AV : Control
    {
        private bool _marcado;
        private bool _hot;

        public ItemChequeo06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 58;
            MinimumSize = new Size(260, 58);
            Margin = new Padding(0, 0, 0, 8);
            Cursor = Cursors.Hand;
        }

        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;

        public bool Marcado
        {
            get { return _marcado; }
            set
            {
                if (_marcado == value) return;
                _marcado = value;
                Invalidate();
                MarcadoCambiado?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler MarcadoCambiado;

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            Marcado = !Marcado;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Marcado = !Marcado;
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoPanel))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(2, 1, Width - 5, Height - 4);
            Color acento = Marcado ? Tema.Exito : Tema.Acero500;

            Color fondo = Marcado ? Pintura06AV.Suave(Tema.Exito, Tema.EsOscuro ? 38 : 22)
                        : _hot ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.05f)
                        : (Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50);
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                              Marcado ? Tema.Exito : (Focused ? Tema.Primario : Tema.Borde),
                              Marcado || Focused ? 1.8f : 1f);

            var marca = new Rectangle(16, 18, 24, 24);
            if (Marcado)
            {
                using (var b = new SolidBrush(Tema.Exito)) g.FillEllipse(b, marca);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                   new RectangleF(marca.X + 4, marca.Y + 4, 16, 16), Color.White, 2.4f);
            }
            else
            {
                using (var p = new Pen(Tema.EsOscuro ? Tema.Acero500 : Tema.Acero300, 2f))
                    g.DrawEllipse(p, marca);
            }

            int x = 52;
            int ancho = Math.Max(30, caja.Right - x - 14);
            Pintura06AV.TextoIzquierda(g, Titulo, Tema.FuenteBold,
                                       Marcado ? Tema.TextoFuerte : Tema.Texto,
                                       new Rectangle(x, string.IsNullOrWhiteSpace(Detalle) ? 18 : 12, ancho, 20));
            if (!string.IsNullOrWhiteSpace(Detalle))
                Pintura06AV.TextoIzquierda(g, Detalle, Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(x, 32, ancho, 16));
        }
    }
}
