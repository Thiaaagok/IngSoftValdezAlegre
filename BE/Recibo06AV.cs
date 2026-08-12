using System;

namespace BE
{
    /// <summary>
    /// Recibo de seña (RFN1, paso 3). Se emite cuando el cliente abona la seña sobre una
    /// computadora ya registrada, antes de que exista la orden de producción.
    /// MontoAbonado y SaldoPendiente son SNAPSHOTS del momento de emitir (valor histórico);
    /// el cliente y la computadora se navegan vía Pago.Computadora / Pago.Computadora.Cliente.
    /// </summary>
    public class Recibo06AV
    {
        public string Id { get; set; }                     // PK (GeneradorCodigo06AV, ej. "RC-2026-0001")
        public Pago06AV Pago { get; set; }                 // el pago de seña que originó este recibo
        public DateTime FechaEmision { get; set; }
        public decimal MontoAbonado { get; set; }          // snapshot
        public decimal SaldoPendiente { get; set; }        // snapshot: PrecioTotal - abonado hasta ese momento
        public DateTime FechaEntregaEstimada { get; set; }

        public override string ToString() => $"Recibo {Id} - ${MontoAbonado:0.00}";
    }
}
