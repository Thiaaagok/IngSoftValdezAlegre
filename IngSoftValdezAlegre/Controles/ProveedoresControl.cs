using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Proveedores (PC Factory). Id autonumérico (oculto).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ProveedoresControl : AbmBaseControl06AV
    {
        private readonly ProveedoresBLL06AV _bll = new ProveedoresBLL06AV();
        private List<Proveedor06AV> _items = new List<Proveedor06AV>();
        private Proveedor06AV _sel;

        private Label lblNombre, lblCuit, lblEmail, lblTel, lblDir;
        private TextBox txtNombre, txtCuit, txtEmail, txtTel, txtDir;

        public ProveedoresControl()
        {
            lblNombre = new Label(); lblCuit = new Label(); lblEmail = new Label();
            lblTel = new Label(); lblDir = new Label();
            txtNombre = new TextBox(); txtCuit = new TextBox(); txtEmail = new TextBox();
            txtTel = new TextBox(); txtDir = new TextBox();
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_proveedores_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(lblCuit, txtCuit);
            AgregarCampo(lblEmail, txtEmail);
            AgregarCampo(lblTel, txtTel);
            AgregarCampo(lblDir, txtDir);
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodos() ?? new List<Proveedor06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
        }

        protected override void PrepararNuevo()
        {
            _sel = null;
            txtNombre.Clear(); txtCuit.Clear(); txtEmail.Clear(); txtTel.Clear(); txtDir.Clear();
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Proveedor06AV p)) return false;
            _sel = p;
            txtNombre.Text = p.Nombre;
            txtCuit.Text = p.Cuit;
            txtEmail.Text = p.Email;
            txtTel.Text = p.Telefono;
            txtDir.Text = p.Direccion;
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            var p = new Proveedor06AV
            {
                Id = editando && _sel != null ? _sel.Id : 0,
                Nombre = txtNombre.Text.Trim(),
                Cuit = txtCuit.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Telefono = txtTel.Text.Trim(),
                Direccion = txtDir.Text.Trim()
            };
            if (editando) _bll.Modificar(p); else _bll.Crear(p);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is Proveedor06AV p))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (!Confirmar($"¿Eliminar el proveedor {p.Nombre}?",
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(p.Id); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblNombre.Text = t.Obtener("pcf_prov_nombre") + ":";
            lblCuit.Text = t.Obtener("cuit") + ":";
            lblEmail.Text = t.Obtener("email") + ":";
            lblTel.Text = t.Obtener("telefono") + ":";
            lblDir.Text = t.Obtener("direccion") + ":";
        }
    }
}
