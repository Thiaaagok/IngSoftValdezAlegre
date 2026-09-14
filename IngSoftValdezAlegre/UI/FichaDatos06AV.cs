using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.UI
{
    /// <summary>Una fila de la ficha: rótulo chico arriba, valor abajo.</summary>
    internal class DatoFicha06AV
    {
        public string Rotulo { get; set; }
        public string Valor { get; set; }
        /// <summary>Ocupa toda la fila en vez de media (para textos largos).</summary>
        public bool Ancho { get; set; }

        public DatoFicha06AV() { }
        public DatoFicha06AV(string rotulo, string valor, bool ancho = false)
        {
            Rotulo = rotulo; Valor = valor; Ancho = ancho;
        }
    }

    /// <summary>
    /// FICHA DE CONTEXTO — el "de qué estamos hablando" de las pantallas de acción.
    ///
    /// Las pantallas de registrar seña, entrega y orden arrancaban con un párrafo gris
    /// donde cliente, DNI, total y abonado iban pegados con puntos medios: para encontrar
    /// un dato había que leer la línea entera.
    ///
    /// Acá los mismos datos van en una grilla de dos columnas, cada uno con su rótulo
    /// arriba en chico y su valor abajo en negrita, de modo que el ojo salta directo al
    /// que busca. Opcionalmente destaca UN número — el que la pantalla va a mover — en
    /// grande y en color, porque es el dato que el operador tiene que confirmar.
    /// </summary>
    internal class FichaDatos06AV : Control
    {
        private readonly List<DatoFicha06AV> _datos = new List<DatoFicha06AV>();

        private const int AltoFila = 44;

        public FichaDatos06AV()
        {
            SetStyle(Pintura06AV.EstilosDibujo, true);
            Height = 96;
        }

        public string Titulo { get; set; }

        /// <summary>Rótulo del número destacado (vacío = sin destacado).</summary>
        public string RotuloDestacado { get; set; }

        public string ValorDestacado { get; set; }

        /// <summary>Color del número destacado. Por defecto, el primario del tema.</summary>
        public Color? ColorDestacado { get; set; }

        public void Definir(IEnumerable<DatoFicha06AV> datos)
        {
            _datos.Clear();
            if (datos != null) _datos.AddRange(datos);
            Height = AltoNecesario;
            Invalidate();
        }

        /// <summary>Alto que necesita la ficha con los datos actuales.</summary>
        public int AltoNecesario
        {
            get
            {
                int filas = 0, col = 0;
                foreach (DatoFicha06AV d in _datos)
                {
                    if (d.Ancho) { if (col > 0) { filas++; col = 0; } filas++; }
                    else { col++; if (col == 2) { filas++; col = 0; } }
                }
                if (col > 0) filas++;

                int alto = 16 + filas * AltoFila + 14;
                if (!string.IsNullOrEmpty(Titulo)) alto += 26;
                if (!string.IsNullOrEmpty(ValorDestacado)) alto += 56;
                return Math.Max(70, alto);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pintura06AV.Calidad(g);
            using (var b = new SolidBrush(Parent != null ? Parent.BackColor : Tema.FondoApp))
                g.FillRectangle(b, ClientRectangle);

            var caja = new Rectangle(2, 2, Math.Max(20, Width - 5), Math.Max(20, Height - 6));
            Pintura06AV.Rellenar(g, caja, Pintura06AV.RadioTarjeta, Tema.FondoPanel);
            Pintura06AV.Borde(g, caja, Pintura06AV.RadioTarjeta, Tema.Borde);

            int y = caja.Y + 12;
            int x0 = caja.X + 18;
            int anchoUtil = caja.Width - 36;

            if (!string.IsNullOrEmpty(Titulo))
            {
                Pintura06AV.TextoIzquierda(g, Titulo, Tema.FuenteSubtit, Tema.TextoFuerte,
                                           new Rectangle(x0, y, anchoUtil, 22));
                y += 26;
            }

            // ── Número destacado ─────────────────────────────────
            if (!string.IsNullOrEmpty(ValorDestacado))
            {
                Color c = ColorDestacado ?? Tema.Primario;
                var franja = new Rectangle(x0, y, anchoUtil, 48);
                Pintura06AV.Rellenar(g, franja, 8, Pintura06AV.Suave(c, Tema.EsOscuro ? 40 : 24));

                Pintura06AV.TextoIzquierda(g, RotuloDestacado, Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(franja.X + 14, franja.Y + 6, franja.Width - 28, 14));
                using (var fuente = new Font("Segoe UI Semibold", 17f, FontStyle.Bold))
                    Pintura06AV.TextoIzquierda(g, ValorDestacado, fuente, c,
                                               new Rectangle(franja.X + 14, franja.Y + 20, franja.Width - 28, 24));
                y += 56;
            }

            // ── Datos en dos columnas ────────────────────────────
            int anchoCol = anchoUtil / 2;
            int col = 0;
            foreach (DatoFicha06AV d in _datos)
            {
                if (d.Ancho && col > 0) { col = 0; y += AltoFila; }

                int x = x0 + col * anchoCol;
                int ancho = d.Ancho ? anchoUtil : anchoCol - 12;

                Pintura06AV.TextoIzquierda(g, d.Rotulo, Tema.FuenteMini, Tema.TextoSuave,
                                           new Rectangle(x, y, ancho, 15));
                Pintura06AV.TextoIzquierda(g, d.Valor, Tema.FuenteBold, Tema.Texto,
                                           new Rectangle(x, y + 16, ancho, 20));

                if (d.Ancho) { y += AltoFila; col = 0; }
                else { col++; if (col == 2) { col = 0; y += AltoFila; } }
            }
        }
    }
}
