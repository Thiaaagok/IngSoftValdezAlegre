using BE;
using BLL.Excepciones;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class ReporteVentasBLL06AV
    {
        public const int RangoMaximoMeses = 24;

        public const int LargoMinimoCliente = 3;

        public const int LargoMaximoCliente = 100;

        public const int TopModelos = 5;

        public const string NombreAMedida = "(a medida)";

        private readonly ReportesMPP06AV _mpp = new ReportesMPP06AV();

        public ReporteVentas06AV Generar(FiltroReporteVentas06AV filtro)
        {
            ExigirPermiso();
            ValidarFiltro(filtro, DateTime.Today);

            List<FilaReporteVentas06AV> filas;
            try { filas = _mpp.ObtenerVentasProduccion(filtro) ?? new List<FilaReporteVentas06AV>(); }
            catch (Exception ex) { throw new AccesoDatosException06AV("No se pudo generar el reporte de ventas.", ex); }

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
