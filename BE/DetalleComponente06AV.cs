namespace BE
{
    /// <summary>
    /// Línea de detalle (componente + cantidad). Reemplaza a DetalleInsumo06AV:
    /// su propiedad Insumo pasó a llamarse Componente.
    /// </summary>
    public class DetalleComponente06AV
    {
        public Componente06AV Componente { get; set; }
        public int Cantidad { get; set; }

        /// <summary>
        /// Precio por unidad acordado para esta línea: el que ofreció el proveedor en la
        /// cotización. En las líneas que no tienen precio propio (orden de compra) queda en 0.
        /// </summary>
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => System.Math.Round(PrecioUnitario * Cantidad, 2);

        public override string ToString() => $"{Componente?.Descripcion} x{Cantidad}";
    }
}
