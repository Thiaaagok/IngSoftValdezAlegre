using BE;
using System;
using System.Collections.Generic;

namespace BLL
{
    public enum EstadoPagoProveedor06AV
    {
        SinFacturar,
        Pendiente,
        Parcial,
        Pagada
    }

    public class ResumenPagoProveedor06AV
    {
        public int Recepciones { get; set; }
        public decimal Facturado { get; set; }
        public decimal Pagado { get; set; }
        public List<PagoProveedor06AV> Pagos { get; set; } = new List<PagoProveedor06AV>();

        public decimal Saldo => Math.Max(0m, Facturado - Pagado);

        public EstadoPagoProveedor06AV Estado =>
            Recepciones == 0 ? EstadoPagoProveedor06AV.SinFacturar
            : Pagado <= 0m ? EstadoPagoProveedor06AV.Pendiente
            : Saldo > 0m ? EstadoPagoProveedor06AV.Parcial
            : EstadoPagoProveedor06AV.Pagada;
    }
}
