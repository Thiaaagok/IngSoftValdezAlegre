using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Controles;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;

namespace PCFORGE_ValdezThiago_96VA.Common
{
    internal sealed class ReporteVentasPdf06AV : IDisposable
    {
        public const string ImpresoraPdf = "Microsoft Print to PDF";

        public const float AnchoPagina = 1169f, AltoPagina = 827f;

        private const float Margen = 42f;
        private const float AltoFila = 19f;
        private const float AltoCabeceraTabla = 26f;
        private const float InicioContenido = 92f;
        private const float AltoPie = 34f;
        private const int MaxAtrasadasListadas = 10;

        // Paleta clara fija: el PDF no sigue el modo oscuro de la pantalla.
        private static readonly Color Tinta = Tema.Grafito900;
        private static readonly Color TintaSuave = Tema.Acero500;
        private static readonly Color Linea = Tema.Acero200;
        private static readonly Color FondoSuave = Tema.Acero50;
        private static readonly Color Primario = Tema.Cian600;
        private static readonly Color Exito = Tema.Verde600;
        private static readonly Color Acento = Tema.Naranja500;
        private static readonly Color Peligro = Tema.Rojo600;

        private readonly ReporteVentas06AV _reporte;
        private readonly ResumenReporteVentas06AV _resumen;
        private readonly DateTime _hoy;
        private readonly GestorIdioma06AV _t = GestorIdioma06AV.Instancia;
        private readonly List<List<FilaReporteVentas06AV>> _paginasDetalle;

        private readonly Font _fMarca, _fTitulo, _fSubtitulo, _fSeccion, _fTexto, _fTextoBold,
                              _fMini, _fMiniBold, _fKpiValor, _fGrande, _fTabla, _fTablaBold;

        public ReporteVentasPdf06AV(ReporteVentas06AV reporte, DateTime hoy)
        {
            _reporte = reporte ?? throw new ArgumentNullException(nameof(reporte));
            _resumen = reporte.Resumen ?? new ResumenReporteVentas06AV();
            _hoy = hoy.Date;

            _fMarca = Fuente("Segoe UI Semibold", 8f, FontStyle.Bold);
            _fTitulo = Fuente("Segoe UI Semibold", 20f, FontStyle.Bold);
            _fSubtitulo = Fuente("Segoe UI", 10f);
            _fSeccion = Fuente("Segoe UI Semibold", 11f, FontStyle.Bold);
            _fTexto = Fuente("Segoe UI", 8.5f);
            _fTextoBold = Fuente("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            _fMini = Fuente("Segoe UI", 7f);
            _fMiniBold = Fuente("Segoe UI Semibold", 7f, FontStyle.Bold);
            _fKpiValor = Fuente("Segoe UI Semibold", 16f, FontStyle.Bold);
            _fGrande = Fuente("Segoe UI Semibold", 26f, FontStyle.Bold);
            _fTabla = Fuente("Segoe UI", 7.25f);
            _fTablaBold = Fuente("Segoe UI Semibold", 7.25f, FontStyle.Bold);

            _paginasDetalle = Paginar(reporte.Filas ?? new List<FilaReporteVentas06AV>());
        }

        public int CantidadPaginas => 2 + _paginasDetalle.Count;

        public void Exportar(string ruta)
        {
            var impresora = new PrinterSettings { PrinterName = ImpresoraPdf, PrintToFile = true, PrintFileName = ruta };
            if (!impresora.IsValid)
                throw new InvalidOperationException(_t.Obtener("rpdf_sin_impresora"));

            using (var documento = new PrintDocument())
            {
                documento.DocumentName = _t.Obtener("rep_titulo");
                documento.PrinterSettings = impresora;
                documento.PrintController = new StandardPrintController();
                PaperSize a4 = impresora.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4);
                if (a4 != null) documento.DefaultPageSettings.PaperSize = a4;
                documento.DefaultPageSettings.Landscape = true;
                documento.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                int pagina = 0;
                documento.PrintPage += (s, e) =>
                {
                    float escala = Math.Min(e.PageBounds.Width / AnchoPagina, e.PageBounds.Height / AltoPagina);
                    DibujarPagina(e.Graphics, pagina, escala, e.PageSettings.HardMarginX / escala, e.PageSettings.HardMarginY / escala);
                    pagina++;
                    e.HasMorePages = pagina < CantidadPaginas;
                };
                documento.Print();
            }
        }

        public void DibujarPagina(Graphics g, int indice, float escala = 1f, float corrimientoX = 0f, float corrimientoY = 0f)
        {
            GraphicsState estado = g.Save();
            g.PageUnit = GraphicsUnit.Pixel;
            g.ResetTransform();
            g.ScaleTransform(g.DpiX / 100f * escala, g.DpiY / 100f * escala);
            g.TranslateTransform(-corrimientoX, -corrimientoY);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var fondo = new SolidBrush(Color.White)) g.FillRectangle(fondo, 0, 0, AnchoPagina, AltoPagina);

            if (indice == 0) PaginaResumen(g);
            else if (indice == 1) PaginaAnalisis(g);
            else PaginaDetalle(g, indice - 2);

            Pie(g, indice);
            g.Restore(estado);
        }

