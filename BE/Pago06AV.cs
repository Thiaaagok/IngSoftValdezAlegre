using System;

namespace BE
{
    /// <summary>
    /// Pago del cliente. La FK apunta a la Computadora06AV (no a la orden de producción),
    /// porque la seña se cobra antes de que exista la orden.
    /// </summary>
    public class Pago06AV
    {
        public string Id { get; set; }
        public Computadora06AV Computadora { get; set; }   // FK a Computadora06AV
        public TipoPago06AV Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public override string ToString() => $"{Tipo}: ${Monto:0.00} ({Fecha:dd/MM/yyyy})";
    }
}
