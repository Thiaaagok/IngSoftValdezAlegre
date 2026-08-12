namespace BE
{
    public class LineaEnsamblaje06AV
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Disponible { get; set; } = true;
        public override string ToString() =>
            $"{Nombre} ({(Disponible ? "Disponible" : "Ocupada")})";
    }
}
