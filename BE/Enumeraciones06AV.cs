namespace BE
{
    /// <summary>Tipo de componente que puede formar parte de una computadora.</summary>
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

    /// <summary>Cómo se armó la computadora solicitada por el cliente.</summary>
    public enum TipoConfiguracion06AV
    {
        Estandar,
        Personalizada
    }

    /// <summary>Estados por los que pasa una Orden de Producción (RFN1).</summary>
    public enum EstadoOrdenProduccion06AV
    {
        Pendiente,
        Planificada,
        EnEnsamblaje,
        Finalizada,
        Entregada
    }

    /// <summary>Tipo de pago que hace el cliente sobre una orden.</summary>
    public enum TipoPago06AV
    {
        Sena,       
        SaldoFinal  
    }

    /// <summary>Estados de un Pedido de Cotización (RFN2).</summary>
    public enum EstadoCotizacion06AV
    {
        PorAprobar,
        Aprobado,
        Desaprobada
    }

    /// <summary>Estados de una Orden de Compra de insumos (RFN2).</summary>
    public enum EstadoOrdenCompra06AV
    {
        Pendiente,
        Enviada,
        Finalizada
    }
}
