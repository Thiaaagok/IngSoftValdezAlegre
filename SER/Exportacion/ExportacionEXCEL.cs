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

            sb.AppendLine(string.Join("\t", columnas.Keys));

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

            File.WriteAllText(rutaArchivo, sb.ToString(), Encoding.Unicode);
        }

        private string Escapar(string valor)
        {
            if (valor.Contains("\"") || valor.Contains("\n") || valor.Contains("\t"))
            {
                valor = valor.Replace("\"", "\"\"");
                return $"\"{valor}\"";
            }
            return valor;
        }
    }
}
