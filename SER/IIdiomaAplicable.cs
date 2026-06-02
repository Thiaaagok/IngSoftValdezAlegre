namespace SER
{
    /// <summary>
    /// Contrato para que FRMMain pueda refrescar el control activo
    /// cuando el usuario cambia de idioma.
    /// </summary>
    public interface IIdiomaAplicable
    {
        void AplicarIdioma();
    }
}
