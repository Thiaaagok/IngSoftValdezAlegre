using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public class ModeloEstandar06AV
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public List<Componente06AV> Componentes { get; set; } = new List<Componente06AV>();
        public decimal PrecioTotal => Componentes?.Sum(c => c.PrecioUnitario) ?? 0m;
        public override string ToString() =>
            $"{Nombre} ({Componentes?.Count ?? 0} comp.) - ${PrecioTotal:0.00}";
    }
}
