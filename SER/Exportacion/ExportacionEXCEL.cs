using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SER.Exportacion
{
    public class ExportacionEXCEL
    {
        public void Exportar<T>(
            IEnumerable<T> datos,
            Dictionary<string, Func<T, object>> columnas,
            string rutaArchivo)
        {
            var sb = new StringBuilder();

            // Encabezados separados por TAB
            sb.AppendLine(string.Join("\t", columnas.Keys));

            // Filas
            foreach (var item in datos)
            {
                var valores = new List<string>();
                foreach (var col in columnas.Values)
                {
                    var valor = col(item)?.ToString() ?? "";
                    valores.Add(Escapar(valor));
                }
                sb.AppendLine(string.Join("\t", valores));
            }

            // UTF-16 (Unicode) — Excel lo detecta perfecto, sin sep=; ni BOM raros
            File.WriteAllText(rutaArchivo, sb.ToString(), Encoding.Unicode);
        }

        private string Escapar(string valor)
        {
            // Con tabs como separador, solo escapamos comillas y saltos de línea
            if (valor.Contains("\"") || valor.Contains("\n") || valor.Contains("\t"))
            {
                valor = valor.Replace("\"", "\"\"");
                return $"\"{valor}\"";
            }
            return valor;
        }
    }
}
/*
==========================================================================
EXPORTACIÓN EXCEL — Explicación de funcionamiento
==========================================================================

VISIÓN GENERAL
--------------
ExportacionEXCEL es una clase genérica que recibe cualquier colección de
objetos y genera un archivo tabular que Excel puede abrir directamente.
Es genérica porque usa <T>, así sirve para Bitácora, Usuarios o cualquier
módulo: vos le decís cómo extraer cada columna del objeto y la clase
hace el resto.

La estrategia NO es generar un .xlsx real (que sería un ZIP con XMLs
adentro). Genera un archivo de TEXTO con valores separados por
TABULADORES (TSV — Tab Separated Values), codificado en UTF-16, que
Excel reconoce y abre como una planilla, ubicando cada valor en su celda
correspondiente.

Esto evita depender de librerías externas (OpenXML, EPPlus, ClosedXML)
y mantiene el código simple y portable.


POR QUÉ TAB (\t) Y NO COMA O PUNTO Y COMA
------------------------------------------
- COMA (,): Excel en inglés la usa como separador, pero Excel en español
  usa el PUNTO Y COMA. Si guardamos con coma, en máquinas en español
  todas las columnas aparecen pegadas en la primera celda.
- PUNTO Y COMA (;): al revés, falla en Excel en inglés.
- TABULADOR (\t): es universal. Excel SIEMPRE respeta tabuladores como
  separador de columnas, sin importar el idioma del sistema operativo
  ni la configuración regional.


POR QUÉ UTF-16 (Encoding.Unicode) Y NO UTF-8
--------------------------------------------
Excel tiene un comportamiento confuso con UTF-8: a veces lo detecta y a
veces no, dependiendo de la versión y del SO. Cuando no lo detecta, los
acentos y eñes aparecen como caracteres raros (Ã³ en vez de ó, Ã±
en vez de ñ, etc).

UTF-16 (también llamado "Unicode" en .NET) tiene un BOM
(Byte Order Mark) muy claro al inicio del archivo (los bytes FF FE) que
Excel SIEMPRE detecta correctamente, sin ambigüedad. Resultado: acentos
y eñes se ven perfecto en cualquier Excel.

El costo es que el archivo pesa el doble que en UTF-8 (cada caracter
ocupa 2 bytes en lugar de 1 para ASCII), pero para una bitácora o
listado de usuarios es despreciable.


MÉTODO Exportar<T>
------------------
Parámetros:
- datos: la colección de objetos a exportar.
- columnas: diccionario donde la CLAVE es el nombre del encabezado
  ("DNI", "Evento") y el VALOR es una función Func<T, object> que dice
  cómo sacar ese dato del objeto. Ej: b => b.UsuarioDni.
  Este patrón es lo que hace la clase reutilizable: cada módulo decide
  qué mostrar sin tocar la clase.
- rutaArchivo: dónde guardar el archivo.

Pasos del método:

1) Construir los encabezados.
   string.Join("\t", columnas.Keys) une los nombres de columna separados
   por tabulador. Después AppendLine agrega un salto de línea al final.
   Ejemplo de fila de encabezados generada:
     "DNI\tFecha\tHora\tMódulo\tEvento\tCriticidad\r\n"

2) Construir las filas de datos.
   Por cada objeto:
   - Recorre las funciones extractoras (columnas.Values).
   - Ejecuta col(item) para obtener el valor.
   - Lo convierte a string (con ?.ToString() ?? "" por si es null).
   - Lo pasa por Escapar (ver más abajo).
   - Junta todos los valores con tabuladores y los agrega al StringBuilder.

3) Escribir el archivo en disco.
   File.WriteAllText con Encoding.Unicode genera el archivo en UTF-16,
   que Excel reconoce nativamente.

POR QUÉ USAR STRINGBUILDER
--------------------------
Concatenar strings con + en un loop es muy ineficiente porque cada
concatenación crea un nuevo string en memoria (los strings son
inmutables en .NET). Con miles de filas eso se nota en performance.

StringBuilder mantiene un buffer interno mutable y solo genera el string
final una vez, al llamar a ToString(). Para una exportación de 5000
filas, la diferencia puede ser de varios segundos vs. milisegundos.


MÉTODO Escapar
--------------
Maneja el caso de que un valor contenga caracteres especiales que
romperían el formato del archivo.

Casos que escapa:
- COMILLAS DOBLES ("): se duplican (" → "") y todo el valor se envuelve
  en comillas. Es el estándar para TSV/CSV.
- SALTO DE LÍNEA (\n): si un valor tiene un enter adentro, sin escape
  Excel lo interpretaría como una fila nueva. Envolverlo en comillas
  le dice a Excel "esto es parte de la misma celda".
- TABULADOR (\t): si un valor contiene un tab adentro, sin escape Excel
  lo interpretaría como una columna nueva. Mismo tratamiento: comillas.

Ejemplo:
  Valor original:    Hola "mundo"
  Valor escapado:    "Hola ""mundo"""

Si el valor no tiene ninguno de estos caracteres, lo devuelve tal cual
(no hace falta escapar).


SOBRE EL DNI CON CEROS A LA IZQUIERDA
-------------------------------------
Excel tiene una "feature" molesta: cuando ve un valor numérico, le
quita los ceros a la izquierda. "00000000" lo muestra como 0.

Solución desde el lado del que usa esta clase: pasar el valor con un
TABULADOR al principio:
  { "DNI", b => "\t" + b.UsuarioDni }
El tab le dice a Excel "esto es texto literal", preserva los ceros, y
visualmente no se ve porque el tab está dentro del valor de la celda.


POR QUÉ ESTE DISEÑO ES BUENO
----------------------------
1) Genérico: <T> + Dictionary<string, Func<T, object>> permite reusar
   la clase para cualquier módulo.
2) Sin dependencias externas: 100% nativo de .NET, sin paquetes NuGet.
3) Universal: funciona en Excel en cualquier idioma (tabs + UTF-16).
4) Performance: StringBuilder + una sola escritura a disco.
5) Robusto: escapa caracteres especiales para no romper el formato.


LIMITACIONES
------------
- No soporta formato (negritas, colores, anchos personalizados, etc).
  Para eso habría que generar XLSX real con OpenXML o similar.
- No soporta múltiples hojas: una sola hoja por archivo.
- Para reportes que necesitan formato visual, conviene usar el PDF.
==========================================================================
*/