        private void PaginaResumen(Graphics g)
        {
            float ancho = AnchoPagina - 2 * Margen;

            Relleno(g, Tinta, new RectangleF(0, 0, AnchoPagina, 118));
            Relleno(g, Tema.Cian400, new RectangleF(0, 118, AnchoPagina, 4));
            Texto(g, "PC FORGE  ·  PC FACTORY", _fMarca, Tema.Cian400, new RectangleF(Margen, 24, 500, 16));
            Texto(g, _t.Obtener("rep_titulo"), _fTitulo, Color.White, new RectangleF(Margen, 40, 760, 40));
            Texto(g, _t.Obtener("rpdf_periodo", Fecha(_reporte.Filtro?.Desde), Fecha(_reporte.Filtro?.Hasta)),
                _fSubtitulo, Tema.Acero300, new RectangleF(Margen, 80, 760, 20));

            var derecha = new RectangleF(AnchoPagina - Margen - 300, 30, 300, 20);
            Texto(g, _t.Obtener("rpdf_generado_el", _reporte.GeneradoEl.ToString("dd/MM/yyyy HH:mm")), _fTexto, Tema.Acero300, derecha, StringAlignment.Far);
            derecha.Y += 18;
            Texto(g, _t.Obtener("rpdf_generado_por", Nombre(_reporte.GeneradoPor)), _fTexto, Tema.Acero300, derecha, StringAlignment.Far);
            derecha.Y += 22;
            Texto(g, ReporteVentasControl.Plural("rpdf_ventas_en_detalle", (_reporte.Filas ?? new List<FilaReporteVentas06AV>()).Count, (_reporte.Filas ?? new List<FilaReporteVentas06AV>()).Count),
                _fTextoBold, Color.White, derecha, StringAlignment.Far);

            float y = 138;
            float x = Margen;
            var f = _reporte.Filtro ?? new FiltroReporteVentas06AV();
            x = Etiqueta(g, x, y, _t.Obtener("rep_estado"), f.Estado.HasValue ? ReporteVentasControl.TextoEstadoVenta(f.Estado.Value) : _t.Obtener("rep_todos"));
            x = Etiqueta(g, x, y, _t.Obtener("rep_tipo"), f.Tipo.HasValue ? ReporteVentasControl.TextoTipo(f.Tipo.Value.ToString()) : _t.Obtener("rep_todos"));
            x = Etiqueta(g, x, y, _t.Obtener("rep_col_cliente"), string.IsNullOrWhiteSpace(f.Cliente) ? _t.Obtener("rep_todos") : f.Cliente);
            Etiqueta(g, x, y, _t.Obtener("rep_solo_atrasadas"), _t.Obtener(f.SoloAtrasadas ? "rep_si" : "rep_no"));

            y = 180;
            float sep = 12;
            float anchoKpi = (ancho - 5 * sep) / 6;
            var r = _resumen;
            var kpis = new[]
            {
                Tuple.Create(_t.Obtener("rep_kpi_ventas"), r.CantidadVentas.ToString(),
                    _t.Obtener("rep_kpi_ventas_pie", ReporteVentasControl.Plural("rep_n_vigentes", r.CantidadVigentes, r.CantidadVigentes),
                        ReporteVentasControl.Plural("rep_n_anuladas", r.CantidadAnuladas, r.CantidadAnuladas)), Primario),
                Tuple.Create(_t.Obtener("rep_kpi_facturacion"), ReporteVentasControl.Plata(r.Facturacion),
                    _t.Obtener("rep_kpi_facturacion_pie"), Primario),
                Tuple.Create(_t.Obtener("rep_kpi_cobrado"), ReporteVentasControl.Plata(r.TotalCobrado),
                    _t.Obtener("rep_kpi_cobrado_pie", r.PorcentajeSenadas.ToString("0.#", CultureInfo.CurrentCulture)), Exito),
                Tuple.Create(_t.Obtener("rep_kpi_saldo"), ReporteVentasControl.Plata(r.SaldoPendiente),
                    ReporteVentasControl.Plural("rep_kpi_saldo_pie", r.PendientesDeSena, r.PendientesDeSena), Acento),
                Tuple.Create(_t.Obtener("rep_kpi_ticket"), ReporteVentasControl.Plata(r.TicketPromedio),
                    r.DiasPromedioEntrega.HasValue
                        ? ReporteVentasControl.Plural("rep_kpi_ticket_pie", r.DiasPromedioEntrega.Value == 1m ? 1 : 2, r.DiasPromedioEntrega.Value.ToString("0.#", CultureInfo.CurrentCulture))
                        : _t.Obtener("rep_kpi_ticket_sin"), Primario),
                Tuple.Create(_t.Obtener("rep_kpi_atrasadas"), r.Atrasadas.ToString(),
                    _t.Obtener("rep_kpi_atrasadas_pie", r.EnProduccion, ReporteVentasControl.Plural("rep_n_listas", r.ListasParaEntregar, r.ListasParaEntregar)), r.Atrasadas > 0 ? Peligro : Exito)
            };
            for (int i = 0; i < kpis.Length; i++)
                Indicador(g, new RectangleF(Margen + i * (anchoKpi + sep), y, anchoKpi, 84),
                    kpis[i].Item1, kpis[i].Item2, kpis[i].Item3, kpis[i].Item4);

            y = 286;
            float alto = AltoPagina - AltoPie - 14 - y;
            float anchoPanel = (ancho - 2 * 16) / 3;
            PanelEstados(g, new RectangleF(Margen, y, anchoPanel, alto));
            PanelCobranza(g, new RectangleF(Margen + anchoPanel + 16, y, anchoPanel, alto));
            PanelCircuito(g, new RectangleF(Margen + 2 * (anchoPanel + 16), y, anchoPanel, alto));
        }

        private void PanelEstados(Graphics g, RectangleF caja)
        {
            RectangleF area = Panel(g, caja, _t.Obtener("rpdf_ventas_por_estado"), _t.Obtener("rpdf_ventas_por_estado_sub"));
            var grupos = _resumen.PorEstado ?? new List<GrupoReporte06AV>();
            int total = grupos.Sum(x => x.Cantidad);

            float diametro = Math.Min(Math.Min(205, area.Width - 40), area.Height - 6 - 18 - 17 - 5 * 26 - 4);
            var dona = new RectangleF(area.X + (area.Width - diametro) / 2, area.Y + 6, diametro, diametro);
            if (total == 0)
            {
                using (var p = new Pen(Linea, 30)) g.DrawEllipse(p, Achicar(dona, 15));
            }
            else
            {
                float inicio = -90;
                foreach (var gr in grupos.Where(x => x.Cantidad > 0))
                {
                    float barrido = 360f * gr.Cantidad / total;
                    using (var p = new Pen(ColorEstado(Estado(gr.Clave)), 30)) g.DrawArc(p, Achicar(dona, 15), inicio, barrido);
                    inicio += barrido;
                }
            }
            Texto(g, total.ToString(), _fGrande, Tinta, new RectangleF(dona.X, dona.Y + diametro / 2 - 30, diametro, 44), StringAlignment.Center);
            Texto(g, ReporteVentasControl.Plural("rpdf_ventas", total), _fMini, TintaSuave, new RectangleF(dona.X, dona.Y + diametro / 2 + 12, diametro, 14), StringAlignment.Center);

            float y = dona.Bottom + 18;
            Texto(g, _t.Obtener("rep_col_estado_venta"), _fMiniBold, TintaSuave, new RectangleF(area.X + 16, y, 150, 14));
            Texto(g, _t.Obtener("rep_col_cant"), _fMiniBold, TintaSuave, new RectangleF(area.Right - 196, y, 44, 14), StringAlignment.Far);
            Texto(g, "%", _fMiniBold, TintaSuave, new RectangleF(area.Right - 148, y, 44, 14), StringAlignment.Far);
            Texto(g, _t.Obtener("rep_col_importe"), _fMiniBold, TintaSuave, new RectangleF(area.Right - 100, y, 96, 14), StringAlignment.Far);
            y += 17;
            foreach (var gr in grupos)
            {
                var est = Estado(gr.Clave);
                Relleno(g, FondoSuave, new RectangleF(area.X, y - 3, area.Width, 22), 4);
                Relleno(g, ColorEstado(est), new RectangleF(area.X + 5, y + 3, 10, 10), 2);
                TextoAjustado(g, ReporteVentasControl.TextoEstadoVenta(est), _fTexto, Tinta, new RectangleF(area.X + 21, y, area.Width - 21 - 196, 16));
                Texto(g, gr.Cantidad.ToString(), _fTextoBold, Tinta, new RectangleF(area.Right - 196, y, 44, 16), StringAlignment.Far);
                Texto(g, gr.Porcentaje.ToString("0.#", CultureInfo.CurrentCulture), _fTexto, TintaSuave, new RectangleF(area.Right - 148, y, 44, 16), StringAlignment.Far);
                TextoAjustado(g, est == EstadoVenta06AV.Anulada ? "—" : ReporteVentasControl.Plata(gr.Importe), _fTexto, Tinta,
                    new RectangleF(area.Right - 100, y, 96, 16), StringAlignment.Far);
                y += 26;
            }
        }

