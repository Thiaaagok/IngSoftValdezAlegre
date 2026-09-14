using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>
    /// Primitivas de dibujo GDI+ compartidas por los controles propios de PC Factory.
    ///
    /// Todo el look "de producto" (esquinas redondeadas, sombras, chips, medidores)
    /// se resuelve acá y no en cada control, para que un cambio de radio o de sombra
    /// se aplique a toda la aplicación de una sola vez.
    ///
    /// No usa imágenes ni librerías externas: sólo GraphicsPath + brushes. Así escala
    /// en cualquier DPI y no agrega archivos al instalador.
    /// </summary>
    internal static class Pintura06AV
    {
        /// <summary>Radio estándar de tarjetas y paneles.</summary>
        public const int RadioTarjeta = 10;

        /// <summary>Radio estándar de chips, botones chicos y badges.</summary>
        public const int RadioChip = 999;   // se recorta a la mitad del alto

        /// <summary>Activa antialiasing y texto nítido. Llamar al inicio de cada OnPaint.</summary>
        public static void Calidad(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>Camino cerrado de un rectángulo con esquinas redondeadas.</summary>
        public static GraphicsPath Redondeado(Rectangle r, int radio)
        {
            var path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) { path.AddRectangle(r); return path; }

            int max = Math.Min(r.Width, r.Height) / 2;
            int rad = Math.Max(0, Math.Min(radio, max));
            if (rad == 0) { path.AddRectangle(r); return path; }

            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Rellena un rectángulo redondeado.</summary>
        public static void Rellenar(Graphics g, Rectangle r, int radio, Color color)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (var path = Redondeado(r, radio))
            using (var b = new SolidBrush(color))
                g.FillPath(b, path);
        }

        /// <summary>Rellena con un degradado vertical suave (sensación de material, no de "flat").</summary>
        public static void RellenarDegradado(Graphics g, Rectangle r, int radio, Color arriba, Color abajo)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (var path = Redondeado(r, radio))
            using (var b = new LinearGradientBrush(
                       new Rectangle(r.X, r.Y, r.Width, r.Height + 1), arriba, abajo, 90f))
                g.FillPath(b, path);
        }

        /// <summary>Dibuja el borde de un rectángulo redondeado.</summary>
        public static void Borde(Graphics g, Rectangle r, int radio, Color color, float grosor = 1f)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            var rr = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (var path = Redondeado(rr, radio))
            using (var p = new Pen(color, grosor))
                g.DrawPath(p, path);
        }

        /// <summary>
        /// Sombra simulada: capas concéntricas translúcidas hacia afuera.
        /// WinForms no tiene sombras reales; esto da la elevación sin costo perceptible.
        /// </summary>
        public static void Sombra(Graphics g, Rectangle r, int radio, int capas = 5, int alphaBase = 16)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            for (int i = capas; i >= 1; i--)
            {
                var rr = new Rectangle(r.X - i, r.Y - i + 1, r.Width + i * 2, r.Height + i * 2);
                int alpha = Math.Max(1, alphaBase / i);
                using (var path = Redondeado(rr, radio + i))
                using (var b = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    g.FillPath(b, path);
            }
        }

        /// <summary>Chip/badge de estado: fondo suave, texto en color fuerte.</summary>
        public static void Chip(Graphics g, Rectangle r, string texto, Font fuente,
                                Color fondo, Color colorTexto)
        {
            Rellenar(g, r, r.Height / 2, fondo);
            TextoCentrado(g, texto, fuente, colorTexto, r);
        }

        /// <summary>Mide el ancho que necesita un chip para un texto dado.</summary>
        public static int AnchoChip(Graphics g, string texto, Font fuente, int alto)
        {
            SizeF s = g.MeasureString(texto ?? string.Empty, fuente);
            return (int)Math.Ceiling(s.Width) + alto;   // alto/2 de padding a cada lado
        }

        public static void TextoCentrado(Graphics g, string texto, Font fuente, Color color, Rectangle r)
        {
            using (var b = new SolidBrush(color))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(texto ?? string.Empty, fuente, b, r, sf);
        }

        public static void TextoIzquierda(Graphics g, string texto, Font fuente, Color color, Rectangle r)
        {
            using (var b = new SolidBrush(color))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(texto ?? string.Empty, fuente, b, r, sf);
        }

        public static void TextoDerecha(Graphics g, string texto, Font fuente, Color color, Rectangle r)
        {
            using (var b = new SolidBrush(color))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(texto ?? string.Empty, fuente, b, r, sf);
        }

        /// <summary>Interpola dos colores (0 = a, 1 = b). Para hovers y degradados de estado.</summary>
        public static Color Mezclar(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>Versión translúcida de un color, para fondos suaves calculados.</summary>
        public static Color Suave(Color color, int alpha) => Color.FromArgb(alpha, color);

        /// <summary>
        /// Estilos que todo control pintado a mano necesita: sin parpadeo y repintado
        /// completo al cambiar de tamaño. Se aplica desde el constructor del control.
        /// </summary>
        public const ControlStyles EstilosDibujo =
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw;
    }
}
