namespace BE
{
    /// <summary>
    /// Pieza individual que forma parte de una computadora (procesador, RAM, disco,
    /// placa madre, fuente, gabinete, placa de video, etc.).
    /// </summary>
    public class Componente06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public TipoComponente06AV Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal PrecioUnitario { get; set; }

        /// <summary>Stock disponible del componente en depósito.</summary>
        public int StockDisponible { get; set; }

        public override string ToString() => $"{Descripcion} - {Marca} {Modelo}";
    }
}