        private void PanelCobranza(Graphics g, RectangleF caja)
        {
            RectangleF area = Panel(g, caja, _t.Obtener("rpdf_cobranza"), _t.Obtener("rpdf_cobranza_sub"));
            var r = _resumen;
            decimal facturado = r.Facturacion;
            float porcCobrado = facturado <= 0 ? 0f : (float)Math.Min(1m, r.TotalCobrado / facturado);

            Texto(g, (porcCobrado * 100).ToString("0.#", CultureInfo.CurrentCulture) + " %", _fGrande, Exito,
                new RectangleF(area.X, area.Y + 4, area.Width, 46));
            Texto(g, _t.Obtener("rpdf_cobrado_de_facturado"), _fTexto, TintaSuave, new RectangleF(area.X, area.Y + 50, area.Width, 16));

            float y = area.Y + 80;
            var barra = new RectangleF(area.X, y, area.Width, 32);
            Relleno(g, Tema.Naranja50, barra, 6);
            using (var p = new Pen(Tema.Naranja500, 1)) Contorno(g, p, barra, 6);
            if (porcCobrado > 0)
                Relleno(g, Exito, new RectangleF(barra.X, barra.Y, Math.Max(12, barra.Width * porcCobrado), barra.Height), 6);
            y = barra.Bottom + 16;

            y = Renglon(g, area, y, Exito, _t.Obtener("rep_kpi_cobrado"), ReporteVentasControl.Plata(r.TotalCobrado));
            y = Renglon(g, area, y, Acento, _t.Obtener("rep_kpi_saldo"), ReporteVentasControl.Plata(r.SaldoPendiente));
            y = Renglon(g, area, y, Primario, _t.Obtener("rep_kpi_facturacion"), ReporteVentasControl.Plata(facturado));

            y = Math.Max(y + 16, area.Bottom - 150);
            Separador(g, area.X, y, area.Width);
            y += 18;
            Texto(g, _t.Obtener("rpdf_ventas_con_sena"), _fTextoBold, Tinta, new RectangleF(area.X, y, area.Width - 60, 16));
            Texto(g, r.PorcentajeSenadas.ToString("0.#", CultureInfo.CurrentCulture) + " %", _fTextoBold, Primario,
                new RectangleF(area.Right - 70, y, 70, 16), StringAlignment.Far);
            y += 22;
            var progreso = new RectangleF(area.X, y, area.Width, 10);
            Relleno(g, Linea, progreso, 5);
            float ps = (float)Math.Min(100m, Math.Max(0m, r.PorcentajeSenadas)) / 100f;
            if (ps > 0) Relleno(g, Primario, new RectangleF(progreso.X, progreso.Y, Math.Max(10, progreso.Width * ps), progreso.Height), 5);
            y += 24;
            Texto(g, ReporteVentasControl.Plural("rpdf_pendientes_sena", r.PendientesDeSena, r.PendientesDeSena), _fTexto, TintaSuave, new RectangleF(area.X, y, area.Width, 34), StringAlignment.Near, true);
            y += 36;
            Texto(g, _t.Obtener("rpdf_ticket", ReporteVentasControl.Plata(r.TicketPromedio)), _fTexto, TintaSuave, new RectangleF(area.X, y, area.Width, 18));
        }

        private void PanelCircuito(Graphics g, RectangleF caja)
        {
            RectangleF area = Panel(g, caja, _t.Obtener("rpdf_circuito"), _t.Obtener("rpdf_circuito_sub"));
            var r = _resumen;
            var pasos = new[]
            {
                Tuple.Create(_t.Obtener("rpdf_paso_sena"), r.PendientesDeSena, Tema.Amber500),
                Tuple.Create(_t.Obtener("rpdf_paso_produccion"), r.EnProduccion, Tema.Cian700),
                Tuple.Create(_t.Obtener("rpdf_paso_listas"), r.ListasParaEntregar, Tema.Cian400),
                Tuple.Create(_t.Obtener("rpdf_paso_entregadas"), r.Entregadas, Exito)
            };
            int maximo = Math.Max(1, pasos.Max(p => p.Item2));
            float alto = Math.Min(64, (area.Height - 110) / pasos.Length);
            float y = area.Y + 4;
            for (int i = 0; i < pasos.Length; i++)
            {
                var p = pasos[i];
                Relleno(g, p.Item3, new RectangleF(area.X, y + 2, 22, 22), 11);
                Texto(g, (i + 1).ToString(), _fMiniBold, Color.White, new RectangleF(area.X, y + 6, 22, 14), StringAlignment.Center);
                Texto(g, p.Item1, _fTextoBold, Tinta, new RectangleF(area.X + 30, y, area.Width - 80, 16));
                Texto(g, p.Item2.ToString(), _fSeccion, p.Item3, new RectangleF(area.Right - 60, y - 2, 60, 20), StringAlignment.Far);
                var pista = new RectangleF(area.X + 30, y + 19, area.Width - 30, 9);
                Relleno(g, FondoSuave, pista, 4);
                if (p.Item2 > 0) Relleno(g, p.Item3, new RectangleF(pista.X, pista.Y, Math.Max(9, pista.Width * p.Item2 / maximo), pista.Height), 4);
                if (i < pasos.Length - 1)
                    using (var pen = new Pen(Linea, 2)) g.DrawLine(pen, area.X + 11, y + 26, area.X + 11, y + alto - 2);
                y += alto;
            }

            bool hayAtraso = r.Atrasadas > 0;
            var aviso = new RectangleF(area.X, area.Bottom - 94, area.Width, 66);
            Relleno(g, hayAtraso ? Tema.Rojo50 : Tema.Verde50, aviso, 8);
            Relleno(g, hayAtraso ? Peligro : Exito, new RectangleF(aviso.X, aviso.Y, 5, aviso.Height), 2);
            Texto(g, hayAtraso ? ReporteVentasControl.Plural("rpdf_atrasadas_titulo", r.Atrasadas, r.Atrasadas) : _t.Obtener("rpdf_sin_atrasadas_titulo"),
                _fTextoBold, hayAtraso ? Tema.Rojo700 : Tema.Verde700, new RectangleF(aviso.X + 14, aviso.Y + 10, aviso.Width - 24, 16));
            Texto(g, hayAtraso ? ReporteVentasControl.Plural("rpdf_atrasadas_sub", r.Atrasadas) : _t.Obtener("rpdf_sin_atrasadas_sub"),
                _fMini, Tinta, new RectangleF(aviso.X + 14, aviso.Y + 28, aviso.Width - 24, 34), StringAlignment.Near, true);

            y = aviso.Bottom + 10;
            string entrega = r.DiasPromedioEntrega.HasValue
                ? ReporteVentasControl.Plural("rpdf_dias_entrega", r.DiasPromedioEntrega.Value == 1m ? 1 : 2, r.DiasPromedioEntrega.Value.ToString("0.#", CultureInfo.CurrentCulture))
                : _t.Obtener("rep_kpi_ticket_sin");
            Texto(g, entrega, _fTexto, TintaSuave, new RectangleF(area.X, y, area.Width, 30));
        }

