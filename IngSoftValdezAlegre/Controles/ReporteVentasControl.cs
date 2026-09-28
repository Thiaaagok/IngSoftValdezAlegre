using BE;
using BLL;
using BLL.Excepciones;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.UI;
using SER;
using SER.Exportacion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Reporte RF1 (ventas y producción) en tres pasos, como el resto del sistema:
    ///   1. PERÍODO   — tarjetas con los períodos habituales y "otro período" con fechas.
    ///   2. FILTROS   — estado, tipo de equipo, cliente y solo atrasadas; todo opcional.
    ///   3. RESULTADO — indicadores y, debajo, una vista a la vez: detalle o cortes.
    /// A la derecha quedan los criterios elegidos y, con el resultado, la exportación.
    /// Los números salen de <see cref="ReporteVentasBLL06AV"/>; acá solo se muestran.
    /// </summary>
    [DesignerCategory("Code")]
    internal class ReporteVentasControl : AsistenteBase06AV
    {
        private enum Periodo { EsteMes, MesAnterior, Ultimos90, EsteAnio, Otro }
        private enum Vista { Detalle, Estado, Tipo, Modelo }

        private readonly ReporteVentasBLL06AV _bll = new ReporteVentasBLL06AV();
        private ReporteVentas06AV _reporte;

        private Periodo _periodo = Periodo.EsteMes;
        private EstadoVenta06AV? _estado;
        private TipoConfiguracion06AV? _tipo;
        private Vista _vista = Vista.Detalle;

        // Paso 1
        private Label lblPeriodoTit, lblDesde, lblHasta, lblErrorPeriodo;
        private FlowLayoutPanel flpPeriodos, flpFechas;
        private DateTimePicker dtpDesde, dtpHasta;

        // Paso 2
        private Label lblEstadoTit, lblTipoTit, lblClienteTit, lblErrorFiltro;
        private FlowLayoutPanel flpEstados, flpTipos;
        private TextBox txtCliente;
        private CheckBox chkAtrasadas;
        private Button btnGenerar;

        // Paso 3
        private FlowLayoutPanel flpKpi, flpVistas;
        private Indicador kpiVentas, kpiFacturacion, kpiCobrado, kpiSaldo, kpiTicket, kpiAtrasadas;
        private readonly Dictionary<Vista, Button> _botonesVista = new Dictionary<Vista, Button>();
        private DataGridView grilla;
        private Label lblPie;

        // Lateral
        private Label lblLateralTit;
        private FichaDatos06AV ficha;
        private FlowLayoutPanel flpExportar;
        private Button btnPdf, btnExcel, btnNuevo;

        public ReporteVentasControl()
        {
            ConstruirPeriodo(NuevaPagina());
            ConstruirFiltros(NuevaPagina());
            ConstruirResultado(NuevaPagina());
            ConstruirLateral();

            btnGenerar = AgregarBotonBarra(180, (s, e) => Generar());

            AplicarTema();
            ElegirPeriodo(Periodo.EsteMes);
            IrA(0);
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Armado
        // ══════════════════════════════════════════════════════════════════

        private void ConstruirPeriodo(Panel pagina)
        {
            lblPeriodoTit = Rotulo();
            flpPeriodos = Fila();
            foreach (Periodo p in Enum.GetValues(typeof(Periodo)))
            {
                var tarjeta = new TarjetaOpcion06AV { Valor = p, Width = 250, Icono = IconoPcf06AV.Calendario };
                tarjeta.Elegida += (s, e) => ElegirPeriodo((Periodo)((TarjetaOpcion06AV)s).Valor);
                flpPeriodos.Controls.Add(tarjeta);
            }

            lblDesde = new Label { AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
            lblHasta = new Label { AutoSize = true, Margin = new Padding(18, 8, 6, 0) };
            dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140 };
            dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140 };
            dtpDesde.ValueChanged += (s, e) => CriteriosCambiaron();
            dtpHasta.ValueChanged += (s, e) => CriteriosCambiaron();
            flpFechas = Fila();
            flpFechas.Padding = new Padding(0, 6, 0, 0);
            flpFechas.Controls.AddRange(new Control[] { lblDesde, dtpDesde, lblHasta, dtpHasta });

            lblErrorPeriodo = new Label { AutoSize = true, Padding = new Padding(0, 10, 0, 0) };

            pagina.Controls.Add(Apilar(lblPeriodoTit, flpPeriodos, flpFechas, lblErrorPeriodo));
        }

        private void ConstruirFiltros(Panel pagina)
        {
            lblEstadoTit = Rotulo();
            flpEstados = Fila();
            flpEstados.Controls.Add(TarjetaFiltro(null, IconoPcf06AV.Bandeja, (s, e) => ElegirEstado(null)));
            foreach (EstadoVenta06AV est in Enum.GetValues(typeof(EstadoVenta06AV)))
            {
                EstadoVenta06AV local = est;
                flpEstados.Controls.Add(TarjetaFiltro(est, IconoEstado(est), (s, e) => ElegirEstado(local)));
            }

            lblTipoTit = Rotulo();
            flpTipos = Fila();
            flpTipos.Controls.Add(TarjetaFiltro(null, IconoPcf06AV.Chip, (s, e) => ElegirTipo(null)));
            flpTipos.Controls.Add(TarjetaFiltro(TipoConfiguracion06AV.Estandar, IconoPcf06AV.Caja, (s, e) => ElegirTipo(TipoConfiguracion06AV.Estandar)));
            flpTipos.Controls.Add(TarjetaFiltro(TipoConfiguracion06AV.Configurable, IconoPcf06AV.Destornillador, (s, e) => ElegirTipo(TipoConfiguracion06AV.Configurable)));

            lblClienteTit = Rotulo();
            txtCliente = new TextBox { Width = 320, MaxLength = ReporteVentasBLL06AV.LargoMaximoCliente };
            txtCliente.TextChanged += (s, e) => CriteriosCambiaron();
            txtCliente.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Generar(); } };

            chkAtrasadas = new CheckBox { AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
            chkAtrasadas.CheckedChanged += (s, e) => CriteriosCambiaron();

            lblErrorFiltro = new Label { AutoSize = true, Padding = new Padding(0, 10, 0, 0) };

            pagina.Controls.Add(Apilar(lblEstadoTit, flpEstados, lblTipoTit, flpTipos, lblClienteTit,
                                       Fila(txtCliente), Fila(chkAtrasadas), lblErrorFiltro));
        }

        private static TarjetaOpcion06AV TarjetaFiltro(object valor, IconoPcf06AV icono, EventHandler elegida)
        {
            var t = new TarjetaOpcion06AV { Valor = valor, Width = 210, Icono = icono };
            t.Elegida += elegida;
            return t;
        }

        private void ConstruirResultado(Panel pagina)
        {
            kpiVentas = new Indicador(); kpiFacturacion = new Indicador(); kpiCobrado = new Indicador();
            kpiSaldo = new Indicador(); kpiTicket = new Indicador(); kpiAtrasadas = new Indicador();
            flpKpi = Fila();
            flpKpi.Padding = new Padding(0, 0, 0, 6);
            flpKpi.Controls.AddRange(new Control[] { kpiVentas, kpiFacturacion, kpiCobrado, kpiSaldo, kpiTicket, kpiAtrasadas });

            flpVistas = Fila();
            flpVistas.Padding = new Padding(0, 4, 0, 8);
            foreach (Vista v in Enum.GetValues(typeof(Vista)))
            {
                Vista local = v;
                Button b = NuevoBoton(150);
                b.Margin = new Padding(0, 0, 6, 0);
                b.Click += (s, e) => { _vista = local; MostrarVista(); };
                _botonesVista[v] = b;
                flpVistas.Controls.Add(b);
            }

            grilla = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
                BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Both
            };
            grilla.DataBindingComplete += (s, e) => FormatearGrilla();
            grilla.CellFormatting += PintarFila;

            lblPie = new Label { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(2, 6, 0, 2) };

            pagina.Controls.Add(grilla);
            pagina.Controls.Add(lblPie);
            pagina.Controls.Add(flpVistas);
            pagina.Controls.Add(flpKpi);
        }

        private void ConstruirLateral()
        {
            lblLateralTit = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 8) };
            ficha = new FichaDatos06AV { Dock = DockStyle.Top };

            btnPdf = NuevoBoton(200);
            btnExcel = NuevoBoton(200);
            btnNuevo = NuevoBoton(200);
            foreach (Button b in new[] { btnPdf, btnExcel, btnNuevo })
            {
                b.Margin = new Padding(0, 0, 0, 8);
                b.MinimumSize = new Size(250, 34);
            }
            btnPdf.Click += (s, e) => ExportarPdf();
            btnExcel.Click += (s, e) => ExportarExcel();
            btnNuevo.Click += (s, e) => NuevoReporte();

            flpExportar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            flpExportar.Controls.AddRange(new Control[] { btnPdf, btnExcel, btnNuevo });

            Lateral.Controls.Add(flpExportar);
            Lateral.Controls.Add(ficha);
            Lateral.Controls.Add(lblLateralTit);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Pasos
        // ══════════════════════════════════════════════════════════════════

        protected override bool PasoCompleto(int paso)
        {
            if (paso == 0) return ErrorPeriodo() == null;
            if (paso == 1) return _reporte != null;
            return true;
        }

        // En el paso 2 el avance es "Generar reporte"; en el 3 no hay siguiente.
        protected override bool MuestraSiguiente(int paso) => paso == 0;

        protected override void AlSiguiente()
        {
            string error = ErrorPeriodo();
            MostrarError(lblErrorPeriodo, error);
            if (error == null) IrA(1);
        }

        protected override void AlMostrarPaso(int paso)
        {
            flpFechas.Visible = _periodo == Periodo.Otro;
            MostrarError(lblErrorPeriodo, null);
            MostrarError(lblErrorFiltro, null);
            if (paso == 2) { DibujarResultado(); MostrarVista(); }
            MarcarTarjetas();
            ActualizarLateral();
        }

        protected override void ActualizarBotonesPropios()
        {
            if (btnGenerar == null) return;
            btnGenerar.Visible = PasoActual == 1;
            Tema.AplicarBotonAcento(btnGenerar);

            bool hayFilas = _reporte != null && _reporte.Filas.Count > 0;
            flpExportar.Visible = PasoActual == 2;
            Habilitar(btnPdf, hayFilas, false);
            Habilitar(btnExcel, hayFilas, false);
            Habilitar(btnNuevo, true, false);
        }

        // ── Paso 1 · Período ─────────────────────────────────────────────
        private static void Rango(Periodo p, DateTime hoy, out DateTime desde, out DateTime hasta)
        {
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            switch (p)
            {
                case Periodo.MesAnterior: desde = inicioMes.AddMonths(-1); hasta = inicioMes.AddDays(-1); break;
                case Periodo.Ultimos90: desde = hoy.AddDays(-89); hasta = hoy; break;
                case Periodo.EsteAnio: desde = new DateTime(hoy.Year, 1, 1); hasta = hoy; break;
                default: desde = inicioMes; hasta = hoy; break;
            }
        }

        private void ElegirPeriodo(Periodo p)
        {
            _periodo = p;
            if (p != Periodo.Otro)
            {
                Rango(p, DateTime.Today, out DateTime desde, out DateTime hasta);
                dtpDesde.Value = desde;
                dtpHasta.Value = hasta;
            }
            flpFechas.Visible = p == Periodo.Otro;
            MarcarTarjetas();
            CriteriosCambiaron();
        }

        private FiltroReporteVentas06AV Criterios() => new FiltroReporteVentas06AV
        {
            Desde = dtpDesde.Value.Date,
            Hasta = dtpHasta.Value.Date,
            Estado = _estado,
            Tipo = _tipo,
            Cliente = txtCliente.Text,
            SoloAtrasadas = chkAtrasadas.Checked
        };

        /// <summary>Error del período con las reglas de la BLL, o null si es válido.</summary>
        private string ErrorPeriodo()
        {
            var f = Criterios();
            f.Cliente = null;
            try { ReporteVentasBLL06AV.ValidarFiltro(f, DateTime.Today); return null; }
            catch (ValidacionException06AV ex) { return ex.Message; }
        }

        // ── Paso 2 · Filtros ─────────────────────────────────────────────
        private void ElegirEstado(EstadoVenta06AV? e) { _estado = e; MarcarTarjetas(); CriteriosCambiaron(); }
        private void ElegirTipo(TipoConfiguracion06AV? t) { _tipo = t; MarcarTarjetas(); CriteriosCambiaron(); }

        /// <summary>Cambiar un criterio descarta el resultado anterior: hay que volver a generarlo.</summary>
        private void CriteriosCambiaron()
        {
            _reporte = null;
            MostrarError(lblErrorPeriodo, null);
            MostrarError(lblErrorFiltro, null);
            ActualizarLateral();
            ActualizarBarra();
        }

        private void Generar()
        {
            string errorPeriodo = ErrorPeriodo();
            if (errorPeriodo != null) { IrA(0); MostrarError(lblErrorPeriodo, errorPeriodo); return; }
            try
            {
                Cursor = Cursors.WaitCursor;
                _reporte = _bll.Generar(Criterios());
            }
            catch (ValidacionException06AV ex)
            {
                _reporte = null;
                MostrarError(lblErrorFiltro, ex.Message);
                return;
            }
            catch (Exception ex)
            {
                _reporte = null;
                ConfirmacionForm.MostrarInfo(ex.Message, GestorIdioma06AV.Instancia.Obtener("aviso"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
                return;
            }
            finally { Cursor = Cursors.Default; }
            IrA(2);
        }

        private void NuevoReporte()
        {
            _estado = null;
            _tipo = null;
            txtCliente.Clear();
            chkAtrasadas.Checked = false;
            _vista = Vista.Detalle;
            ElegirPeriodo(Periodo.EsteMes);
            IrA(0);
        }

        // ── Paso 3 · Resultado ───────────────────────────────────────────
        private void DibujarResultado()
        {
            var t = GestorIdioma06AV.Instancia;
            var r = _reporte?.Resumen ?? new ResumenReporteVentas06AV();

            kpiVentas.Mostrar(t.Obtener("rep_kpi_ventas"), r.CantidadVentas.ToString(),
                t.Obtener("rep_kpi_ventas_pie", r.CantidadVigentes, r.CantidadAnuladas), Tema.Primario);
            kpiFacturacion.Mostrar(t.Obtener("rep_kpi_facturacion"), Plata(r.Facturacion),
                t.Obtener("rep_kpi_facturacion_pie"), Tema.Primario);
            kpiCobrado.Mostrar(t.Obtener("rep_kpi_cobrado"), Plata(r.TotalCobrado),
                t.Obtener("rep_kpi_cobrado_pie", r.PorcentajeSenadas.ToString("0.#", CultureInfo.CurrentCulture)), Tema.Exito);
            kpiSaldo.Mostrar(t.Obtener("rep_kpi_saldo"), Plata(r.SaldoPendiente),
                t.Obtener("rep_kpi_saldo_pie", r.PendientesDeSena), Tema.Acento);
            kpiTicket.Mostrar(t.Obtener("rep_kpi_ticket"), Plata(r.TicketPromedio),
                r.DiasPromedioEntrega.HasValue
                    ? t.Obtener("rep_kpi_ticket_pie", r.DiasPromedioEntrega.Value.ToString("0.#", CultureInfo.CurrentCulture))
                    : t.Obtener("rep_kpi_ticket_sin"), Tema.Primario);
            kpiAtrasadas.Mostrar(t.Obtener("rep_kpi_atrasadas"), r.Atrasadas.ToString(),
                t.Obtener("rep_kpi_atrasadas_pie", r.EnProduccion, r.ListasParaEntregar),
                r.Atrasadas > 0 ? Tema.Peligro : Tema.Exito);

            lblPie.Text = _reporte == null
                ? t.Obtener("rep_sin_datos")
                : _reporte.Filas.Count == 0
                    ? t.Obtener("rep_sin_datos")
                    : t.Obtener("rep_pie", _reporte.GeneradoEl.ToString("dd/MM/yyyy HH:mm"),
                                string.IsNullOrWhiteSpace(_reporte.GeneradoPor) ? "-" : _reporte.GeneradoPor,
                                _reporte.Filas.Count);
        }

        private void MostrarVista()
        {
            var t = GestorIdioma06AV.Instancia;
            var r = _reporte?.Resumen ?? new ResumenReporteVentas06AV();
            DateTime hoy = DateTime.Today;

            foreach (var par in _botonesVista)
                Habilitar(par.Value, true, par.Key == _vista);

            grilla.DataSource = null;
            switch (_vista)
            {
                case Vista.Estado:
                    grilla.DataSource = r.PorEstado.Select(g => new CorteVm(TextoEstadoVenta(Estado(g.Clave)), g)).ToList();
                    break;
                case Vista.Tipo:
                    grilla.DataSource = r.PorTipo.Select(g => new CorteVm(TextoTipo(g.Clave), g)).ToList();
                    break;
                case Vista.Modelo:
                    grilla.DataSource = r.PorModelo.Select(g => new CorteVm(
                        g.Clave == ReporteVentasBLL06AV.NombreAMedida ? t.Obtener("rep_a_medida") : g.Clave, g)).ToList();
                    break;
                default:
                    grilla.DataSource = (_reporte?.Filas ?? new List<FilaReporteVentas06AV>())
                        .Select(f => new FilaVm(f, hoy)).ToList();
                    break;
            }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k, float peso, int minimo, bool derecha = false)
            {
                DataGridViewColumn col = grilla.Columns[c];
                if (col == null) return;
                col.HeaderText = k.StartsWith("rep_") ? t.Obtener(k) : k;
                col.FillWeight = peso;
                col.MinimumWidth = minimo;
                if (derecha) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            // Con mínimos por columna la grilla muestra scroll horizontal en vez de aplastar títulos.
            H("Venta", "rep_col_venta", 45, 60);
            H("Fecha", "rep_col_fecha", 65, 85);
            H("Cliente", "rep_col_cliente", 110, 130);
            H("Equipo", "rep_col_equipo", 120, 140);
            H("Tipo", "rep_col_tipo", 60, 80);
            H("Total", "rep_col_total", 70, 90, true);
            H("Cobrado", "rep_col_cobrado", 70, 90, true);
            H("Saldo", "rep_col_saldo", 70, 90, true);
            H("EstadoVenta", "rep_col_estado_venta", 80, 110);
            H("Orden", "rep_col_orden", 90, 140);
            H("Linea", "rep_col_linea", 55, 70);
            H("Entrega", "rep_col_entrega", 65, 85);
            H("Atraso", "rep_col_atraso", 50, 65, true);
            H("Concepto", "rep_col_concepto", 160, 160);
            H("Cantidad", "rep_col_cant", 60, 70, true);
            H("Porcentaje", "%", 60, 60, true);
            H("Importe", "rep_col_importe", 90, 110, true);
            grilla.ClearSelection();
        }

        private void PintarFila(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !(grilla.Rows[e.RowIndex].DataBoundItem is FilaVm vm)) return;
            if (vm.Fila.EstadoVenta == EstadoVenta06AV.Anulada)
                e.CellStyle.ForeColor = Tema.TextoSuave;
            else if (vm.DiasAtraso > 0)
            {
                e.CellStyle.BackColor = Tema.AdvertenciaSuave;
                if (grilla.Columns[e.ColumnIndex].Name == "Atraso")
                {
                    e.CellStyle.ForeColor = Tema.Peligro;
                    e.CellStyle.Font = Tema.FuenteBold;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Lateral: criterios elegidos
        // ══════════════════════════════════════════════════════════════════

        private void ActualizarLateral()
        {
            if (ficha == null) return;
            var t = GestorIdioma06AV.Instancia;
            string cliente = (txtCliente.Text ?? "").Trim();

            ficha.RotuloDestacado = t.Obtener("rep_periodo");
            // Año corto: con cuatro dígitos el rango no entra en el destacado de la ficha.
            ficha.ValorDestacado = t.Obtener("rep_rango", dtpDesde.Value.ToString("dd/MM/yy"), dtpHasta.Value.ToString("dd/MM/yy"));
            ficha.ColorDestacado = ErrorPeriodo() == null ? Tema.Primario : Tema.Peligro;
            ficha.Definir(new[]
            {
                new DatoFicha06AV(t.Obtener("rep_estado"), _estado.HasValue ? TextoEstadoVenta(_estado.Value) : t.Obtener("rep_todos"), true),
                new DatoFicha06AV(t.Obtener("rep_tipo"), _tipo.HasValue ? TextoTipo(_tipo.Value.ToString()) : t.Obtener("rep_todos")),
                new DatoFicha06AV(t.Obtener("rep_solo_atrasadas"), t.Obtener(chkAtrasadas.Checked ? "rep_si" : "rep_no")),
                new DatoFicha06AV(t.Obtener("rep_col_cliente"), cliente.Length == 0 ? t.Obtener("rep_todos") : cliente, true)
            });
            ficha.Height = ficha.AltoNecesario;
            ficha.Invalidate();
        }

        private void MarcarTarjetas()
        {
            foreach (TarjetaOpcion06AV c in flpPeriodos.Controls.OfType<TarjetaOpcion06AV>())
                c.Seleccionada = (Periodo)c.Valor == _periodo;
            foreach (TarjetaOpcion06AV c in flpEstados.Controls.OfType<TarjetaOpcion06AV>())
                c.Seleccionada = Equals(c.Valor as EstadoVenta06AV?, _estado);
            foreach (TarjetaOpcion06AV c in flpTipos.Controls.OfType<TarjetaOpcion06AV>())
                c.Seleccionada = Equals(c.Valor as TipoConfiguracion06AV?, _tipo);
        }

        private static void MostrarError(Label l, string texto)
        {
            l.Text = texto ?? "";
            l.Visible = !string.IsNullOrEmpty(texto);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Exportación
        // ══════════════════════════════════════════════════════════════════

        private Dictionary<string, Func<FilaReporteVentas06AV, object>> ColumnasExportacion()
        {
            var t = GestorIdioma06AV.Instancia;
            DateTime hoy = DateTime.Today;
            return new Dictionary<string, Func<FilaReporteVentas06AV, object>>
            {
                { t.Obtener("rep_col_venta"),        f => f.NumeroVenta },
                { t.Obtener("rep_col_fecha"),        f => f.FechaVenta.ToString("dd/MM/yyyy") },
                { t.Obtener("rep_col_cliente"),      f => f.ClienteNombre + " (" + f.ClienteDni + ")" },
                { t.Obtener("rep_col_equipo"),       f => f.Equipo },
                { t.Obtener("rep_col_tipo"),         f => TextoTipo(f.TipoConfiguracion.ToString()) },
                { t.Obtener("rep_col_total"),        f => f.Total.ToString("0.00", CultureInfo.CurrentCulture) },
                { t.Obtener("rep_col_cobrado"),      f => f.TotalCobrado.ToString("0.00", CultureInfo.CurrentCulture) },
                { t.Obtener("rep_col_saldo"),        f => f.SaldoPendiente.ToString("0.00", CultureInfo.CurrentCulture) },
                { t.Obtener("rep_col_estado_venta"), f => TextoEstadoVenta(f.EstadoVenta) },
                { t.Obtener("rep_col_orden"),        f => TextoOrden(f) },
                { t.Obtener("rep_col_entrega"),      f => f.FechaEntregaComprometida.ToString("dd/MM/yyyy") },
                { t.Obtener("rep_col_atraso"),       f => f.DiasDeAtraso(hoy) }
            };
        }

        private void ExportarPdf()
        {
            if (_reporte == null || _reporte.Filas.Count == 0) return;
            using (var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"Reporte_Ventas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    var t = GestorIdioma06AV.Instancia;
                    string titulo = t.Obtener("rep_titulo") + "  ·  " +
                                    _reporte.Filtro.Desde.ToString("dd/MM/yyyy") + " – " +
                                    _reporte.Filtro.Hasta.ToString("dd/MM/yyyy");
                    new ExportacionPDF().Exportar(_reporte.Filas, ColumnasExportacion(), sfd.FileName, titulo,
                        new float[] { 5, 8, 18, 18, 8, 9, 9, 9, 10, 14, 8, 6 });
                    ConfirmacionForm.MostrarInfo(t.Obtener("pdf_generado"), t.Obtener("rep_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                }
                catch (Exception ex) { Aviso(ex.Message); }
            }
        }

        private void ExportarExcel()
        {
            if (_reporte == null || _reporte.Filas.Count == 0) return;
            using (var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"Reporte_Ventas_{DateTime.Now:yyyyMMdd_HHmmss}.csv" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    new ExportacionEXCEL().Exportar(_reporte.Filas, ColumnasExportacion(), sfd.FileName);
                    var t = GestorIdioma06AV.Instancia;
                    ConfirmacionForm.MostrarInfo(t.Obtener("archivo_generado"), t.Obtener("rep_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                }
                catch (Exception ex) { Aviso(ex.Message); }
            }
        }

        private void Aviso(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        // ══════════════════════════════════════════════════════════════════
        //  Textos
        // ══════════════════════════════════════════════════════════════════

        private static string TextoEstadoVenta(EstadoVenta06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoVenta06AV.Pendiente: return t.Obtener("pcf_est_vta_pendiente");
                case EstadoVenta06AV.Senada: return t.Obtener("pcf_est_vta_senada");
                case EstadoVenta06AV.EnProduccion: return t.Obtener("pcf_est_vta_produccion");
                case EstadoVenta06AV.Entregada: return t.Obtener("pcf_est_vta_entregada");
                default: return t.Obtener("pcf_est_vta_anulada");
            }
        }

        private static IconoPcf06AV IconoEstado(EstadoVenta06AV e)
        {
            switch (e)
            {
                case EstadoVenta06AV.Pendiente: return IconoPcf06AV.Reloj;
                case EstadoVenta06AV.Senada: return IconoPcf06AV.Check;
                case EstadoVenta06AV.EnProduccion: return IconoPcf06AV.Destornillador;
                case EstadoVenta06AV.Entregada: return IconoPcf06AV.Camion;
                default: return IconoPcf06AV.Alerta;
            }
        }

        private static string TextoEstadoOrden(EstadoOrdenProduccion06AV e)
        {
            var t = GestorIdioma06AV.Instancia;
            switch (e)
            {
                case EstadoOrdenProduccion06AV.Pendiente: return t.Obtener("pcf_est_op_pendiente");
                case EstadoOrdenProduccion06AV.Planificada: return t.Obtener("pcf_est_op_planificada");
                case EstadoOrdenProduccion06AV.EnEnsamblaje: return t.Obtener("pcf_est_op_ensamblaje");
                case EstadoOrdenProduccion06AV.Finalizada: return t.Obtener("pcf_est_op_finalizada");
                case EstadoOrdenProduccion06AV.Entregada: return t.Obtener("pcf_est_op_entregada");
                default: return t.Obtener("pcf_est_op_revision");
            }
        }

        private static string TextoTipo(string tipo) =>
            GestorIdioma06AV.Instancia.Obtener(tipo == TipoConfiguracion06AV.Estandar.ToString() ? "rep_tipo_estandar" : "rep_tipo_medida");

        private static string TextoOrden(FilaReporteVentas06AV f) =>
            f.NumeroOrden.HasValue && f.EstadoOrden.HasValue
                ? "#" + f.NumeroOrden.Value + " · " + TextoEstadoOrden(f.EstadoOrden.Value)
                : GestorIdioma06AV.Instancia.Obtener("rep_sin_orden");

        private static EstadoVenta06AV Estado(string clave) => (EstadoVenta06AV)Enum.Parse(typeof(EstadoVenta06AV), clave);

        private static string Plata(decimal v) => v.ToString("C0", CultureInfo.CurrentCulture);

        // ══════════════════════════════════════════════════════════════════
        //  Tema e idioma
        // ══════════════════════════════════════════════════════════════════

        public override void AplicarTema()
        {
            base.AplicarTema();
            foreach (Label l in new[] { lblPeriodoTit, lblEstadoTit, lblTipoTit, lblClienteTit }) Tema.AplicarSubtitulo(l);
            foreach (Label l in new[] { lblDesde, lblHasta, lblPie }) { l.Font = Tema.FuenteRegular; l.ForeColor = Tema.TextoSuave; }
            foreach (Label l in new[] { lblErrorPeriodo, lblErrorFiltro }) { l.Font = Tema.FuenteBold; l.ForeColor = Tema.Peligro; }
            Tema.AplicarSubtitulo(lblLateralTit);
            lblLateralTit.BackColor = Tema.FondoPanel;
            Tema.AplicarEntrada(txtCliente);
            Tema.AplicarEntrada(dtpDesde);
            Tema.AplicarEntrada(dtpHasta);
            chkAtrasadas.ForeColor = Tema.Texto;
            chkAtrasadas.Font = Tema.FuenteRegular;
            Tema.AplicarGrilla(grilla);
            foreach (Indicador k in new[] { kpiVentas, kpiFacturacion, kpiCobrado, kpiSaldo, kpiTicket, kpiAtrasadas }) k.AplicarTema();
            foreach (Control c in flpExportar.Controls) c.BackColor = Tema.FondoPanel;
            flpExportar.BackColor = Tema.FondoPanel;
            ActualizarBarra();
            if (PasoActual == 2) MostrarVista();
            Invalidate(true);
        }

        public override void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            LblTitulo.Text = t.Obtener("rep_titulo");
            LblSubtitulo.Text = t.Obtener("rep_subtitulo");
            Pasos.DefinirPasos(new[] { t.Obtener("rep_paso1"), t.Obtener("rep_paso2"), t.Obtener("rep_paso3") });
            LblAyuda.Text = t.Obtener(PasoActual == 0 ? "rep_hint1" : PasoActual == 1 ? "rep_hint2" : "rep_hint3");
            BtnAtras.Text = "←  " + t.Obtener(PasoActual == 2 ? "rep_cambiar" : "pcf_asis_atras");
            BtnSiguiente.Text = t.Obtener("pcf_asis_siguiente") + "  →";
            if (btnGenerar != null) btnGenerar.Text = t.Obtener("rep_generar_reporte") + "  →";

            lblPeriodoTit.Text = t.Obtener("rep_periodo_tit");
            DateTime hoy = DateTime.Today;
            foreach (TarjetaOpcion06AV c in flpPeriodos.Controls.OfType<TarjetaOpcion06AV>())
            {
                var p = (Periodo)c.Valor;
                c.Titulo = t.Obtener(p == Periodo.EsteMes ? "rep_per_mes" : p == Periodo.MesAnterior ? "rep_per_mes_ant"
                                   : p == Periodo.Ultimos90 ? "rep_per_90" : p == Periodo.EsteAnio ? "rep_per_anio" : "rep_per_otro");
                if (p == Periodo.Otro) c.Subtitulo = t.Obtener("rep_per_otro_sub");
                else
                {
                    Rango(p, hoy, out DateTime d, out DateTime h);
                    c.Subtitulo = t.Obtener("rep_rango", d.ToString("dd/MM/yyyy"), h.ToString("dd/MM/yyyy"));
                }
                c.Invalidate();
            }
            lblDesde.Text = t.Obtener("rep_desde");
            lblHasta.Text = t.Obtener("rep_hasta");

            lblEstadoTit.Text = t.Obtener("rep_estado");
            foreach (TarjetaOpcion06AV c in flpEstados.Controls.OfType<TarjetaOpcion06AV>())
            {
                c.Titulo = c.Valor is EstadoVenta06AV e ? TextoEstadoVenta(e) : t.Obtener("rep_todos");
                c.Invalidate();
            }
            lblTipoTit.Text = t.Obtener("rep_tipo");
            foreach (TarjetaOpcion06AV c in flpTipos.Controls.OfType<TarjetaOpcion06AV>())
            {
                c.Titulo = c.Valor is TipoConfiguracion06AV tp ? TextoTipo(tp.ToString()) : t.Obtener("rep_todos");
                c.Invalidate();
            }
            lblClienteTit.Text = t.Obtener("rep_cliente_opc");
            chkAtrasadas.Text = t.Obtener("rep_solo_atrasadas");

            _botonesVista[Vista.Detalle].Text = t.Obtener("rep_vista_detalle");
            _botonesVista[Vista.Estado].Text = t.Obtener("rep_corte_estado");
            _botonesVista[Vista.Tipo].Text = t.Obtener("rep_corte_tipo");
            _botonesVista[Vista.Modelo].Text = t.Obtener("rep_corte_modelo", ReporteVentasBLL06AV.TopModelos);

            lblLateralTit.Text = t.Obtener("rep_criterios");
            btnPdf.Text = t.Obtener("rep_pdf");
            btnExcel.Text = t.Obtener("rep_excel");
            btnNuevo.Text = t.Obtener("rep_nuevo");

            ActualizarLateral();
            if (PasoActual == 2) { DibujarResultado(); MostrarVista(); }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Piezas
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tarjeta de indicador. El alto sale de las fuentes y no de números fijos, para que
        /// no se corte el pie con la escala de Windows al 125% o 150%.
        /// </summary>
        private sealed class Indicador : Control
        {
            private string _rotulo = "", _valor = "", _pie = "";
            private Color _acento = Color.Gray;

            public Indicador()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Margin = new Padding(0, 0, 10, 10);
                Tag = "propio";
                Ajustar();
            }

            public void Mostrar(string rotulo, string valor, string pie, Color acento)
            {
                _rotulo = rotulo; _valor = valor; _pie = pie; _acento = acento;
                Ajustar();
                Invalidate();
            }

            public void AplicarTema() { Ajustar(); Invalidate(); }

            private void Ajustar()
            {
                int alto = 10 + Tema.FuenteMini.Height + 4 + Tema.FuenteTitulo.Height + 6 + Tema.FuenteRegular.Height + 12;
                Size = new Size(220, alto);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Parent != null ? Parent.BackColor : Tema.FondoApp);
                var caja = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var b = new SolidBrush(Tema.FondoPanel)) g.FillRectangle(b, caja);
                using (var p = new Pen(Tema.Borde)) g.DrawRectangle(p, caja);
                using (var b = new SolidBrush(_acento)) g.FillRectangle(b, 0, 0, 4, Height);

                int x = 14, y = 10, ancho = Width - x - 10;
                TextRenderer.DrawText(g, _rotulo, Tema.FuenteMini, new Rectangle(x, y, ancho, Tema.FuenteMini.Height + 2),
                    Tema.TextoSuave, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                y += Tema.FuenteMini.Height + 4;
                TextRenderer.DrawText(g, _valor, Tema.FuenteTitulo, new Rectangle(x, y, ancho, Tema.FuenteTitulo.Height + 2),
                    _acento, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                y += Tema.FuenteTitulo.Height + 6;
                TextRenderer.DrawText(g, _pie, Tema.FuenteRegular, new Rectangle(x, y, ancho, Tema.FuenteRegular.Height + 2),
                    Tema.TextoSuave, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private class FilaVm
        {
            public FilaVm(FilaReporteVentas06AV f, DateTime hoy)
            {
                Fila = f;
                Venta = f.NumeroVenta;
                Fecha = f.FechaVenta.ToString("dd/MM/yyyy");
                Cliente = f.ClienteNombre;
                Equipo = f.Equipo;
                Tipo = TextoTipo(f.TipoConfiguracion.ToString());
                Total = f.Total.ToString("C0", CultureInfo.CurrentCulture);
                Cobrado = f.TotalCobrado.ToString("C0", CultureInfo.CurrentCulture);
                Saldo = f.SaldoPendiente.ToString("C0", CultureInfo.CurrentCulture);
                EstadoVenta = TextoEstadoVenta(f.EstadoVenta);
                Orden = TextoOrden(f);
                Linea = string.IsNullOrWhiteSpace(f.Linea) ? "-" : f.Linea;
                Entrega = f.FechaEntregaComprometida.ToString("dd/MM/yyyy");
                DiasAtraso = f.DiasDeAtraso(hoy);
                Atraso = DiasAtraso > 0 ? DiasAtraso + " d" : "";
            }

            public int Venta { get; }
            public string Fecha { get; }
            public string Cliente { get; }
            public string Equipo { get; }
            public string Tipo { get; }
            public string Total { get; }
            public string Cobrado { get; }
            public string Saldo { get; }
            public string EstadoVenta { get; }
            public string Orden { get; }
            public string Linea { get; }
            public string Entrega { get; }
            public string Atraso { get; }
            [Browsable(false)] public int DiasAtraso { get; }
            [Browsable(false)] public FilaReporteVentas06AV Fila { get; }
        }

        private class CorteVm
        {
            public CorteVm(string concepto, GrupoReporte06AV g)
            {
                Concepto = concepto;
                Cantidad = g.Cantidad;
                Porcentaje = g.Porcentaje.ToString("0.#", CultureInfo.CurrentCulture);
                Importe = g.Importe.ToString("C0", CultureInfo.CurrentCulture);
            }

            public string Concepto { get; }
            public int Cantidad { get; }
            public string Porcentaje { get; }
            public string Importe { get; }
        }
    }
}
