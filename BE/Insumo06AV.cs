// Insumo06AV fue FUSIONADO en Componente06AV (todo componente es un insumo con stock).
// Clase eliminada del modelo. Este archivo se quita del csproj; se deja el placeholder
// para no romper el proyecto si quedó una referencia externa al archivo.
namespace BE
{
    public class Insumo06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }

        public int Stock { get; set; }

        public int StockMinimo { get; set; }

        public bool BajoStock => Stock <= StockMinimo;

        public override string ToString() =>
            $"{Descripcion} (stock {Stock}/{StockMinimo})";
    }
}