        private void PaginaAnalisis(Graphics g)
        {
            Cabecera(g, _t.Obtener("rpdf_analisis"));
            float ancho = AnchoPagina - 2 * Margen;
            float y = InicioContenido;
            float altoEvolucion = 300;
            PanelEvolucion(g, new RectangleF(Margen, y, ancho, altoEvolucion));

            y += altoEvolucion + 16;
            float alto = AltoPagina - AltoPie - 14 - y;
            float anchoTipo = 300, anchoModelos = 380;
            PanelTipos(g, new RectangleF(Margen, y, anchoTipo, alto));
            PanelModelos(g, new RectangleF(Margen + anchoTipo + 16, y, anchoModelos, alto));
            PanelAtrasadas(g, new RectangleF(Margen + anchoTipo + anchoModelos + 32, y, ancho - anchoTipo - anchoModelos - 32, alto));
        }

        private void PanelEvolucion(Graphics g, RectangleF caja)
        {
            var tramos = Tramos(out string subtitulo);
            RectangleF area = Panel(g, caja, _t.Obtener("rpdf_evolucion"), subtitulo);
            if (tramos.All(t => t.Item3 == 0))
            {
                SinDatos(g, area);
                return;
            }

            decimal maximo = EscalaRedonda(tramos.Max(t => t.Item2));
            var grafico = new RectangleF(area.X + 64, area.Y + 6, area.Width - 70, area.Height - 44);

            using (var guia = new Pen(Linea, 1) { DashStyle = DashStyle.Dash })
            {
                for (int i = 0; i <= 4; i++)
                {
                    float gy = grafico.Bottom - grafico.Height * i / 4f;
                    g.DrawLine(guia, grafico.X, gy, grafico.Right, gy);
                    Texto(g, Compacto(maximo * i / 4), _fMini, TintaSuave, new RectangleF(area.X, gy - 7, 58, 14), StringAlignment.Far);
                }
            }
            using (var eje = new Pen(Tema.Acero300, 1)) g.DrawLine(eje, grafico.X, grafico.Bottom, grafico.Right, grafico.Bottom);

            float paso = grafico.Width / tramos.Count;
            float anchoBarra = Math.Min(46, paso * 0.62f);
            bool etiquetas = tramos.Count <= 16;
            int saltoRotulo = Math.Max(1, (int)Math.Ceiling(tramos.Count / 16.0));
            for (int i = 0; i < tramos.Count; i++)
            {
                var tr = tramos[i];
                float cx = grafico.X + paso * i + paso / 2;
                float h = maximo <= 0 ? 0 : (float)(tr.Item2 / maximo) * grafico.Height;
                if (h > 0)
                {
                    var barra = new RectangleF(cx - anchoBarra / 2, grafico.Bottom - h, anchoBarra, h);
                    using (var pincel = new LinearGradientBrush(new RectangleF(barra.X, barra.Y - 1, barra.Width, barra.Height + 2),
                        Tema.Cian400, Primario, LinearGradientMode.Vertical))
                        RellenoSuperior(g, pincel, barra, Math.Min(5, anchoBarra / 2));
                    if (etiquetas)
                        Texto(g, Compacto(tr.Item2), _fMiniBold, Tinta, new RectangleF(cx - 40, barra.Y - 15, 80, 13), StringAlignment.Center);
                }
                if (i % saltoRotulo == 0)
                {
                    Texto(g, tr.Item1, _fMini, TintaSuave, new RectangleF(cx - paso * saltoRotulo / 2, grafico.Bottom + 5, paso * saltoRotulo, 13), StringAlignment.Center);
                    if (etiquetas && tr.Item3 > 0)
                        Texto(g, ReporteVentasControl.Plural("rpdf_n_ventas", tr.Item3, tr.Item3), _fMini, Tema.Acero300,
                            new RectangleF(cx - paso / 2, grafico.Bottom + 18, paso, 13), StringAlignment.Center);
                }
            }
        }

        private void PanelTipos(Graphics g, RectangleF caja)
        {
            RectangleF area = Panel(g, caja, _t.Obtener("rep_corte_tipo"), _t.Obtener("rpdf_sin_anuladas"));
            var grupos = _resumen.PorTipo ?? new List<GrupoReporte06AV>();
            if (grupos.Sum(x => x.Cantidad) == 0) { SinDatos(g, area); return; }
            Color[] colores = { Primario, Acento };
            float y = area.Y + 6;
            for (int i = 0; i < grupos.Count; i++)
            {
                var gr = grupos[i];
                Color c = colores[i % colores.Length];
                Texto(g, ReporteVentasControl.TextoTipo(gr.Clave), _fTextoBold, Tinta, new RectangleF(area.X, y, area.Width - 60, 16));
                Texto(g, gr.Porcentaje.ToString("0.#", CultureInfo.CurrentCulture) + " %", _fSeccion, c,
                    new RectangleF(area.Right - 70, y - 3, 70, 20), StringAlignment.Far);
                var pista = new RectangleF(area.X, y + 22, area.Width, 14);
                Relleno(g, FondoSuave, pista, 7);
                if (gr.Porcentaje > 0) Relleno(g, c, new RectangleF(pista.X, pista.Y, Math.Max(14, pista.Width * (float)gr.Porcentaje / 100f), pista.Height), 7);
                Texto(g, ReporteVentasControl.Plural("rpdf_cant_importe", gr.Cantidad, gr.Cantidad, ReporteVentasControl.Plata(gr.Importe)), _fMini, TintaSuave,
                    new RectangleF(area.X, y + 40, area.Width, 14));
                y += 84;
            }
        }

