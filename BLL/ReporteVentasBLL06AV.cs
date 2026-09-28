using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Reporte RF1: ventas y producción. Una fila por venta del período con su
    /// cobro (CU03), su orden de producción (CU04 a CU06) y su entrega (CU07),
    /// más los indicadores y cortes del período.
    ///
    /// La base filtra (sp_Reporte_VentasProduccion) y la BLL calcula. El cálculo
    /// vive en <see cref="CalcularResumen"/>, que no depende de la base.
    /// </summary>
    public class ReporteVentasBLL06AV
    {
        /// <summary>Tope del período para no pedirle a la base años de ventas de una vez.</summary>
        public const int RangoMaximoMeses = 24;

        /// <summary>Con menos caracteres la búsqueda de cliente trae prácticamente todo.</summary>
        public const int LargoMinimoCliente = 3;

        /// <summary>Largo del parámetro @Cliente del procedimiento.</summary>
        public const int LargoMaximoCliente = 100;

        public const int TopModelos = 5;

        /// <summary>Clave con la que se agrupan los equipos sin modelo de catálogo.</summary>
        public const string NombreAMedida = "(a medida)";

        private readonly ReportesMPP06AV _mpp = new ReportesMPP06AV();

        /// <summary>
        /// Genera el reporte con los criterios dados.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Sin la patente VerReporteVentas o con criterios inválidos.</exception>
        /// <exception cref="AccesoDatosException06AV">Si falla la consulta.</exception>
        public ReporteVentas06AV Generar(FiltroReporteVentas06AV filtro)
        {
            ExigirPermiso();
            ValidarFiltro(filtro, DateTime.Today);

            List<FilaReporteVentas06AV> filas;
            try { filas = _mpp.ObtenerVentasProduccion(filtro) ?? new List<FilaReporteVentas06AV>(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo generar el reporte de ventas.", ex); }

            // El atraso depende de la fecha de hoy, por eso se filtra acá y no en el procedimiento.
            DateTime hoy = DateTime.Today;
            if (filtro.SoloAtrasadas)
                filas = filas.Where(f => f.EstaAtrasada(hoy)).ToList();

            return new ReporteVentas06AV
            {
                Filtro = filtro,
                Filas = filas,
                Resumen = CalcularResumen(filas, hoy),
                GeneradoEl = DateTime.Now,
                GeneradoPor = UsuarioActual()
            };
        }

        /// <summary>
        /// Valida los criterios. Rechaza un período invertido, que empiece en el
        /// futuro o que supere <see cref="RangoMaximoMeses"/>, y un texto de cliente
        /// fuera de 3 a 100 caracteres. Deja <c>filtro.Cliente</c> recortado, o en
        /// null si quedó vacío.
        /// </summary>
        /// <exception cref="ValidacionException06AV">Con el campo que no cumple.</exception>
        public static void ValidarFiltro(FiltroReporteVentas06AV filtro, DateTime hoy)
        {
            if (filtro == null)
                throw new ValidacionException06AV("filtro", "Indicá los criterios del reporte.");

            if (filtro.Desde.Date > filtro.Hasta.Date)
                throw new ValidacionException06AV("Desde", "La fecha \"desde\" no puede ser posterior a la fecha \"hasta\".");

            if (filtro.Desde.Date > hoy.Date)
                throw new ValidacionException06AV("Desde", "La fecha \"desde\" no puede ser posterior a hoy.");

            if (filtro.Hasta.Date > filtro.Desde.Date.AddMonths(RangoMaximoMeses))
                throw new ValidacionException06AV("Hasta",
                    $"El período no puede superar los {RangoMaximoMeses} meses. Achicá el rango de fechas.");

            string cliente = (filtro.Cliente ?? "").Trim();
            if (cliente.Length > 0 && cliente.Length < LargoMinimoCliente)
                throw new ValidacionException06AV("Cliente",
                    $"Para buscar un cliente escribí al menos {LargoMinimoCliente} caracteres del DNI, apellido o nombre.");
            if (cliente.Length > LargoMaximoCliente)
                throw new ValidacionException06AV("Cliente",
                    $"El texto de búsqueda no puede superar los {LargoMaximoCliente} caracteres.");
            filtro.Cliente = cliente.Length == 0 ? null : cliente;
        }

        /// <summary>
        /// Indicadores del período. Las anuladas se cuentan pero no suman importes ni
        /// entran en promedios ni porcentajes. Importes redondeados a 2 decimales y
        /// porcentajes a 1.
        /// </summary>
        public static ResumenReporteVentas06AV CalcularResumen(IEnumerable<FilaReporteVentas06AV> filas, DateTime hoy)
        {
            var todas = (filas ?? Enumerable.Empty<FilaReporteVentas06AV>()).Where(f => f != null).ToList();
            var vigentes = todas.Where(f => f.EstadoVenta != EstadoVenta06AV.Anulada).ToList();

            var r = new ResumenReporteVentas06AV
            {
                CantidadVentas = todas.Count,
                CantidadAnuladas = todas.Count - vigentes.Count,
                CantidadVigentes = vigentes.Count,
                Facturacion = Math.Round(vigentes.Sum(f => f.Total), 2),
                TotalCobrado = Math.Round(vigentes.Sum(f => f.TotalCobrado), 2),
                SaldoPendiente = Math.Round(vigentes.Sum(f => f.SaldoPendiente), 2),
                PendientesDeSena = vigentes.Count(f => f.EstadoVenta == EstadoVenta06AV.Pendiente),
                // La venta sigue "EnProduccion" hasta la entrega aunque la orden ya esté
                // Finalizada; esas se cuentan aparte como listas para retirar.
                EnProduccion = vigentes.Count(f => f.EstadoVenta == EstadoVenta06AV.EnProduccion
                                                   && f.EstadoOrden != EstadoOrdenProduccion06AV.Finalizada),
                ListasParaEntregar = vigentes.Count(f => f.EstadoOrden == EstadoOrdenProduccion06AV.Finalizada
                                                         && f.EstadoVenta != EstadoVenta06AV.Entregada),
                Entregadas = vigentes.Count(f => f.EstadoVenta == EstadoVenta06AV.Entregada),
                Atrasadas = vigentes.Count(f => f.EstaAtrasada(hoy))
            };

            r.TicketPromedio = vigentes.Count == 0 ? 0m : Math.Round(r.Facturacion / vigentes.Count, 2);
            r.PorcentajeSenadas = Porcentaje(vigentes.Count(f => f.TieneSena), vigentes.Count);

            var entregadas = vigentes.Where(f => f.EstadoVenta == EstadoVenta06AV.Entregada && f.FechaEntrega.HasValue).ToList();
            r.DiasPromedioEntrega = entregadas.Count == 0
                ? (decimal?)null
                : Math.Round((decimal)entregadas.Average(f => (f.FechaEntrega.Value.Date - f.FechaVenta.Date).TotalDays), 1);

            // Van todos los estados, aun en cero, para que el corte tenga siempre la misma forma.
            foreach (EstadoVenta06AV e in Enum.GetValues(typeof(EstadoVenta06AV)))
            {
                var grupo = todas.Where(f => f.EstadoVenta == e).ToList();
                r.PorEstado.Add(new GrupoReporte06AV
                {
                    Clave = e.ToString(),
                    Cantidad = grupo.Count,
                    Importe = e == EstadoVenta06AV.Anulada ? 0m : Math.Round(grupo.Sum(f => f.Total), 2),
                    Porcentaje = Porcentaje(grupo.Count, todas.Count)
                });
            }

            foreach (TipoConfiguracion06AV t in Enum.GetValues(typeof(TipoConfiguracion06AV)))
            {
                var grupo = vigentes.Where(f => f.TipoConfiguracion == t).ToList();
                r.PorTipo.Add(new GrupoReporte06AV
                {
                    Clave = t.ToString(),
                    Cantidad = grupo.Count,
                    Importe = Math.Round(grupo.Sum(f => f.Total), 2),
                    Porcentaje = Porcentaje(grupo.Count, vigentes.Count)
                });
            }

            // Empates: primero el de mayor importe y después por nombre, para que el orden sea estable.
            r.PorModelo = vigentes
                .GroupBy(f => string.IsNullOrWhiteSpace(f.ModeloOrigen) ? NombreAMedida : f.ModeloOrigen.Trim())
                .Select(g => new GrupoReporte06AV
                {
                    Clave = g.Key,
                    Cantidad = g.Count(),
                    Importe = Math.Round(g.Sum(f => f.Total), 2),
                    Porcentaje = Porcentaje(g.Count(), vigentes.Count)
                })
                .OrderByDescending(g => g.Cantidad)
                .ThenByDescending(g => g.Importe)
                .ThenBy(g => g.Clave, StringComparer.CurrentCultureIgnoreCase)
                .Take(TopModelos)
                .ToList();

            return r;
        }

        /// <summary>Porcentaje con un decimal; 0 si el total es 0.</summary>
        public static decimal Porcentaje(int parte, int total) =>
            total <= 0 ? 0m : Math.Round(parte * 100m / total, 1);

        // El menú ya se oculta sin la patente; esto cubre llamadas que no pasen por la pantalla.
        private static void ExigirPermiso()
        {
            if (!UsuarioSesion06AV.Instancia().TienePermiso(PatenteEnum06AV.VerReporteVentas))
                throw new ValidacionException06AV("permiso",
                    "No tenés permiso para ver el reporte de ventas y producción (patente VerReporteVentas).");
        }

        private static string UsuarioActual()
        {
            try
            {
                var sesion = UsuarioSesion06AV.Instancia();
                string nombre = sesion.NombreCompleto();
                return string.IsNullOrWhiteSpace(nombre) ? sesion.UsuarioActual?.Dni : nombre;
            }
            catch { return null; }
        }
    }
}
