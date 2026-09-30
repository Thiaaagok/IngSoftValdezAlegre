using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    internal class MedidorReposicion06AV : Control
    {
        private int _stock;
        private int _minimo = 1;

        public MedidorReposicion06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            Height = 34;
            BackColor = Color.Transparent;
        }

        public int Stock
        {
            get { return _stock; }
            set { _stock = value; Invalidate(); }
        }

        public int StockMinimo
        {
            get { return _minimo; }
            set { _minimo = Math.Max(1, value); Invalidate(); }
        }

        public string TextoDerecha { get; set; }

        public int Nivel
        {
            get
            {
                float r = _stock / (float)_minimo;
                if (r < 0.5f) return 0;
                if (r < 1f) return 1;
                return 2;
            }
        }

        public Color ColorNivel()
        {
            switch (Nivel)
            {
                case 0: return Tema.Peligro;
                case 1: return Tema.Advertencia;
                default: return Tema.Exito;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);

            int tope = Math.Max(_minimo * 2, Math.Max(_stock, 1));
            var pista = new Rectangle(0, 12, Math.Max(10, Width - 1), 10);

            Pintura06AV.Rellenar(g, pista, 5, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero200);

            int xMin = pista.X + (int)(pista.Width * (_minimo / (float)tope));
            var zona = new Rectangle(pista.X, pista.Y, Math.Max(1, xMin - pista.X), pista.Height);
            Pintura06AV.Rellenar(g, zona, 5, Pintura06AV.Suave(Tema.Peligro, Tema.EsOscuro ? 46 : 26));

            Color color = ColorNivel();
            int ancho = (int)(pista.Width * (Math.Min(_stock, tope) / (float)tope));
            if (ancho > 0)
                Pintura06AV.Rellenar(g, new Rectangle(pista.X, pista.Y, Math.Max(4, ancho), pista.Height), 5, color);

            using (var p = new Pen(Tema.EsOscuro ? Tema.Acero300 : Tema.Grafito900, 2f))
                g.DrawLine(p, xMin, pista.Y - 5, xMin, pista.Bottom + 5);

            Pintura06AV.TextoIzquierda(g, _stock + " u.", Tema.FuenteBold, color,
                                       new Rectangle(0, 0, Width / 2, 12));

            string derecha = string.IsNullOrEmpty(TextoDerecha) ? ("mín. " + _minimo) : TextoDerecha;
            Pintura06AV.TextoDerecha(g, derecha, Tema.FuenteMini, Tema.TextoSuave,
                                     new Rectangle(Width / 2, 0, Width / 2 - 1, 12));
        }
    }
}
