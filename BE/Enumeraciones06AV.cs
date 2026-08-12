namespace BE
{
    public enum TipoComponente06AV
    {
        Procesador,
        MemoriaRAM,
        Disco,
        PlacaMadre,
        Fuente,
        Gabinete,
        PlacaDeVideo,
        Refrigeracion,
        Otro
    }

    public enum TipoConfiguracion06AV
    {
        Estandar,
        Configurable
    }

    /// <summary>Rol de negocio de un usuario en el circuito de compras/ventas (RFN1/RFN2).</summary>
    public enum RolUsuario06AV
    {
        Repositor,
        GerenteCompras,
        Almacenista,
        Recepcionista,
        Gerente
    }

    /// <summary>
    /// Pendiente: registrada, todavía sin seña.
    /// Señada: seña cobrada, habilitada para generar la orden de producción.
    /// EnProduccion: ya tiene orden de producción asociada.
    /// Entregada: el cliente retiró el equipo y canceló el saldo.
    /// Anulada: la venta se dio de baja antes de producirse.
    /// </summary>
    public enum EstadoVenta06AV
    {
        Pendiente,
        Senada,
        EnProduccion,
        Entregada,
        Anulada
    }

    public enum EstadoOrdenProduccion06AV
    {
        Pendiente,
        Planificada,
        EnEnsamblaje,
        Finalizada,
        Entregada,
        EnRevision
    }

    public enum TipoPago06AV
    {
        Sena,
        SaldoFinal
    }

    public enum FormaPago06AV
    {
        Efectivo,
        Transferencia,
        Tarjeta
    }

    public enum EstadoCotizacion06AV
    {
        PorAprobar,
        Aprobado,
        Desaprobada
    }

    public enum EstadoOrdenCompra06AV
    {
        Pendiente,
        Enviada,
        Finalizada
    }
}
