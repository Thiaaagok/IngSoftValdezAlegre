using BE;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    internal class TarjetaCotizacion06AV : Control
    {
        private bool _hotAprobar, _hotDescartar;

        public TarjetaCotizacion06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Width = 268;
            Height = 232;
            MinimumSize = new Size(240, 232);
            Margin = new Padding(0, 0, 12, 12);
        }

        #region Datos

        public PedidoCotizacion06AV Cotizacion { get; set; }

        public decimal MejorCosto { get; set; }

        public decimal PeorCosto { get; set; }

        public int Unidades { get; set; }

        public bool EsMejor { get; set; }

        public bool PermiteResolver { get; set; }

        public string TextoMejor { get; set; } = "mejor oferta";
        public string TextoAprobar { get; set; } = "Adjudicar";
        public string TextoDescartar { get; set; } = "Descartar";
        public string TextoUnidad { get; set; } = "por unidad";
        public string TextoSinCondiciones { get; set; } = "Sin condiciones informadas";

        #endregion

        public event EventHandler Aprobar;
        public event EventHandler Descartar;

        private bool HayBotones =>
            PermiteResolver && Cotizacion != null &&
            Cotizacion.Estado == EstadoCotizacion06AV.PorAprobar;

        private Rectangle CajaAprobar() =>
            HayBotones ? new Rectangle(14, Height - 44, (Width - 40) / 2, 30) : Rectangle.Empty;

        private Rectangle CajaDescartar() =>
            HayBotones ? new Rectangle(14 + (Width - 40) / 2 + 12, Height - 44, (Width - 40) / 2, 30)
                       : Rectangle.Empty;

        #region Interacción

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool a = CajaAprobar().Contains(e.Location);
            bool d = CajaDescartar().Contains(e.Location);
            if (a != _hotAprobar || d != _hotDescartar)
            {
                _hotAprobar = a; _hotDescartar = d;
                Cursor = (a || d) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hotAprobar = _hotDescartar = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (CajaAprobar().Contains(e.Location)) Aprobar?.Invoke(this, EventArgs.Empty);
            else if (CajaDescartar().Contains(e.Location)) Descartar?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!HayBotones) return;
            if (e.KeyCode == Keys.Enter) { Aprobar?.Invoke(this, EventArgs.Empty); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { Descartar?.Invoke(this, EventArgs.Empty); e.Handled = true; }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            if (Cotizacion == null) return;
            PedidoCotizacion06AV c = Cotizacion;

            bool aprobada = c.Estado == EstadoCotizacion06AV.Aprobado;
            bool descartada = c.Estado == EstadoCotizacion06AV.Desaprobada;

            var caja = new Rectangle(2, 2, Width - 5, Height - 5);
            Color borde = aprobada ? Tema.Exito
                        : descartada ? Tema.Borde
                        : (EsMejor ? Tema.Exito : (Focused ? Tema.Primario : Tema.Borde));

            if (!descartada) Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, aprobada ? 26 : 18);
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta,
                                 descartada ? (Tema.EsOscuro ? Tema.Grafito900 : Tema.Acero50) : Tema.FondoPanel);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta, borde,
                              aprobada || EsMejor || Focused ? 2f : 1f);

            Color apagado = descartada ? Tema.TextoSuave : Tema.Texto;
            int x = 16;
            int ancho = caja.Right - x - 14;

            Pintura06AV.TextoIzquierda(g, c.Proveedor != null ? c.Proveedor.Nombre : "-",
                                       Tema.FuenteSubtit,
                                       descartada ? Tema.TextoSuave : Tema.TextoFuerte,
                                       new Rectangle(x, 12, ancho, 22));
            Pintura06AV.TextoIzquierda(g, c.Proveedor != null ? "CUIT " + c.Proveedor.Cuit : "",
                                       Tema.FuenteMini, Tema.TextoSuave,
                                       new Rectangle(x, 34, ancho, 16));

            using (var fuentePrecio = new Font("Segoe UI Semibold", 20f, FontStyle.Bold))
                Pintura06AV.TextoIzquierda(g, c.Costo.ToString("C0"), fuentePrecio,
                                           descartada ? Tema.TextoSuave : Tema.TextoFuerte,
                                           new Rectangle(x, 56, ancho, 34));

            if (Unidades > 0)
                Pintura06AV.TextoIzquierda(g, (c.Costo / Unidades).ToString("C2") + "  " + TextoUnidad,
                                           Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(x, 90, ancho, 16));

            var pista = new Rectangle(x, 112, ancho, 8);
            Pintura06AV.Rellenar(g, pista, 4, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero200);

            decimal escala = PeorCosto > 0 ? PeorCosto : c.Costo;
            if (escala > 0)
            {
                int w = (int)(pista.Width * (double)(c.Costo / escala));
                Color colorBarra = descartada ? Tema.Acero500 : (EsMejor ? Tema.Exito : Tema.Acento);
                Pintura06AV.Rellenar(g, new Rectangle(pista.X, pista.Y, Math.Max(6, w), pista.Height),
                                     4, colorBarra);
            }

            string chipTexto;
            Color chipColor;
            if (EsMejor && !descartada)
            {
                chipTexto = TextoMejor;
                chipColor = Tema.Exito;
            }
            else if (MejorCosto > 0 && c.Costo > MejorCosto)
            {
                decimal dif = (c.Costo - MejorCosto) / MejorCosto * 100m;
                chipTexto = "+" + dif.ToString("0.#") + "%";
                chipColor = dif >= 15m ? Tema.Peligro : Tema.Advertencia;
            }
            else { chipTexto = null; chipColor = Tema.TextoSuave; }

            if (!string.IsNullOrEmpty(chipTexto))
            {
                int anchoChip = Pintura06AV.AnchoChip(g, chipTexto, Tema.FuenteMini, 20);
                var chip = new Rectangle(caja.Right - 14 - anchoChip, 60, anchoChip, 20);
                Pintura06AV.Chip(g, chip, chipTexto, Tema.FuenteMini,
                                 Pintura06AV.Suave(chipColor, 34), chipColor);
            }

            string cond = string.IsNullOrWhiteSpace(c.Condiciones) ? TextoSinCondiciones : c.Condiciones;
            Pintura06AV.TextoIzquierda(g, cond, Tema.FuenteRegular, apagado,
                                       new Rectangle(x, 130, ancho, 18));
            Pintura06AV.TextoIzquierda(g, c.FechaEmision.ToShortDateString() + "  ·  " + c.Numero,
                                       Tema.FuenteMini, Tema.TextoSuave,
                                       new Rectangle(x, 150, ancho, 16));

            if (HayBotones)
            {
                Rectangle rA = CajaAprobar();
                Pintura06AV.Rellenar(g, rA, 7, _hotAprobar ? Tema.ExitoSuave : Tema.Exito);
                Pintura06AV.TextoCentrado(g, TextoAprobar, Tema.FuenteBold,
                                          _hotAprobar ? Tema.Exito : Color.White, rA);

                Rectangle rD = CajaDescartar();
                Pintura06AV.Rellenar(g, rD, 7, _hotDescartar ? Tema.PeligroSuave : Tema.FondoPanel);
                Pintura06AV.Borde(g, rD, 7, Tema.Borde);
                Pintura06AV.TextoCentrado(g, TextoDescartar, Tema.FuenteRegular,
                                          _hotDescartar ? Tema.Peligro : Tema.TextoSuave, rD);
            }
            else
            {
                Color cEstado = aprobada ? Tema.Exito : descartada ? Tema.Peligro : Tema.Primario;
                string texto = aprobada ? "✓  " + EstadoTexto() : EstadoTexto();
                var franja = new Rectangle(14, Height - 44, Width - 32, 30);
                Pintura06AV.Rellenar(g, franja, 7, Pintura06AV.Suave(cEstado, 30));
                Pintura06AV.TextoCentrado(g, texto, Tema.FuenteBold, cEstado, franja);
            }
        }

        public string TextoEstadoAprobada { get; set; } = "Adjudicada";
        public string TextoEstadoDescartada { get; set; } = "Descartada";
        public string TextoEstadoPendiente { get; set; } = "Por resolver";

        private string EstadoTexto()
        {
            if (Cotizacion == null) return string.Empty;
            switch (Cotizacion.Estado)
            {
                case EstadoCotizacion06AV.Aprobado: return TextoEstadoAprobada;
                case EstadoCotizacion06AV.Desaprobada: return TextoEstadoDescartada;
                default: return TextoEstadoPendiente;
            }
        }
    }
}
