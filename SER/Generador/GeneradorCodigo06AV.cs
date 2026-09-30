using System;

namespace SER.Generador
{
    public class GeneradorCodigo06AV
    {
        public string Generar(string prefijo, int correlativo)
            => $"{prefijo}-{DateTime.Now:yyyy}-{correlativo:0000}";

        public string Generar(string prefijo)
            => $"{prefijo}-{DateTime.Now:yyyyMMddHHmmssfff}";
    }
}
