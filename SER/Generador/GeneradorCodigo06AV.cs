using System;

namespace SER.Generador
{
    /// <summary>
    /// Generador de códigos de negocio con prefijo y año, ej. "OC-2026-0001".
    /// El correlativo (número secuencial por prefijo) lo provee la capa de persistencia;
    /// esta clase solo compone el string con el formato acordado.
    /// Prefijos sugeridos: OC (orden compra), OP (orden producción), CO (cotización),
    /// PC (computadora), FC (factura compra), FV (factura venta), RC (recibo).
    /// </summary>
    public class GeneradorCodigo06AV
    {
        /// <summary>Código con correlativo, ej. Generar("OC", 1) → "OC-2026-0001".</summary>
        public string Generar(string prefijo, int correlativo)
            => $"{prefijo}-{DateTime.Now:yyyy}-{correlativo:0000}";

        /// <summary>
        /// Código único sin correlativo (usa marca de tiempo). Útil como PK cuando no se
        /// dispone del siguiente número secuencial en el momento.
        /// </summary>
        public string Generar(string prefijo)
            => $"{prefijo}-{DateTime.Now:yyyyMMddHHmmssfff}";
    }
}
