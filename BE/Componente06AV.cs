namespace BE
{
    /// <summary>
    /// Componente de ensamblaje. Absorbe el antiguo Insumo06AV: todo componente es
    /// además un insumo que se compra y tiene stock (mismo concepto, una sola clase).
    /// </summary>
    public class Componente06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
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
