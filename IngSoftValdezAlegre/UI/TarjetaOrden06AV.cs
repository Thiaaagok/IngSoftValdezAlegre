using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Tarjeta de una orden dentro del tablero de estaciones.
    ///
    /// Lee como una ficha de trabajo colgada en la línea, no como una fila de grilla:
    ///   · franja de urgencia a la izquierda, calculada contra la fecha comprometida
    ///     (verde / ámbar / rojo). El atraso se ve antes de leer la fecha;
    ///   · el número de orden es el dato ancla y va en grande;
    ///   · píldora de acción al pie con el ÚNICO próximo paso posible en esa estación,
    ///     así el operario no tiene que ir al panel de detalle para avanzar la orden.
    /// </summary>
    internal class TarjetaOrden06AV : Control
    {
        private bool _hot;
        private bool _hotAccion;

        public TarjetaOrden06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Width = 236;
            Height = 122;
            MinimumSize = new Size(150, 122);
            Margin = new Padding(8, 6, 8, 6);
            Cursor = Cursors.Hand;
        }

        #region Datos que muestra la tarjeta

        /// <summary>Objeto de negocio asociado (la orden). Lo usa la pantalla, no la tarjeta.</summary>
        public object Etiqueta { get; set; }

        /// <summary>Clave visible, p. ej. "#14".</summary>
        public string Clave { get; set; } = string.Empty;

        /// <summary>Título principal: cliente o proveedor.</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Segunda línea: equipo, modelo, detalle.</summary>
        public string Subtitulo { get; set; } = string.Empty;

        /// <summary>Chips cortos: línea de ensamblaje, responsable.</summary>
        public string[] Chips { get; set; }

        /// <summary>Texto del pie: fecha comprometida.</summary>
        public string PieIzquierda { get; set; } = string.Empty;

        /// <summary>Texto corto de plazo: "en 3 días", "atrasada 2 d".</summary>
        public string TextoPlazo { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de urgencia 0 = a tiempo, 1 = por vencer, 2 = vencida.
        /// Define la franja lateral y el color del plazo.
        /// </summary>
        public int Urgencia { get; set; }

        /// <summary>Texto de la píldora de acción. Vacío = la tarjeta no ofrece acción.</summary>
        public string TextoAccion { get; set; }

        public bool Seleccionada { get; set; }

        #endregion

        public event EventHandler Elegida;
        public event EventHandler AccionElegida;

        private Color ColorUrgencia()
        {
            switch (Urgencia)
            {
                case 2: return Tema.Peligro;
                case 1: return Tema.Advertencia;
                default: return Tema.Exito;
            }
        }

        private Rectangle CajaAccion()
        {
            if (string.IsNullOrEmpty(TextoAccion)) return Rectangle.Empty;
            return new Rectangle(12, Height - 34, Width - 24, 26);
        }

        #region Interacción

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hot = false; _hotAccion = false; Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool sobre = CajaAccion().Contains(e.Location);
            if (sobre != _hotAccion) { _hotAccion = sobre; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (CajaAccion().Contains(e.Location)) AccionElegida?.Invoke(this, EventArgs.Empty);
            else Elegida?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            if (!string.IsNullOrEmpty(TextoAccion)) AccionElegida?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) { Elegida?.Invoke(this, EventArgs.Empty); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(TextoAccion)) AccionElegida?.Invoke(this, EventArgs.Empty);
                else Elegida?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Elegida?.Invoke(this, EventArgs.Empty);   // moverse con Tab ya muestra el detalle
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);

            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(2, 2, Width - 5, Height - 5);
            bool destacada = Seleccionada || Focused;

            if (_hot || destacada) Pintura06AV.Sombra(g, caja, Pintura06AV.RadioTarjeta, 4, 22);

            Color fondo = Tema.FondoPanel;
            if (_hot && !destacada) fondo = Pintura06AV.Mezclar(fondo, Tema.Primario, 0.05f);
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, fondo);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta,
                              destacada ? Tema.Primario : Tema.Borde, destacada ? 1.8f : 1f);

            // Franja de urgencia (izquierda), recortada al radio de la tarjeta.
            using (var recorte = Pintura06AV.Redondeado(caja, Pintura06AV.RadioTarjeta))
            {
                Region previa = g.Clip;
                g.SetClip(recorte, System.Drawing.Drawing2D.CombineMode.Intersect);
                using (var b = new SolidBrush(ColorUrgencia()))
                    g.FillRectangle(b, caja.X, caja.Y, 5, caja.Height);
                g.Clip = previa;
            }

            int x = 18;
            int ancho = caja.Right - x - 12;

            // Clave + plazo
            var rClave = new Rectangle(x, 10, 64, 24);
            Pintura06AV.TextoIzquierda(g, Clave, Tema.FuenteTitulo, Tema.TextoFuerte, rClave);

            if (!string.IsNullOrEmpty(TextoPlazo))
            {
                int anchoChip = Pintura06AV.AnchoChip(g, TextoPlazo, Tema.FuenteMini, 19);
                var chip = new Rectangle(caja.Right - 12 - anchoChip, 13, anchoChip, 19);
                Color c = ColorUrgencia();
                Pintura06AV.Chip(g, chip, TextoPlazo, Tema.FuenteMini, Pintura06AV.Suave(c, 34), c);
            }

            Pintura06AV.TextoIzquierda(g, Titulo, Tema.FuenteBold, Tema.Texto,
                                       new Rectangle(x, 36, ancho, 18));
            Pintura06AV.TextoIzquierda(g, Subtitulo, Tema.FuenteRegular, Tema.TextoSuave,
                                       new Rectangle(x, 54, ancho, 17));

            // Chips de contexto
            if (Chips != null)
            {
                int cx = x;
                foreach (string chipTexto in Chips)
                {
                    if (string.IsNullOrWhiteSpace(chipTexto)) continue;
                    int w = Math.Min(Pintura06AV.AnchoChip(g, chipTexto, Tema.FuenteMini, 18), caja.Right - 12 - cx);
                    if (w < 26) break;
                    var chip = new Rectangle(cx, 74, w, 18);
                    Pintura06AV.Chip(g, chip, chipTexto, Tema.FuenteMini,
                                     Tema.EsOscuro ? Tema.Acero700 : Tema.Acero100, Tema.TextoSuave);
                    cx += w + 6;
                }
            }

            Rectangle rAccion = CajaAccion();
            if (rAccion != Rectangle.Empty)
            {
                Color fondoAccion = _hotAccion ? Tema.PrimarioHover : Tema.Primario;
                Pintura06AV.Rellenar(g, rAccion, 7, fondoAccion);
                Pintura06AV.TextoCentrado(g, TextoAccion, Tema.FuenteBold, Color.White, rAccion);
            }
            else if (!string.IsNullOrEmpty(PieIzquierda))
            {
                Pintura06AV.TextoIzquierda(g, PieIzquierda, Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(x, Height - 32, ancho, 18));
            }
        }
    }
}