        private void PanelModelos(Graphics g, RectangleF caja)
        {
            RectangleF area = Panel(g, caja, _t.Obtener("rep_corte_modelo", ReporteVentasBLL06AV.TopModelos), _t.Obtener("rpdf_modelos_sub"));
            var grupos = _resumen.PorModelo ?? new List<GrupoReporte06AV>();
            if (grupos.Count == 0) { SinDatos(g, area); return; }
            int maximo = Math.Max(1, grupos.Max(x => x.Cantidad));
            float y = area.Y + 4;
            float alto = Math.Min(40, (area.Height - 8) / Math.Max(1, grupos.Count));
            for (int i = 0; i < grupos.Count; i++)
            {
                var gr = grupos[i];
                string nombre = gr.Clave == ReporteVentasBLL06AV.NombreAMedida ? _t.Obtener("rep_a_medida") : gr.Clave;
                Texto(g, (i + 1) + ".  " + nombre, _fTextoBold, Tinta, new RectangleF(area.X, y, area.Width - 110, 16));
                Texto(g, ReporteVentasControl.Plata(gr.Importe), _fTexto, TintaSuave, new RectangleF(area.Right - 110, y, 110, 16), StringAlignment.Far);
                var pista = new RectangleF(area.X, y + 18, area.Width - 40, 10);
                Relleno(g, FondoSuave, pista, 5);
                Relleno(g, i == 0 ? Primario : Tema.Cian400, new RectangleF(pista.X, pista.Y, Math.Max(10, pista.Width * gr.Cantidad / maximo), pista.Height), 5);
                Texto(g, gr.Cantidad.ToString(), _fMiniBold, Tinta, new RectangleF(pista.Right + 4, pista.Y - 2, 36, 14), StringAlignment.Far);
                y += alto;
            }
        }

        private void PanelAtrasadas(Graphics g, RectangleF caja)
        {
            var atrasadas = (_reporte.Filas ?? new List<FilaReporteVentas06AV>())
                .Where(f => f.EstadoVenta != EstadoVenta06AV.Anulada && f.EstaAtrasada(_hoy))
                .OrderByDescending(f => f.DiasDeAtraso(_hoy)).ThenBy(f => f.NumeroVenta).ToList();
            RectangleF area = Panel(g, caja, _t.Obtener("rpdf_atencion"), ReporteVentasControl.Plural("rpdf_atencion_sub", atrasadas.Count, atrasadas.Count));
            if (atrasadas.Count == 0)
            {
                var ok = new RectangleF(area.X, area.Y + 10, area.Width, 60);
                Relleno(g, Tema.Verde50, ok, 8);
                Texto(g, "✓  " + _t.Obtener("rpdf_sin_atrasadas_titulo"), _fTextoBold, Tema.Verde700, new RectangleF(ok.X + 14, ok.Y + 12, ok.Width - 20, 16));
                Texto(g, _t.Obtener("rpdf_sin_atrasadas_sub"), _fMini, Tinta, new RectangleF(ok.X + 14, ok.Y + 30, ok.Width - 20, 28), StringAlignment.Near, true);
                return;
            }
            float y = area.Y + 2;
            int mostradas = Math.Min(MaxAtrasadasListadas, atrasadas.Count);
            float alto = Math.Min(34, (area.Height - 20) / mostradas);
            for (int i = 0; i < mostradas; i++)
            {
                var f = atrasadas[i];
                Relleno(g, i % 2 == 0 ? Tema.Rojo50 : Color.White, new RectangleF(area.X, y, area.Width, alto - 3), 5);
                var insignia = new RectangleF(area.Right - 64, y + (alto - 3 - 18) / 2, 58, 18);
                Relleno(g, Peligro, insignia, 9);
                Texto(g, ReporteVentasControl.Plural("rpdf_dias", f.DiasDeAtraso(_hoy), f.DiasDeAtraso(_hoy)), _fMiniBold, Color.White, new RectangleF(insignia.X, insignia.Y + 3, insignia.Width, 13), StringAlignment.Center);
                Texto(g, "#" + f.NumeroVenta + "  " + f.ClienteNombre, _fTextoBold, Tinta, new RectangleF(area.X + 8, y + 2, area.Width - 84, 15));
                Texto(g, _t.Obtener("rpdf_comprometida", f.FechaEntregaComprometida.ToString("dd/MM/yyyy")) + "  ·  " + ReporteVentasControl.TextoOrden(f),
                    _fMini, TintaSuave, new RectangleF(area.X + 8, y + 16, area.Width - 84, 13));
                y += alto;
            }
            if (atrasadas.Count > mostradas)
                Texto(g, ReporteVentasControl.Plural("rpdf_y_mas", atrasadas.Count - mostradas, atrasadas.Count - mostradas), _fMini, TintaSuave, new RectangleF(area.X, y + 2, area.Width, 14));
        }

        private static readonly float[] PesosColumnas = { 5.5f, 7f, 14.5f, 16f, 7f, 8.5f, 8.5f, 8.5f, 11.5f, 14f, 7f, 5.5f };

