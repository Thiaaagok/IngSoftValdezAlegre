using System;
using System.Text;

namespace SER.Integridad
{
    public sealed class CalculadorHexadecimal06AV : ICalculadorDigito06AV
    {
        public string Calcular(string datos)
        {
            if (string.IsNullOrEmpty(datos))
                return "0";

            long suma = 0;
            foreach (char c in datos)
                suma += c;

            return suma.ToString("X");
        }
    }

    public sealed class CalculadorModulo1106AV : ICalculadorDigito06AV
    {
        public string Calcular(string datos)
        {
            if (string.IsNullOrEmpty(datos))
                return "0";

            long suma = 0;
            int peso = 2;
            for (int i = datos.Length - 1; i >= 0; i--)
            {
                suma += datos[i] * peso;
                peso = peso == 7 ? 2 : peso + 1;
            }

            int resto = (int)(suma % 11);
            int dv = 11 - resto;
            if (dv >= 10) dv = 0;

            return dv.ToString();
        }
    }
    public sealed class CalculadorSha25606AV : ICalculadorDigito06AV
    {
        public string Calcular(string datos)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(datos ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
