using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Clientes (PC Factory). Grilla full + formulario de alta/edición.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ClientesControl : AbmBaseControl06AV
    {
        private readonly ClientesBLL06AV _bll = new ClientesBLL06AV();
        private List<Cliente06AV> _items = new List<Cliente06AV>();

        private Label lblDni, lblNombre, lblApellido, lblTelefono, lblDireccion;
        private TextBox txtDni, txtNombre, txtApellido, txtTelefono, txtDireccion;

        public ClientesControl()
        {
            lblDni = new Label(); lblNombre = new Label(); lblApellido = new Label();
            lblTelefono = new Label(); lblDireccion = new Label();
            txtDni = new TextBox(); txtNombre = new TextBox(); txtApellido = new TextBox();
            txtTelefono = new TextBox(); txtDireccion = new TextBox();
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_clientes_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblDni, txtDni);
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(lblApellido, txtApellido);
            AgregarCampo(lblTelefono, txtTelefono);
            AgregarCampo(lblDireccion, txtDireccion);
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodos() ?? new List<Cliente06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
            if (grilla.Columns["NombreCompleto"] != null)
                grilla.Columns["NombreCompleto"].Visible = false;
        }

        protected override void PrepararNuevo()
        {
            txtDni.ReadOnly = false;
            txtDni.Clear(); txtNombre.Clear(); txtApellido.Clear();
            txtTelefono.Clear(); txtDireccion.Clear();
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Cliente06AV c)) return false;
            txtDni.Text = c.Dni;
            txtNombre.Text = c.Nombre;
            txtApellido.Text = c.Apellido;
            txtTelefono.Text = c.Telefono;
            txtDireccion.Text = c.Direccion;
            txtDni.ReadOnly = true;   // el DNI es la clave: no se edita
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            var c = new Cliente06AV
            {
                Dni = txtDni.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Direccion = txtDireccion.Text.Trim()
            };
            if (editando) _bll.Modificar(c); else _bll.Crear(c);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Cliente06AV c))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (!Confirmar($"¿Eliminar el cliente con DNI {c.Dni}?",
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(c.Dni); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblDni.Text = t.Obtener("dni") + ":";
            lblNombre.Text = t.Obtener("nombre") + ":";
            lblApellido.Text = t.Obtener("apellido") + ":";
            lblTelefono.Text = t.Obtener("telefono") + ":";
            lblDireccion.Text = t.Obtener("direccion") + ":";
        }
    }
}
