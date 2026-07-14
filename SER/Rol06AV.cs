namespace SER
{
    public class Rol06AV : ComponentePermisoCompuesto06AV
    {
        public string Codigo { get; set; }

        protected override string NombreContenedor() => "este rol";
    }
}
