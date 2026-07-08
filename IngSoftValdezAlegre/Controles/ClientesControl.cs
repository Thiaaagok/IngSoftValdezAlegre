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
    /// <summary>
    /// ABM de Clientes (PC Factory). Grilla a la izquierda y formulario a la derecha.
    /// Construido en código (sin Designer) respetando el Tema y ConfirmacionForm del sistema.
    /// </summary>
    public class ClientesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly ClientesBLL06AV _clientesBLL = new ClientesBLL06AV();
        private List<Cliente06AV> _clientes = new List<Cliente06AV>();
        private bool _editando;

        private Label lblTitulo;
        private DataGridView grilla;
        private Label lblDni, lblNombre, lblApellido, lblTelefono, lblDireccion;
        private TextBox txtDni, txtNombre, txtApellido, txtTelefono, txtDireccion;
        private Button btnNuevo, btnGuardar, btnEliminar;

        public ClientesControl()
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
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AutoGenerateColumns = true
            };
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();

            lblDni = new Label { AutoSize = true };
            lblNombre = new Label { AutoSize = true };
            lblApellido = new Label { AutoSize = true };
            lblTelefono = new Label { AutoSize = true };
            lblDireccion = new Label { AutoSize = true };

            txtDni = new TextBox();
            txtNombre = new TextBox();
            txtApellido = new TextBox();
            txtTelefono = new TextBox();
            txtDireccion = new TextBox();

            btnNuevo = new Button { Text = "Nuevo" };
            btnGuardar = new Button { Text = "Guardar" };
            btnEliminar = new Button { Text = "Eliminar" };

            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla,
                lblDni, txtDni, lblNombre, txtNombre, lblApellido, txtApellido,
                lblTelefono, txtTelefono, lblDireccion, txtDireccion,
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
            // Textos literales (se pueden migrar a claves de GestorIdioma más adelante).
            lblTitulo.Text = "Gestión de Clientes";
            lblDni.Text = "DNI:";
            lblNombre.Text = "Nombre:";
            lblApellido.Text = "Apellido:";
            lblTelefono.Text = "Teléfono:";
            lblDireccion.Text = "Dirección:";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
        }

        private void AjustarLayout()
        {
            int margen = 12;
            int ancho = Math.Max(720, ClientSize.Width);
            int alto = Math.Max(440, ClientSize.Height);

            lblTitulo.SetBounds(margen, margen, 300, 30);

            int grillaW = (int)(ancho * 0.55);
            grilla.SetBounds(margen, 52, grillaW - margen, alto - 64);

            int fx = grillaW + margen;          // inicio del formulario (derecha)
            int etiquetaW = 90;
            int campoX = fx + etiquetaW;
            int campoW = ancho - campoX - margen;
            int y = 60;
            int paso = 40;

            void Fila(Label l, TextBox t)
            {
                l.SetBounds(fx, y + 3, etiquetaW, 22);
                t.SetBounds(campoX, y, campoW, 26);
                y += paso;
            }

            Fila(lblDni, txtDni);
            Fila(lblNombre, txtNombre);
            Fila(lblApellido, txtApellido);
            Fila(lblTelefono, txtTelefono);
            Fila(lblDireccion, txtDireccion);

            y += 8;
            btnNuevo.SetBounds(campoX, y, 100, 34);
            btnGuardar.SetBounds(campoX + 108, y, 100, 34);
            btnEliminar.SetBounds(campoX + 216, y, 100, 34);
        }

        // ── Datos ────────────────────────────────────────────────────
        private void CargarDatos()
        {
            try
            {
                _clientes = _clientesBLL.ObtenerTodos() ?? new List<Cliente06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _clientes;
                OcultarColumnasAuxiliares();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void OcultarColumnasAuxiliares()
        {
            if (grilla.Columns["NombreCompleto"] != null)
                grilla.Columns["NombreCompleto"].Visible = false;
        }

        private void MostrarSeleccion()
        {
            if (grilla.CurrentRow?.DataBoundItem is Cliente06AV c)
            {
                txtDni.Text = c.Dni;
                txtNombre.Text = c.Nombre;
                txtApellido.Text = c.Apellido;
                txtTelefono.Text = c.Telefono;
                txtDireccion.Text = c.Direccion;
                _editando = true;
                txtDni.ReadOnly = true; // el DNI es la clave: no se edita
            }
        }

        private void Nuevo()
        {
            _editando = false;
            txtDni.ReadOnly = false;
            txtDni.Clear();
            txtNombre.Clear();
            txtApellido.Clear();
            txtTelefono.Clear();
            txtDireccion.Clear();
            txtDni.Focus();
        }

        private void Guardar()
        {
            var cliente = new Cliente06AV
            {
                Dni = txtDni.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Direccion = txtDireccion.Text.Trim()
            };

            try
            {
                if (_editando) _clientesBLL.Modificar(cliente);
                else _clientesBLL.Crear(cliente);

                CargarDatos();
                Nuevo();
                ConfirmacionForm.MostrarInfo("Cliente guardado correctamente.",
                    "Clientes", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void Eliminar()
        {
            string dni = txtDni.Text.Trim();
            if (string.IsNullOrWhiteSpace(dni))
            {
                MostrarError("Seleccioná un cliente de la lista para eliminar.");
                return;
            }

            bool ok = ConfirmacionForm.Mostrar(
                $"¿Eliminar el cliente con DNI {dni}?",
                titulo: "Eliminar cliente",
                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                textoSi: "Eliminar", textoNo: "Cancelar", owner: FindForm());
            if (!ok) return;

            try
            {
                _clientesBLL.Eliminar(dni);
                CargarDatos();
                Nuevo();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void MostrarError(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                GestorIdioma06AV.Instancia.Obtener("aviso"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                FindForm());
        }
    }
}
