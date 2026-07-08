namespace BE
{
    /// <summary>
    /// Línea de detalle que asocia un insumo con una cantidad. Se usa tanto en la orden
    /// de compra (insumos faltantes) como en el pedido de cotización (insumos pedidos).
    /// </summary>
    public class DetalleInsumo06AV
    {
        public Insumo06AV Insumo { get; set; }
        public int Cantidad { get; set; }

        public override string ToString() =>
            $"{Insumo?.Descripcion} x{Cantidad}";
    }
}
