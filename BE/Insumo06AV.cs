namespace BE
{
    /// <summary>
    /// Material o componente usado en el ensamblaje. Se controla su stock y, cuando cae
    /// por debajo del mínimo, el sistema lo resalta como faltante (RFN2).
    /// </summary>
    public class Insumo06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }

        /// <summary>Cantidad disponible en depósito.</summary>
        public int Stock { get; set; }

        /// <summary>Umbral por debajo del cual se considera bajo stock.</summary>
        public int StockMinimo { get; set; }

        /// <summary>True si el stock actual está en o por debajo del mínimo.</summary>
        public bool BajoStock => Stock <= StockMinimo;

        public override string ToString() =>
            $"{Descripcion} (stock {Stock}/{StockMinimo})";
    }
}
