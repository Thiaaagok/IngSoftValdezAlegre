using System;

namespace BE
{
    /// <summary>
    /// Pago que realiza el cliente sobre una orden de producción: la seña (50% inicial)
    /// o el saldo final (50% restante al retirar el equipo).
    /// </summary>
    public class Pago06AV
    {
        public int Id { get; set; }
        public int NumeroOrden { get; set; }
        public TipoPago06AV Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public override string ToString() => $"{Tipo}: ${Monto:0.00} ({Fecha:dd/MM/yyyy})";
    }
}
