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
    /// Recibos de seña emitidos (RFN1). Cada recibo documenta el pago de seña sobre una
    /// computadora registrada, con su monto abonado y saldo pendiente (valores snapshot).
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class RecibosControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly VentasBLL06AV _bll = new VentasBLL06AV();

        private Label lblTitulo;
        private Button btnRefrescar;
        private DataGridView grilla;

        public RecibosControl()
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

            btnRefrescar = new Button { Width = 120, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0) };
            btnRefrescar.Click += (s, e) => Cargar();

            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 8, 0)
            };
            flp.Controls.Add(btnRefrescar);

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
                var filas = (_bll.ObtenerRecibos() ?? new List<Recibo06AV>())
                    .OrderByDescending(r => r.FechaEmision)
                    .Select(r => new ReciboVm
                    {
                        Numero = r.Id,
                        Computadora = r.Venta?.Computadora?.Nombre ?? "",
                        Cliente = r.Venta?.Cliente?.NombreCompleto ?? "",
                        Emision = r.FechaEmision.ToShortDateString(),
                        Abonado = r.MontoAbonado.ToString("C0"),
                        Saldo = r.SaldoPendiente.ToString("C0"),
                        Entrega = r.FechaEntregaEstimada.ToShortDateString()
                    })
                    .ToList();

                grilla.DataSource = null;
                grilla.DataSource = filas;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string c, string k) { if (grilla.Columns[c] != null) grilla.Columns[c].HeaderText = t.Obtener(k); }
            H("Numero", "pcf_col_numero");
            H("Computadora", "pcf_menu_componentes");
            H("Cliente", "pcf_cliente");
            H("Emision", "pcf_f_emision");
            H("Abonado", "pcf_abonado");
            H("Saldo", "pcf_saldo");
            H("Entrega", "pcf_f_entrega");
            foreach (string c in new[] { "Abonado", "Saldo" })
                if (grilla.Columns[c] != null) grilla.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonSecundario(btnRefrescar);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("menu_recibos");
            btnRefrescar.Text = t.Obtener("pcf_actualizar");
            if (grilla.DataSource != null) FormatearGrilla();
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        private class ReciboVm
        {
            public string Numero { get; set; }
            public string Computadora { get; set; }
            public string Cliente { get; set; }
            public string Emision { get; set; }
            public string Abonado { get; set; }
            public string Saldo { get; set; }
            public string Entrega { get; set; }
        }
    }
}
