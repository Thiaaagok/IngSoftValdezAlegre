using System;

namespace BE
{
    public class FilaReporteProveedores06AV
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public bool Activo { get; set; }
        public string Tipos { get; set; }
        public int Cotizaciones { get; set; }
        public int OrdenesCompra { get; set; }
        public decimal TotalComprado { get; set; }
        public DateTime? UltimaCompra { get; set; }

        public string Codigo => "PRV-" + Id.ToString("0000");
    }
}
