namespace SER
{
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

        // Pantalla de permisos
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
        GestionarVentas,
        GestionarEntregas,
        GestionarProduccion,
        GestionarCompras,
        GestionarModelosEstandar,

        // Acciones sensibles del circuito de compras (RFN2).
        // Separan funciones dentro del mismo módulo: quien registra la orden
        // no es necesariamente quien aprueba la cotización.
        RegistrarOrdenCompra,
        AprobarCotizacion,

        // Mesa de cotizaciones: acceso al módulo de comparación de ofertas.
        GestionarCotizaciones
    }
}

