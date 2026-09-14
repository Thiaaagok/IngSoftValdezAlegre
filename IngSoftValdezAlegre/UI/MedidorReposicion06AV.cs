using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// MEDIDOR DE REPOSICIÓN — barra de cobertura de stock de un componente.
    ///
    /// La tabla de checkboxes mostraba "Stock 4 / Mínimo 10" como dos números sueltos:
    /// el operario tenía que restar mentalmente, fila por fila, para saber qué es urgente.
    /// Acá el punto de reposición es una MARCA FIJA sobre la barra y el stock es el
    /// llenado: la distancia hasta la marca ES la urgencia, y el color la confirma
    /// (rojo por debajo de la mitad del mínimo, ámbar por debajo del mínimo, verde arriba).
    ///
    /// La escala llega hasta 2× el mínimo, que es el objetivo de reposición sugerido,
    /// así todas las barras de la pantalla son comparables entre sí.
    /// </summary>
    internal class MedidorReposicion06AV : Control
    {
        private int _stock;
        private int _minimo = 1;

        public MedidorReposicion06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            Height = 34;
            // Transparente: vive dentro de una tarjeta pintada a mano y debe dejarla ver.
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

        /// <summary>Texto opcional a la derecha (p. ej. "faltan 6").</summary>
        public string TextoDerecha { get; set; }

        /// <summary>0 = crítico (menos de la mitad del mínimo), 1 = bajo, 2 = cubierto.</summary>
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

            // Zona de riesgo: todo lo que está por debajo del punto de reposición.
            int xMin = pista.X + (int)(pista.Width * (_minimo / (float)tope));
            var zona = new Rectangle(pista.X, pista.Y, Math.Max(1, xMin - pista.X), pista.Height);
            Pintura06AV.Rellenar(g, zona, 5, Pintura06AV.Suave(Tema.Peligro, Tema.EsOscuro ? 46 : 26));

            // Llenado real
            Color color = ColorNivel();
            int ancho = (int)(pista.Width * (Math.Min(_stock, tope) / (float)tope));
            if (ancho > 0)
                Pintura06AV.Rellenar(g, new Rectangle(pista.X, pista.Y, Math.Max(4, ancho), pista.Height), 5, color);

            // Marca del punto de reposición
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
