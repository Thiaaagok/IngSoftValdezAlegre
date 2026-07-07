using System;
using System.Text;

namespace SER.Integridad
{
    /// <summary>
    /// Cálculo del dígito según la especificación: cada valor se convierte a su
    /// equivalente hexadecimal y se suma. Es aditivo/lineal, lo que permite luego
    /// sumar los dígitos de filas/columnas/tablas sin perder consistencia.
    /// </summary>
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

    /// <summary>Alternativa: dígito verificador clásico por módulo 11 (pesos 2..7).</summary>
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

    /// <summary>
    /// Alternativa robusta por hash SHA-256. No es aditivo, así que sirve para
    /// verificar un bloque completo, no para el esquema agregado DVH/DVV.
    /// </summary>
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
