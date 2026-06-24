namespace SER
{
    /// <summary>
    /// Identificadores de permisos del sistema.
    /// IMPORTANTE: el nombre de cada valor debe ser idéntico al Id
    /// de la tabla Patentes en la base de datos.
    /// </summary>
    public enum PatenteEnum06AV
    {
        // ── Usuarios ──
        VerUsuarios,
        CrearUsuarios,
        EditarUsuarios,
        ActDesactivarUsuarios,
        DesbloquearUsuarios,

        // ── Bitácora ──
        VerBitacora,
        ExportarBitacora,

        // ── Acceso a pantallas de permisos ──
        GestionarRoles,
        GestionarFamilias,
        GestionarPatentes
    }
}
