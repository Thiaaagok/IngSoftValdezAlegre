using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using SER.Exportacion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class ReporteProveedoresControl06AV : UserControl, IIdiomaAplicable06AV
    {
        private readonly ProveedoresBLL06AV _bll = new ProveedoresBLL06AV();
        private List<FilaReporteProveedores06AV> _todas = new List<FilaReporteProveedores06AV>();
        private List<FilaReporteProveedores06AV> _visibles = new List<FilaReporteProveedores06AV>();

        private Label lblTitulo, lblSubtitulo, lblBuscar, lblEstado, lblTipo;
        private TextBox txtBuscar;
        private ComboBox cboEstado, cboTipo;
        private CheckBox chkConCompras;
        private Button btnActualizar, btnPdf, btnExcel;
        private FlowLayoutPanel flpKpis;
        private readonly List<Kpi> _kpis = new List<Kpi>();
        private DataGridView grid;
        private Panel pnlCabecera, pnlFiltros;

        public ReporteProveedoresControl06AV()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;
            Cargar();
        }

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true, Location = new Point(6, 12) };
            lblSubtitulo = new Label { AutoSize = true, Location = new Point(8, 42) };
            pnlCabecera = new Panel { Dock = DockStyle.Top, Height = 68 };
            pnlCabecera.Controls.Add(lblSubtitulo);
            pnlCabecera.Controls.Add(lblTitulo);

            lblBuscar = Rotulo();
            txtBuscar = new TextBox { Width = 200, Margin = new Padding(0, 4, 14, 0) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            lblEstado = Rotulo();
            cboEstado = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 14, 0) };
            cboEstado.SelectedIndexChanged += (s, e) => Filtrar();
            lblTipo = Rotulo();
            cboTipo = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 14, 0) };
            cboTipo.SelectedIndexChanged += (s, e) => Filtrar();
            chkConCompras = new CheckBox { AutoSize = true, Margin = new Padding(0, 7, 14, 0) };
            chkConCompras.CheckedChanged += (s, e) => Filtrar();

            btnActualizar = Boton(110);
            btnPdf = Boton(150);
            btnExcel = Boton(150);
            btnActualizar.Click += (s, e) => Cargar();
            btnPdf.Click += (s, e) => ExportarPdf();
            btnExcel.Click += (s, e) => ExportarExcel();

            var flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(6, 10, 0, 0)
            };
            flpFiltros.Controls.AddRange(new Control[] { lblBuscar, txtBuscar, lblEstado, cboEstado, lblTipo, cboTipo, chkConCompras });
            var flpBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 14, 8, 0)
            };
            flpBotones.Controls.AddRange(new Control[] { btnActualizar, btnPdf, btnExcel });
            pnlFiltros = new Panel { Dock = DockStyle.Top, Height = 46 };
            pnlFiltros.Controls.Add(flpFiltros);
            pnlCabecera.Controls.Add(flpBotones);

            flpKpis = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 92, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                Padding = new Padding(4, 8, 4, 8)
            };
            for (int i = 0; i < 4; i++)
            {
                var k = new Kpi(i == 2);
                _kpis.Add(k);
                flpKpis.Controls.Add(k.Panel);
            }

            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
                BorderStyle = BorderStyle.None
            };
            grid.DataBindingComplete += (s, e) => FormatearGrilla();

            Controls.Add(grid);
            Controls.Add(flpKpis);
            Controls.Add(pnlFiltros);
            Controls.Add(pnlCabecera);
        }

        private static Label Rotulo() => new Label { AutoSize = true, Margin = new Padding(0, 8, 4, 0) };

        private static Button Boton(int ancho) => new Button
        {
            Width = ancho, Height = 32, Margin = new Padding(6, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };

        private void Cargar()
        {
            try
            {
                _todas = _bll.ObtenerReporte() ?? new List<FilaReporteProveedores06AV>();
                CargarTipos();
                Filtrar();
            }
            catch (Exception ex) { Aviso(ex.Message); }
        }

        private void CargarTipos()
        {
            var t = GestorIdioma06AV.Instancia;
            string actual = cboTipo.SelectedIndex > 0 ? cboTipo.SelectedItem as string : null;
            var tipos = _todas.Where(f => !string.IsNullOrWhiteSpace(f.Tipos))
                              .SelectMany(f => f.Tipos.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                              .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            cboTipo.Items.Clear();
            cboTipo.Items.Add(t.Obtener("rep_todos"));
            foreach (string tipo in tipos) cboTipo.Items.Add(tipo);
            int idx = actual != null ? cboTipo.Items.IndexOf(actual) : 0;
            cboTipo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void Filtrar()
        {
            if (grid == null || cboEstado == null || cboTipo == null) return;
            string texto = (txtBuscar.Text ?? "").Trim();
            string tipo = cboTipo.SelectedIndex > 0 ? cboTipo.SelectedItem as string : null;

            _visibles = _todas.Where(f =>
                (cboEstado.SelectedIndex != 1 || f.Activo) &&
                (cboEstado.SelectedIndex != 2 || !f.Activo) &&
                (!chkConCompras.Checked || f.OrdenesCompra > 0) &&
                (tipo == null || (f.Tipos ?? "").Split(new[] { ", " }, StringSplitOptions.None)
                                                 .Any(x => string.Equals(x, tipo, StringComparison.OrdinalIgnoreCase))) &&
                (texto.Length == 0 || Contiene(f.Nombre, texto) || Contiene(f.Cuit, texto) || Contiene(f.Email, texto) ||
                 Contiene(f.Codigo, texto) || Contiene(f.Cuit?.Replace("-", ""), texto)))
                .OrderByDescending(f => f.TotalComprado).ThenBy(f => f.Nombre).ToList();

            var t = GestorIdioma06AV.Instancia;
            grid.DataSource = null;
            grid.DataSource = _visibles.Select(f => new FilaVm
            {
                Codigo = f.Codigo,
                Proveedor = f.Nombre,
                Cuit = f.Cuit,
                Contacto = string.Join("  ·  ", new[] { f.Email, f.Telefono }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Tipos = f.Tipos,
                Cotizaciones = f.Cotizaciones,
                Ordenes = f.OrdenesCompra,
                Total = f.TotalComprado.ToString("C2"),
                Ultima = f.UltimaCompra.HasValue ? f.UltimaCompra.Value.ToString("dd/MM/yyyy") : "—",
                Estado = t.Obtener(f.Activo ? "activo" : "pcf_prov_inactivo"),
                Fila = f
            }).ToList();

            ActualizarKpis();
            bool hay = _visibles.Count > 0;
            btnPdf.Enabled = hay;
            btnExcel.Enabled = hay;
            AplicarEstiloBotones();
        }

        private static bool Contiene(string valor, string texto) =>
            !string.IsNullOrEmpty(valor) && valor.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;

        private void ActualizarKpis()
        {
            var t = GestorIdioma06AV.Instancia;
            decimal total = _visibles.Sum(f => f.TotalComprado);
            var top = _visibles.Where(f => f.TotalComprado > 0).OrderByDescending(f => f.TotalComprado).FirstOrDefault();
            _kpis[0].Definir(t.Obtener("rprov_kpi_proveedores"), _visibles.Count.ToString(),
                             t.Obtener("rprov_kpi_activos", _visibles.Count(f => f.Activo)));
            _kpis[1].Definir(t.Obtener("rprov_kpi_con_compras"), _visibles.Count(f => f.OrdenesCompra > 0).ToString(),
                             t.Obtener("rprov_kpi_cotizaciones", _visibles.Sum(f => f.Cotizaciones)));
            _kpis[2].Definir(t.Obtener("rprov_kpi_total"), total.ToString("C0"),
                             t.Obtener("rprov_kpi_ordenes", _visibles.Sum(f => f.OrdenesCompra)));
            _kpis[3].Definir(t.Obtener("rprov_kpi_principal"), top?.Nombre ?? "—",
                             top != null && total > 0 ? t.Obtener("rprov_kpi_participacion", Math.Round(top.TotalComprado * 100m / total)) : "");
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key, float peso)
            {
                if (grid.Columns[col] == null) return;
                grid.Columns[col].HeaderText = t.Obtener(key);
                grid.Columns[col].FillWeight = peso;
            }
            H("Codigo", "rprov_col_codigo", 60);
            H("Proveedor", "rprov_col_proveedor", 130);
            H("Cuit", "cuit", 85);
            H("Contacto", "rprov_col_contacto", 160);
            H("Tipos", "rprov_col_tipos", 130);
            H("Cotizaciones", "rprov_col_cotizaciones", 60);
            H("Ordenes", "rprov_col_ordenes", 55);
            H("Total", "rprov_col_total", 85);
            H("Ultima", "rprov_col_ultima", 70);
            H("Estado", "pcf_col_estado", 55);
            foreach (string c in new[] { "Cotizaciones", "Ordenes", "Total" })
                if (grid.Columns[c] != null) grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewRow row in grid.Rows)
                if (row.DataBoundItem is FilaVm vm && !vm.Fila.Activo)
                {
                    row.DefaultCellStyle.ForeColor = Tema.TextoSuave;
                    if (grid.Columns["Estado"] != null) row.Cells["Estado"].Style.ForeColor = Tema.Peligro;
                }
        }

        private string DescripcionFiltro()
        {
            var partes = new List<string>();
            if (cboEstado.SelectedIndex > 0) partes.Add(cboEstado.SelectedItem as string);
            if (cboTipo.SelectedIndex > 0) partes.Add(cboTipo.SelectedItem as string);
            if (chkConCompras.Checked) partes.Add(chkConCompras.Text);
            if (!string.IsNullOrWhiteSpace(txtBuscar.Text)) partes.Add("\"" + txtBuscar.Text.Trim() + "\"");
            return partes.Count == 0 ? GestorIdioma06AV.Instancia.Obtener("rprov_doc_todos") : string.Join("  ·  ", partes);
        }

        private void ExportarPdf()
        {
            if (_visibles.Count == 0) return;
            var t = GestorIdioma06AV.Instancia;
            using (var sfd = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"Reporte_Proveedores_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    Cursor = Cursors.WaitCursor;
                    Image logo = null;
                    try { logo = Properties.Resources.pcforge_logo1; } catch { }
                    string usuario = UsuarioSesion06AV.Instancia().UsuarioActual?.Login;
                    ReporteProveedoresPdf06AV.Guardar(sfd.FileName, _visibles, DescripcionFiltro(), usuario, logo);
                    Cursor = Cursors.Default;
                    if (ConfirmacionForm.Mostrar(t.Obtener("pdf_generado"), t.Obtener("rprov_titulo"),
                            ConfirmacionForm.TipoConfirmacion.Info, t.Obtener("pcf_oc_abrir"), t.Obtener("aceptar"), FindForm()))
                        ComprobanteCompras06AV.Abrir(sfd.FileName);
                }
                catch (Exception ex) { Cursor = Cursors.Default; Aviso(ex.Message); }
            }
        }

        private void ExportarExcel()
        {
            if (_visibles.Count == 0) return;
            var t = GestorIdioma06AV.Instancia;
            using (var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"Reporte_Proveedores_{DateTime.Now:yyyyMMdd_HHmmss}.csv" })
            {
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    var columnas = new Dictionary<string, Func<FilaReporteProveedores06AV, object>>
                    {
                        { t.Obtener("rprov_col_codigo"), f => f.Codigo },
                        { t.Obtener("rprov_col_proveedor"), f => f.Nombre },
                        { t.Obtener("cuit"), f => f.Cuit },
                        { t.Obtener("email"), f => f.Email },
                        { t.Obtener("telefono"), f => f.Telefono },
                        { t.Obtener("direccion"), f => f.Direccion },
                        { t.Obtener("rprov_col_tipos"), f => f.Tipos },
                        { t.Obtener("rprov_col_cotizaciones"), f => f.Cotizaciones },
                        { t.Obtener("rprov_col_ordenes"), f => f.OrdenesCompra },
                        { t.Obtener("rprov_col_total"), f => f.TotalComprado.ToString("0.00", CultureInfo.CurrentCulture) },
                        { t.Obtener("rprov_col_ultima"), f => f.UltimaCompra.HasValue ? f.UltimaCompra.Value.ToString("dd/MM/yyyy") : "" },
                        { t.Obtener("pcf_col_estado"), f => t.Obtener(f.Activo ? "activo" : "pcf_prov_inactivo") }
                    };
                    new ExportacionEXCEL().Exportar(_visibles, columnas, sfd.FileName);
                    ConfirmacionForm.MostrarInfo(t.Obtener("archivo_generado"), t.Obtener("rprov_titulo"),
                        ConfirmacionForm.TipoConfirmacion.Info, FindForm());
                }
                catch (Exception ex) { Aviso(ex.Message); }
            }
        }

        private void Aviso(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private void AplicarEstiloBotones()
        {
            Tema.AplicarBotonSecundario(btnActualizar);
            if (btnPdf.Enabled) Tema.AplicarBotonPrimario(btnPdf); else Tema.AplicarBotonDeshabilitado(btnPdf);
            if (btnExcel.Enabled) Tema.AplicarBotonSecundario(btnExcel); else Tema.AplicarBotonDeshabilitado(btnExcel);
        }

        public void AplicarTema()
        {
            Tema.AplicarControl(this);
            BackColor = Tema.FondoApp;
            foreach (Control c in new Control[] { pnlCabecera, pnlFiltros, flpKpis })
            {
                c.BackColor = Tema.FondoApp;
                foreach (Control h in c.Controls) if (h is FlowLayoutPanel) h.BackColor = Tema.FondoApp;
            }
            Tema.AplicarTitulo(lblTitulo);
            lblSubtitulo.Font = Tema.FuenteRegular;
            lblSubtitulo.ForeColor = Tema.TextoSuave;
            foreach (Label l in new[] { lblTitulo, lblSubtitulo })
                l.BackColor = Tema.FondoApp;
            foreach (Label l in new[] { lblBuscar, lblEstado, lblTipo })
            {
                l.Font = Tema.FuenteMini;
                l.ForeColor = Tema.TextoSuave;
                l.BackColor = Tema.FondoApp;
            }
            foreach (Control c in new Control[] { txtBuscar, cboEstado, cboTipo }) Tema.AplicarEntrada(c);
            chkConCompras.ForeColor = Tema.Texto;
            chkConCompras.BackColor = Tema.FondoApp;
            chkConCompras.Font = Tema.FuenteRegular;
            Tema.AplicarGrilla(grid);
            foreach (Kpi k in _kpis) k.AplicarTema();
            AplicarEstiloBotones();
            if (grid.DataSource != null) FormatearGrilla();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("rprov_titulo");
            lblSubtitulo.Text = t.Obtener("rprov_subtitulo");
            lblBuscar.Text = t.Obtener("buscar");
            lblEstado.Text = t.Obtener("pcf_col_estado");
            lblTipo.Text = t.Obtener("rprov_tipo");
            chkConCompras.Text = t.Obtener("rprov_con_compras");
            btnActualizar.Text = t.Obtener("pcf_actualizar");
            btnPdf.Text = t.Obtener("rep_pdf");
            btnExcel.Text = t.Obtener("rep_excel");

            int estado = cboEstado.SelectedIndex;
            cboEstado.Items.Clear();
            cboEstado.Items.AddRange(new object[] { t.Obtener("rep_todos"), t.Obtener("rprov_activos"), t.Obtener("rprov_inactivos") });
            cboEstado.SelectedIndex = estado >= 0 ? estado : 0;

            if (cboTipo.Items.Count > 0) cboTipo.Items[0] = t.Obtener("rep_todos");
            Filtrar();
        }

        private sealed class Kpi
        {
            private readonly Label _titulo, _valor, _pie;
            private readonly bool _acento;
            public Panel Panel { get; }

            public Kpi(bool acento)
            {
                _acento = acento;
                Panel = new Panel { Width = 230, Height = 74, Margin = new Padding(6, 0, 6, 0) };
                _titulo = new Label { AutoSize = false, Location = new Point(14, 8), Size = new Size(210, 16) };
                _valor = new Label { AutoSize = false, Location = new Point(14, 26), Size = new Size(210, 26), AutoEllipsis = true };
                _pie = new Label { AutoSize = false, Location = new Point(14, 52), Size = new Size(210, 16), AutoEllipsis = true };
                Panel.Controls.AddRange(new Control[] { _titulo, _valor, _pie });
                Panel.Paint += (s, e) =>
                {
                    using (var b = new SolidBrush(_acento ? Tema.Acento : Tema.Primario))
                        e.Graphics.FillRectangle(b, 0, 10, 4, Panel.Height - 20);
                    using (var p = new Pen(Tema.Borde))
                        e.Graphics.DrawRectangle(p, 0, 0, Panel.Width - 1, Panel.Height - 1);
                };
            }

            public void Definir(string titulo, string valor, string pie)
            {
                _titulo.Text = (titulo ?? "").ToUpper();
                _valor.Text = valor;
                _pie.Text = pie;
            }

            public void AplicarTema()
            {
                Panel.BackColor = Tema.FondoPanel;
                _titulo.Font = Tema.FuenteMini;
                _titulo.ForeColor = Tema.TextoSuave;
                _valor.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
                _valor.ForeColor = Tema.TextoFuerte;
                _pie.Font = new Font("Segoe UI", 8.25f);
                _pie.ForeColor = Tema.TextoSuave;
                foreach (Control c in Panel.Controls) c.BackColor = Tema.FondoPanel;
                Panel.Invalidate();
            }
        }

        private class FilaVm
        {
            public string Codigo { get; set; }
            public string Proveedor { get; set; }
            public string Cuit { get; set; }
            public string Contacto { get; set; }
            public string Tipos { get; set; }
            public int Cotizaciones { get; set; }
            public int Ordenes { get; set; }
            public string Total { get; set; }
            public string Ultima { get; set; }
            public string Estado { get; set; }
            [Browsable(false)] public FilaReporteProveedores06AV Fila { get; set; }
        }
    }
}
