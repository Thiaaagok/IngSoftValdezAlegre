using System;
using System.Data;
using System.Text;

namespace SER.Integridad
{
    /// <summary>
    /// Aplica el esquema de la especificación sobre el contenido de una tabla:
    ///   DVH = suma de un dígito por REGISTRO (calculado sobre sus columnas).
    ///   DVV = suma de un dígito por COLUMNA (calculado sobre todos sus registros).
    /// El dígito por grupo lo produce la estrategia <see cref="ICalculadorDigito06AV"/>.
    /// </summary>
    public sealed class MotorDigitoVerificador06AV
    {
        private readonly ICalculadorDigito06AV _calculador;

        public MotorDigitoVerificador06AV(ICalculadorDigito06AV calculador)
        {
            _calculador = calculador ?? throw new ArgumentNullException(nameof(calculador));
        }

        public MotorDigitoVerificador06AV() : this(new CalculadorHexadecimal06AV()) { }

        public void Calcular(DataTable tabla, out long dvh, out long dvv)
        {
            dvh = 0;
            dvv = 0;
            if (tabla == null) return;

            foreach (DataRow fila in tabla.Rows)
            {
                var sb = new StringBuilder();
                foreach (DataColumn col in tabla.Columns)
                    sb.Append(Normalizar(fila[col])).Append('|');
                dvh += Numero(_calculador.Calcular(sb.ToString()));
            }

            foreach (DataColumn col in tabla.Columns)
            {
                var sb = new StringBuilder();
                foreach (DataRow fila in tabla.Rows)
                    sb.Append(Normalizar(fila[col])).Append('|');
                dvv += Numero(_calculador.Calcular(sb.ToString()));
            }
        }

        private static string Normalizar(object valor)
        {
            return (valor == null || valor == DBNull.Value) ? string.Empty : valor.ToString();
        }

        // Si el dígito es un hexadecimal muy largo (p. ej. SHA-256) se degrada a la
        // suma de code-points para no desbordar el long.
        private static long Numero(string digito)
        {
            if (string.IsNullOrEmpty(digito)) return 0;
            try
            {
                return Convert.ToInt64(digito, 16);
            }
            catch
            {
                long s = 0;
                foreach (char c in digito) s += c;
                return s;
            }
        }
    }
}
