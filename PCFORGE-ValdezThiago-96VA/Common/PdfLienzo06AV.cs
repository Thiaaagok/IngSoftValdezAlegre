using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal enum AlineacionPdf06AV
    {
        Izquierda,
        Centro,
        Derecha
    }

    internal enum FuentePdf06AV
    {
        Normal,
        Negrita,
        Cursiva
    }

    internal sealed class PdfLienzo06AV
    {
        public const float Ancho = 595.28f;
        public const float Alto = 841.89f;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Encoding WinAnsi = Encoding.GetEncoding(1252);

        private readonly List<StringBuilder> _paginas = new List<StringBuilder>();
        private StringBuilder _pagina;
        private byte[] _jpeg;
        private int _jpegAncho;
        private int _jpegAlto;
        private byte[] _mascara;

        public int CantidadPaginas => _paginas.Count;

        public int PaginaActual => _paginas.IndexOf(_pagina) + 1;

        public void NuevaPagina()
        {
            _pagina = new StringBuilder();
            _paginas.Add(_pagina);
        }

        public void IrAPagina(int numero)
        {
            _pagina = _paginas[numero - 1];
        }

        public void DefinirImagen(Image imagen)
        {
            if (imagen == null) return;
            using (var bmp = new Bitmap(imagen.Width, imagen.Height, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.DrawImage(imagen, 0, 0, imagen.Width, imagen.Height);
                }
                var codec = Array.Find(ImageCodecInfo.GetImageEncoders(), c => c.FormatID == ImageFormat.Jpeg.Guid);
                using (var ms = new MemoryStream())
                using (var parametros = new EncoderParameters(1))
                {
                    parametros.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                    bmp.Save(ms, codec, parametros);
                    _jpeg = ms.ToArray();
                }
                _jpegAncho = bmp.Width;
                _jpegAlto = bmp.Height;
                _mascara = MascaraDifuminada(_jpegAncho, _jpegAlto);
            }
        }

        private static byte[] MascaraDifuminada(int ancho, int alto)
        {
            var datos = new byte[ancho * alto];
            float borde = Math.Max(1f, Math.Min(ancho, alto) * 0.10f);
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    float d = Math.Min(Math.Min(x, ancho - 1 - x), Math.Min(y, alto - 1 - y));
                    float a = Math.Min(1f, d / borde);
                    a = a * a * (3f - 2f * a);
                    datos[y * ancho + x] = (byte)Math.Round(a * 255f);
                }
            }
            return datos;
        }

        public bool TieneImagen => _jpeg != null;

        public float ProporcionImagen => _jpeg == null ? 1f : (float)_jpegAncho / _jpegAlto;

        public void Imagen(float x, float y, float ancho, float alto)
        {
            if (_jpeg == null) return;
            _pagina.Append("q ").Append(N(ancho)).Append(" 0 0 ").Append(N(alto)).Append(' ')
                   .Append(N(x)).Append(' ').Append(N(Alto - y - alto)).Append(" cm /Im1 Do Q\n");
        }

        public void Rect(float x, float y, float ancho, float alto, Color relleno)
        {
            Pintar(relleno, false);
            _pagina.Append(N(x)).Append(' ').Append(N(Alto - y - alto)).Append(' ')
                   .Append(N(ancho)).Append(' ').Append(N(alto)).Append(" re f\n");
        }

        public void RectRedondeado(float x, float y, float ancho, float alto, float radio,
                                   Color? relleno, Color? borde = null, float grosor = 0.75f)
        {
            if (relleno == null && borde == null) return;
            radio = Math.Min(radio, Math.Min(ancho, alto) / 2f);
            const float k = 0.5523f;
            float x0 = x, x1 = x + ancho;
            float y0 = Alto - y - alto, y1 = Alto - y;
            float c = radio * k;
            if (relleno.HasValue) Pintar(relleno.Value, false);
            if (borde.HasValue)
            {
                Pintar(borde.Value, true);
                _pagina.Append(N(grosor)).Append(" w\n");
            }
            var p = _pagina;
            p.Append(N(x0 + radio)).Append(' ').Append(N(y0)).Append(" m\n");
            p.Append(N(x1 - radio)).Append(' ').Append(N(y0)).Append(" l\n");
            Curva(x1 - radio + c, y0, x1, y0 + radio - c, x1, y0 + radio);
            p.Append(N(x1)).Append(' ').Append(N(y1 - radio)).Append(" l\n");
            Curva(x1, y1 - radio + c, x1 - radio + c, y1, x1 - radio, y1);
            p.Append(N(x0 + radio)).Append(' ').Append(N(y1)).Append(" l\n");
            Curva(x0 + radio - c, y1, x0, y1 - radio + c, x0, y1 - radio);
            p.Append(N(x0)).Append(' ').Append(N(y0 + radio)).Append(" l\n");
            Curva(x0, y0 + radio - c, x0 + radio - c, y0, x0 + radio, y0);
            p.Append(relleno.HasValue && borde.HasValue ? "b\n" : relleno.HasValue ? "f\n" : "s\n");
        }

        public void Linea(float x1, float y1, float x2, float y2, Color color, float grosor = 0.75f)
        {
            Pintar(color, true);
            _pagina.Append(N(grosor)).Append(" w ")
                   .Append(N(x1)).Append(' ').Append(N(Alto - y1)).Append(" m ")
                   .Append(N(x2)).Append(' ').Append(N(Alto - y2)).Append(" l S\n");
        }

        public void Texto(string texto, float x, float linea, float tamanio, FuentePdf06AV fuente, Color color,
                          AlineacionPdf06AV alineacion = AlineacionPdf06AV.Izquierda, float espaciado = 0f)
        {
            if (string.IsNullOrEmpty(texto)) return;
            float ancho = Medir(texto, tamanio, fuente, espaciado);
            if (alineacion == AlineacionPdf06AV.Derecha) x -= ancho;
            else if (alineacion == AlineacionPdf06AV.Centro) x -= ancho / 2f;
            Pintar(color, false);
            _pagina.Append("BT /").Append(NombreFuente(fuente)).Append(' ').Append(N(tamanio)).Append(" Tf ")
                   .Append(N(espaciado)).Append(" Tc ")
                   .Append(N(x)).Append(' ').Append(N(Alto - linea)).Append(" Td (")
                   .Append(Cadena(texto)).Append(") Tj ET\n");
        }

        public void TextoRotado(string texto, float centroX, float centroY, float tamanio, float grados,
                                FuentePdf06AV fuente, Color color, float espaciado = 0f)
        {
            if (string.IsNullOrEmpty(texto)) return;
            double rad = grados * Math.PI / 180.0;
            float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
            float ancho = Medir(texto, tamanio, fuente, espaciado);
            float px = centroX, py = Alto - centroY;
            float ox = px - cos * ancho / 2f + sin * tamanio * 0.35f;
            float oy = py - sin * ancho / 2f - cos * tamanio * 0.35f;
            Pintar(color, false);
            _pagina.Append("BT /").Append(NombreFuente(fuente)).Append(' ').Append(N(tamanio)).Append(" Tf ")
                   .Append(N(espaciado)).Append(" Tc ")
                   .Append(N(cos)).Append(' ').Append(N(sin)).Append(' ').Append(N(-sin)).Append(' ').Append(N(cos)).Append(' ')
                   .Append(N(ox)).Append(' ').Append(N(oy)).Append(" Tm (")
                   .Append(Cadena(texto)).Append(") Tj ET\n");
        }

        public float Medir(string texto, float tamanio, FuentePdf06AV fuente, float espaciado = 0f)
        {
            if (string.IsNullOrEmpty(texto)) return 0f;
            short[] tabla = fuente == FuentePdf06AV.Negrita ? AnchosNegrita : AnchosNormal;
            byte[] bytes = WinAnsi.GetBytes(texto);
            int total = 0;
            foreach (byte b in bytes) total += b < 32 ? 0 : tabla[b - 32];
            return total * tamanio / 1000f + espaciado * bytes.Length;
        }

        public string Recortar(string texto, float anchoMaximo, float tamanio, FuentePdf06AV fuente)
        {
            if (string.IsNullOrEmpty(texto) || Medir(texto, tamanio, fuente) <= anchoMaximo) return texto ?? "";
            const string puntos = "...";
            string s = texto;
            while (s.Length > 0 && Medir(s + puntos, tamanio, fuente) > anchoMaximo) s = s.Substring(0, s.Length - 1);
            return s.TrimEnd() + puntos;
        }

        public List<string> Envolver(string texto, float anchoMaximo, float tamanio, FuentePdf06AV fuente)
        {
            var lineas = new List<string>();
            if (string.IsNullOrWhiteSpace(texto)) return lineas;
            string actual = "";
            foreach (string palabra in texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (Medir(prueba, tamanio, fuente) <= anchoMaximo || actual.Length == 0)
                {
                    actual = prueba;
                }
                else
                {
                    lineas.Add(actual);
                    actual = palabra;
                }
            }
            if (actual.Length > 0) lineas.Add(actual);
            for (int i = 0; i < lineas.Count; i++) lineas[i] = Recortar(lineas[i], anchoMaximo, tamanio, fuente);
            return lineas;
        }

        public void Guardar(string ruta, string titulo)
        {
            if (_paginas.Count == 0) NuevaPagina();
            int n = _paginas.Count;
            int objFuentes = 3;
            int objImagen = objFuentes + 3;
            int objMascara = objImagen + 1;
            int objInfo = objMascara + 1;
            int primeraPagina = objInfo + 1;

            var objetos = new List<byte[]>();
            var kids = new StringBuilder();
            for (int i = 0; i < n; i++) kids.Append(primeraPagina + i * 2).Append(" 0 R ");

            objetos.Add(Ascii("<< /Type /Catalog /Pages 2 0 R >>"));
            objetos.Add(Ascii("<< /Type /Pages /Kids [" + kids + "] /Count " + n + " >>"));
            objetos.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
            objetos.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
            objetos.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique /Encoding /WinAnsiEncoding >>"));

            if (_jpeg != null)
            {
                var img = new MemoryStream();
                Escribir(img, "<< /Type /XObject /Subtype /Image /Width " + _jpegAncho + " /Height " + _jpegAlto +
                              " /ColorSpace /DeviceRGB /BitsPerComponent 8 /SMask " + objMascara + " 0 R /Filter /DCTDecode /Length " + _jpeg.Length +
                              " >>\nstream\n");
                img.Write(_jpeg, 0, _jpeg.Length);
                Escribir(img, "\nendstream");
                objetos.Add(img.ToArray());

                var mascara = new MemoryStream();
                Escribir(mascara, "<< /Type /XObject /Subtype /Image /Width " + _jpegAncho + " /Height " + _jpegAlto +
                                  " /ColorSpace /DeviceGray /BitsPerComponent 8 /Length " + _mascara.Length + " >>\nstream\n");
                mascara.Write(_mascara, 0, _mascara.Length);
                Escribir(mascara, "\nendstream");
                objetos.Add(mascara.ToArray());
            }
            else
            {
                objetos.Add(Ascii("null"));
                objetos.Add(Ascii("null"));
            }

            objetos.Add(Ascii("<< /Title (" + Cadena(titulo ?? "") + ") /Producer (PC Forge) /CreationDate (D:" +
                              DateTime.Now.ToString("yyyyMMddHHmmss", Inv) + ") >>"));

            string recursos = "/Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >>" +
                              (_jpeg != null ? " /XObject << /Im1 " + objImagen + " 0 R >>" : "") + " >>";
            for (int i = 0; i < n; i++)
            {
                int contenido = primeraPagina + i * 2 + 1;
                objetos.Add(Ascii("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + N(Ancho) + " " + N(Alto) + "] " +
                                  recursos + " /Contents " + contenido + " 0 R >>"));
                byte[] cuerpo = Ascii(_paginas[i].ToString());
                var ms = new MemoryStream();
                Escribir(ms, "<< /Length " + cuerpo.Length + " >>\nstream\n");
                ms.Write(cuerpo, 0, cuerpo.Length);
                Escribir(ms, "\nendstream");
                objetos.Add(ms.ToArray());
            }

            var salida = new MemoryStream();
            Escribir(salida, "%PDF-1.4\n");
            salida.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A }, 0, 6);
            var offsets = new long[objetos.Count + 1];
            for (int i = 0; i < objetos.Count; i++)
            {
                offsets[i + 1] = salida.Position;
                Escribir(salida, (i + 1) + " 0 obj\n");
                salida.Write(objetos[i], 0, objetos[i].Length);
                Escribir(salida, "\nendobj\n");
            }
            long xref = salida.Position;
            var sb = new StringBuilder();
            sb.Append("xref\n0 ").Append(objetos.Count + 1).Append('\n');
            sb.Append("0000000000 65535 f \n");
            for (int i = 1; i <= objetos.Count; i++) sb.Append(offsets[i].ToString("D10", Inv)).Append(" 00000 n \n");
            sb.Append("trailer\n<< /Size ").Append(objetos.Count + 1).Append(" /Root 1 0 R /Info ")
              .Append(objInfo).Append(" 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
            Escribir(salida, sb.ToString());

            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            File.WriteAllBytes(ruta, salida.ToArray());
        }

        private void Curva(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            _pagina.Append(N(x1)).Append(' ').Append(N(y1)).Append(' ')
                   .Append(N(x2)).Append(' ').Append(N(y2)).Append(' ')
                   .Append(N(x3)).Append(' ').Append(N(y3)).Append(" c\n");
        }

        private void Pintar(Color c, bool trazo)
        {
            _pagina.Append(N(c.R / 255f)).Append(' ').Append(N(c.G / 255f)).Append(' ').Append(N(c.B / 255f))
                   .Append(trazo ? " RG\n" : " rg\n");
        }

        private static string NombreFuente(FuentePdf06AV fuente)
        {
            switch (fuente)
            {
                case FuentePdf06AV.Negrita: return "F2";
                case FuentePdf06AV.Cursiva: return "F3";
                default: return "F1";
            }
        }

        private static string N(float v) => Math.Round(v, 2).ToString("0.##", Inv);

        private static string Cadena(string texto)
        {
            var sb = new StringBuilder();
            foreach (byte b in WinAnsi.GetBytes(texto.Replace("\r", "").Replace("\n", " ")))
            {
                if (b == (byte)'(' || b == (byte)')' || b == (byte)'\\') sb.Append('\\').Append((char)b);
                else if (b < 32 || b > 126) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                else sb.Append((char)b);
            }
            return sb.ToString();
        }

        private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

        private static void Escribir(Stream s, string texto)
        {
            byte[] b = Encoding.ASCII.GetBytes(texto);
            s.Write(b, 0, b.Length);
        }

        private static readonly short[] AnchosNormal =
        {
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
            1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
            333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
            556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584, 556,
            556, 556, 222, 556, 333, 1000, 556, 556, 333, 1000, 667, 333, 1000, 556, 611, 556,
            556, 222, 222, 333, 333, 350, 556, 1000, 333, 1000, 500, 333, 944, 556, 500, 667,
            278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 552,
            400, 549, 333, 333, 333, 576, 537, 333, 333, 333, 365, 556, 834, 834, 834, 611,
            667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
            722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
            556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 549, 611, 556, 556, 556, 556, 500, 556, 500
        };

        private static readonly short[] AnchosNegrita =
        {
            278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
            975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
            333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
            611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584, 556,
            556, 556, 278, 556, 500, 1000, 556, 556, 333, 1000, 667, 333, 1000, 556, 611, 556,
            556, 278, 278, 500, 500, 350, 556, 1000, 333, 1000, 556, 333, 944, 556, 500, 667,
            278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 552,
            400, 549, 333, 333, 333, 576, 556, 333, 333, 333, 365, 556, 834, 834, 834, 611,
            722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
            722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
            556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278,
            611, 611, 611, 611, 611, 611, 611, 549, 611, 611, 611, 611, 611, 556, 611, 556
        };
    }
}
