using System;

namespace BE
{
    /// <summary>
    /// Pago del cliente. La FK apunta a la Computadora06AV (no a la orden de producción),
    /// porque la seña se cobra antes de que exista la orden.
    /// </summary>
    public class Pago06AV
    {
        public int Id { get; set; }
        public int NumeroOrden { get; set; }
        public TipoPago06AV Tipo { get; set; }
        public string NumeroRecibo { get; set; }
        public decimal Monto { get; set; }
        public FormaPago06AV FormaPago { get; set; } = FormaPago06AV.Efectivo;
        public string Referencia { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Usuario { get; set; }
        public override string ToString() =>
            $"{Tipo} {NumeroRecibo}: ${Monto:0.00} ({FormaPago}, {Fecha:dd/MM/yyyy})";
    }
}
