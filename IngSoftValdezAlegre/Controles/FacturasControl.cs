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
    /// <summary>
    /// Facturas de venta: lista las órdenes de producción ya entregadas (que tienen
    /// factura) y permite generar/abrir el PDF de la factura. La misma pantalla se usa
    /// para "Facturas" (consultar) y "Generar factura".
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class FacturasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly OrdenProduccionBLL06AV _ordenesBLL = new OrdenProduccionBLL06AV();

        private Label lblTitulo;
        private Button btnAbrir, btnRefrescar;
        private DataGridView grilla;

        public FacturasControl()
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

            btnAbrir = new Button { Width = 170, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0) };
            btnRefrescar = new Button { Width = 120, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0) };
            btnAbrir.Click += (s, e) => AbrirFactura();
            btnRefrescar.Click += (s, e) => Cargar();

            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flp.Controls.AddRange(new Control[] { btnRefrescar, btnAbrir });

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
            grilla.DoubleClick += (s, e) => AbrirFactura();

            Controls.Add(grilla);
            Controls.Add(barra);
        }

        private void Cargar()
        {
            try
            {
                var entregadas = (_ordenesBLL.ObtenerTodas() ?? new List<OrdenProduccion06AV>())
                    .Where(o => o.Estado == EstadoOrdenProduccion06AV.Entregada)
                    .OrderByDescending(o => o.NumeroOrden)
                    .Select(o => new FacturaVm
                    {
                        Numero = o.NumeroOrden,
                        Cliente = o.Cliente != null ? o.Cliente.Apellido + ", " + o.Cliente.Nombre : "",
                        Entrega = o.FechaEntrega.ToShortDateString(),
                        Total = o.PrecioTotal.ToString("C0"),
                        Abonado = o.TotalAbonado.ToString("C0"),
                        Orden = o
                    })
                    .ToList();

                grilla.DataSource = null;
                grilla.DataSource = entregadas;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AbrirFactura()
        {
            var vm = grilla.CurrentRow?.DataBoundItem as FacturaVm;
            if (vm == null) { MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro")); return; }
            try
            {
                string ruta = ComprobantePcFactory06AV.GenerarFactura(vm.Orden);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k) { if (grilla.Columns[c] != null) grilla.Columns[c].HeaderText = t.Obtener(k); }
            H("Numero", "pcf_col_numero");
            H("Cliente", "pcf_cliente");
            H("Entrega", "pcf_f_entrega");
            H("Total", "pcf_total");
            H("Abonado", "pcf_abonado");
            if (grilla.Columns["Orden"] != null) grilla.Columns["Orden"].Visible = false;
            if (grilla.Columns["Numero"] != null) grilla.Columns["Numero"].FillWeight = 30;
            foreach (string c in new[] { "Total", "Abonado" })
                if (grilla.Columns[c] != null) grilla.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnAbrir);
            Tema.AplicarBotonSecundario(btnRefrescar);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("menu_facturas");
            btnAbrir.Text = t.Obtener("fact_abrir");
            btnRefrescar.Text = t.Obtener("pcf_actualizar");
            if (grilla.DataSource != null) FormatearGrilla();
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private class FacturaVm
        {
            public int Numero { get; set; }
            public string Cliente { get; set; }
            public string Entrega { get; set; }
            public string Total { get; set; }
            public string Abonado { get; set; }
            [Browsable(false)] public OrdenProduccion06AV Orden { get; set; }
        }
    }
}