        private void PaginaDetalle(Graphics g, int indice)
        {
            Cabecera(g, _t.Obtener("rpdf_detalle", indice + 1, _paginasDetalle.Count));
            float ancho = AnchoPagina - 2 * Margen;
            float suma = PesosColumnas.Sum();
            float[] anchos = PesosColumnas.Select(p => ancho * p / suma).ToArray();
            string[] titulos =
            {
                _t.Obtener("rep_col_venta"), _t.Obtener("rep_col_fecha"), _t.Obtener("rep_col_cliente"), _t.Obtener("rep_col_equipo"),
                _t.Obtener("rep_col_tipo"), _t.Obtener("rep_col_total"), _t.Obtener("rep_col_cobrado"), _t.Obtener("rep_col_saldo"),
                _t.Obtener("rep_col_estado_venta"), _t.Obtener("rep_col_orden"), _t.Obtener("rep_col_entrega"), _t.Obtener("rep_col_atraso")
            };
            bool[] derecha = { false, false, false, false, false, true, true, true, false, false, false, true };

            float y = InicioContenido;
            var cabecera = new RectangleF(Margen, y, ancho, AltoCabeceraTabla);
            Relleno(g, Tinta, cabecera, 5);
            float x = Margen;
            for (int c = 0; c < titulos.Length; c++)
            {
                TextoAjustado(g, titulos[c], _fTablaBold, Color.White, new RectangleF(x + 5, y + 7, anchos[c] - 10, 14),
                    derecha[c] ? StringAlignment.Far : StringAlignment.Near);
                x += anchos[c];
            }
            y += AltoCabeceraTabla + 2;

            var filas = _paginasDetalle[indice];
            for (int i = 0; i < filas.Count; i++)
            {
                var f = filas[i];
                bool anulada = f.EstadoVenta == EstadoVenta06AV.Anulada;
                int atraso = anulada ? 0 : f.DiasDeAtraso(_hoy);
                Color fondo = atraso > 0 ? Tema.Amber50 : (i % 2 == 1 ? FondoSuave : Color.White);
                Relleno(g, fondo, new RectangleF(Margen, y, ancho, AltoFila));
                using (var p = new Pen(Linea, 0.6f)) g.DrawLine(p, Margen, y + AltoFila, Margen + ancho, y + AltoFila);
                if (atraso > 0) Relleno(g, Tema.Amber500, new RectangleF(Margen, y, 3, AltoFila));

                Color tinta = anulada ? Tema.Acero300 : Tinta;
                string[] valores =
                {
                    "#" + f.NumeroVenta, f.FechaVenta.ToString("dd/MM/yy"), f.ClienteNombre, f.Equipo,
                    ReporteVentasControl.TextoTipo(f.TipoConfiguracion.ToString()),
                    ReporteVentasControl.Plata(f.Total), ReporteVentasControl.Plata(f.TotalCobrado), ReporteVentasControl.Plata(f.SaldoPendiente),
                    null, ReporteVentasControl.TextoOrden(f), f.FechaEntregaComprometida.ToString("dd/MM/yy"),
                    atraso > 0 ? _t.Obtener("rpdf_dias_corto", atraso) : "—"
                };
                x = Margen;
                for (int c = 0; c < valores.Length; c++)
                {
                    var celda = new RectangleF(x + 5, y + 4, anchos[c] - 10, AltoFila - 5);
                    if (c == 8)
                        Pastilla(g, new RectangleF(x + 4, y + 3, anchos[c] - 8, AltoFila - 6), ReporteVentasControl.TextoEstadoVenta(f.EstadoVenta), ColorEstado(f.EstadoVenta));
                    else
                    {
                        Font fuente = c == 0 || c == 5 ? _fTablaBold : _fTabla;
                        Color color = tinta;
                        if (!anulada && c == 7 && f.SaldoPendiente > 0) color = Tema.Naranja600;
                        if (!anulada && c == 6 && f.TotalCobrado > 0) color = Tema.Verde700;
                        if (c == 11 && atraso > 0) { color = Peligro; fuente = _fTablaBold; }
                        if (c == 9 && !f.NumeroOrden.HasValue) color = TintaSuave;
                        Texto(g, valores[c], fuente, color, celda, derecha[c] ? StringAlignment.Far : StringAlignment.Near);
                    }
                    x += anchos[c];
                }
                if (anulada)
                    using (var p = new Pen(Tema.Acero300, 0.8f))
                        g.DrawLine(p, Margen + anchos[0] + 4, y + AltoFila / 2, Margen + anchos.Take(8).Sum() - 4, y + AltoFila / 2);
                y += AltoFila;
            }

            if (indice == _paginasDetalle.Count - 1) FilaTotales(g, y + 4, anchos);
        }

        private void FilaTotales(Graphics g, float y, float[] anchos)
        {
            float ancho = anchos.Sum();
            var fila = new RectangleF(Margen, y, ancho, AltoFila + 6);
            Relleno(g, Tema.Cian50, fila, 5);
            using (var p = new Pen(Primario, 1.2f)) g.DrawLine(p, Margen, y, Margen + ancho, y);
            float x0 = Margen + anchos.Take(5).Sum();
            Texto(g, _t.Obtener("rpdf_totales", ReporteVentasControl.Plural("rpdf_n_vigentes", _resumen.CantidadVigentes, _resumen.CantidadVigentes),
                ReporteVentasControl.Plural("rpdf_anuladas_no_suman", _resumen.CantidadAnuladas, _resumen.CantidadAnuladas)), _fTablaBold, Tinta,
                new RectangleF(Margen + 8, y + 7, x0 - Margen - 16, 14));
            decimal[] montos = { _resumen.Facturacion, _resumen.TotalCobrado, _resumen.SaldoPendiente };
            Color[] colores = { Tinta, Tema.Verde700, Tema.Naranja600 };
            float x = x0;
            for (int i = 0; i < 3; i++)
            {
                Texto(g, ReporteVentasControl.Plata(montos[i]), _fTablaBold, colores[i], new RectangleF(x + 5, y + 7, anchos[5 + i] - 10, 14), StringAlignment.Far);
                x += anchos[5 + i];
            }
        }

        private static List<List<FilaReporteVentas06AV>> Paginar(IList<FilaReporteVentas06AV> filas)
        {
            int porPagina = (int)Math.Floor((AltoPagina - AltoPie - 14 - InicioContenido - AltoCabeceraTabla - 2) / AltoFila);
            var paginas = new List<List<FilaReporteVentas06AV>>();
            int i = 0;
            do
            {
                var pagina = filas.Skip(i).Take(porPagina).ToList();
                i += pagina.Count;
                if (i >= filas.Count && pagina.Count > porPagina - 2 && pagina.Count > 1)
                {
                    pagina.RemoveAt(pagina.Count - 1);
                    i--;
                }
                paginas.Add(pagina);
            } while (i < filas.Count);
            return paginas;
        }

        private void Cabecera(Graphics g, string seccion)
        {
            Texto(g, "PC FORGE  ·  PC FACTORY", _fMarca, Primario, new RectangleF(Margen, 26, 400, 14));
            Texto(g, _t.Obtener("rep_titulo"), _fSeccion, Tinta, new RectangleF(Margen, 40, 600, 22));
            Texto(g, seccion, _fSeccion, Primario, new RectangleF(AnchoPagina - Margen - 460, 40, 460, 22), StringAlignment.Far);
            Texto(g, _t.Obtener("rpdf_periodo", Fecha(_reporte.Filtro?.Desde), Fecha(_reporte.Filtro?.Hasta)), _fMini, TintaSuave,
                new RectangleF(AnchoPagina - Margen - 460, 26, 460, 14), StringAlignment.Far);
            Relleno(g, Tinta, new RectangleF(Margen, 68, AnchoPagina - 2 * Margen, 2));
            Relleno(g, Tema.Cian400, new RectangleF(Margen, 68, 90, 2));
        }

