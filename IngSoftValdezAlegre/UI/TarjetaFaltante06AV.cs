using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    internal class FaltanteItem06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }

        /// <summary>Ya está en una orden de compra en curso: no se puede volver a pedir (RFN2).</summary>
        public bool Bloqueado { get; set; }

        public int OrdenBloqueo { get; set; }

        public int CantidadSugerida
        {
            get
            {
                int objetivo = Math.Max(1, StockMinimo) * 2;
                return Math.Max(1, objetivo - Stock);
            }
        }
    }

    internal class TarjetaFaltante06AV : Control
    {
        private readonly MedidorReposicion06AV _medidor;
        private readonly SelectorCantidad06AV _cantidad;
        private readonly Label _lblReponer;

        private const int AnchoBloqueCantidad = 132;
        private bool _incluido;
        private bool _hot;

        public TarjetaFaltante06AV(FaltanteItem06AV item, string textoReponer, string textoBloqueado)
        {
            Item = item ?? new FaltanteItem06AV();
            TextoBloqueado = textoBloqueado;

            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 96;
            MinimumSize = new Size(300, 96);
            Margin = new Padding(6, 5, 6, 5);
            Cursor = Item.Bloqueado ? Cursors.Default : Cursors.Hand;

            _medidor = new MedidorReposicion06AV
            {
                Stock = Item.Stock,
                StockMinimo = Item.StockMinimo,
                TextoDerecha = "mín. " + Item.StockMinimo,
                Left = 44,
                Top = 48,
                Height = 34
            };

            _lblReponer = new Label
            {
                Text = textoReponer,
                AutoSize = true,
                Top = 40,
                BackColor = Color.Transparent
            };

            _cantidad = new SelectorCantidad06AV
            {
                Minimo = 1,
                Maximo = 9999,
                Valor = Math.Min(9999, Math.Max(1, Item.CantidadSugerida)),
                Size = new Size(AnchoBloqueCantidad - 16, 30),
                Top = 58,
                Visible = !Item.Bloqueado
            };
            _cantidad.ValorCambiado += (s, e) => CantidadCambiada?.Invoke(this, EventArgs.Empty);

            Controls.Add(_medidor);
            Controls.Add(_lblReponer);
            Controls.Add(_cantidad);

            if (Item.Bloqueado) { _lblReponer.Visible = false; }
            AplicarTema();
        }

        public FaltanteItem06AV Item { get; }
        public string TextoBloqueado { get; set; }

        public event EventHandler IncluidoCambiado;
        public event EventHandler CantidadCambiada;

        public bool Incluido
        {
            get { return _incluido; }
            set
            {
                if (Item.Bloqueado) value = false;
                if (_incluido == value) return;
                _incluido = value;
                Invalidate();
                IncluidoCambiado?.Invoke(this, EventArgs.Empty);
            }
        }

        public int Cantidad
        {
            get { return _cantidad.Valor; }
            set { _cantidad.Valor = value; }
        }

        public int Nivel => _medidor.Nivel;

        public void AplicarTema()
        {
            _lblReponer.Font = Tema.FuenteMini;
            _lblReponer.ForeColor = Tema.TextoSuave;
            _lblReponer.BackColor = Color.Transparent;
            _cantidad.Invalidate();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_medidor == null || _cantidad == null || _lblReponer == null) return;

            int xBloque = Math.Max(140, Width - AnchoBloqueCantidad);
            _lblReponer.Left = xBloque;
            _cantidad.Left = xBloque;
            _medidor.Width = Math.Max(60, xBloque - 44 - 14);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Item.Bloqueado) return;
            Focus();
            Incluido = !Incluido;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space && !Item.Bloqueado)
            {
                Incluido = !Incluido;
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

            var caja = new Rectangle(2, 2, Width - 5, Height - 5);
            Color acento = _medidor.ColorNivel();

            if (Item.Bloqueado)
            {
                Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta,
                                     Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50);
                Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta, Tema.Borde);
            }
            else
            {
                if (_hot || Incluido || Focused)
                    Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, 20);

                Color fondo = Incluido ? Pintura06AV.Mezclar(Tema.FondoPanel, Tema.Primario, 0.09f)
                                       : Tema.FondoPanel;
                Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
                Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                                  Incluido ? Tema.Primario : (Focused ? Tema.Primario : Tema.Borde),
                                  Incluido || Focused ? 1.8f : 1f);
            }

            var sel = new Rectangle(14, 16, 20, 20);
            if (Item.Bloqueado)
            {
                using (var p = new Pen(Tema.TextoSuave, 1.6f)) g.DrawEllipse(p, sel);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Alerta,
                                   new RectangleF(sel.X + 3, sel.Y + 3, 14, 14), Tema.TextoSuave, 1.6f);
            }
            else if (Incluido)
            {
                using (var b = new SolidBrush(Tema.Primario)) g.FillEllipse(b, sel);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                   new RectangleF(sel.X + 3, sel.Y + 3, 14, 14), Color.White, 2.2f);
            }
            else
            {
                using (var p = new Pen(Tema.EsOscuro ? Tema.Acero500 : Tema.Acero300, 1.8f))
                    g.DrawEllipse(p, sel);
            }

            string textoChip = Item.Bloqueado
                ? (TextoBloqueado ?? "en OC") + " #" + Item.OrdenBloqueo
                : (Item.StockMinimo - Item.Stock > 0
                       ? "-" + (Item.StockMinimo - Item.Stock)
                       : "ok");
            int anchoChipReserva = Math.Min(Pintura06AV.AnchoChip(g, textoChip, Tema.FuenteMini, 20), 120);

            Color tinta = Item.Bloqueado ? Tema.TextoSuave : Tema.TextoFuerte;
            Pintura06AV.TextoIzquierda(g, Item.Codigo, Tema.FuenteBold, tinta,
                                       new Rectangle(44, 12, 90, 18));
            Pintura06AV.TextoIzquierda(g, Item.Descripcion, Tema.FuenteRegular,
                                       Item.Bloqueado ? Tema.TextoSuave : Tema.Texto,
                                       new Rectangle(136, 12,
                                                     Math.Max(10, Width - AnchoBloqueCantidad - anchoChipReserva - 150),
                                                     18));

            string texto = textoChip;
            Color cChip = Item.Bloqueado ? Tema.TextoSuave : acento;
            int anchoChip = anchoChipReserva;
            var chip = new Rectangle(Math.Max(150, Width - AnchoBloqueCantidad - anchoChip - 10), 12,
                                     anchoChip, 20);
            Pintura06AV.Chip(g, chip, texto, Tema.FuenteMini, Pintura06AV.Suave(cChip, 34), cChip);
        }
    }
}
