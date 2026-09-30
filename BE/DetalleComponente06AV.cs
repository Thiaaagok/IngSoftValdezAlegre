namespace BE
{
    public class DetalleComponente06AV
    {
        public Componente06AV Componente { get; set; }
        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => System.Math.Round(PrecioUnitario * Cantidad, 2);

        public override string ToString() => $"{Componente?.Descripcion} x{Cantidad}";
    }
}
