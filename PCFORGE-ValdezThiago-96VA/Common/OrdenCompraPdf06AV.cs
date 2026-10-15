using BE;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class OrdenCompraPdf06AV : DocumentoPdf06AV
    {
        private readonly OrdenCompra06AV _orden;
        private readonly PedidoCotizacion06AV _cotizacion;

        private OrdenCompraPdf06AV(OrdenCompra06AV orden, PedidoCotizacion06AV cotizacion)
        {
            _orden = orden;
            _cotizacion = cotizacion;
        }

        public static void Guardar(string ruta, OrdenCompra06AV orden, PedidoCotizacion06AV cotizacion, Image logo)
        {
            if (orden == null) throw new ArgumentNullException(nameof(orden));
            new OrdenCompraPdf06AV(orden, cotizacion).Generar(ruta, logo);
        }

        protected override string Titulo => _t.Obtener("pcf_oc_doc_titulo");

        protected override string Numero => _orden.NumeroCompra.ToString("00000");

        protected override string MarcaAgua =>
            _orden.Estado == EstadoOrdenCompra06AV.Anulada ? _t.Obtener("pcf_doc_anulada") : null;

        private void Generar(string ruta, Image logo)
        {
            _pdf.DefinirImagen(logo);
            NuevaPagina();
            float y = EncabezadoCompleto();
            y = Tarjetas(y + 22f);
            y = TablaItems(y + 22f, Items(), _t.Obtener("pcf_oc_doc_sin_items"));

            const float altoResumen = 150f;
            y = y + 20f + altoResumen > LimiteContenido ? PaginaContinuacion() : y + 20f;
            y = Resumen(y);
            Firma(y);
            Finalizar(ruta);
        }

        private float Tarjetas(float y)
        {
            Proveedor06AV p = _cotizacion?.Proveedor;
            var lineas = new List<string>();
            if (p != null)
            {
                if (!string.IsNullOrWhiteSpace(p.Cuit)) lineas.Add("CUIT " + p.Cuit);
                if (!string.IsNullOrWhiteSpace(p.Email)) lineas.Add(p.Email);
                if (!string.IsNullOrWhiteSpace(p.Telefono)) lineas.Add(_t.Obtener("pcf_doc_telefono") + " " + p.Telefono);
                if (!string.IsNullOrWhiteSpace(p.Direccion))
                    lineas.AddRange(_pdf.Envolver(p.Direccion, AnchoTarjeta - 28f, 9f, FuentePdf06AV.Normal).Take(2));
            }

            var datos = new List<KeyValuePair<string, string>>
            {
                Par(_t.Obtener("pcf_orden_num"), "#" + _orden.NumeroCompra),
                Par(_t.Obtener("pcf_oc_doc_entregar_antes"), _orden.FechaLimite.ToString("dd/MM/yyyy"))
            };
            if (_cotizacion != null)
            {
                datos.Add(Par(_t.Obtener("pcf_oc_doc_cotizacion"), _cotizacion.Numero));
                datos.Add(Par(_t.Obtener("pcf_oc_doc_fecha_cotizacion"), _cotizacion.FechaEmision.ToString("dd/MM/yyyy")));
            }
            string solicito = NombreUsuario(_orden.RepositorSolicitante);
            if (solicito != null) datos.Add(Par(_t.Obtener("pcf_oc_doc_solicito"), solicito));
            string aprobo = NombreUsuario(_cotizacion?.GerenteAprobador);
            if (aprobo != null) datos.Add(Par(_t.Obtener("pcf_oc_doc_aprobo"), aprobo));

            return DibujarTarjetas(y, _t.Obtener("pcf_oc_doc_proveedor"), p?.Nombre ?? "-", lineas,
                                   _t.Obtener("pcf_oc_doc_datos"), datos);
        }

        private static string NombreUsuario(SER.Usuario06AV u)
        {
            if (u == null) return null;
            string nombre = ((u.Nombre ?? "") + " " + (u.Apellido ?? "")).Trim();
            return nombre.Length > 0 ? nombre : u.Login;
        }

        private List<ItemPdf> Items()
        {
            List<DetalleComponente06AV> lineas = _cotizacion?.ComponentesPedidos != null && _cotizacion.ComponentesPedidos.Count > 0
                ? _cotizacion.ComponentesPedidos
                : _orden.ComponentesFaltantes;
            bool conPrecios = _cotizacion != null && _cotizacion.TienePreciosPorItem;
            decimal unitarioProrrateado = 0m;
            int unidades = lineas.Sum(d => d.Cantidad);
            if (!conPrecios && _cotizacion != null && _cotizacion.Costo > 0 && unidades > 0)
                unitarioProrrateado = Math.Round(_cotizacion.Costo / unidades, 2);

            return lineas.Where(d => d.Componente != null).Select(d => new ItemPdf
            {
                Cantidad = d.Cantidad,
                Descripcion = d.Componente.Descripcion,
                Detalle = ((d.Componente.Marca ?? "") + " " + (d.Componente.Modelo ?? "")).Trim(),
                Codigo = d.Componente.Codigo,
                Unitario = conPrecios ? d.PrecioUnitario : unitarioProrrateado
            }).ToList();
        }

        private decimal Total => _cotizacion != null && _cotizacion.Costo > 0 ? _cotizacion.Costo : Items().Sum(i => i.Importe);

        private float Resumen(float y)
        {
            float xr = Derecha - AnchoResumen;
            float ry = y + 4f;
            int unidades = Items().Sum(i => i.Cantidad);
            ry = FilaResumen(_t.Obtener("pcf_cotp_unidades"), unidades.ToString(), xr, ry, Tinta);
            ry = BarraTotal(_t.Obtener("pcf_oc_doc_total_orden"), Plata(Total), xr, ry + 4f);

            Etiqueta(_t.Obtener("pcf_oc_doc_condiciones"), M, y + 12f);
            float ly = y + 30f;
            string condiciones = string.IsNullOrWhiteSpace(_cotizacion?.Condiciones)
                ? _t.Obtener("pcf_cot_sin_cond") : _cotizacion.Condiciones;
            foreach (string l in _pdf.Envolver(condiciones, AnchoIzquierda, 9f, FuentePdf06AV.Normal).Take(4))
            {
                _pdf.Texto(l, M, ly, 9f, FuentePdf06AV.Normal, Tinta);
                ly += 13f;
            }

            ly += 10f;
            float alto = 46f;
            _pdf.Rect(M, ly, AnchoIzquierda, alto, CianSuave);
            _pdf.Rect(M, ly, 4f, alto, Cian);
            float ty = ly + 17f;
            foreach (string l in _pdf.Envolver(_t.Obtener("pcf_oc_doc_instrucciones", _orden.NumeroCompra), AnchoIzquierda - 30f, 8.5f, FuentePdf06AV.Normal).Take(2))
            {
                _pdf.Texto(l, M + 16f, ty, 8.5f, FuentePdf06AV.Normal, Tinta);
                ty += 12f;
            }
            ly += alto;

            if (!string.IsNullOrWhiteSpace(_orden.MotivoCierre) && _orden.Cerrada && _orden.Estado != EstadoOrdenCompra06AV.Finalizada)
            {
                ly += 14f;
                Pastilla(_t.Obtener("pcf_oc_est_" + _orden.Estado).ToUpper(), M, ly, RojoSuave, Rojo, true);
                ly += 30f;
                foreach (string l in _pdf.Envolver(_orden.MotivoCierre, AnchoIzquierda, 8.5f, FuentePdf06AV.Cursiva).Take(3))
                {
                    _pdf.Texto(l, M, ly, 8.5f, FuentePdf06AV.Cursiva, Gris);
                    ly += 12f;
                }
            }
            return Math.Max(ry, ly);
        }

        private void Firma(float y)
        {
            if (LimiteContenido - y < 60f) y = PaginaContinuacion() - 20f;
            float fy = Math.Max(y + 50f, Math.Min(LimiteContenido - 26f, y + 80f));
            float xr = Derecha - AnchoResumen;
            _pdf.Linea(xr + 10f, fy, Derecha - 10f, fy, GrisClaro, 0.8f);
            _pdf.Texto(_t.Obtener("pcf_oc_doc_firma"), xr + AnchoResumen / 2f, fy + 13f, 8f, FuentePdf06AV.Normal, Gris,
                       AlineacionPdf06AV.Centro);
        }
    }
}
