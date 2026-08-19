using System;

namespace BE
{
    public class Pago06AV
    {
        public int Id { get; set; }
        public int NumeroVenta { get; set; }
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
