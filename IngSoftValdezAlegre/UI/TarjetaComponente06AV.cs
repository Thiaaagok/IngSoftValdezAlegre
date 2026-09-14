using BE;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Ficha de un componente en el catálogo de "Armá tu PC".
    ///
    /// Reemplaza a la fila de ListBox, que mostraba todo en una sola línea de texto
    /// separada por puntos y obligaba a leer para comparar. Acá cada dato tiene su
    /// lugar fijo: marca y modelo mandan, el precio va grande a la derecha, y la barra
    /// bajo el precio es proporcional a la opción más cara del paso, así el salto de
    /// precio entre alternativas se ve como longitud.
    ///
    /// El stock es parte de la decisión (no se puede vender lo que no hay), así que
    /// sin unidades libres la ficha se apaga y no se puede elegir.
    /// </summary>
    internal class TarjetaComponente06AV : Control
    {
        private bool _hot;
        private bool _seleccionada;

        public TarjetaComponente06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 78;
            MinimumSize = new Size(260, 78);
            Margin = new Padding(0, 0, 0, 8);
        }

        public Componente06AV Componente { get; set; }

        /// <summary>Precio de la opción más cara del paso (escala de la barra).</summary>
        public decimal PrecioTope { get; set; }

        public IconoPcf06AV Icono { get; set; } = IconoPcf06AV.Chip;

        public string TextoSinStock { get; set; } = "sin stock";
        public string TextoStock { get; set; } = "{0} libres";

        public bool Disponible =>
            Componente != null && Componente.StockLibre > 0;

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
            Cursor = Disponible ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Disponible) return;
            Focus();
            Elegida?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) && Disponible)
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

            if (Componente == null) return;
            Componente06AV c = Componente;
            bool hay = Disponible;

            var caja = new Rectangle(2, 1, Width - 5, Height - 5);
            if ((_hot && hay) || Seleccionada) Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, 20);

            Color fondo = !hay ? (Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50)
                        : Seleccionada ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.10f)
                        : _hot ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.04f)
                        : Tema.FondoPanel;
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                              Seleccionada ? Tema.Primario : (Focused && hay ? Tema.Primario : Tema.Borde),
                              Seleccionada ? 2f : 1f);

            // Marca de selección / ícono del tipo
            var marca = new Rectangle(14, 28, 22, 22);
            if (Seleccionada)
            {
                using (var b = new SolidBrush(Tema.Primario)) g.FillEllipse(b, marca);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                   new RectangleF(marca.X + 3, marca.Y + 3, 16, 16), Color.White, 2.2f);
            }
            else
            {
                Iconos06AV.Dibujar(g, Icono, new RectangleF(marca.X, marca.Y, 22, 22),
                                   hay ? Tema.TextoSuave : Tema.Acero300, 1.7f);
            }

            int x = 48;
            int anchoPrecio = 116;
            int ancho = Math.Max(40, caja.Right - x - anchoPrecio - 14);

            Color tinta = hay ? Tema.TextoFuerte : Tema.TextoSuave;
            string titulo = string.IsNullOrWhiteSpace(c.Marca) && string.IsNullOrWhiteSpace(c.Modelo)
                ? c.Descripcion
                : (c.Marca + " " + c.Modelo).Trim();

            Pintura06AV.TextoIzquierda(g, titulo, Tema.FuenteBold, tinta, new Rectangle(x, 14, ancho, 20));
            Pintura06AV.TextoIzquierda(g, c.Descripcion, Tema.FuenteRegular,
                                       hay ? Tema.TextoSuave : Tema.Acero300,
                                       new Rectangle(x, 34, ancho, 18));

            // Stock
            string stock = hay ? string.Format(TextoStock, c.StockLibre) : TextoSinStock;
            Color cStock = !hay ? Tema.Peligro : (c.StockLibre <= 2 ? Tema.Advertencia : Tema.Exito);
            int anchoChip = Pintura06AV.AnchoChip(g, stock, Tema.FuenteMini, 18);
            var chip = new Rectangle(x, 54, Math.Min(anchoChip, ancho), 18);
            Pintura06AV.Chip(g, chip, stock, Tema.FuenteMini, Pintura06AV.Suave(cStock, 32), cStock);

            // Precio + barra comparativa
            var rPrecio = new Rectangle(caja.Right - anchoPrecio - 12, 16, anchoPrecio, 26);
            using (var fuente = new Font("Segoe UI Semibold", 14f, FontStyle.Bold))
                Pintura06AV.TextoDerecha(g, c.PrecioUnitario.ToString("C0"), fuente, tinta, rPrecio);

            var pista = new Rectangle(caja.Right - anchoPrecio - 12, 48, anchoPrecio, 6);
            Pintura06AV.Rellenar(g, pista, 3, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero200);
            if (PrecioTope > 0)
            {
                int w = (int)(pista.Width * (double)(c.PrecioUnitario / PrecioTope));
                Pintura06AV.Rellenar(g, new Rectangle(pista.X, pista.Y, Math.Max(4, w), pista.Height), 3,
                                     hay ? (Seleccionada ? Tema.Primario : Tema.Acento) : Tema.Acero500);
            }
        }
    }
}
