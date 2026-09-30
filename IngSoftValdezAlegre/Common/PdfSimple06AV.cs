using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IngSoftValdezAlegre.Common
{
    public static class PdfSimple06AV
    {
        private const int CodePageWinAnsi = 1252;

        public static void Guardar(string ruta, string titulo, IList<string> lineas)
        {
            Encoding enc = Encoding.GetEncoding(CodePageWinAnsi);

            var cs = new StringBuilder();
            cs.Append("BT\n");
            cs.Append("/F1 16 Tf\n");
            cs.Append("1 0 0 1 60 770 Tm\n");
            cs.Append("(").Append(Escapar(titulo)).Append(") Tj\n");
            cs.Append("/F1 11 Tf\n");
            cs.Append("0 -30 Td\n");
            cs.Append("15 TL\n");
            bool primera = true;
            foreach (string l in lineas)
            {
                if (!primera) cs.Append("T*\n");
                cs.Append("(").Append(Escapar(l)).Append(") Tj\n");
                primera = false;
            }
            cs.Append("ET");
            string content = cs.ToString();
            int contentLen = enc.GetByteCount(content);

            var objetos = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                    "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
                "<< /Length " + contentLen + " >>\nstream\n" + content + "\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
            };

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");
            var offsets = new int[objetos.Count + 1];
            for (int i = 0; i < objetos.Count; i++)
            {
                offsets[i + 1] = enc.GetByteCount(sb.ToString());
                sb.Append(i + 1).Append(" 0 obj\n").Append(objetos[i]).Append("\nendobj\n");
            }

            int xrefPos = enc.GetByteCount(sb.ToString());
            sb.Append("xref\n");
            sb.Append("0 ").Append(objetos.Count + 1).Append('\n');
            sb.Append("0000000000 65535 f \n");
            for (int i = 1; i <= objetos.Count; i++)
                sb.Append(offsets[i].ToString("D10")).Append(" 00000 n \n");
            sb.Append("trailer\n<< /Size ").Append(objetos.Count + 1).Append(" /Root 1 0 R >>\n");
            sb.Append("startxref\n").Append(xrefPos).Append("\n%%EOF");

            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            File.WriteAllBytes(ruta, enc.GetBytes(sb.ToString()));
        }

        private static string Escapar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)")
                    .Replace("\r", "")
                    .Replace("\n", " ");
        }
    }
}
