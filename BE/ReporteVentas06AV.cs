using System;
using System.Collections.Generic;

namespace BE
{
    /// <summary>Criterios del reporte RF1. Los opcionales en null no filtran.</summary>
    public class FiltroReporteVentas06AV
    {
        /// <summary>Primer día del período, sobre la fecha de la venta.</summary>
        public DateTime Desde { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        /// <summary>Último día del período, inclusive.</summary>
        public DateTime Hasta { get; set; } = DateTime.Today;

        public EstadoVenta06AV? Estado { get; set; }

        public TipoConfiguracion06AV? Tipo { get; set; }

        /// <summary>Parte del DNI, del apellido o del nombre del cliente.</summary>
        public string Cliente { get; set; }

        public bool SoloAtrasadas { get; set; }
    }

    /// <summary>
    /// Una venta con sus cobros y su orden de producción. <see cref="Total"/> es el
    /// precio congelado al vender (Computadoras.PrecioTotal), no el de los precios
    /// actuales de los componentes.
    /// </summary>
    public class FilaReporteVentas06AV
    {
        public int NumeroVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteNombre { get; set; }
        public string Equipo { get; set; }
        public TipoConfiguracion06AV TipoConfiguracion { get; set; }

        /// <summary>Nombre del modelo del catálogo, o null si el equipo es a medida.</summary>
        public string ModeloOrigen { get; set; }

        public decimal Total { get; set; }
        public decimal Sena { get; set; }
        public decimal SaldoCobrado { get; set; }
        public EstadoVenta06AV EstadoVenta { get; set; }
        public string UsuarioRegistro { get; set; }

        public int? NumeroOrden { get; set; }
        public EstadoOrdenProduccion06AV? EstadoOrden { get; set; }
        public string Linea { get; set; }
        public string NumeroSerie { get; set; }

        /// <summary>
        /// La de la orden si existe (la fija el gerente en CU04); si no, la estimada al vender.
        /// </summary>
        public DateTime FechaEntregaComprometida { get; set; }

        public DateTime? FechaCierre { get; set; }

        /// <summary>
        /// Fecha del cobro del saldo final. La entrega no guarda fecha propia; si no
        /// quedaba saldo por cobrar no hay pago y queda en null.
        /// </summary>
        public DateTime? FechaEntrega { get; set; }

        public decimal TotalCobrado => Sena + SaldoCobrado;

        /// <summary>Nunca negativo; una venta anulada no tiene saldo exigible.</summary>
        public decimal SaldoPendiente =>
            EstadoVenta == EstadoVenta06AV.Anulada ? 0m : Math.Max(0m, Total - TotalCobrado);

        public bool TieneSena => Sena > 0m;

        /// <summary>Días de entrega vencida; 0 si está en fecha, entregada o anulada.</summary>
        public int DiasDeAtraso(DateTime hoy)
        {
            if (EstadoVenta == EstadoVenta06AV.Entregada || EstadoVenta == EstadoVenta06AV.Anulada) return 0;
            int dias = (hoy.Date - FechaEntregaComprometida.Date).Days;
            return dias > 0 ? dias : 0;
        }

        public bool EstaAtrasada(DateTime hoy) => DiasDeAtraso(hoy) > 0;
    }

    /// <summary>Renglón de un corte del resumen (por estado, por tipo o por modelo).</summary>
    public class GrupoReporte06AV
    {
        public string Clave { get; set; }
        public int Cantidad { get; set; }
        public decimal Importe { get; set; }

        /// <summary>Sobre la cantidad de ventas del corte, de 0 a 100.</summary>
        public decimal Porcentaje { get; set; }

        public override string ToString() => $"{Clave}: {Cantidad} (${Importe:0.00})";
    }

    /// <summary>Indicadores del período. Ver ReporteVentasBLL06AV.CalcularResumen.</summary>
    public class ResumenReporteVentas06AV
    {
        /// <summary>Incluye las anuladas.</summary>
        public int CantidadVentas { get; set; }
        public int CantidadAnuladas { get; set; }

        /// <summary>Las que cuentan para importes y promedios: todas menos las anuladas.</summary>
        public int CantidadVigentes { get; set; }

        public decimal Facturacion { get; set; }
        public decimal TotalCobrado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal TicketPromedio { get; set; }

        /// <summary>Vigentes con seña cobrada, de 0 a 100.</summary>
        public decimal PorcentajeSenadas { get; set; }

        public int PendientesDeSena { get; set; }
        public int EnProduccion { get; set; }
        public int ListasParaEntregar { get; set; }
        public int Entregadas { get; set; }
        public int Atrasadas { get; set; }

        /// <summary>Días entre venta y retiro de las entregadas; null si todavía no hay ninguna.</summary>
        public decimal? DiasPromedioEntrega { get; set; }

        public List<GrupoReporte06AV> PorEstado { get; set; } = new List<GrupoReporte06AV>();
        public List<GrupoReporte06AV> PorTipo { get; set; } = new List<GrupoReporte06AV>();
        public List<GrupoReporte06AV> PorModelo { get; set; } = new List<GrupoReporte06AV>();
    }

    public class ReporteVentas06AV
    {
        public FiltroReporteVentas06AV Filtro { get; set; }
        public List<FilaReporteVentas06AV> Filas { get; set; } = new List<FilaReporteVentas06AV>();
        public ResumenReporteVentas06AV Resumen { get; set; } = new ResumenReporteVentas06AV();
        public DateTime GeneradoEl { get; set; } = DateTime.Now;
        public string GeneradoPor { get; set; }
    }
}
