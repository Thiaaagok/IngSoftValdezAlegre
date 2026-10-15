using System;

namespace BE
{
    public class PagoProveedor06AV
    {
        public string Numero { get; set; }
        public string IdOrdenCompra { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Today;
        public FormaPago06AV FormaPago { get; set; } = FormaPago06AV.Transferencia;
        public string Referencia { get; set; }
        public string DniUsuario { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public override string ToString() => $"Pago {Numero} - ${Monto:0.00}";
    }
}
