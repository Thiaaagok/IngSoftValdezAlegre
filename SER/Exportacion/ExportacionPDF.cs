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

        // Pesos relativos para distribuir el ancho. Ajustá según tu módulo.
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
            _anchosColumnas = null; // se calcula en el primer PrintPage

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

            // Calcular anchos según pesos (una sola vez)
            if (_anchosColumnas == null)
            {
                float[] pesos = _pesosDefault ?? GenerarPesosUniformes(_encabezados.Length);
                float sumaPesos = pesos.Sum();
                _anchosColumnas = pesos.Select(p => (p / sumaPesos) * ancho).ToArray();
            }

            // Título (solo en primera página)
            if (_filaActual == 0)
            {
                g.DrawString(_titulo, fontTitulo, Brushes.Black, x, y);
                y += 35;
            }

            // Encabezados
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

            // Filas con alto dinámico según el contenido
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

                // Calcular alto necesario para esta fila
                float altoFila = 18; // mínimo
                for (int i = 0; i < fila.Length; i++)
                {
                    var size = g.MeasureString(fila[i], fontCelda,
                        (int)(_anchosColumnas[i] - 6), formatoCelda);
                    if (size.Height + 8 > altoFila)
                        altoFila = size.Height + 8;
                }

                // ¿Entra en esta página?
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
/*
==========================================================================
EXPORTACIÓN PDF — Explicación de funcionamiento
==========================================================================

VISIÓN GENERAL
--------------
ExportacionPDF es una clase genérica que recibe cualquier colección de
objetos y genera un PDF tabular (con encabezados y filas). Es genérica
porque usa <T>, así sirve para Bitácora, Usuarios o cualquier módulo:
vos le decís cómo extraer cada columna del objeto y la clase hace el resto.

La estrategia para generar el PDF NO es escribir bytes del formato PDF a
mano. Usa PrintDocument de WinForms (que sirve para mandar a impresora)
y le indica que la "impresora" sea "Microsoft Print to PDF", un driver
virtual incluido en Windows 10/11 que en vez de imprimir en papel genera
un archivo PDF. Así evitamos depender de librerías externas
(iText, PdfSharp, etc).


CAMPOS PRIVADOS
---------------
_filas, _encabezados, _titulo, _filaActual, _anchosColumnas, _pesosDefault

Guardan el "estado" entre páginas. Lo crítico es entender que
PrintDocument puede llamar al evento PrintPage VARIAS VECES (una por
página), y cada llamada es independiente. Por eso necesitamos guardar:

- _filaActual: en qué fila quedamos cuando terminó la página anterior,
  para arrancar desde ahí en la próxima.
- _anchosColumnas: calculados una sola vez, reutilizados en todas las
  páginas (sino cada página tendría anchos distintos).
- _filas y _encabezados: datos pre-procesados como strings.


MÉTODO Exportar<T>
------------------
Parámetros:
- datos: la colección de objetos a exportar (ej: lista de eventos).
- columnas: diccionario donde la CLAVE es el nombre del encabezado
  ("DNI", "Evento") y el VALOR es una función Func<T, object> que dice
  cómo sacar ese dato del objeto. Ej: b => b.UsuarioDni.
  Este patrón es lo que hace la clase reutilizable: cada módulo decide
  qué mostrar sin tocar la clase.
- rutaArchivo: dónde guardar el PDF.
- titulo: encabezado del reporte.
- pesosColumnas: pesos relativos para distribuir el ancho.

Pasos del método:

1) Preparar los datos como strings.
   Recorre los objetos, ejecuta las funciones extractoras (col(item))
   y guarda los resultados como string en _filas. Así durante el
   dibujado no volvemos a tocar los objetos originales: una sola
   conversión al inicio para ganar performance.

2) Configurar el PrintDocument.
   - PrinterName = "Microsoft Print to PDF": impresora virtual de Windows.
   - PrintToFile = true + PrintFileName: en vez de mandar a una impresora
     física, escribe directo a un archivo.
   - Landscape = true: orientación horizontal, mejor para tablas anchas.

3) Suscribir el evento y disparar la impresión.
   pd.Print() arranca el proceso. El framework llama a Pd_PrintPage por
   cada página hasta que el evento marque e.HasMorePages = false.


MÉTODO Pd_PrintPage (el corazón del dibujado)
---------------------------------------------
Se llama UNA VEZ POR CADA PÁGINA. Su trabajo es dibujar sobre
e.Graphics, que es una superficie gráfica que después se traduce a PDF.

> Posicionamiento:
  MarginBounds es el rectángulo "imprimible" de la página (sin márgenes).
  y lleva la cuenta de la posición vertical actual a medida que dibujamos
  filas hacia abajo.

> Cálculo de anchos de columna:
  Convierte "pesos relativos" en píxeles reales.
  Ejemplo: pesos {1, 1, 0.8, 1.2, 3, 1} suman 8.0
  Si la página tiene 800px de ancho útil:
    - Columna 1 (peso 1):    1/8 * 800 = 100px
    - Columna 5 (peso 3):    3/8 * 800 = 300px  <- "Evento" 3x más ancha
  
  El if (_anchosColumnas == null) asegura que el cálculo se haga UNA SOLA
  VEZ: en la primera página se calcula y cachea; las siguientes lo reusan.

> Título (solo en primera página):
  Como _filaActual arranca en 0 y se va incrementando, solo es 0 en la
  primera página. En las siguientes el título no se vuelve a dibujar.

> Encabezados:
  - Pinta una franja gris de fondo (FillRectangle).
  - Para cada encabezado: dibuja el borde y escribe el texto.
  - cx avanza horizontalmente sumando los anchos ya dibujados.
  - Los +3 y -6 son padding entre borde y texto.
  Los encabezados se dibujan en CADA página, no solo en la primera, así
  el usuario siempre ve los nombres de columna aunque el reporte tenga
  varias páginas.

> Formato de celda con word-wrap:
  StringFormat configurado con:
  - StringAlignment.Near: alineación a la izquierda/arriba.
  - Trimming.Word: si el texto no entra, corta por PALABRAS COMPLETAS.
  - FormatFlags.LineLimit: solo dibuja líneas completas, no parciales.
  Sin esto, DrawString deja que el texto se desborde fuera del rectángulo
  y se pisa con la fila siguiente.


LOOP PRINCIPAL DE FILAS
-----------------------
Por cada fila pendiente:

1) Calcular el alto necesario para esa fila.
   MeasureString es como DrawString pero NO DIBUJA, solo calcula qué
   tamaño ocuparía. Le pasamos texto, fuente, ancho máximo y el mismo
   formatoCelda (importante: el cálculo tiene que considerar el wrap).
   Devuelve un SizeF con el alto necesario. Recorremos todas las celdas
   de la fila y nos quedamos con el ALTO MÁS GRANDE: ese será el alto
   de la fila completa (todas las celdas de una fila igualadas).

2) Salto de página.
   if (y + altoFila > e.MarginBounds.Bottom)
     - e.HasMorePages = true le dice al framework "necesito otra página".
     - return: termina ESTA llamada al PrintPage. El framework llamará
       de nuevo a Pd_PrintPage con la página siguiente, y como
       _filaActual NO se incrementó, retomamos exactamente desde donde
       quedamos.

3) Dibujar la fila.
   Para cada celda: borde + texto con word-wrap.
   Después incrementamos y (posición vertical) y _filaActual.

4) Si terminamos todas las filas: e.HasMorePages = false y se acaba la
   impresión, generando el PDF final.


MÉTODO GenerarPesosUniformes
----------------------------
Si el usuario no pasa pesos personalizados, todas las columnas tienen
el mismo peso (1). Resultado: todas con el mismo ancho.


POR QUÉ ESTE DISEÑO ES BUENO
----------------------------
1) Genérico: <T> + Dictionary<string, Func<T, object>> permite reusar
   la clase para cualquier módulo (Bitácora, Usuarios, etc.).
2) Sin dependencias externas: 100% nativo de .NET / WinForms + Windows.
3) Multi-página automático: el framework de PrintDocument maneja la
   paginación; nosotros solo decimos "necesito más páginas".
4) Word-wrap real: el texto largo no rompe el layout, las filas se
   adaptan en altura al contenido.
5) Anchos personalizables: pesos relativos por columna permiten dar
   más espacio a las columnas que lo necesitan (ej: "Evento").
==========================================================================
*/