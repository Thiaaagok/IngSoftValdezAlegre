using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Selector de cantidad dibujado a mano: [ − ][ 17 ][ + ].
    ///
    /// Reemplaza al NumericUpDown nativo, que rompe la estética de la tarjeta
    /// (borde cuadrado gris, flechitas de 8 px imposibles de acertar con el mouse
    /// y sin forma de teñirlo con el tema).
    ///
    /// Conserva todo lo que hacía el control nativo:
    ///   · clic en − / + (y auto-repetición si se mantiene apretado),
    ///   · rueda del mouse sobre el control,
    ///   · flechas ↑ ↓ y Re Pág / Av Pág (de a 10) con el foco puesto,
    ///   · tipeo directo de dígitos y Backspace para corregir.
    /// </summary>
    internal class SelectorCantidad06AV : Control
    {
        private int _valor = 1;
        private int _minimo = 1;
        private int _maximo = 9999;
        private int _hot;              // 0 = ninguno, -1 = menos, 1 = mas
        private int _presionado;
        private readonly Timer _repeticion;
        private int _pasoRepeticion;

        private const int AnchoBoton = 30;

        public SelectorCantidad06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(104, 30);

            _repeticion = new Timer { Interval = 380 };
            _repeticion.Tick += (s, e) =>
            {
                _repeticion.Interval = 70;      // arranca lento y acelera
                Valor += _pasoRepeticion;
            };
        }

        public event EventHandler ValorCambiado;

        public int Minimo
        {
            get { return _minimo; }
            set { _minimo = value; Valor = _valor; }
        }

        public int Maximo
        {
            get { return _maximo; }
            set { _maximo = value; Valor = _valor; }
        }

        public int Valor
        {
            get { return _valor; }
            set
            {
                int nuevo = Math.Max(_minimo, Math.Min(_maximo, value));
                if (nuevo == _valor) return;
                _valor = nuevo;
                Invalidate();
                ValorCambiado?.Invoke(this, EventArgs.Empty);
            }
        }

        private Rectangle CajaMenos() => new Rectangle(0, 0, AnchoBoton, Height);
        private Rectangle CajaMas() => new Rectangle(Width - AnchoBoton, 0, AnchoBoton, Height);

        private int ZonaEn(Point p)
        {
            if (CajaMenos().Contains(p)) return -1;
            if (CajaMas().Contains(p)) return 1;
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _repeticion.Stop(); _repeticion.Dispose(); }
            base.Dispose(disposing);
        }

        #region Interacción

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int z = ZonaEn(e.Location);
            if (z != _hot)
            {
                _hot = z;
                Cursor = z != 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hot = 0; _presionado = 0;
            _repeticion.Stop();
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            int z = ZonaEn(e.Location);
            if (z == 0) { Invalidate(); return; }

            _presionado = z;
            Valor += z;
            Invalidate();

            _pasoRepeticion = z;
            _repeticion.Interval = 380;
            _repeticion.Start();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _presionado = 0;
            _repeticion.Stop();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Valor += Math.Sign(e.Delta);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up: Valor++; e.Handled = true; break;
                case Keys.Down: Valor--; e.Handled = true; break;
                case Keys.PageUp: Valor += 10; e.Handled = true; break;
                case Keys.PageDown: Valor -= 10; e.Handled = true; break;
                case Keys.Home: Valor = _minimo; e.Handled = true; break;
                case Keys.End: Valor = _maximo; e.Handled = true; break;
                case Keys.Back:
                    Valor = _valor / 10;         // borra el último dígito
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (!char.IsDigit(e.KeyChar)) return;

            // Tipeo directo: se va componiendo el número dígito a dígito.
            long compuesto = (long)_valor * 10 + (e.KeyChar - '0');
            Valor = (int)Math.Min(_maximo, compuesto);
            e.Handled = true;
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoPanel))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(0, 0, Width - 1, Height - 1);
            Pintura06AV.Rellenar(g, caja, 7, Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50);
            Pintura06AV.Borde(g, caja, 7, Focused ? Tema.Primario : Tema.Borde, Focused ? 1.8f : 1f);

            DibujarBoton(g, CajaMenos(), -1, IconoPcf06AV.Ninguno);
            DibujarBoton(g, CajaMas(), 1, IconoPcf06AV.Mas);

            var rValor = new Rectangle(AnchoBoton, 0, Width - AnchoBoton * 2, Height);
            Pintura06AV.TextoCentrado(g, _valor.ToString(), Tema.FuenteBold, Tema.TextoFuerte, rValor);
        }

        private void DibujarBoton(Graphics g, Rectangle r, int signo, IconoPcf06AV icono)
        {
            bool habilitado = signo < 0 ? _valor > _minimo : _valor < _maximo;
            bool activo = _presionado == signo;
            bool caliente = _hot == signo;

            if (habilitado && (activo || caliente))
            {
                var fondo = activo ? Tema.Primario : Pintura06AV.Suave(Tema.Primario, 34);
                var interior = new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
                Pintura06AV.Rellenar(g, interior, 5, fondo);
            }

            Color tinta = !habilitado ? Tema.EsOscuro ? Tema.Acero700 : Tema.Acero300
                        : activo ? Color.White
                                 : Tema.Primario;

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            using (var p = new Pen(tinta, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round,
                                                EndCap = System.Drawing.Drawing2D.LineCap.Round })
            {
                g.DrawLine(p, cx - 5, cy, cx + 5, cy);
                if (signo > 0) g.DrawLine(p, cx, cy - 5, cx, cy + 5);
            }
        }
    }
}
