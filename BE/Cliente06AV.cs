namespace BE
{
    /// <summary>
    /// Cliente que solicita la compra de una computadora (particular o empresa).
    /// Se identifica por su DNI.
    /// </summary>
    public class Cliente06AV
    {
        public string Dni { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }

        public string NombreCompleto => $"{Apellido}, {Nombre}";

        public override string ToString() => $"{NombreCompleto} (DNI {Dni})";
    }
}
