namespace BE
{
    public class Componente06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public TipoComponente06AV Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal PrecioUnitario { get; set; }

        public int Stock { get; set; }

        public int StockMinimo { get; set; }

        public int StockReservado { get; set; }

        public int StockLibre => Stock - StockReservado;

        public bool BajoStock => Stock <= StockMinimo;

        public bool BajaLogica { get; set; }

        public override string ToString() => $"{Descripcion} - {Marca} {Modelo}";
    }
}
