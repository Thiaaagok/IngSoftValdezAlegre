using BE;
using MPP;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    public class AvisosBLL06AV
    {
        private readonly ComprasMPP06AV _compras = new ComprasMPP06AV();
        private readonly ComponentesMPP06AV _componentes = new ComponentesMPP06AV();

        public List<Aviso06AV> Obtener()
        {
            var avisos = new List<Aviso06AV>();
            UsuarioSesion06AV sesion;
            try { sesion = UsuarioSesion06AV.Instancia(); }
            catch { return avisos; }
            if (sesion?.UsuarioActual == null) return avisos;

            bool repositor = sesion.TienePermiso(PatenteEnum06AV.RegistrarOrdenCompra);
            bool gerente = sesion.TienePermiso(PatenteEnum06AV.AprobarCotizacion);
            bool compras = sesion.TienePermiso(PatenteEnum06AV.GestionarCompras);
            bool pagos = sesion.TienePermiso(PatenteEnum06AV.PagarProveedores);
            if (!repositor && !gerente && !compras && !pagos) return avisos;

            List<OrdenCompra06AV> ordenes;
            List<PedidoCotizacion06AV> cotizaciones;
            try
            {
                ordenes = _compras.ObtenerOrdenesCompra();
                cotizaciones = _compras.ObtenerCotizaciones();
            }
            catch { return avisos; }

            if (repositor) Agregar(avisos, BajoStock(ordenes));
            if (gerente) Agregar(avisos, PorAprobar(ordenes, cotizaciones));
            if (repositor) Agregar(avisos, Rechazadas(ordenes, cotizaciones));
            if (compras || repositor || gerente) Agregar(avisos, Atrasadas(ordenes));
            if (pagos) Agregar(avisos, PagosPendientes(ordenes));
            return avisos;
        }

        private static void Agregar(List<Aviso06AV> avisos, Aviso06AV aviso)
        {
            if (aviso != null && aviso.Cantidad > 0) avisos.Add(aviso);
        }

        private Aviso06AV BajoStock(List<OrdenCompra06AV> ordenes)
        {
            List<Componente06AV> bajos;
            try { bajos = _componentes.ObtenerBajoStock(); }
            catch { return null; }
            var enCurso = new HashSet<string>(
                ordenes.Where(o => o.EnCurso).SelectMany(o => o.ComponentesFaltantes)
                       .Where(d => d.Componente != null).Select(d => d.Componente.Codigo),
                StringComparer.OrdinalIgnoreCase);
            var sinPedir = bajos.Where(c => !c.BajaLogica && !enCurso.Contains(c.Codigo))
                                .OrderBy(c => c.Stock - c.StockMinimo).ToList();
            return new Aviso06AV
            {
                Tipo = TipoAviso06AV.BajoStock,
                Cantidad = sinPedir.Count,
                Urgente = sinPedir.Any(c => c.Stock <= 0),
                Detalle = sinPedir.Take(6).Select(c => $"{c.Descripcion} ({c.Stock}/{c.StockMinimo})").ToList()
            };
        }

        private static Aviso06AV PorAprobar(List<OrdenCompra06AV> ordenes, List<PedidoCotizacion06AV> cotizaciones)
        {
            var pendientes = ordenes.Where(o => o.Estado == EstadoOrdenCompra06AV.Pendiente).ToDictionary(o => o.Id);
            var lista = cotizaciones.Where(c => c.Estado == EstadoCotizacion06AV.PorAprobar && pendientes.ContainsKey(c.NumeroCompra))
                                    .OrderBy(c => c.FechaEmision).ToList();
            return new Aviso06AV
            {
                Tipo = TipoAviso06AV.CotizacionesPorAprobar,
                Cantidad = lista.Count,
                Detalle = lista.Take(6).Select(c => $"OC #{pendientes[c.NumeroCompra].NumeroCompra} · {c.Proveedor?.Nombre} · {c.Costo:C0}").ToList()
            };
        }

        private static Aviso06AV Rechazadas(List<OrdenCompra06AV> ordenes, List<PedidoCotizacion06AV> cotizaciones)
        {
            var lista = ordenes.Where(o => o.Estado == EstadoOrdenCompra06AV.Pendiente)
                .Where(o =>
                {
                    var propias = cotizaciones.Where(c => c.NumeroCompra == o.Id).ToList();
                    return propias.Count > 0 && propias.All(c => c.Estado == EstadoCotizacion06AV.Desaprobada);
                })
                .OrderBy(o => o.NumeroCompra).ToList();
            return new Aviso06AV
            {
                Tipo = TipoAviso06AV.CotizacionesRechazadas,
                Cantidad = lista.Count,
                Detalle = lista.Take(6).Select(o => "OC #" + o.NumeroCompra).ToList()
            };
        }

        private static Aviso06AV Atrasadas(List<OrdenCompra06AV> ordenes)
        {
            var lista = ordenes.Where(o => o.EnCurso && o.FechaLimite.Date < DateTime.Today)
                               .OrderBy(o => o.FechaLimite).ToList();
            return new Aviso06AV
            {
                Tipo = TipoAviso06AV.OrdenesAtrasadas,
                Cantidad = lista.Count,
                Urgente = lista.Count > 0,
                Detalle = lista.Take(6).Select(o => $"OC #{o.NumeroCompra} · {(DateTime.Today - o.FechaLimite.Date).Days} d").ToList()
            };
        }

        private Aviso06AV PagosPendientes(List<OrdenCompra06AV> ordenes)
        {
            var bll = new CompraInsumosBLL06AV();
            List<PagoProveedor06AV> pagos;
            try { pagos = bll.ObtenerPagos(); }
            catch { return null; }

            var conRecepcion = ordenes.Where(o => o.Estado == EstadoOrdenCompra06AV.RecibidaParcial ||
                                                  o.Estado == EstadoOrdenCompra06AV.Finalizada ||
                                                  o.Estado == EstadoOrdenCompra06AV.CerradaConFaltante);
            var deudas = new List<KeyValuePair<OrdenCompra06AV, decimal>>();
            foreach (OrdenCompra06AV o in conRecepcion)
            {
                try
                {
                    ResumenPagoProveedor06AV r = bll.ResumenPagos(o, pagos);
                    if (r.Saldo > 0m) deudas.Add(new KeyValuePair<OrdenCompra06AV, decimal>(o, r.Saldo));
                }
                catch { }
            }
            return new Aviso06AV
            {
                Tipo = TipoAviso06AV.PagosPendientes,
                Cantidad = deudas.Count,
                Detalle = deudas.Take(6).Select(d => $"OC #{d.Key.NumeroCompra} · {d.Value:C0}").ToList()
            };
        }
    }
}
