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
        Tarjeta,        // tarjeta de crédito (valor 2, ya persistido en Pagos)
        TarjetaDebito   // se agrega al final para no alterar los valores guardados
    }

    public enum EstadoCotizacion06AV
    {
        PorAprobar,
        Aprobado,
        Desaprobada
    }

    /// <summary>
    /// Pendiente: registrada, esperando cotización.
    /// Enviada: cotización adjudicada, esperando la mercadería.
    /// Finalizada: llegó todo lo pedido.
    /// RecibidaParcial: llegó parte; el resto sigue pendiente en la misma orden.
    ///
    /// El valor numérico se persiste: los existentes NO se reordenan, los nuevos
    /// se agregan al final.
    /// </summary>
    public enum EstadoOrdenCompra06AV
    {
        Pendiente,
        Enviada,
        Finalizada,
        RecibidaParcial
    }

    /// <summary>
    /// Clases que se pueden serializar (A03): solo la venta, con todo lo que contiene.
    /// El nombre es también el elemento XML de cada objeto dentro del archivo.
    /// </summary>
    public enum ClaseSerializable06AV
    {
        Venta
    }

    public enum EstadoVerificacion06AV
    {
        Coincide,
        Difiere,
        NoExisteEnBase,
        FaltaEnArchivo
    }
}
