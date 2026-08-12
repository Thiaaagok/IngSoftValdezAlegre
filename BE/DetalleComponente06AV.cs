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

        public override string ToString() => $"{Componente?.Descripcion} x{Cantidad}";
    }
}
