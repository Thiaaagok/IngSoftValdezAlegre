using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Cabecera de pasos del asistente: galones encadenados que muestran dónde estoy,
    /// qué ya resolví y qué falta. Los pasos ya completados son clickeables para volver
    /// atrás sin perder lo cargado; los futuros no, porque todavía no tienen sentido.
    /// </summary>
    internal class PasosWizard06AV : Control
    {
        private readonly List<string> _pasos = new List<string>();
        private int _actual;
        private int _hot = -1;

        public PasosWizard06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Height = 54;
            // Sin BackColor transparente: es un Control puro (no lo admite) y pinta
            // su propio fondo en OnPaint con el color del contenedor.

        }

        public event EventHandler<int> PasoElegido;

        public void DefinirPasos(IEnumerable<string> pasos)
        {
            _pasos.Clear();
            if (pasos != null) _pasos.AddRange(pasos);
            Invalidate();
        }

        public int Actual
        {
            get { return _actual; }
            set { _actual = value; Invalidate(); }
        }

        private Rectangle CajaPaso(int i)
        {
            if (_pasos.Count == 0) return Rectangle.Empty;
            int ancho = (Width - 16) / _pasos.Count;
            return new Rectangle(8 + ancho * i, 8, ancho - 8, Height - 18);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int nuevo = -1;
            for (int i = 0; i < _pasos.Count; i++)
                if (i < _actual && CajaPaso(i).Contains(e.Location)) { nuevo = i; break; }
            if (nuevo != _hot)
            {
                _hot = nuevo;
                Cursor = nuevo >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hot != -1) { _hot = -1; Cursor = Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            for (int i = 0; i < _actual; i++)
                if (CajaPaso(i).Contains(e.Location)) { PasoElegido?.Invoke(this, i); return; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            for (int i = 0; i < _pasos.Count; i++)
            {
                Rectangle caja = CajaPaso(i);
                bool hecho = i < _actual;
                bool actual = i == _actual;

                Color fondo = actual ? Tema.Primario
                            : hecho ? Tema.PrimarioSuave
                            : (Tema.EsOscuro ? Tema.Grafito800 : Tema.Acero100);
                if (hecho && _hot == i) fondo = Pintura06AV.Mezclar(fondo, Tema.Primario, 0.2f);

                Pintura06AV.Rellenar(g, caja, 8, fondo);
                if (!actual) Pintura06AV.Borde(g, caja, 8, Tema.Borde);

                Color tinta = actual ? Color.White : (hecho ? Tema.Primario : Tema.TextoSuave);

                var num = new Rectangle(caja.X + 10, caja.Y + (caja.Height - 22) / 2, 22, 22);
                if (hecho)
                {
                    using (var p = new Pen(tinta, 1.6f)) g.DrawEllipse(p, num);
                    Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                       new RectangleF(num.X + 4, num.Y + 4, 14, 14), tinta, 2f);
                }
                else
                {
                    using (var p = new Pen(tinta, 1.6f)) g.DrawEllipse(p, num);
                    Pintura06AV.TextoCentrado(g, (i + 1).ToString(), Tema.FuenteMini, tinta, num);
                }

                var rTexto = new Rectangle(num.Right + 8, caja.Y, caja.Right - num.Right - 14, caja.Height);
                Pintura06AV.TextoIzquierda(g, _pasos[i], actual ? Tema.FuenteBold : Tema.FuenteRegular,
                                           tinta, rTexto);
            }
        }
    }
}
