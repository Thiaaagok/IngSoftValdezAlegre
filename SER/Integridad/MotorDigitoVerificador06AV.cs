using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SER.Integridad
{
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

            var filas = new List<DataRow>();
            foreach (DataRow f in tabla.Rows)
                filas.Add(f);

            string ClaveFila(DataRow f)
            {
                var sb = new StringBuilder();
                foreach (DataColumn col in tabla.Columns)
                    sb.Append(Normalizar(f[col])).Append('|');
                return sb.ToString();
            }

            filas.Sort((a, b) => string.CompareOrdinal(ClaveFila(a), ClaveFila(b)));

            foreach (DataRow fila in filas)
                dvh += Numero(_calculador.Calcular(ClaveFila(fila)));

            foreach (DataColumn col in tabla.Columns)
            {
                var sb = new StringBuilder();
                foreach (DataRow fila in filas)
                    sb.Append(Normalizar(fila[col])).Append('|');
                dvv += Numero(_calculador.Calcular(sb.ToString()));
            }
        }

        private static string Normalizar(object valor)
        {
            return (valor == null || valor == DBNull.Value) ? string.Empty : valor.ToString();
        }

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
