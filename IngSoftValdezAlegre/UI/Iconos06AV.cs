using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Íconos del dominio PC Factory.</summary>
    internal enum IconoPcf06AV
    {
        Ninguno,
        Bandeja,        // orden pendiente / entrada de trabajo
        Calendario,     // planificada
        Destornillador, // en ensamblaje
        Escudo,         // control de calidad / finalizada
        Camion,         // entregada
        Alerta,         // en revisión / faltante crítico
        Caja,           // insumo / componente
        Carrito,        // compra
        Chip,           // componente electrónico
        Check,
        Reloj,
        Flecha,
        Mas,
        Lupa
    }

    /// <summary>
    /// Íconos dibujados con GraphicsPath sobre una grilla virtual de 24x24.
    ///
    /// Por qué vectorial y no PNG/SVG: la app corre en monitores de taller con DPI
    /// distintos y el instalador no debería arrastrar assets. Un path escala perfecto,
    /// se tiñe con el color del tema (claro/oscuro) sin generar variantes, y no suma
    /// ni una dependencia al proyecto.
    /// </summary>
    internal static class Iconos06AV
    {
        /// <summary>Dibuja el ícono centrado y escalado dentro de la caja indicada.</summary>
        public static void Dibujar(Graphics g, IconoPcf06AV icono, RectangleF caja, Color color, float grosor = 1.9f)
        {
            if (icono == IconoPcf06AV.Ninguno || caja.Width <= 0 || caja.Height <= 0) return;

            GraphicsState estado = g.Save();
            try
            {
                float escala = Math.Min(caja.Width, caja.Height) / 24f;
                g.TranslateTransform(
                    caja.X + (caja.Width - 24f * escala) / 2f,
                    caja.Y + (caja.Height - 24f * escala) / 2f);
                g.ScaleTransform(escala, escala);

                using (var p = new Pen(color, grosor / escala * escala))
                using (var b = new SolidBrush(color))
                {
                    p.Width = grosor;
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    p.LineJoin = LineJoin.Round;
                    Trazar(g, p, b, icono);
                }
            }
            finally { g.Restore(estado); }
        }

        private static void Trazar(Graphics g, Pen p, Brush b, IconoPcf06AV icono)
        {
            switch (icono)
            {
                case IconoPcf06AV.Bandeja:
                    // Bandeja de entrada: caja con tapa abierta.
                    g.DrawLines(p, new[]
                    {
                        new PointF(3, 14), new PointF(6, 5), new PointF(18, 5), new PointF(21, 14)
                    });
                    g.DrawRectangle(p, 3, 14, 18, 5);
                    g.DrawLine(p, 8, 14, 16, 14);
                    break;

                case IconoPcf06AV.Calendario:
                    g.DrawRectangle(p, 4, 6, 16, 14);
                    g.DrawLine(p, 4, 11, 20, 11);
                    g.DrawLine(p, 8, 3, 8, 7);
                    g.DrawLine(p, 16, 3, 16, 7);
                    g.FillRectangle(b, 8, 14, 3, 3);
                    break;

                case IconoPcf06AV.Destornillador:
                    // Destornillador en diagonal: la acción física del ensamblaje.
                    g.DrawLine(p, 4, 20, 12, 12);
                    g.DrawLines(p, new[]
                    {
                        new PointF(12, 12), new PointF(15, 9), new PointF(13, 7),
                        new PointF(17, 3), new PointF(21, 7), new PointF(17, 11),
                        new PointF(15, 9)
                    });
                    g.DrawLine(p, 6, 18, 8, 20);
                    break;

                case IconoPcf06AV.Escudo:
                    g.DrawLines(p, new[]
                    {
                        new PointF(12, 3), new PointF(20, 6), new PointF(20, 12),
                        new PointF(12, 21), new PointF(4, 12), new PointF(4, 6),
                        new PointF(12, 3)
                    });
                    g.DrawLines(p, new[] { new PointF(8.5f, 12), new PointF(11, 14.5f), new PointF(15.5f, 9.5f) });
                    break;

                case IconoPcf06AV.Camion:
                    g.DrawRectangle(p, 2, 7, 11, 9);
                    g.DrawLines(p, new[]
                    {
                        new PointF(13, 10), new PointF(18, 10), new PointF(21, 13),
                        new PointF(21, 16), new PointF(13, 16)
                    });
                    g.DrawEllipse(p, 5, 16, 4, 4);
                    g.DrawEllipse(p, 15, 16, 4, 4);
                    break;

                case IconoPcf06AV.Alerta:
                    g.DrawLines(p, new[]
                    {
                        new PointF(12, 3), new PointF(22, 20), new PointF(2, 20), new PointF(12, 3)
                    });
                    g.DrawLine(p, 12, 9, 12, 14);
                    g.FillEllipse(b, 11, 16, 2, 2);
                    break;

                case IconoPcf06AV.Caja:
                    g.DrawLines(p, new[]
                    {
                        new PointF(12, 3), new PointF(21, 8), new PointF(21, 17),
                        new PointF(12, 22), new PointF(3, 17), new PointF(3, 8),
                        new PointF(12, 3)
                    });
                    g.DrawLine(p, 3, 8, 12, 13);
                    g.DrawLine(p, 21, 8, 12, 13);
                    g.DrawLine(p, 12, 13, 12, 22);
                    break;

                case IconoPcf06AV.Carrito:
                    g.DrawLines(p, new[]
                    {
                        new PointF(2, 4), new PointF(5, 4), new PointF(8, 15), new PointF(19, 15)
                    });
                    g.DrawLines(p, new[]
                    {
                        new PointF(6, 7), new PointF(21, 7), new PointF(19, 15)
                    });
                    g.DrawEllipse(p, 8, 17, 3.2f, 3.2f);
                    g.DrawEllipse(p, 16, 17, 3.2f, 3.2f);
                    break;

                case IconoPcf06AV.Chip:
                    g.DrawRectangle(p, 7, 7, 10, 10);
                    g.DrawRectangle(p, 10, 10, 4, 4);
                    for (int i = 0; i < 3; i++)
                    {
                        float o = 9 + i * 3;
                        g.DrawLine(p, o, 4, o, 7);
                        g.DrawLine(p, o, 17, o, 20);
                        g.DrawLine(p, 4, o, 7, o);
                        g.DrawLine(p, 17, o, 20, o);
                    }
                    break;

                case IconoPcf06AV.Check:
                    g.DrawLines(p, new[] { new PointF(5, 12.5f), new PointF(10, 17.5f), new PointF(19, 6.5f) });
                    break;

                case IconoPcf06AV.Reloj:
                    g.DrawEllipse(p, 3, 3, 18, 18);
                    g.DrawLines(p, new[] { new PointF(12, 7), new PointF(12, 12.5f), new PointF(16, 15) });
                    break;

                case IconoPcf06AV.Flecha:
                    g.DrawLine(p, 4, 12, 19, 12);
                    g.DrawLines(p, new[] { new PointF(13, 6), new PointF(19, 12), new PointF(13, 18) });
                    break;

                case IconoPcf06AV.Mas:
                    g.DrawLine(p, 12, 5, 12, 19);
                    g.DrawLine(p, 5, 12, 19, 12);
                    break;

                case IconoPcf06AV.Lupa:
                    g.DrawEllipse(p, 4, 4, 12, 12);
                    g.DrawLine(p, 15, 15, 20, 20);
                    break;
            }
        }
    }
}