        private void Pie(Graphics g, int indice)
        {
            float y = AltoPagina - AltoPie;
            using (var p = new Pen(Linea, 1)) g.DrawLine(p, Margen, y, AnchoPagina - Margen, y);
            Texto(g, "PC Forge · PC Factory  —  " + _t.Obtener("rep_titulo") + "  ·  " +
                     _t.Obtener("rpdf_generado_el", _reporte.GeneradoEl.ToString("dd/MM/yyyy HH:mm")) + "  ·  " +
                     _t.Obtener("rpdf_generado_por", Nombre(_reporte.GeneradoPor)),
                _fMini, TintaSuave, new RectangleF(Margen, y + 9, 800, 14));
            Texto(g, _t.Obtener("rpdf_pagina", indice + 1, CantidadPaginas), _fMiniBold, TintaSuave,
                new RectangleF(AnchoPagina - Margen - 200, y + 9, 200, 14), StringAlignment.Far);
        }

        private RectangleF Panel(Graphics g, RectangleF caja, string titulo, string subtitulo)
        {
            Relleno(g, Color.White, caja, 10);
            using (var p = new Pen(Linea, 1)) Contorno(g, p, caja, 10);
            Texto(g, titulo, _fSeccion, Tinta, new RectangleF(caja.X + 16, caja.Y + 12, caja.Width - 32, 20));
            float y = caja.Y + 33;
            if (!string.IsNullOrEmpty(subtitulo))
            {
                Texto(g, subtitulo, _fMini, TintaSuave, new RectangleF(caja.X + 16, y, caja.Width - 32, 14));
                y += 16;
            }
            y += 8;
            return new RectangleF(caja.X + 16, y, caja.Width - 32, caja.Bottom - y - 14);
        }

        private void Indicador(Graphics g, RectangleF caja, string rotulo, string valor, string pie, Color acento)
        {
            Relleno(g, Color.White, caja, 8);
            using (var p = new Pen(Linea, 1)) Contorno(g, p, caja, 8);
            Relleno(g, acento, new RectangleF(caja.X, caja.Y + 10, 4, caja.Height - 20), 2);
            Texto(g, rotulo, _fMiniBold, TintaSuave, new RectangleF(caja.X + 14, caja.Y + 12, caja.Width - 22, 13));
            TextoAjustado(g, valor, _fKpiValor, acento, new RectangleF(caja.X + 12, caja.Y + 28, caja.Width - 20, 30));
            Texto(g, pie, _fMini, TintaSuave, new RectangleF(caja.X + 14, caja.Y + 60, caja.Width - 20, 14));
        }

        private float Etiqueta(Graphics g, float x, float y, string rotulo, string valor)
        {
            string texto = rotulo + ":";
            float a1 = g.MeasureString(rotulo + ":", _fMini).Width + 5;
            float a2 = Math.Min(260, g.MeasureString(valor, _fMiniBold).Width);
            var caja = new RectangleF(x, y, a1 + a2 + 22, 22);
            Relleno(g, Tema.Acero100, caja, 11);
            Texto(g, texto, _fMini, TintaSuave, new RectangleF(x + 11, y + 5, a1 + 2, 13));
            Texto(g, valor, _fMiniBold, Tinta, new RectangleF(x + 11 + a1, y + 5, a2 + 4, 13));
            return caja.Right + 8;
        }

        private float Renglon(Graphics g, RectangleF area, float y, Color color, string rotulo, string valor)
        {
            Relleno(g, color, new RectangleF(area.X, y + 4, 10, 10), 2);
            Texto(g, rotulo, _fTexto, Tinta, new RectangleF(area.X + 16, y, area.Width - 140, 16));
            Texto(g, valor, _fTextoBold, Tinta, new RectangleF(area.Right - 140, y, 140, 16), StringAlignment.Far);
            return y + 26;
        }

        private void Pastilla(Graphics g, RectangleF caja, string texto, Color color)
        {
            Relleno(g, Color.FromArgb(38, color), caja, caja.Height / 2);
            TextoAjustado(g, texto, _fTablaBold, Oscurecer(color), new RectangleF(caja.X + 5, caja.Y + 1, caja.Width - 10, caja.Height - 1), StringAlignment.Center);
        }

        private void SinDatos(Graphics g, RectangleF area)
        {
            Texto(g, _t.Obtener("rep_sin_datos"), _fTexto, TintaSuave, new RectangleF(area.X, area.Y + area.Height / 2 - 10, area.Width, 20), StringAlignment.Center);
        }

        private static void Separador(Graphics g, float x, float y, float ancho)
        {
            using (var p = new Pen(Linea, 1)) g.DrawLine(p, x, y, x + ancho, y);
        }

        private static void Texto(Graphics g, string texto, Font fuente, Color color, RectangleF caja,
            StringAlignment alineacion = StringAlignment.Near, bool envolver = false)
        {
            if (string.IsNullOrEmpty(texto)) return;
            using (var pincel = new SolidBrush(color))
            using (var formato = new StringFormat(envolver ? 0 : StringFormatFlags.NoWrap)
            {
                Alignment = alineacion,
                LineAlignment = StringAlignment.Near,
                Trimming = envolver ? StringTrimming.Word : StringTrimming.EllipsisCharacter
            })
                g.DrawString(texto, fuente, pincel, caja, formato);
        }

        private static void TextoAjustado(Graphics g, string texto, Font fuente, Color color, RectangleF caja,
            StringAlignment alineacion = StringAlignment.Near)
        {
            if (string.IsNullOrEmpty(texto)) return;
            float ancho = g.MeasureString(texto, fuente, PointF.Empty, StringFormat.GenericTypographic).Width + 2;
            if (ancho <= caja.Width) { Texto(g, texto, fuente, color, caja, alineacion); return; }
            float tamano = Math.Max(fuente.Size * 0.6f, fuente.Size * caja.Width / ancho);
            using (var chica = new Font(fuente.FontFamily, tamano, fuente.Style, fuente.Unit))
            {
                float dy = (fuente.GetHeight(g) - chica.GetHeight(g)) / 2;
                Texto(g, texto, chica, color, new RectangleF(caja.X, caja.Y + Math.Max(0, dy), caja.Width, caja.Height), alineacion);
            }
        }

