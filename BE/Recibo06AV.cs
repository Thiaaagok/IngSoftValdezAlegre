using System;

namespace BE
{
    /// <summary>
    /// Recibo de seña (RFN1 - CU03). Se emite cuando el cliente abona la seña sobre una
    /// venta ya registrada, antes de que exista la orden de producción.
    ///
    /// No se persiste: lo arma VentasBLL06AV.ObtenerRecibos a partir del pago de seña.
    /// SaldoPendiente es el total pactado menos la seña, es decir, el saldo tal como
    /// quedó al cobrarla. El cliente y la computadora se navegan por <see cref="Venta"/>.
    /// </summary>
    public class Recibo06AV
    {
        public string Id { get; set; }

        public Pago06AV Pago { get; set; }

        public Venta06AV Venta { get; set; }

        public DateTime FechaEmision { get; set; }

        public decimal MontoAbonado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public DateTime FechaEntregaEstimada { get; set; }

        public override string ToString() => $"Recibo {Id} - ${MontoAbonado:0.00}";
    }
}
