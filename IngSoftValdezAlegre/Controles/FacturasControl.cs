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
    /// Facturas de venta: lista las VENTAS ya entregadas (CU07) y permite regenerar
    /// y abrir el PDF de la factura, con el detalle del anticipo, el saldo cancelado
    /// y el número de serie del equipo.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class FacturasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly VentasBLL06AV _ventasBLL = new VentasBLL06AV();

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
                var entregadas = (_ventasBLL.ObtenerTodas() ?? new List<Venta06AV>())
                    .Where(v => v.Estado == EstadoVenta06AV.Entregada)
                    .OrderByDescending(v => v.NumeroVenta)
                    .Select(v =>
                    {
                        var saldo = v.Pagos.FirstOrDefault(p => p.Tipo == TipoPago06AV.SaldoFinal);
                        return new FacturaVm
                        {
                            Comprobante = saldo != null ? saldo.NumeroRecibo : "-",
                            Numero = v.NumeroVenta,
                            Cliente = v.Cliente != null ? v.Cliente.Apellido + ", " + v.Cliente.Nombre : "",
                            Fecha = (saldo != null ? saldo.Fecha : v.FechaVenta).ToShortDateString(),
                            Total = v.PrecioTotal.ToString("C0"),
                            Abonado = v.TotalAbonado.ToString("C0"),
                            Venta = v
                        };
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
                var orden = vm.Venta.NumeroOrdenProduccion.HasValue
                    ? _ventasBLL.ObtenerOrdenDeVenta(vm.Venta.NumeroVenta) : null;
                string ruta = ComprobantePcFactory06AV.GenerarFactura(vm.Venta, orden);
                ComprobantePcFactory06AV.Abrir(ruta);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k) { if (grilla.Columns[c] != null) grilla.Columns[c].HeaderText = t.Obtener(k); }
            H("Comprobante", "pcf_comprobante");
            H("Numero", "pcf_venta");
            H("Cliente", "pcf_cliente");
            H("Fecha", "pcf_fecha");
            H("Total", "pcf_total");
            H("Abonado", "pcf_abonado");
            if (grilla.Columns["Venta"] != null) grilla.Columns["Venta"].Visible = false;
            if (grilla.Columns["Numero"] != null) grilla.Columns["Numero"].FillWeight = 26;
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
            public string Comprobante { get; set; }
            public int Numero { get; set; }
            public string Cliente { get; set; }
            public string Fecha { get; set; }
            public string Total { get; set; }
            public string Abonado { get; set; }
            [Browsable(false)] public Venta06AV Venta { get; set; }
        }
    }
}
