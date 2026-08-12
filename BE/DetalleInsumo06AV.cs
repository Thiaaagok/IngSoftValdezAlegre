// DetalleInsumo06AV fue RENOMBRADO a DetalleComponente06AV (su propiedad Insumo pasó a
// llamarse Componente). Clase eliminada. Este archivo se quita del csproj.
namespace BE
{
    public class DetalleInsumo06AV
    {
        public Insumo06AV Insumo { get; set; }
        public int Cantidad { get; set; }

        public override string ToString() =>
            $"{Insumo?.Descripcion} x{Cantidad}";
    }
}
