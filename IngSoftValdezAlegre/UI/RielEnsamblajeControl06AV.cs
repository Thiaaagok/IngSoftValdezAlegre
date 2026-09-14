using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Una estación del riel de ensamblaje (un estado del proceso físico).</summary>
    internal class EstacionRiel06AV
    {
        public string Titulo { get; set; }
        /// <summary>Dato corto bajo el título: fecha, línea, responsable.</summary>
        public string Detalle { get; set; }
        public IconoPcf06AV Icono { get; set; }

        public EstacionRiel06AV() { }
        public EstacionRiel06AV(string titulo, IconoPcf06AV icono, string detalle = null)
        {
            Titulo = titulo; Icono = icono; Detalle = detalle;
        }
    }

    /// <summary>
    /// RIEL DE ENSAMBLAJE — reemplaza la lista de estados en texto del panel de detalle.
    ///
    /// Metáfora: la orden de producción es una PC que avanza físicamente por una línea.
    /// Cada estado es una ESTACIÓN del riel, no un ítem de una lista:
    ///   · el riel se "energiza" (degradado cian → naranja) hasta la estación actual,
    ///     así el avance se lee de un vistazo sin comparar bullets;
    ///   · la estación actual late suavemente: indica trabajo en curso, no un estado inerte;
    ///   · "En revisión" NO es un paso más: se dibuja como un DESVÍO que sale del riel y
    ///     vuelve, que es exactamente lo que pasa en el taller (la máquina no avanza,
    ///     se retrabaja). Una lista de enum no puede expresar eso y por eso confundía.
    ///
    /// Es un Control puro (sin hijos): se pinta entero en OnPaint, se navega con mouse
    /// y con flechas del teclado, y cada estación es clickeable para ver su información.
    /// </summary>
    internal class RielEnsamblajeControl06AV : Control
    {
        private readonly List<EstacionRiel06AV> _estaciones = new List<EstacionRiel06AV>();
        private readonly Timer _pulso;
        private float _fase;
        private int _hot = -1;
        private int _foco = -1;

        private const int Diametro = 42;
        private const int AltoRiel = 6;
        private const int MargenLateral = 38;

        public RielEnsamblajeControl06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = 136;
            // Sin BackColor transparente: un Control puro no lo admite (ArgumentException).
            // El fondo se resuelve en OnPaint tomando el color del contenedor.


            _pulso = new Timer { Interval = 60 };
            _pulso.Tick += (s, e) =>
            {
                _fase += 0.09f;
                if (_fase > (float)Math.PI * 2f) _fase -= (float)Math.PI * 2f;
                Invalidate();
            };
        }

        #region API pública

        /// <summary>Índice de la estación en curso (0..N-1). -1 = ninguna.</summary>
        public int IndiceActual { get; private set; } = -1;

        /// <summary>True si la orden está desviada (en revisión) sobre la estación actual.</summary>
        public bool EnDesvio { get; private set; }

        /// <summary>Texto del desvío, p. ej. "En revisión".</summary>
        public string TextoDesvio { get; private set; }

        /// <summary>Se dispara al hacer clic (o Enter) sobre una estación.</summary>
        public event EventHandler<int> EstacionElegida;

        /// <summary>Carga las estaciones del proceso. Se llama una vez por pantalla.</summary>
        public void DefinirEstaciones(IEnumerable<EstacionRiel06AV> estaciones)
        {
            _estaciones.Clear();
            if (estaciones != null) _estaciones.AddRange(estaciones);
            if (_foco >= _estaciones.Count) _foco = -1;
            Invalidate();
        }

        /// <summary>Actualiza el avance. <paramref name="desvio"/> dibuja la rama de retrabajo.</summary>
        public void Avanzar(int indiceActual, bool desvio = false, string textoDesvio = null)
        {
            IndiceActual = indiceActual;
            EnDesvio = desvio;
            TextoDesvio = textoDesvio;
            if (_foco < 0) _foco = indiceActual;
            ActualizarPulso();
            Invalidate();
        }

        /// <summary>Texto del detalle de una estación ya cargada (fecha, responsable, etc.).</summary>
        public void EstablecerDetalle(int indice, string detalle)
        {
            if (indice < 0 || indice >= _estaciones.Count) return;
            _estaciones[indice].Detalle = detalle;
            Invalidate();
        }

        #endregion

        #region Ciclo de vida

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            ActualizarPulso();
        }

        private void ActualizarPulso()
        {
            bool debe = Visible && IndiceActual >= 0 && IndiceActual < _estaciones.Count;
            if (debe && !_pulso.Enabled) _pulso.Start();
            else if (!debe && _pulso.Enabled) _pulso.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _pulso.Stop(); _pulso.Dispose(); }
            base.Dispose(disposing);
        }

        #endregion

        #region Geometría

        private int CentroY => 46;

        private Point CentroEstacion(int i)
        {
            if (_estaciones.Count == 0) return new Point(0, CentroY);
            if (_estaciones.Count == 1) return new Point(Width / 2, CentroY);
            int util = Math.Max(1, Width - MargenLateral * 2);
            int paso = util / (_estaciones.Count - 1);
            return new Point(MargenLateral + paso * i, CentroY);
        }

        private Rectangle CajaEstacion(int i)
        {
            Point c = CentroEstacion(i);
            return new Rectangle(c.X - Diametro / 2, c.Y - Diametro / 2, Diametro, Diametro);
        }

        private int EstacionEn(Point p)
        {
            for (int i = 0; i < _estaciones.Count; i++)
            {
                Rectangle caja = CajaEstacion(i);
                caja.Inflate(10, 34);   // zona sensible: incluye la etiqueta de abajo
                if (caja.Contains(p)) return i;
            }
            return -1;
        }

        #endregion

        #region Interacción

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = EstacionEn(e.Location);
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
            Focus();
            int i = EstacionEn(e.Location);
            if (i >= 0)
            {
                _foco = i;
                Invalidate();
                EstacionElegida?.Invoke(this, i);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_estaciones.Count == 0) return;

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                int delta = e.KeyCode == Keys.Right ? 1 : -1;
                int inicial = _foco < 0 ? Math.Max(0, IndiceActual) : _foco;
                _foco = Math.Max(0, Math.Min(_estaciones.Count - 1, inicial + delta));
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                if (_foco >= 0) EstacionElegida?.Invoke(this, _foco);
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        #endregion

        #region Dibujo

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);

            using (var fondo = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoPanel))
                g.FillRectangle(fondo, ClientRectangle);

            if (_estaciones.Count == 0) return;

            DibujarRiel(g);
            if (EnDesvio) DibujarDesvio(g);
            for (int i = 0; i < _estaciones.Count; i++) DibujarEstacion(g, i);
        }

        private void DibujarRiel(Graphics g)
        {
            Point ini = CentroEstacion(0);
            Point fin = CentroEstacion(_estaciones.Count - 1);
            var pista = new Rectangle(ini.X, ini.Y - AltoRiel / 2, Math.Max(1, fin.X - ini.X), AltoRiel);

            // Pista completa (apagada)
            Pintura06AV.Rellenar(g, pista, AltoRiel / 2, Tema.EsOscuro ? Tema.Acero700 : Tema.Acero200);

            // Tramo energizado hasta la estación actual
            int hasta = Math.Min(IndiceActual, _estaciones.Count - 1);
            if (hasta > 0)
            {
                Point c = CentroEstacion(hasta);
                var activa = new Rectangle(ini.X, pista.Y, Math.Max(1, c.X - ini.X), AltoRiel);
                using (var path = Pintura06AV.Redondeado(activa, AltoRiel / 2))
                using (var b = new LinearGradientBrush(
                           new Rectangle(activa.X, activa.Y, activa.Width, activa.Height + 1),
                           Tema.Primario, Tema.Acento, 0f))
                    g.FillPath(b, path);
            }

            // Si está desviada, el tramo siguiente se marca punteado: el avance está detenido.
            if (EnDesvio && IndiceActual >= 0 && IndiceActual < _estaciones.Count - 1)
            {
                Point a = CentroEstacion(IndiceActual);
                Point b2 = CentroEstacion(IndiceActual + 1);
                using (var p = new Pen(Tema.Peligro, 2f) { DashStyle = DashStyle.Dash })
                    g.DrawLine(p, a.X + Diametro / 2, a.Y, b2.X - Diametro / 2, b2.Y);
            }
        }

        private void DibujarDesvio(Graphics g)
        {
            if (IndiceActual < 0 || IndiceActual >= _estaciones.Count) return;

            Point c = CentroEstacion(IndiceActual);
            int xIni = c.X + Diametro / 2 - 4;
            int yIni = c.Y + 6;
            int xFin = xIni + 54;
            int yFin = c.Y + 30;

            using (var p = new Pen(Tema.Peligro, 2.2f) { EndCap = LineCap.Round, StartCap = LineCap.Round })
                g.DrawBezier(p, xIni, yIni, xIni + 22, yIni + 6, xFin - 26, yFin, xFin, yFin);

            string txt = string.IsNullOrEmpty(TextoDesvio) ? "?" : TextoDesvio;
            int ancho = Pintura06AV.AnchoChip(g, txt, Tema.FuenteMini, 20);
            var chip = new Rectangle(xFin, yFin - 10, ancho, 20);
            Pintura06AV.Chip(g, chip, txt, Tema.FuenteMini, Tema.PeligroSuave, Tema.Peligro);
            Pintura06AV.Borde(g, chip, 10, Tema.Peligro);
        }

        private void DibujarEstacion(Graphics g, int i)
        {
            EstacionRiel06AV est = _estaciones[i];
            Rectangle caja = CajaEstacion(i);
            Point c = CentroEstacion(i);

            bool completada = IndiceActual > i;
            bool actual = IndiceActual == i;
            bool resaltada = _hot == i || (Focused && _foco == i);

            Color fondo, borde, tinta;
            if (completada) { fondo = Tema.Exito; borde = Tema.Exito; tinta = Color.White; }
            else if (actual && EnDesvio) { fondo = Tema.Peligro; borde = Tema.Peligro; tinta = Color.White; }
            else if (actual) { fondo = Tema.Acento; borde = Tema.Acento; tinta = Color.White; }
            else
            {
                fondo = Tema.FondoPanel;
                borde = Tema.EsOscuro ? Tema.Acero700 : Tema.Acero300;
                tinta = Tema.TextoSuave;
            }

            // Latido de la estación en curso: trabajo en marcha, no estado congelado.
            if (actual)
            {
                float amp = (float)(Math.Sin(_fase) * 0.5 + 0.5);       // 0..1
                int extra = (int)(4 + amp * 7);
                var halo = new Rectangle(caja.X - extra, caja.Y - extra,
                                         caja.Width + extra * 2, caja.Height + extra * 2);
                using (var b = new SolidBrush(Color.FromArgb((int)(38 - amp * 22), fondo)))
                    g.FillEllipse(b, halo);
            }

            if (resaltada)
            {
                var anillo = new Rectangle(caja.X - 4, caja.Y - 4, caja.Width + 8, caja.Height + 8);
                using (var p = new Pen(Tema.Primario, 2f))
                    g.DrawEllipse(p, anillo);
            }

            using (var b = new SolidBrush(fondo)) g.FillEllipse(b, caja);
            using (var p = new Pen(borde, 1.6f)) g.DrawEllipse(p, caja);

            var cajaIcono = new RectangleF(caja.X + 10, caja.Y + 10, caja.Width - 20, caja.Height - 20);
            Iconos06AV.Dibujar(g, completada ? IconoPcf06AV.Check : est.Icono, cajaIcono, tinta, 2f);

            // Etiquetas bajo la estación, recortadas al ancho del control para que
            // la primera y la última no queden cortadas contra los bordes.
            int ancho = Math.Max(70, (Width - MargenLateral * 2) / Math.Max(1, _estaciones.Count) + 24);
            ancho = Math.Min(ancho, Width);
            int xEtiqueta = Math.Max(0, Math.Min(c.X - ancho / 2, Width - ancho));
            var rTitulo = new Rectangle(xEtiqueta, caja.Bottom + 10, ancho, 18);
            Color colorTitulo = actual ? (EnDesvio ? Tema.Peligro : Tema.Acento)
                                       : (completada ? Tema.Texto : Tema.TextoSuave);
            Pintura06AV.TextoCentrado(g, est.Titulo, actual ? Tema.FuenteBold : Tema.FuenteRegular,
                                      colorTitulo, rTitulo);

            if (!string.IsNullOrWhiteSpace(est.Detalle))
            {
                var rDet = new Rectangle(xEtiqueta, rTitulo.Bottom, ancho, 16);
                Pintura06AV.TextoCentrado(g, est.Detalle, Tema.FuenteMini, Tema.TextoSuave, rDet);
            }
        }

        #endregion
    }
}
