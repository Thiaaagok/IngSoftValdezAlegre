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
    /// <summary>ABM de Proveedores (PC Factory). Id autonumérico (oculto).</summary>
    public class ProveedoresControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly ProveedoresBLL06AV _bll = new ProveedoresBLL06AV();
        private List<Proveedor06AV> _items = new List<Proveedor06AV>();
        private int _idEditando;

        private Label lblTitulo;
        private DataGridView grilla;
        private Label lblNombre, lblCuit, lblEmail, lblTel, lblDir;
        private TextBox txtNombre, txtCuit, txtEmail, txtTel, txtDir;
        private Button btnNuevo, btnGuardar, btnEliminar;

        public ProveedoresControl()
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

            lblNombre = new Label { AutoSize = true };
            lblCuit = new Label { AutoSize = true };
            lblEmail = new Label { AutoSize = true };
            lblTel = new Label { AutoSize = true };
            lblDir = new Label { AutoSize = true };
            txtNombre = new TextBox();
            txtCuit = new TextBox();
            txtEmail = new TextBox();
            txtTel = new TextBox();
            txtDir = new TextBox();

            btnNuevo = new Button { Text = "Nuevo" };
            btnGuardar = new Button { Text = "Guardar" };
            btnEliminar = new Button { Text = "Eliminar" };
            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla,
                lblNombre, txtNombre, lblCuit, txtCuit, lblEmail, txtEmail,
                lblTel, txtTel, lblDir, txtDir,
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
            lblTitulo.Text = "Gestión de Proveedores";
            lblNombre.Text = "Nombre:";
            lblCuit.Text = "CUIT:";
            lblEmail.Text = "Email:";
            lblTel.Text = "Teléfono:";
            lblDir.Text = "Dirección:";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
        }

        private void AjustarLayout()
        {
            int margen = 12;
            int ancho = Math.Max(740, ClientSize.Width);
            int alto = Math.Max(440, ClientSize.Height);
            lblTitulo.SetBounds(margen, margen, 320, 30);

            int grillaW = (int)(ancho * 0.52);
            grilla.SetBounds(margen, 52, grillaW - margen, alto - 64);

            int fx = grillaW + margen, etiqW = 90, campoX = fx + etiqW;
            int campoW = ancho - campoX - margen, y = 60, paso = 40;
            void Fila(Control l, Control c) { l.SetBounds(fx, y + 3, etiqW, 22); c.SetBounds(campoX, y, campoW, 26); y += paso; }
            Fila(lblNombre, txtNombre);
            Fila(lblCuit, txtCuit);
            Fila(lblEmail, txtEmail);
            Fila(lblTel, txtTel);
            Fila(lblDir, txtDir);

            y += 8;
            btnNuevo.SetBounds(campoX, y, 100, 34);
            btnGuardar.SetBounds(campoX + 108, y, 100, 34);
            btnEliminar.SetBounds(campoX + 216, y, 100, 34);
        }

        private void CargarDatos()
        {
            try
            {
                _items = _bll.ObtenerTodos() ?? new List<Proveedor06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _items;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarSeleccion()
        {
            if (grilla.CurrentRow?.DataBoundItem is Proveedor06AV p)
            {
                _idEditando = p.Id;
                txtNombre.Text = p.Nombre;
                txtCuit.Text = p.Cuit;
                txtEmail.Text = p.Email;
                txtTel.Text = p.Telefono;
                txtDir.Text = p.Direccion;
            }
        }

        private void Nuevo()
        {
            _idEditando = 0;
            txtNombre.Clear(); txtCuit.Clear(); txtEmail.Clear(); txtTel.Clear(); txtDir.Clear();
            txtNombre.Focus();
        }

        private void Guardar()
        {
            var p = new Proveedor06AV
            {
                Id = _idEditando,
                Nombre = txtNombre.Text.Trim(),
                Cuit = txtCuit.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Telefono = txtTel.Text.Trim(),
                Direccion = txtDir.Text.Trim()
            };

            try
            {
                if (_idEditando > 0) _bll.Modificar(p); else _bll.Crear(p);
                CargarDatos(); Nuevo();
                ConfirmacionForm.MostrarInfo("Proveedor guardado correctamente.",
                    "Proveedores", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Eliminar()
        {
            if (_idEditando <= 0) { MostrarError("Seleccioná un proveedor de la lista."); return; }
            bool ok = ConfirmacionForm.Mostrar($"¿Eliminar el proveedor {txtNombre.Text}?",
                "Eliminar proveedor", ConfirmacionForm.TipoConfirmacion.Advertencia,
                "Eliminar", "Cancelar", FindForm());
            if (!ok) return;
            try { _bll.Eliminar(_idEditando); CargarDatos(); Nuevo(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
    }
}
