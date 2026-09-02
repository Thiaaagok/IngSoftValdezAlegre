namespace BE
{
    /// <summary>
    /// Componente de ensamblaje. Absorbe el antiguo Insumo06AV: todo componente es
    /// además un insumo que se compra y tiene stock (mismo concepto, una sola clase).
    ///
    /// El stock se lleva en dos niveles:
    ///   Stock          → unidades físicas en depósito.
    ///   StockReservado → unidades comprometidas por ventas registradas cuya orden de
    ///                    producción todavía no se cerró (RFN1: la venta reserva, el
    ///                    cierre de la orden consume).
    ///   StockLibre     → lo realmente vendible.
    /// </summary>
    public class Componente06AV
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public TipoComponente06AV Tipo { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public decimal PrecioUnitario { get; set; }

        /// <summary>Unidades físicas en depósito.</summary>
        public int Stock { get; set; }

        /// <summary>Punto de reposición: en o por debajo de este valor hay que reponer (RFN2).</summary>
        public int StockMinimo { get; set; }

        /// <summary>
        /// Unidades reservadas por una venta cuya orden de producción todavía no se cerró.
        /// </summary>
        public int StockReservado { get; set; }

        /// <summary>Unidades realmente disponibles para una venta nueva.</summary>
        public int StockLibre => Stock - StockReservado;

        /// <summary>RFN2: el componente llegó al mínimo y dispara la orden de compra.</summary>
        public bool BajoStock => Stock <= StockMinimo;

        /// <summary>
        /// Baja lógica (columna Bit_Lo_Bo). Los componentes no se borran nunca:
        /// el borrado físico está prohibido por trigger. Un componente dado de baja
        /// no aparece en los listados, pero conserva su historial en Componentes_C.
        /// </summary>
        public bool BajaLogica { get; set; }

        public override string ToString() => $"{Descripcion} - {Marca} {Modelo}";
    }
}
