using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Pantalla del proceso de Compras (RFN2): armar la orden de compra de faltantes,
    /// pedir cotización a un proveedor, aprobar/desaprobar (gerente) y recibir insumos.
    /// </summary>
    public class ComprasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly CompraInsumosBLL06AV _bll = new CompraInsumosBLL06AV();
        private readonly ProveedoresBLL06AV _proveedoresBLL = new ProveedoresBLL06AV();

        private BindingList<FaltanteVm06AV> _faltantes = new BindingList<FaltanteVm06AV>();

        private Label lblTitulo, lblFaltantes, lblOC, lblCot;
        private DataGridView grFaltantes, grOC, grCot;
        private DateTimePicker dtpLimite;
        private TextBox txtRepositor;
        private Button btnCrearOC;
        private ComboBox cboProveedor;
        private Button btnCotizar, btnRecibir, btnAprobar, btnDesaprobar;

        public ComprasControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            CargarTodo();
        }

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true };
            lblFaltantes = new Label { AutoSize = true };
            lblOC = new Label { AutoSize = true };
            lblCot = new Label { AutoSize = true };

            grFaltantes = NuevaGrilla(false); // editable (para Cantidad)
            grFaltantes.DataBindingComplete += (s, e) => SoloCantidadEditable();
            grOC = NuevaGrilla(true);
            grOC.DataBindingComplete += (s, e) => Ocultar(grOC, "InsumosFaltantes");
            grCot = NuevaGrilla(true);
            grCot.DataBindingComplete += (s, e) => Ocultar(grCot, "InsumosPedidos");

            dtpLimite = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(5) };
            txtRepositor = new TextBox();
            btnCrearOC = new Button { Text = "Crear OC" };
            btnCrearOC.Click += (s, e) => CrearOrdenCompra();

            cboProveedor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            btnCotizar = new Button { Text = "Cotizar" };
            btnRecibir = new Button { Text = "Recibir insumos" };
            btnCotizar.Click += (s, e) => Cotizar();
            btnRecibir.Click += (s, e) => Recibir();

            btnAprobar = new Button { Text = "Aprobar" };
            btnDesaprobar = new Button { Text = "Desaprobar" };
            btnAprobar.Click += (s, e) => AprobarDesaprobar(true);
            btnDesaprobar.Click += (s, e) => AprobarDesaprobar(false);

            Controls.AddRange(new Control[]
            {
                lblTitulo, lblFaltantes, lblOC, lblCot, grFaltantes, grOC, grCot,
                dtpLimite, txtRepositor, btnCrearOC, cboProveedor, btnCotizar, btnRecibir, btnAprobar, btnDesaprobar
            });
        }

        private static DataGridView NuevaGrilla(bool soloLectura) => new DataGridView
        {
            ReadOnly = soloLectura, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grFaltantes);
            Tema.AplicarGrilla(grOC);
            Tema.AplicarGrilla(grCot);
            Tema.AplicarBotonPrimario(btnCrearOC);
            Tema.AplicarBotonAcento(btnCotizar);
            Tema.AplicarBotonPrimario(btnRecibir);
            Tema.AplicarBotonPrimario(btnAprobar);
            Tema.AplicarBotonPeligro(btnDesaprobar);
        }

        public void AplicarIdioma()
        {
            lblTitulo.Text = "Compras de Insumos";
            lblFaltantes.Text = "Faltantes (bajo stock) — indicá la cantidad a pedir:";
            lblOC.Text = "Órdenes de compra:";
            lblCot.Text = "Cotizaciones:";
            btnCrearOC.Text = "Crear OC";
            btnCotizar.Text = "Cotizar";
            btnRecibir.Text = "Recibir insumos";
            btnAprobar.Text = "Aprobar";
            btnDesaprobar.Text = "Desaprobar";
        }

        private void AjustarLayout()
        {
            int m = 12;
            int ancho = Math.Max(980, ClientSize.Width);
            int alto = Math.Max(560, ClientSize.Height);
            lblTitulo.SetBounds(m, m, 320, 30);

            int colW = (ancho - m * 4) / 3;
            int x1 = m, x2 = m * 2 + colW, x3 = m * 3 + colW * 2;
            int top = 52, gridH = alto - 180;

            // Columna 1: faltantes + alta OC
            lblFaltantes.SetBounds(x1, top - 22, colW, 20);
            grFaltantes.SetBounds(x1, top, colW, gridH);
            int y1 = top + gridH + 8;
            dtpLimite.SetBounds(x1, y1, 120, 26);
            txtRepositor.SetBounds(x1 + 128, y1, colW - 128, 26);
            btnCrearOC.SetBounds(x1, y1 + 34, 120, 32);

            // Columna 2: OC + cotizar / recibir
            lblOC.SetBounds(x2, top - 22, colW, 20);
            grOC.SetBounds(x2, top, colW, gridH);
            int y2 = top + gridH + 8;
            cboProveedor.SetBounds(x2, y2, colW, 26);
            btnCotizar.SetBounds(x2, y2 + 34, 120, 32);
            btnRecibir.SetBounds(x2 + 128, y2 + 34, 130, 32);

            // Columna 3: cotizaciones + aprobar / desaprobar
            lblCot.SetBounds(x3, top - 22, colW, 20);
            grCot.SetBounds(x3, top, colW, gridH);
            int y3 = top + gridH + 8;
            btnAprobar.SetBounds(x3, y3, 120, 32);
            btnDesaprobar.SetBounds(x3 + 128, y3, 120, 32);
        }

        private void CargarTodo()
        {
            CargarFaltantes();
            CargarOrdenes();
            CargarCotizaciones();
            CargarProveedores();
        }

        private void CargarFaltantes()
        {
            try
            {
                _faltantes = new BindingList<FaltanteVm06AV>();
                foreach (var i in _bll.ObtenerFaltantes())
                    _faltantes.Add(new FaltanteVm06AV
                    {
                        Codigo = i.Codigo, Descripcion = i.Descripcion,
                        Stock = i.Stock, StockMinimo = i.StockMinimo,
                        Cantidad = Math.Max(1, i.StockMinimo - i.Stock + 1)  // sugerido
                    });
                grFaltantes.DataSource = null;
                grFaltantes.DataSource = _faltantes;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarOrdenes()
        {
            try { grOC.DataSource = null; grOC.DataSource = _bll.ObtenerOrdenesCompra(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarCotizaciones()
        {
            try { grCot.DataSource = null; grCot.DataSource = _bll.ObtenerCotizaciones(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void CargarProveedores()
        {
            try { cboProveedor.DataSource = _proveedoresBLL.ObtenerTodos(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void SoloCantidadEditable()
        {
            foreach (DataGridViewColumn col in grFaltantes.Columns)
                col.ReadOnly = col.Name != "Cantidad";
        }

        private static void Ocultar(DataGridView g, string columna)
        {
            if (g.Columns[columna] != null) g.Columns[columna].Visible = false;
        }

        private void CrearOrdenCompra()
        {
            var detalles = new List<DetalleInsumo06AV>();
            foreach (var f in _faltantes)
                if (f.Cantidad > 0)
                    detalles.Add(new DetalleInsumo06AV
                    {
                        Cantidad = f.Cantidad,
                        Insumo = new Insumo06AV { Codigo = f.Codigo, Descripcion = f.Descripcion, Stock = f.Stock, StockMinimo = f.StockMinimo }
                    });

            try
            {
                var oc = _bll.RegistrarOrdenCompra(detalles, dtpLimite.Value, txtRepositor.Text.Trim());
                CargarOrdenes();
                ConfirmacionForm.MostrarInfo($"Orden de compra #{oc.NumeroCompra} creada.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Cotizar()
        {
            var oc = grOC.CurrentRow?.DataBoundItem as OrdenCompra06AV;
            if (oc == null) { MostrarError("Elegí una orden de compra."); return; }
            var prov = cboProveedor.SelectedItem as Proveedor06AV;
            if (prov == null) { MostrarError("Elegí un proveedor."); return; }
            try
            {
                var cot = _bll.RegistrarCotizacion(oc.NumeroCompra, prov.Id);
                CargarCotizaciones();
                ConfirmacionForm.MostrarInfo($"Cotización #{cot.Numero} enviada a {prov.Nombre}.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Recibir()
        {
            var oc = grOC.CurrentRow?.DataBoundItem as OrdenCompra06AV;
            if (oc == null) { MostrarError("Elegí una orden de compra."); return; }
            bool ok = ConfirmacionForm.Mostrar(
                $"¿Recibir los insumos de la OC #{oc.NumeroCompra}? Se sumará el stock.",
                "Recibir", ConfirmacionForm.TipoConfirmacion.Advertencia, "Recibir", "Cancelar", FindForm());
            if (!ok) return;
            try
            {
                _bll.RecibirInsumos(oc.NumeroCompra);
                CargarOrdenes();
                CargarFaltantes();
                ConfirmacionForm.MostrarInfo("Insumos recibidos y stock actualizado.",
                    "Compras", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void AprobarDesaprobar(bool aprobar)
        {
            var cot = grCot.CurrentRow?.DataBoundItem as PedidoCotizacion06AV;
            if (cot == null) { MostrarError("Elegí una cotización."); return; }
            try
            {
                if (aprobar) _bll.AprobarCotizacion(cot.Numero);
                else _bll.DesaprobarCotizacion(cot.Numero);
                CargarCotizaciones();
                CargarOrdenes();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        /// <summary>Fila editable para armar la orden de compra (insumo + cantidad a pedir).</summary>
        private class FaltanteVm06AV
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public int Stock { get; set; }
            public int StockMinimo { get; set; }
            public int Cantidad { get; set; }
        }
    }
}
