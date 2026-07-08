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
    /// <summary>ABM de Componentes (PC Factory). Grilla + formulario, code-only.</summary>
    public class ComponentesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly ComponentesBLL06AV _bll = new ComponentesBLL06AV();
        private List<Componente06AV> _items = new List<Componente06AV>();
        private bool _editando;

        private Label lblTitulo;
        private DataGridView grilla;
        private Label lblCodigo, lblDesc, lblTipo, lblMarca, lblModelo, lblPrecio, lblStock;
        private TextBox txtCodigo, txtDesc, txtMarca, txtModelo, txtPrecio, txtStock;
        private ComboBox cboTipo;
        private Button btnNuevo, btnGuardar, btnEliminar;

        public ComponentesControl()
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
            grilla = NuevaGrilla();
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();

            lblCodigo = new Label { AutoSize = true };
            lblDesc = new Label { AutoSize = true };
            lblTipo = new Label { AutoSize = true };
            lblMarca = new Label { AutoSize = true };
            lblModelo = new Label { AutoSize = true };
            lblPrecio = new Label { AutoSize = true };
            lblStock = new Label { AutoSize = true };

            txtCodigo = new TextBox();
            txtDesc = new TextBox();
            txtMarca = new TextBox();
            txtModelo = new TextBox();
            txtPrecio = new TextBox();
            txtStock = new TextBox();
            cboTipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboTipo.DataSource = Enum.GetValues(typeof(TipoComponente06AV));

            btnNuevo = new Button { Text = "Nuevo" };
            btnGuardar = new Button { Text = "Guardar" };
            btnEliminar = new Button { Text = "Eliminar" };
            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla,
                lblCodigo, txtCodigo, lblDesc, txtDesc, lblTipo, cboTipo,
                lblMarca, txtMarca, lblModelo, txtModelo, lblPrecio, txtPrecio, lblStock, txtStock,
                btnNuevo, btnGuardar, btnEliminar
            });
        }

        private static DataGridView NuevaGrilla() => new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };

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
            lblTitulo.Text = "Gestión de Componentes";
            lblCodigo.Text = "Código:";
            lblDesc.Text = "Descripción:";
            lblTipo.Text = "Tipo:";
            lblMarca.Text = "Marca:";
            lblModelo.Text = "Modelo:";
            lblPrecio.Text = "Precio:";
            lblStock.Text = "Stock:";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
        }

        private void AjustarLayout()
        {
            int margen = 12;
            int ancho = Math.Max(760, ClientSize.Width);
            int alto = Math.Max(460, ClientSize.Height);
            lblTitulo.SetBounds(margen, margen, 320, 30);

            int grillaW = (int)(ancho * 0.52);
            grilla.SetBounds(margen, 52, grillaW - margen, alto - 64);

            int fx = grillaW + margen, etiqW = 90, campoX = fx + etiqW;
            int campoW = ancho - campoX - margen, y = 60, paso = 38;

            void Fila(Control l, Control c) { l.SetBounds(fx, y + 3, etiqW, 22); c.SetBounds(campoX, y, campoW, 26); y += paso; }
            Fila(lblCodigo, txtCodigo);
            Fila(lblDesc, txtDesc);
            Fila(lblTipo, cboTipo);
            Fila(lblMarca, txtMarca);
            Fila(lblModelo, txtModelo);
            Fila(lblPrecio, txtPrecio);
            Fila(lblStock, txtStock);

            y += 8;
            btnNuevo.SetBounds(campoX, y, 100, 34);
            btnGuardar.SetBounds(campoX + 108, y, 100, 34);
            btnEliminar.SetBounds(campoX + 216, y, 100, 34);
        }

        private void CargarDatos()
        {
            try
            {
                _items = _bll.ObtenerTodos() ?? new List<Componente06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _items;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarSeleccion()
        {
            if (grilla.CurrentRow?.DataBoundItem is Componente06AV c)
            {
                txtCodigo.Text = c.Codigo;
                txtDesc.Text = c.Descripcion;
                cboTipo.SelectedItem = c.Tipo;
                txtMarca.Text = c.Marca;
                txtModelo.Text = c.Modelo;
                txtPrecio.Text = c.PrecioUnitario.ToString("0.00");
                txtStock.Text = c.StockDisponible.ToString();
                _editando = true;
                txtCodigo.ReadOnly = true;
            }
        }

        private void Nuevo()
        {
            _editando = false;
            txtCodigo.ReadOnly = false;
            txtCodigo.Clear(); txtDesc.Clear(); txtMarca.Clear(); txtModelo.Clear();
            txtPrecio.Text = "0"; txtStock.Text = "0";
            if (cboTipo.Items.Count > 0) cboTipo.SelectedIndex = 0;
            txtCodigo.Focus();
        }

        private void Guardar()
        {
            if (!decimal.TryParse(txtPrecio.Text.Trim(), out decimal precio))
            { MostrarError("El precio debe ser un número válido."); return; }
            if (!int.TryParse(txtStock.Text.Trim(), out int stock))
            { MostrarError("El stock debe ser un número entero."); return; }

            var c = new Componente06AV
            {
                Codigo = txtCodigo.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Tipo = (TipoComponente06AV)(cboTipo.SelectedItem ?? TipoComponente06AV.Otro),
                Marca = txtMarca.Text.Trim(),
                Modelo = txtModelo.Text.Trim(),
                PrecioUnitario = precio,
                StockDisponible = stock
            };

            try
            {
                if (_editando) _bll.Modificar(c); else _bll.Crear(c);
                CargarDatos(); Nuevo();
                ConfirmacionForm.MostrarInfo("Componente guardado correctamente.",
                    "Componentes", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Eliminar()
        {
            string codigo = txtCodigo.Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) { MostrarError("Seleccioná un componente de la lista."); return; }
            bool ok = ConfirmacionForm.Mostrar($"¿Eliminar el componente {codigo}?",
                "Eliminar componente", ConfirmacionForm.TipoConfirmacion.Advertencia,
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
