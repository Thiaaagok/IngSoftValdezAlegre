using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Insumos (PC Factory). Resalta en rojo los que están bajo stock (RFN2).</summary>
    public class InsumosControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly InsumosBLL06AV _bll = new InsumosBLL06AV();
        private List<Insumo06AV> _items = new List<Insumo06AV>();
        private bool _editando;

        private Label lblTitulo;
        private DataGridView grilla;
        private Label lblCodigo, lblDesc, lblStock, lblMin;
        private TextBox txtCodigo, txtDesc, txtStock, txtMin;
        private Button btnNuevo, btnGuardar, btnEliminar;

        public InsumosControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            CargarDatos();
        }

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true };
            grilla = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
            };
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();
            grilla.DataBindingComplete += (s, e) => ResaltarBajoStock();

            lblCodigo = new Label { AutoSize = true };
            lblDesc = new Label { AutoSize = true };
            lblStock = new Label { AutoSize = true };
            lblMin = new Label { AutoSize = true };
            txtCodigo = new TextBox();
            txtDesc = new TextBox();
            txtStock = new TextBox();
            txtMin = new TextBox();

            btnNuevo = new Button { Text = "Nuevo" };
            btnGuardar = new Button { Text = "Guardar" };
            btnEliminar = new Button { Text = "Eliminar" };
            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla,
                lblCodigo, txtCodigo, lblDesc, txtDesc, lblStock, txtStock, lblMin, txtMin,
                btnNuevo, btnGuardar, btnEliminar
            });
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonSecundario(btnNuevo);
            Tema.AplicarBotonPeligro(btnEliminar);
        }

        public void AplicarIdioma()
        {
            lblTitulo.Text = "Gestión de Insumos";
            lblCodigo.Text = "Código:";
            lblDesc.Text = "Descripción:";
            lblStock.Text = "Stock:";
            lblMin.Text = "Stock mín.:";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
        }

        private void AjustarLayout()
        {
            int margen = 12;
            int ancho = Math.Max(720, ClientSize.Width);
            int alto = Math.Max(440, ClientSize.Height);
            lblTitulo.SetBounds(margen, margen, 320, 30);

            int grillaW = (int)(ancho * 0.55);
            grilla.SetBounds(margen, 52, grillaW - margen, alto - 64);

            int fx = grillaW + margen, etiqW = 90, campoX = fx + etiqW;
            int campoW = ancho - campoX - margen, y = 60, paso = 40;
            void Fila(Control l, Control c) { l.SetBounds(fx, y + 3, etiqW, 22); c.SetBounds(campoX, y, campoW, 26); y += paso; }
            Fila(lblCodigo, txtCodigo);
            Fila(lblDesc, txtDesc);
            Fila(lblStock, txtStock);
            Fila(lblMin, txtMin);

            y += 8;
            btnNuevo.SetBounds(campoX, y, 100, 34);
            btnGuardar.SetBounds(campoX + 108, y, 100, 34);
            btnEliminar.SetBounds(campoX + 216, y, 100, 34);
        }

        private void CargarDatos()
        {
            try
            {
                _items = _bll.ObtenerTodos() ?? new List<Insumo06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _items;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        /// <summary>Resalta en rojo suave los insumos con stock por debajo del mínimo (RFN2).</summary>
        private void ResaltarBajoStock()
        {
            foreach (DataGridViewRow fila in grilla.Rows)
            {
                if (fila.DataBoundItem is Insumo06AV ins && ins.BajoStock)
                {
                    fila.DefaultCellStyle.BackColor = Color.MistyRose;
                    fila.DefaultCellStyle.ForeColor = Color.FromArgb(180, 30, 30);
                }
            }
        }

        private void MostrarSeleccion()
        {
            if (grilla.CurrentRow?.DataBoundItem is Insumo06AV i)
            {
                txtCodigo.Text = i.Codigo;
                txtDesc.Text = i.Descripcion;
                txtStock.Text = i.Stock.ToString();
                txtMin.Text = i.StockMinimo.ToString();
                _editando = true;
                txtCodigo.ReadOnly = true;
            }
        }

        private void Nuevo()
        {
            _editando = false;
            txtCodigo.ReadOnly = false;
            txtCodigo.Clear(); txtDesc.Clear();
            txtStock.Text = "0"; txtMin.Text = "0";
            txtCodigo.Focus();
        }

        private void Guardar()
        {
            if (!int.TryParse(txtStock.Text.Trim(), out int stock))
            { MostrarError("El stock debe ser un número entero."); return; }
            if (!int.TryParse(txtMin.Text.Trim(), out int min))
            { MostrarError("El stock mínimo debe ser un número entero."); return; }

            var i = new Insumo06AV
            {
                Codigo = txtCodigo.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Stock = stock,
                StockMinimo = min
            };

            try
            {
                if (_editando) _bll.Modificar(i); else _bll.Crear(i);
                CargarDatos(); Nuevo();
                ConfirmacionForm.MostrarInfo("Insumo guardado correctamente.",
                    "Insumos", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Eliminar()
        {
            string codigo = txtCodigo.Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) { MostrarError("Seleccioná un insumo de la lista."); return; }
            bool ok = ConfirmacionForm.Mostrar($"¿Eliminar el insumo {codigo}?",
                "Eliminar insumo", ConfirmacionForm.TipoConfirmacion.Advertencia,
                "Eliminar", "Cancelar", FindForm());
            if (!ok) return;
            try { _bll.Eliminar(codigo); CargarDatos(); Nuevo(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
    }
}
