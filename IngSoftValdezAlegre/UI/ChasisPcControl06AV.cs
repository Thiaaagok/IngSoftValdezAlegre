using BE;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Una bahía del chasis: un tipo de componente y lo que hay puesto en él.</summary>
    internal class BahiaPc06AV
    {
        public TipoComponente06AV Tipo { get; set; }
        public string Nombre { get; set; }
        public IconoPcf06AV Icono { get; set; }
        public bool Opcional { get; set; }
        public Componente06AV Puesto { get; set; }
    }

    /// <summary>
    /// CHASIS — el panel "Tu PC" del asistente de armado.
    ///
    /// Antes era un ListBox vacío: arrancabas sin saber cuántas piezas faltaban ni
    /// cuáles. Acá el equipo se presenta como una lista de BAHÍAS fijas, una por tipo
    /// de componente, visibles desde el primer segundo:
    ///   · la bahía vacía se dibuja con borde punteado — se ve el hueco;
    ///   · la que se está eligiendo en este momento queda resaltada;
    ///   · al poner una pieza la bahía se llena, muestra el modelo y su precio;
    ///   · las opcionales se marcan como tales, así nadie cree que le falta algo.
    ///
    /// El total vive abajo, fijo, y acompaña cada elección.
    /// </summary>
    internal class ChasisPcControl06AV : Control
    {
        private readonly List<BahiaPc06AV> _bahias = new List<BahiaPc06AV>();
        private int _actual = -1;
        private int _hot = -1;

        private const int AltoBahia = 46;
        private const int AltoTotal = 56;

        public ChasisPcControl06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = false;
        }

        public string TextoTotal { get; set; } = "Total";
        public string TextoOpcional { get; set; } = "opcional";
        public string TextoVacio { get; set; } = "sin elegir";
        public string TextoPiezas { get; set; } = "{0} de {1} piezas";

        /// <summary>Se dispara al hacer clic en una bahía ya recorrida (para volver a ella).</summary>
        public event EventHandler<int> BahiaElegida;

        public void DefinirBahias(IEnumerable<BahiaPc06AV> bahias)
        {
            _bahias.Clear();
            if (bahias != null) _bahias.AddRange(bahias);
            Invalidate();
        }

        public void Poner(TipoComponente06AV tipo, Componente06AV componente)
        {
            BahiaPc06AV b = _bahias.FirstOrDefault(x => x.Tipo == tipo);
            if (b != null) { b.Puesto = componente; Invalidate(); }
        }

        public int Actual
        {
            get { return _actual; }
            set { _actual = value; Invalidate(); }
        }

        public decimal Total => _bahias.Where(b => b.Puesto != null).Sum(b => b.Puesto.PrecioUnitario);

        private Rectangle CajaBahia(int i) =>
            new Rectangle(0, 4 + i * AltoBahia, Math.Max(10, Width - 1), AltoBahia - 6);

        private int BahiaEn(Point p)
        {
            for (int i = 0; i < _bahias.Count; i++)
                if (CajaBahia(i).Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = BahiaEn(e.Location);
            if (i != _hot)
            {
                _hot = i;
                Cursor = i >= 0 ? Cursors.Hand : Cursors.Default;
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
            int i = BahiaEn(e.Location);
            if (i >= 0) BahiaElegida?.Invoke(this, i);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoPanel))
                g.FillRectangle(b, ClientRectangle);

            for (int i = 0; i < _bahias.Count; i++) DibujarBahia(g, i);

            // ── Total ────────────────────────────────────────────
            int y = 4 + _bahias.Count * AltoBahia + 6;
            var linea = new Rectangle(0, y, Math.Max(10, Width - 1), 1);
            using (var b = new SolidBrush(Tema.Borde)) g.FillRectangle(b, linea);

            int puestas = _bahias.Count(x => x.Puesto != null);
            var rTotal = new Rectangle(0, y + 10, Math.Max(10, Width - 1), 30);
            Pintura06AV.TextoIzquierda(g, TextoTotal, Tema.FuenteRegular, Tema.TextoSuave,
                                       new Rectangle(rTotal.X, rTotal.Y, rTotal.Width / 2, rTotal.Height));
            using (var fuente = new Font("Segoe UI Semibold", 17f, FontStyle.Bold))
                Pintura06AV.TextoDerecha(g, Total.ToString("C0"), fuente, Tema.TextoFuerte, rTotal);

            Pintura06AV.TextoIzquierda(g, string.Format(TextoPiezas, puestas, _bahias.Count),
                                       Tema.FuenteMini, Tema.TextoSuave,
                                       new Rectangle(0, rTotal.Bottom, Math.Max(10, Width - 1), 16));
        }

        private void DibujarBahia(Graphics g, int i)
        {
            BahiaPc06AV b = _bahias[i];
            Rectangle caja = CajaBahia(i);
            bool llena = b.Puesto != null;
            bool actual = i == _actual;
            bool caliente = i == _hot;

            if (llena)
            {
                Pintura06AV.Rellenar(g, caja, 8,
                    actual ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.10f)
                           : (Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50));
                Pintura06AV.Borde(g, caja, 8, actual ? Tema.Primario : Tema.Borde, actual ? 1.8f : 1f);
            }
            else
            {
                // Hueco: borde punteado, se ve que falta algo.
                var rr = new Rectangle(caja.X, caja.Y, caja.Width - 1, caja.Height - 1);
                using (var path = Pintura06AV.Redondeado(rr, 8))
                using (var p = new Pen(actual ? Tema.Primario : (Tema.EsOscuro ? Tema.Acero700 : Tema.Acero300),
                                       actual ? 1.8f : 1.2f)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    g.DrawPath(p, path);

                if (actual)
                    Pintura06AV.Rellenar(g, caja, 8, Pintura06AV.Suave(Tema.Primario, 14));
            }

            if (caliente && !actual)
                Pintura06AV.Borde(g, caja, 8, Tema.Primario, 1.4f);

            Color tintaIcono = llena ? (actual ? Tema.Primario : Tema.Exito)
                                     : (actual ? Tema.Primario : Tema.Acero300);
            Iconos06AV.Dibujar(g, llena ? IconoPcf06AV.Check : b.Icono,
                               new RectangleF(caja.X + 10, caja.Y + 12, 18, 18), tintaIcono, 1.8f);

            int x = caja.X + 36;
            int anchoPrecio = 74;
            int ancho = Math.Max(30, caja.Right - x - anchoPrecio - 8);

            string nombre = b.Nombre + (b.Opcional && !llena ? "  (" + TextoOpcional + ")" : "");
            Pintura06AV.TextoIzquierda(g, nombre, Tema.FuenteMini,
                                       llena || actual ? Tema.TextoSuave : Tema.Acero500,
                                       new Rectangle(x, caja.Y + 5, ancho, 14));

            string detalle = llena
                ? (string.IsNullOrWhiteSpace(b.Puesto.Marca) && string.IsNullOrWhiteSpace(b.Puesto.Modelo)
                       ? b.Puesto.Descripcion
                       : (b.Puesto.Marca + " " + b.Puesto.Modelo).Trim())
                : TextoVacio;

            Pintura06AV.TextoIzquierda(g, detalle,
                                       llena ? Tema.FuenteBold : Tema.FuenteRegular,
                                       llena ? Tema.TextoFuerte : Tema.Acero500,
                                       new Rectangle(x, caja.Y + 19, ancho, 18));

            if (llena)
                Pintura06AV.TextoDerecha(g, b.Puesto.PrecioUnitario.ToString("C0"), Tema.FuenteBold,
                                         Tema.Texto,
                                         new Rectangle(caja.Right - anchoPrecio - 10, caja.Y + 14, anchoPrecio, 20));
        }

        /// <summary>Alto que necesita el control para dibujar todas las bahías más el total.</summary>
        public int AltoNecesario => 4 + _bahias.Count * AltoBahia + AltoTotal + 16;
    }
}
