using BE;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Una línea del control de recepción: un componente pedido, y cuántas unidades
    /// de ese componente llegaron realmente.
    ///
    /// Funciona como un ítem de checklist, igual que el control de calidad de
    /// producción: se marca lo que llegó. La diferencia es que acá la respuesta no
    /// es sí/no sino CUÁNTAS, así que la fila trae su propio selector de cantidad,
    /// precargado con lo pedido (el caso normal es que llegue todo) y acotado a ese
    /// máximo, para que nadie infle el stock por un error de tipeo.
    ///
    /// El chip de la derecha dice en vivo si la línea está completa o cuánto falta.
    /// </summary>
    internal class FilaRecepcion06AV : Control
    {
        private readonly SelectorCantidad06AV _cantidad;
        private bool _llego = true;
        private bool _hot;

        private const int AnchoBloque = 132;

        public FilaRecepcion06AV(DetalleComponente06AV pendiente)
        {
            Pendiente = pendiente;

            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 74;
            MinimumSize = new Size(320, 74);
            Margin = new Padding(0, 0, 0, 8);
            Cursor = Cursors.Hand;

            _cantidad = new SelectorCantidad06AV
            {
                Minimo = 1,
                Maximo = Math.Max(1, pendiente.Cantidad),
                Valor = Math.Max(1, pendiente.Cantidad),
                Size = new Size(AnchoBloque - 16, 30),
                Top = 34
            };
            _cantidad.ValorCambiado += (s, e) => { Invalidate(); CambioRecepcion?.Invoke(this, EventArgs.Empty); };
            Controls.Add(_cantidad);
        }

        public DetalleComponente06AV Pendiente { get; }

        public string TextoCompleto { get; set; } = "completo";
        public string TextoFaltan { get; set; } = "faltan {0}";
        public string TextoNoLlego { get; set; } = "no llegó";
        public string TextoPedido { get; set; } = "pedido: {0}";
        public string TextoRecibido { get; set; } = "Recibido";

        public event EventHandler CambioRecepcion;

        /// <summary>False = este componente no vino en la entrega.</summary>
        public bool Llego
        {
            get { return _llego; }
            set
            {
                if (_llego == value) return;
                _llego = value;
                _cantidad.Visible = value;
                Invalidate();
                CambioRecepcion?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Unidades efectivamente recibidas (0 si no llegó).</summary>
        public int CantidadRecibida => _llego ? _cantidad.Valor : 0;

        public bool Completo => CantidadRecibida >= Pendiente.Cantidad;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_cantidad == null) return;
            _cantidad.Left = Math.Max(150, Width - AnchoBloque);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            Llego = !Llego;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) { Llego = !Llego; e.Handled = true; }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(2, 1, Width - 5, Height - 5);
            bool completo = _llego && Completo;
            Color acento = !_llego ? Tema.Peligro : (completo ? Tema.Exito : Tema.Advertencia);

            Color fondo = !_llego ? Pintura06AV.Suave(Tema.Peligro, Tema.EsOscuro ? 34 : 20)
                        : completo ? Pintura06AV.Suave(Tema.Exito, Tema.EsOscuro ? 34 : 20)
                        : Pintura06AV.Suave(Tema.Advertencia, Tema.EsOscuro ? 34 : 22);
            if (_hot) fondo = Pintura06AV.Mezclar(fondo, Tema.FondoPanel, 0.25f);

            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                              Focused ? Tema.Primario : acento, Focused ? 2f : 1.4f);

            // Marca de "llegó"
            var marca = new Rectangle(16, 24, 24, 24);
            if (_llego)
            {
                using (var b = new SolidBrush(acento)) g.FillEllipse(b, marca);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Check,
                                   new RectangleF(marca.X + 4, marca.Y + 4, 16, 16), Color.White, 2.4f);
            }
            else
            {
                using (var p = new Pen(Tema.Peligro, 2f)) g.DrawEllipse(p, marca);
                Iconos06AV.Dibujar(g, IconoPcf06AV.Alerta,
                                   new RectangleF(marca.X + 4, marca.Y + 4, 16, 16), Tema.Peligro, 1.8f);
            }

            int x = 52;
            int xBloque = Math.Max(150, Width - AnchoBloque);
            int ancho = Math.Max(40, xBloque - x - 90);

            Componente06AV c = Pendiente.Componente;
            Pintura06AV.TextoIzquierda(g, c != null ? c.Codigo : "-", Tema.FuenteBold, Tema.TextoFuerte,
                                       new Rectangle(x, 12, 90, 18));
            Pintura06AV.TextoIzquierda(g, c != null ? c.Descripcion : "", Tema.FuenteRegular, Tema.Texto,
                                       new Rectangle(x, 32, ancho + 90, 18));
            Pintura06AV.TextoIzquierda(g, string.Format(TextoPedido, Pendiente.Cantidad),
                                       Tema.FuenteMini, Tema.TextoSuave,
                                       new Rectangle(x, 52, 140, 16));

            // Estado de la línea
            string estado = !_llego ? TextoNoLlego
                          : completo ? TextoCompleto
                          : string.Format(TextoFaltan, Pendiente.Cantidad - CantidadRecibida);
            int anchoChip = Pintura06AV.AnchoChip(g, estado, Tema.FuenteMini, 20);
            var chip = new Rectangle(Math.Max(x + 100, xBloque - anchoChip - 12), 12, anchoChip, 20);
            Pintura06AV.Chip(g, chip, estado, Tema.FuenteMini, Pintura06AV.Suave(acento, 40), acento);

            if (_llego)
                Pintura06AV.TextoIzquierda(g, TextoRecibido, Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(xBloque, 16, AnchoBloque - 16, 16));
        }
    }
}