        private static void Relleno(Graphics g, Color color, RectangleF caja, float radio = 0)
        {
            using (var pincel = new SolidBrush(color)) Relleno(g, pincel, caja, radio);
        }

        private static void Relleno(Graphics g, Brush pincel, RectangleF caja, float radio)
        {
            if (caja.Width <= 0 || caja.Height <= 0) return;
            if (radio <= 0) { g.FillRectangle(pincel, caja); return; }
            using (var camino = Redondeado(caja, radio)) g.FillPath(pincel, camino);
        }

        private static void RellenoSuperior(Graphics g, Brush pincel, RectangleF caja, float radio)
        {
            if (caja.Height <= radio * 2) { g.FillRectangle(pincel, caja); return; }
            using (var camino = new GraphicsPath())
            {
                float d = radio * 2;
                camino.AddArc(caja.X, caja.Y, d, d, 180, 90);
                camino.AddArc(caja.Right - d, caja.Y, d, d, 270, 90);
                camino.AddLine(caja.Right, caja.Bottom, caja.X, caja.Bottom);
                camino.CloseFigure();
                g.FillPath(pincel, camino);
            }
        }

        private static void Contorno(Graphics g, Pen pen, RectangleF caja, float radio)
        {
            using (var camino = Redondeado(caja, radio)) g.DrawPath(pen, camino);
        }

        private static GraphicsPath Redondeado(RectangleF c, float radio)
        {
            float d = Math.Min(radio * 2, Math.Min(c.Width, c.Height));
            var camino = new GraphicsPath();
            camino.AddArc(c.X, c.Y, d, d, 180, 90);
            camino.AddArc(c.Right - d, c.Y, d, d, 270, 90);
            camino.AddArc(c.Right - d, c.Bottom - d, d, d, 0, 90);
            camino.AddArc(c.X, c.Bottom - d, d, d, 90, 90);
            camino.CloseFigure();
            return camino;
        }

        private static RectangleF Achicar(RectangleF r, float d) => new RectangleF(r.X + d, r.Y + d, r.Width - 2 * d, r.Height - 2 * d);

        private static Color Oscurecer(Color c) => Color.FromArgb((int)(c.R * 0.62), (int)(c.G * 0.62), (int)(c.B * 0.62));

        private static Color ColorEstado(EstadoVenta06AV e)
        {
            switch (e)
            {
                case EstadoVenta06AV.Pendiente: return Tema.Amber500;
                case EstadoVenta06AV.Senada: return Tema.Cian400;
                case EstadoVenta06AV.EnProduccion: return Tema.Cian700;
                case EstadoVenta06AV.Entregada: return Tema.Verde600;
                default: return Tema.Acero500;
            }
        }

        private static EstadoVenta06AV Estado(string clave) => (EstadoVenta06AV)Enum.Parse(typeof(EstadoVenta06AV), clave);

        private List<Tuple<string, decimal, int>> Tramos(out string subtitulo)
        {
            DateTime desde = (_reporte.Filtro?.Desde ?? _hoy).Date;
            DateTime hasta = (_reporte.Filtro?.Hasta ?? _hoy).Date;
            if (hasta < desde) hasta = desde;
            var vigentes = (_reporte.Filas ?? new List<FilaReporteVentas06AV>()).Where(f => f.EstadoVenta != EstadoVenta06AV.Anulada).ToList();
            int dias = (hasta - desde).Days + 1;
            var tramos = new List<Tuple<string, decimal, int>>();

            if (dias <= 31)
            {
                subtitulo = _t.Obtener("rpdf_por_dia");
                for (DateTime d = desde; d <= hasta; d = d.AddDays(1))
                {
                    var del = vigentes.Where(f => f.FechaVenta.Date == d).ToList();
                    tramos.Add(Tuple.Create(d.ToString("dd/MM"), del.Sum(f => f.Total), del.Count));
                }
            }
            else if (dias <= 120)
            {
                subtitulo = _t.Obtener("rpdf_por_semana");
                DateTime inicio = desde.AddDays(-(((int)desde.DayOfWeek + 6) % 7));
                for (DateTime d = inicio; d <= hasta; d = d.AddDays(7))
                {
                    DateTime fin = d.AddDays(6);
                    var del = vigentes.Where(f => f.FechaVenta.Date >= d && f.FechaVenta.Date <= fin).ToList();
                    tramos.Add(Tuple.Create(d.ToString("dd/MM"), del.Sum(f => f.Total), del.Count));
                }
            }
            else
            {
                subtitulo = _t.Obtener("rpdf_por_mes");
                for (DateTime d = new DateTime(desde.Year, desde.Month, 1); d <= hasta; d = d.AddMonths(1))
                {
                    var del = vigentes.Where(f => f.FechaVenta.Year == d.Year && f.FechaVenta.Month == d.Month).ToList();
                    tramos.Add(Tuple.Create(d.ToString("MMM yy", CultureInfo.CurrentCulture), del.Sum(f => f.Total), del.Count));
                }
            }
            return tramos;
        }

        private static decimal EscalaRedonda(decimal maximo)
        {
            if (maximo <= 0) return 1;
            double potencia = Math.Pow(10, Math.Floor(Math.Log10((double)maximo)));
            foreach (double m in new[] { 1, 2, 2.5, 5, 10 })
                if (m * potencia >= (double)maximo) return (decimal)(m * potencia);
            return (decimal)(10 * potencia);
        }

        private static string Compacto(decimal v)
        {
            string simbolo = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
            if (v >= 1000000) return simbolo + " " + (v / 1000000m).ToString("0.#", CultureInfo.CurrentCulture) + " M";
            if (v >= 1000) return simbolo + " " + (v / 1000m).ToString("0.#", CultureInfo.CurrentCulture) + " k";
            return simbolo + " " + v.ToString("0", CultureInfo.CurrentCulture);
        }

        private static Font Fuente(string familia, float puntos, FontStyle estilo = FontStyle.Regular) =>
            new Font(familia, puntos * 100f / 72f, estilo, GraphicsUnit.World);

        private static string Fecha(DateTime? d) => d.HasValue ? d.Value.ToString("dd/MM/yyyy") : "-";

        private static string Nombre(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

        public void Dispose()
        {
            foreach (var f in new[] { _fMarca, _fTitulo, _fSubtitulo, _fSeccion, _fTexto, _fTextoBold,
                                      _fMini, _fMiniBold, _fKpiValor, _fGrande, _fTabla, _fTablaBold })
                f.Dispose();
        }
    }
}
