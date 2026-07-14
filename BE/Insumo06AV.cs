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
