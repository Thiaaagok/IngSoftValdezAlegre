namespace BE
{
    public class DetalleInsumo06AV
    {
        public Insumo06AV Insumo { get; set; }
        public int Cantidad { get; set; }
        public override string ToString() =>
            $"{Insumo?.Descripcion} x{Cantidad}";
    }
}
