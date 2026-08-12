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
        public int StockDisponible { get; set; }

        /// <summary>
        /// Unidades reservadas dentro de una orden de producción
        /// </summary>
        public int StockReservado { get; set; }
        public int StockLibre => StockDisponible - StockReservado;
        public override string ToString() => $"{Descripcion} - {Marca} {Modelo}";
    }
}
