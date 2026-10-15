using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal abstract class DocumentoPdf06AV
    {
        protected static readonly Color Oscuro = Color.FromArgb(18, 22, 26);
        protected static readonly Color Grafito = Color.FromArgb(36, 43, 50);
        protected static readonly Color Cian = Color.FromArgb(2, 201, 242);
        protected static readonly Color CianTexto = Color.FromArgb(0, 128, 163);
        protected static readonly Color CianSuave = Color.FromArgb(232, 248, 253);
        protected static readonly Color Naranja = Color.FromArgb(249, 101, 1);
        protected static readonly Color NaranjaTexto = Color.FromArgb(204, 82, 0);
        protected static readonly Color NaranjaSuave = Color.FromArgb(254, 241, 231);
        protected static readonly Color Verde = Color.FromArgb(24, 140, 80);
        protected static readonly Color VerdeSuave = Color.FromArgb(227, 245, 234);
        protected static readonly Color Rojo = Color.FromArgb(200, 40, 45);
        protected static readonly Color RojoSuave = Color.FromArgb(252, 228, 228);
        protected static readonly Color ColorMarcaAgua = Color.FromArgb(250, 222, 222);
        protected static readonly Color Tinta = Color.FromArgb(28, 33, 38);
        protected static readonly Color Gris = Color.FromArgb(104, 113, 122);
        protected static readonly Color GrisClaro = Color.FromArgb(165, 173, 181);
        protected static readonly Color Borde = Color.FromArgb(222, 227, 232);
        protected static readonly Color Fondo = Color.FromArgb(246, 248, 250);
        protected static readonly Color Blanco = Color.White;

        protected const float M = 40f;
        protected const float AnchoUtil = PdfLienzo06AV.Ancho - 2 * M;
        protected const float Derecha = PdfLienzo06AV.Ancho - M;
        protected const float LineaPie = PdfLienzo06AV.Alto - 36f;
        protected const float LimiteContenido = LineaPie - 14f;
        protected const float AltoFila = 30f;
        protected const float AnchoResumen = 205f;
        protected const float AnchoIzquierda = AnchoUtil - AnchoResumen - 20f;
        protected const float AnchoTarjeta = (AnchoUtil - 14f) / 2f;

        protected static readonly float[] Columnas = { 40f, AnchoUtil - 40f - 78f - 80f - 86f, 78f, 80f, 86f };

        protected readonly PdfLienzo06AV _pdf = new PdfLienzo06AV();
        protected readonly GestorIdioma06AV _t = GestorIdioma06AV.Instancia;
        protected readonly DateTime _emision = DateTime.Now;

        protected abstract string Titulo { get; }

        protected abstract string Numero { get; }

        protected virtual string NumeroAlternativo => string.Empty;

        protected virtual string MarcaAgua => null;

        protected virtual string ClavePie => "pcf_doc_generado";

        protected sealed class ItemPdf
        {
            public int Cantidad;
            public string Descripcion;
            public string Detalle;
            public string Codigo;
            public decimal Unitario;
            public decimal Importe => Unitario * Cantidad;
        }

        protected void Finalizar(string ruta)
        {
            int total = _pdf.CantidadPaginas;
            for (int i = 1; i <= total; i++)
            {
                _pdf.IrAPagina(i);
                PiePagina(i, total);
            }
            _pdf.Guardar(ruta, Titulo + (string.IsNullOrWhiteSpace(Numero) ? "" : " " + Numero));
        }

        protected void NuevaPagina()
        {
            _pdf.NuevaPagina();
            if (!string.IsNullOrWhiteSpace(MarcaAgua))
                _pdf.TextoRotado(MarcaAgua, PdfLienzo06AV.Ancho / 2f, PdfLienzo06AV.Alto / 2f + 40f,
                                 96f, 35f, FuentePdf06AV.Negrita, ColorMarcaAgua, 8f);
        }

        protected float PaginaContinuacion()
        {
            NuevaPagina();
            const float alto = 48f;
            _pdf.Rect(0, 0, PdfLienzo06AV.Ancho, alto, Oscuro);
            _pdf.Rect(0, alto, PdfLienzo06AV.Ancho, 2.5f, Cian);
            _pdf.Rect(0, alto, 150f, 2.5f, Naranja);
            _pdf.Texto("PC", M, 30f, 14f, FuentePdf06AV.Negrita, Cian);
            _pdf.Texto("FORGE", M + _pdf.Medir("PC ", 14f, FuentePdf06AV.Negrita), 30f, 14f, FuentePdf06AV.Negrita, Naranja);
            string derecha = Titulo + (string.IsNullOrWhiteSpace(Numero) ? "" : "  ·  " + _t.Obtener("pcf_doc_nro") + " " + Numero);
            _pdf.Texto(derecha, Derecha, 29f, 9f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha, 0.6f);
            return alto + 26f;
        }

        protected float EncabezadoCompleto()
        {
            const float alto = 128f;
            _pdf.Rect(0, 0, PdfLienzo06AV.Ancho, alto, Oscuro);

            if (_pdf.TieneImagen)
            {
                float altoLogo = 104f;
                float anchoLogo = altoLogo * _pdf.ProporcionImagen;
                _pdf.Imagen(M - 10f, (alto - altoLogo) / 2f, anchoLogo, altoLogo);
            }
            else
            {
                _pdf.Texto("PC", M, 72f, 26f, FuentePdf06AV.Negrita, Cian);
                _pdf.Texto("FORGE", M + _pdf.Medir("PC ", 26f, FuentePdf06AV.Negrita), 72f, 26f, FuentePdf06AV.Negrita, Naranja);
            }

            _pdf.Texto(Titulo, Derecha, 54f, 25f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha, 1.5f);

            string numero = string.IsNullOrWhiteSpace(Numero)
                ? NumeroAlternativo
                : _t.Obtener("pcf_doc_nro") + " " + Numero;
            _pdf.Texto(numero, Derecha, 75f, 11.5f, FuentePdf06AV.Negrita, Cian, AlineacionPdf06AV.Derecha, 0.4f);

            float x = Derecha;
            string fecha = _emision.ToString("dd/MM/yyyy");
            _pdf.Texto(fecha, x, 95f, 9f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            x -= _pdf.Medir(fecha, 9f, FuentePdf06AV.Negrita) + 6f;
            _pdf.Texto(_t.Obtener("pcf_doc_emision"), x, 95f, 8.5f, FuentePdf06AV.Normal, GrisClaro, AlineacionPdf06AV.Derecha);

            _pdf.Texto("PC Forge  ·  PC Factory", Derecha, 110f, 8f, FuentePdf06AV.Normal, GrisClaro, AlineacionPdf06AV.Derecha, 0.3f);

            _pdf.Rect(0, alto, PdfLienzo06AV.Ancho, 3f, Cian);
            _pdf.Rect(0, alto, 190f, 3f, Naranja);
            return alto + 3f;
        }

        protected float DibujarTarjetas(float y, string etiquetaIzquierda, string titulo, IList<string> lineas,
                                        string etiquetaDerecha, IList<KeyValuePair<string, string>> datos)
        {
            float ancho = AnchoTarjeta;
            int filas = Math.Max(lineas.Count + 1, datos.Count);
            float alto = 42f + (filas - 1) * 15f + 16f;

            float xi = M, xd = M + ancho + 14f;
            _pdf.RectRedondeado(xi, y, ancho, alto, 6f, Fondo, Borde);
            _pdf.RectRedondeado(xd, y, ancho, alto, 6f, Fondo, Borde);

            Etiqueta(etiquetaIzquierda, xi + 14f, y + 20f);
            Etiqueta(etiquetaDerecha, xd + 14f, y + 20f);

            _pdf.Texto(_pdf.Recortar(string.IsNullOrWhiteSpace(titulo) ? "-" : titulo, ancho - 28f, 12f, FuentePdf06AV.Negrita),
                       xi + 14f, y + 41f, 12f, FuentePdf06AV.Negrita, Tinta);
            float ly = y + 41f;
            foreach (string l in lineas)
            {
                ly += 15f;
                _pdf.Texto(_pdf.Recortar(l, ancho - 28f, 9f, FuentePdf06AV.Normal), xi + 14f, ly, 9f, FuentePdf06AV.Normal, Gris);
            }

            float dy = y + 41f;
            foreach (var d in datos)
            {
                _pdf.Texto(d.Key, xd + 14f, dy, 8.5f, FuentePdf06AV.Normal, Gris);
                float libre = ancho - 28f - _pdf.Medir(d.Key, 8.5f, FuentePdf06AV.Normal) - 10f;
                _pdf.Texto(_pdf.Recortar(d.Value, libre, 9f, FuentePdf06AV.Negrita), xd + ancho - 14f, dy, 9f,
                           FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                dy += 15f;
            }
            return y + alto;
        }

        protected float EncabezadoTabla(float y)
        {
            _pdf.RectRedondeado(M, y, AnchoUtil, 24f, 4f, Grafito);
            _pdf.Rect(M, y + 12f, AnchoUtil, 12f, Grafito);
            string[] titulos =
            {
                _t.Obtener("pcf_doc_col_cant"), _t.Obtener("pcf_doc_col_desc"), _t.Obtener("pcf_doc_col_codigo"),
                _t.Obtener("pcf_doc_col_unit"), _t.Obtener("pcf_doc_col_importe")
            };
            float x = M;
            for (int i = 0; i < titulos.Length; i++)
            {
                float w = Columnas[i];
                var al = i == 0 ? AlineacionPdf06AV.Centro : i >= 3 ? AlineacionPdf06AV.Derecha : AlineacionPdf06AV.Izquierda;
                float tx = al == AlineacionPdf06AV.Centro ? x + w / 2f : al == AlineacionPdf06AV.Derecha ? x + w - 12f : x + 12f;
                _pdf.Texto(titulos[i], tx, y + 15.5f, 7f, FuentePdf06AV.Negrita, Blanco, al, 0.7f);
                x += w;
            }
            return y + 24f;
        }

        protected float TablaItems(float y, IList<ItemPdf> items, string textoVacio)
        {
            y = EncabezadoTabla(y);
            if (items.Count == 0)
            {
                _pdf.Rect(M, y, AnchoUtil, AltoFila, Fondo);
                _pdf.Texto(textoVacio, M + 12f, y + 19f, 9f, FuentePdf06AV.Cursiva, Gris);
                return y + AltoFila;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (y + AltoFila > LimiteContenido)
                    y = EncabezadoTabla(PaginaContinuacion());

                var it = items[i];
                if (i % 2 == 1) _pdf.Rect(M, y, AnchoUtil, AltoFila, Fondo);
                _pdf.Linea(M, y + AltoFila, Derecha, y + AltoFila, Borde, 0.6f);

                float x = M;
                _pdf.Texto(it.Cantidad.ToString(), x + Columnas[0] / 2f, y + 19f, 9.5f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Centro);
                x += Columnas[0];

                float anchoDesc = Columnas[1] - 20f;
                if (string.IsNullOrWhiteSpace(it.Detalle))
                {
                    _pdf.Texto(_pdf.Recortar(it.Descripcion, anchoDesc, 9f, FuentePdf06AV.Negrita), x + 12f, y + 19f, 9f, FuentePdf06AV.Negrita, Tinta);
                }
                else
                {
                    _pdf.Texto(_pdf.Recortar(it.Descripcion, anchoDesc, 9f, FuentePdf06AV.Negrita), x + 12f, y + 13.5f, 9f, FuentePdf06AV.Negrita, Tinta);
                    _pdf.Texto(_pdf.Recortar(it.Detalle, anchoDesc, 7.5f, FuentePdf06AV.Normal), x + 12f, y + 24f, 7.5f, FuentePdf06AV.Normal, Gris);
                }
                x += Columnas[1];

                _pdf.Texto(_pdf.Recortar(it.Codigo, Columnas[2] - 16f, 8f, FuentePdf06AV.Normal), x + 12f, y + 19f, 8f, FuentePdf06AV.Normal, Gris);
                x += Columnas[2];
                _pdf.Texto(Plata(it.Unitario), x + Columnas[3] - 12f, y + 19f, 9f, FuentePdf06AV.Normal, Tinta, AlineacionPdf06AV.Derecha);
                x += Columnas[3];
                _pdf.Texto(Plata(it.Importe), x + Columnas[4] - 12f, y + 19f, 9f, FuentePdf06AV.Negrita, Tinta, AlineacionPdf06AV.Derecha);
                y += AltoFila;
            }
            return y;
        }

        protected void PiePagina(int pagina, int total)
        {
            _pdf.Linea(M, LineaPie, Derecha, LineaPie, Borde, 0.8f);
            _pdf.Rect(M, LineaPie - 1f, 40f, 2f, Cian);
            string izq = "PC Forge  ·  PC Factory   —   " + _t.Obtener(ClavePie, _emision.ToString("dd/MM/yyyy HH:mm"));
            _pdf.Texto(izq, M, LineaPie + 14f, 7.5f, FuentePdf06AV.Normal, GrisClaro);
            _pdf.Texto(_t.Obtener("pcf_doc_pagina", pagina, total), Derecha, LineaPie + 14f, 7.5f, FuentePdf06AV.Normal,
                       GrisClaro, AlineacionPdf06AV.Derecha);
        }

        protected float FilaResumen(string etiqueta, string valor, float x, float y, Color color, bool negrita = false)
        {
            var fuente = negrita ? FuentePdf06AV.Negrita : FuentePdf06AV.Normal;
            float anchoValor = _pdf.Medir(valor, 9.5f, fuente);
            _pdf.Texto(_pdf.Recortar(etiqueta, AnchoResumen - anchoValor - 22f, 9f, FuentePdf06AV.Normal), x + 12f, y + 13f, 9f,
                       FuentePdf06AV.Normal, Gris);
            _pdf.Texto(valor, x + AnchoResumen - 12f, y + 13f, 9.5f, fuente, color, AlineacionPdf06AV.Derecha);
            return y + 20f;
        }

        protected float BarraTotal(string etiqueta, string valor, float x, float y)
        {
            float anchoValor = _pdf.Medir(valor, 15f, FuentePdf06AV.Negrita);
            float anchoEtiqueta = _pdf.Medir(etiqueta, 8f, FuentePdf06AV.Negrita, 1f);
            bool unaLinea = 14f + anchoEtiqueta + 12f + anchoValor + 12f <= AnchoResumen;
            float alto = unaLinea ? 38f : 50f;
            _pdf.RectRedondeado(x, y, AnchoResumen, alto, 5f, Oscuro);
            _pdf.Rect(x, y + 8f, 3.5f, alto - 16f, Naranja);
            if (unaLinea)
            {
                _pdf.Texto(etiqueta, x + 14f, y + 23f, 8f, FuentePdf06AV.Negrita, GrisClaro, AlineacionPdf06AV.Izquierda, 1f);
                _pdf.Texto(valor, x + AnchoResumen - 12f, y + 24.5f, 15f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            }
            else
            {
                _pdf.Texto(_pdf.Recortar(etiqueta, AnchoResumen - 26f, 8f, FuentePdf06AV.Negrita), x + 14f, y + 18f, 8f,
                           FuentePdf06AV.Negrita, GrisClaro, AlineacionPdf06AV.Izquierda, 1f);
                _pdf.Texto(valor, x + AnchoResumen - 12f, y + 38f, 15f, FuentePdf06AV.Negrita, Blanco, AlineacionPdf06AV.Derecha);
            }
            return y + alto;
        }

        protected void Etiqueta(string texto, float x, float linea)
        {
            _pdf.Texto(texto.ToUpper(), x, linea, 7f, FuentePdf06AV.Negrita, CianTexto, AlineacionPdf06AV.Izquierda, 1.1f);
        }

        protected void Pastilla(string texto, float x, float y, Color fondo, Color color, bool grande = false)
        {
            float tam = grande ? 8f : 7f;
            float alto = grande ? 20f : 15f;
            float ancho = _pdf.Medir(texto, tam, FuentePdf06AV.Negrita, 0.6f) + (grande ? 24f : 16f);
            _pdf.RectRedondeado(x, y, ancho, alto, alto / 2f, fondo);
            _pdf.Texto(texto, x + ancho / 2f, y + alto / 2f + tam * 0.36f, tam, FuentePdf06AV.Negrita, color, AlineacionPdf06AV.Centro, 0.6f);
        }

        protected static KeyValuePair<string, string> Par(string k, string v) => new KeyValuePair<string, string>(k, v ?? "-");

        protected static string Plata(decimal v) => v.ToString("C2", CultureInfo.CurrentCulture);
    }
}
