namespace BE
{
    /// <summary>
    /// Sector físico o lógico de la planta donde se arma una computadora. El gerente
    /// asigna una línea disponible a cada orden de producción.
    /// </summary>
    public class LineaEnsamblaje06AV
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        /// <summary>True si la línea está libre para tomar una nueva orden.</summary>
        public bool Disponible { get; set; } = true;

        public override string ToString() =>
            $"{Nombre} ({(Disponible ? "Disponible" : "Ocupada")})";
    }
}
