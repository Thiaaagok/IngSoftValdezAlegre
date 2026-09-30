using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

namespace SER.Exportacion
{
    public class ExportacionPDF
    {
        private List<string[]> _filas;
        private string[] _encabezados;
        private string _titulo;
        private int _filaActual;
        private float[] _anchosColumnas;

        private float[] _pesosDefault = null;

        public void Exportar<T>(
            IEnumerable<T> datos,
            Dictionary<string, Func<T, object>> columnas,
            string rutaArchivo,
            string titulo = "Reporte",
            float[] pesosColumnas = null)
        {
            _titulo = titulo;
            _encabezados = columnas.Keys.ToArray();
            _filas = new List<string[]>();
            _pesosDefault = pesosColumnas;

            foreach (var item in datos)
            {
                var fila = new string[columnas.Count];
                int i = 0;
                foreach (var col in columnas.Values)
                    fila[i++] = col(item)?.ToString() ?? "";
                _filas.Add(fila);
            }

            _filaActual = 0;
            _anchosColumnas = null;

            var pd = new PrintDocument
            {
                PrinterSettings = new PrinterSettings
                {
                    PrinterName = "Microsoft Print to PDF",
                    PrintToFile = true,
                    PrintFileName = rutaArchivo
                },
                DefaultPageSettings = { Landscape = true }
            };

            pd.PrintPage += Pd_PrintPage;
            pd.Print();
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            float y = e.MarginBounds.Top;
            float x = e.MarginBounds.Left;
            float ancho = e.MarginBounds.Width;

            var fontTitulo = new Font("Segoe UI", 16, FontStyle.Bold);
            var fontHeader = new Font("Segoe UI", 9, FontStyle.Bold);
            var fontCelda = new Font("Segoe UI", 8);

            if (_anchosColumnas == null)
            {
                float[] pesos = _pesosDefault ?? GenerarPesosUniformes(_encabezados.Length);
                float sumaPesos = pesos.Sum();
                _anchosColumnas = pesos.Select(p => (p / sumaPesos) * ancho).ToArray();
            }

            if (_filaActual == 0)
            {
                g.DrawString(_titulo, fontTitulo, Brushes.Black, x, y);
                y += 35;
            }

            float altoHeader = 24;
            g.FillRectangle(Brushes.LightGray, x, y, ancho, altoHeader);
            float cx = x;
            for (int i = 0; i < _encabezados.Length; i++)
            {
                g.DrawRectangle(Pens.Black, cx, y, _anchosColumnas[i], altoHeader);
                g.DrawString(_encabezados[i], fontHeader, Brushes.Black,
                    new RectangleF(cx + 3, y + 5, _anchosColumnas[i] - 6, altoHeader));
                cx += _anchosColumnas[i];
            }
            y += altoHeader;

            var formatoCelda = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            };

            while (_filaActual < _filas.Count)
            {
                var fila = _filas[_filaActual];

                float altoFila = 18;
                for (int i = 0; i < fila.Length; i++)
                {
                    var size = g.MeasureString(fila[i], fontCelda,
                        (int)(_anchosColumnas[i] - 6), formatoCelda);
                    if (size.Height + 8 > altoFila)
                        altoFila = size.Height + 8;
                }

                if (y + altoFila > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                cx = x;
                for (int i = 0; i < fila.Length; i++)
                {
                    g.DrawRectangle(Pens.Black, cx, y, _anchosColumnas[i], altoFila);
                    g.DrawString(fila[i], fontCelda, Brushes.Black,
                        new RectangleF(cx + 3, y + 3, _anchosColumnas[i] - 6, altoFila - 6),
                        formatoCelda);
                    cx += _anchosColumnas[i];
                }
                y += altoFila;
                _filaActual++;
            }

            e.HasMorePages = false;
        }

        private float[] GenerarPesosUniformes(int cantidad)
        {
            var pesos = new float[cantidad];
            for (int i = 0; i < cantidad; i++) pesos[i] = 1;
            return pesos;
        }
    }
}
