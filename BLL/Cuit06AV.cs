using System.Linq;

namespace BLL
{
    public static class Cuit06AV
    {
        private static readonly int[] Pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

        public static string SoloDigitos(string cuit) =>
            new string((cuit ?? "").Where(char.IsDigit).ToArray());

        public static bool TieneFormato(string cuit)
        {
            string d = SoloDigitos(cuit);
            if (d.Length != 11) return false;
            string limpio = (cuit ?? "").Trim();
            return limpio.All(c => char.IsDigit(c) || c == '-' || c == ' ' || c == '.');
        }

        public static int? DigitoVerificador(string primerosDiez)
        {
            if (primerosDiez == null || primerosDiez.Length != 10 || !primerosDiez.All(char.IsDigit)) return null;
            int suma = 0;
            for (int i = 0; i < 10; i++) suma += (primerosDiez[i] - '0') * Pesos[i];
            int dv = 11 - suma % 11;
            if (dv == 11) return 0;
            if (dv == 10) return null;
            return dv;
        }

        public static bool EsValido(string cuit)
        {
            if (!TieneFormato(cuit)) return false;
            string d = SoloDigitos(cuit);
            string tipo = d.Substring(0, 2);
            if (!new[] { "20", "23", "24", "25", "26", "27", "30", "33", "34" }.Contains(tipo)) return false;
            int? dv = DigitoVerificador(d.Substring(0, 10));
            return dv.HasValue && dv.Value == d[10] - '0';
        }

        public static string Formatear(string cuit)
        {
            string d = SoloDigitos(cuit);
            return d.Length == 11 ? d.Substring(0, 2) + "-" + d.Substring(2, 8) + "-" + d.Substring(10, 1) : (cuit ?? "").Trim();
        }
    }
}
