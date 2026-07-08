namespace BE
{
    /// <summary>
    /// Empresa o persona que suministra insumos/componentes para el ensamblaje (RFN2).
    /// Se identifica por su CUIT.
    /// </summary>
    public class Proveedor06AV
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Cuit { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }

        public override string ToString() => $"{Nombre} (CUIT {Cuit})";
    }
}
