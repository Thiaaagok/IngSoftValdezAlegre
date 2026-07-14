namespace SER
{
    /// <summary>
    /// Identificadores de permisos. El nombre de cada valor debe ser idéntico al Id
    /// de la tabla Patentes en la base de datos.
    /// </summary>
    public enum PatenteEnum06AV
    {
        // Usuarios
        VerUsuarios,
        CrearUsuarios,
        EditarUsuarios,
        ActDesactivarUsuarios,
        DesbloquearUsuarios,

        // Bitácora
        VerBitacora,
        ExportarBitacora,

        // Acceso a pantallas de permisos
        GestionarRoles,
        GestionarFamilias,
        GestionarPatentes,

        // Habilita el GUI de Reparación del Dígito Verificador
        RepararIntegridad,

        // Acceso a las pantallas del dominio PC Factory
        GestionarClientes,
        GestionarComponentes,
        GestionarInsumos,
        GestionarProveedores,
        GestionarLineasEnsamblaje,
        GestionarProduccion,
        GestionarCompras,
        GestionarModelosEstandar
    }
}
