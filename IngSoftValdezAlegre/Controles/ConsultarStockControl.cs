using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ConsultarStockControl : UserControl, IIdiomaAplicable06AV
    {
        // Tras el refactor, Insumo se fusionó en Componente: una sola fuente de stock.
        private readonly ComponentesBLL06AV _compBLL = new ComponentesBLL06AV();

        private Label lblTitulo, lblFiltro;
        private ComboBox cboFiltro;
        private Button btnRefrescar;
        private DataGridView grilla;

        public ConsultarStockControl()
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
            lblTitulo = new Label { AutoSize = true, Location = new Point(4, 16) };

            lblFiltro = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            cboFiltro = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            cboFiltro.SelectedIndexChanged += (s, e) => Cargar();
            btnRefrescar = new Button { Width = 120, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0) };
            btnRefrescar.Click += (s, e) => Cargar();

            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flp.Controls.AddRange(new Control[] { lblFiltro, cboFiltro, btnRefrescar });

            var barra = new Panel { Dock = DockStyle.Top, Height = 56 };
            barra.Controls.Add(lblTitulo);
            barra.Controls.Add(flp);

            grilla = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grilla.DataBindingComplete += (s, e) => FormatearGrilla();

            Controls.Add(grilla);
            Controls.Add(barra);
        }

        private void Cargar()
        {
            try
            {
                int filtro = cboFiltro.SelectedIndex; // 0 Todos, 1 Bajo stock
                var componentes = _compBLL.ObtenerTodos() ?? new List<Componente06AV>();
                if (filtro == 1)
                    componentes = componentes.Where(c => c.BajoStock).ToList();

                var filas = componentes
                    .Select(c => new StockVm
                    {
                        Codigo = c.Codigo,
                        Descripcion = c.Descripcion,
                        Tipo = c.Tipo.ToString(),
                        Stock = c.Stock,
                        Minimo = c.StockMinimo.ToString(),
                        Bajo = c.BajoStock
                    })
                    .OrderBy(f => f.Descripcion)
                    .ToList();

                grilla.DataSource = null;
                grilla.DataSource = filas;
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(ex.Message, GestorIdioma06AV.Instancia.Obtener("aviso"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
            }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k) { if (grilla.Columns[c] != null) grilla.Columns[c].HeaderText = t.Obtener(k); }
            H("Codigo", "pcf_col_codigo");
            H("Descripcion", "pcf_col_descripcion");
            H("Tipo", "tipo");
            H("Stock", "pcf_col_stock");
            H("Minimo", "pcf_col_minimo");
            if (grilla.Columns["Bajo"] != null) grilla.Columns["Bajo"].Visible = false;
            if (grilla.Columns["Stock"] != null)
                grilla.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewRow row in grilla.Rows)
                if (row.DataBoundItem is StockVm vm && vm.Bajo && grilla.Columns["Stock"] != null)
                {
                    row.Cells["Stock"].Style.ForeColor = Tema.Peligro;
                    row.Cells["Stock"].Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonSecundario(btnRefrescar);
            Tema.AplicarEntrada(cboFiltro);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("menu_consultar_stock");
            lblFiltro.Text = t.Obtener("filtrar") + ":";
            btnRefrescar.Text = t.Obtener("pcf_actualizar");

            int sel = cboFiltro.SelectedIndex < 0 ? 0 : cboFiltro.SelectedIndex;
            cboFiltro.Items.Clear();
            cboFiltro.Items.Add(t.Obtener("todos"));
            cboFiltro.Items.Add(t.Obtener("pcf_stock_bajo"));
            cboFiltro.SelectedIndex = sel;

            if (grilla.DataSource != null) FormatearGrilla();
        }

        private class StockVm
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public string Tipo { get; set; }
            public int Stock { get; set; }
            public string Minimo { get; set; }
            [Browsable(false)] public bool Bajo { get; set; }
        }
    }
}
