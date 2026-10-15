using BE;
using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.UI;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ProveedoresControl : AbmBaseControl06AV
    {
        private readonly ProveedoresBLL06AV _bll = new ProveedoresBLL06AV();
        private List<Proveedor06AV> _items = new List<Proveedor06AV>();
        private Proveedor06AV _sel;

        private Label lblNombre, lblCuit, lblEmail, lblTel, lblDir, lblActivo, lblVacio, lblCuitAyuda;
        private TextBox txtNombre, txtCuit, txtEmail, txtTel, txtDir;
        private CheckBox chkActivo;

        public ProveedoresControl()
        {
            lblNombre = new Label(); lblCuit = new Label(); lblEmail = new Label();
            lblTel = new Label(); lblDir = new Label(); lblActivo = new Label(); lblVacio = new Label();
            txtNombre = new TextBox(); txtCuit = new TextBox(); txtEmail = new TextBox();
            txtTel = new TextBox(); txtDir = new TextBox();
            chkActivo = new CheckBox { AutoSize = true, Checked = true };
            lblCuitAyuda = AyudaCuit06AV.NuevaEtiqueta();
            InicializarAbm();
            Grilla.DataBindingComplete += (s, e) => FormatearGrilla();
            FormatearGrilla();
            Tema.TemaChanged += AjustarCamposTema;
            Disposed += (s, e) => Tema.TemaChanged -= AjustarCamposTema;
        }

        protected override string ClaveTitulo => "pcf_proveedores_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(lblCuit, txtCuit);
            AgregarCampo(lblVacio, lblCuitAyuda);
            AgregarCampo(lblEmail, txtEmail);
            AgregarCampo(lblTel, txtTel);
            AgregarCampo(lblDir, txtDir);
            AgregarCampo(lblActivo, chkActivo);
            txtCuit.MaxLength = 13;
            txtNombre.MaxLength = 150;
            AyudaCuit06AV.Vincular(txtCuit, lblCuitAyuda);
            AjustarCamposTema();
        }

        private void AjustarCamposTema()
        {
            lblCuitAyuda.BackColor = Color.Transparent;
            lblCuitAyuda.Font = Tema.FuenteMini;
            lblCuitAyuda.Margin = new Padding(3, 0, 3, 4);
            lblCuitAyuda.Text = AyudaCuit06AV.Evaluar(txtCuit.Text, out Color c);
            lblCuitAyuda.ForeColor = c;
            chkActivo.BackColor = Color.Transparent;
            chkActivo.ForeColor = Tema.Texto;
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            var t = GestorIdioma06AV.Instancia;
            _items = _bll.ObtenerTodos() ?? new List<Proveedor06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items
                .OrderByDescending(p => p.Activo).ThenBy(p => p.Nombre)
                .Select(p => new ProveedorVm
                {
                    Codigo = "PRV-" + p.Id.ToString("0000"),
                    Nombre = p.Nombre,
                    Cuit = p.Cuit,
                    Email = p.Email,
                    Telefono = p.Telefono,
                    Direccion = p.Direccion,
                    Estado = t.Obtener(p.Activo ? "activo" : "pcf_prov_inactivo"),
                    Proveedor = p
                }).ToList();
        }

        private void FormatearGrilla()
        {
            var t = GestorIdioma06AV.Instancia;
            void H(string col, string key) { if (Grilla.Columns[col] != null) Grilla.Columns[col].HeaderText = t.Obtener(key); }
            H("Codigo", "pcf_col_codigo");
            H("Nombre", "pcf_prov_nombre");
            H("Cuit", "cuit");
            H("Email", "email");
            H("Telefono", "telefono");
            H("Direccion", "direccion");
            H("Estado", "pcf_col_estado");
            if (Grilla.Columns["Codigo"] != null) Grilla.Columns["Codigo"].FillWeight = 55;
            if (Grilla.Columns["Estado"] != null) Grilla.Columns["Estado"].FillWeight = 55;
            foreach (DataGridViewRow row in Grilla.Rows)
                if (row.DataBoundItem is ProveedorVm vm && !vm.Proveedor.Activo)
                {
                    row.DefaultCellStyle.ForeColor = Tema.TextoSuave;
                    if (Grilla.Columns["Estado"] != null) row.Cells["Estado"].Style.ForeColor = Tema.Peligro;
                }
        }

        protected override void PrepararNuevo()
        {
            _sel = null;
            txtNombre.Clear(); txtCuit.Clear(); txtEmail.Clear(); txtTel.Clear(); txtDir.Clear();
            chkActivo.Checked = true;
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is ProveedorVm vm)) return false;
            var p = vm.Proveedor;
            _sel = p;
            txtNombre.Text = p.Nombre;
            txtCuit.Text = p.Cuit;
            txtEmail.Text = p.Email;
            txtTel.Text = p.Telefono;
            txtDir.Text = p.Direccion;
            chkActivo.Checked = p.Activo;
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
                Direccion = txtDir.Text.Trim(),
                Activo = chkActivo.Checked
            };
            if (editando) _bll.Modificar(p); else _bll.Crear(p);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            var t = GestorIdioma06AV.Instancia;
            if (!(Grilla.CurrentRow?.DataBoundItem is ProveedorVm vm))
            {
                MostrarError(t.Obtener("pcf_seleccione_registro"));
                return;
            }
            var p = vm.Proveedor;
            if (!Confirmar(t.Obtener("pcf_prov_confirmar_eliminar", p.Nombre), t.Obtener("eliminar"))) return;
            try
            {
                bool eliminado = _bll.Eliminar(p.Id);
                ConfirmacionForm.MostrarInfo(
                    t.Obtener(eliminado ? "pcf_prov_eliminado" : "pcf_prov_desactivado", p.Nombre),
                    t.Obtener("pcf_proveedores_titulo"),
                    eliminado ? ConfirmacionForm.TipoConfirmacion.Info : ConfirmacionForm.TipoConfirmacion.Advertencia,
                    FindForm());
                RecargarGrilla();
            }
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
            lblActivo.Text = t.Obtener("pcf_col_estado") + ":";
            chkActivo.Text = t.Obtener("pcf_prov_activo_check");
            if (txtCuit != null && lblCuitAyuda != null)
            {
                lblCuitAyuda.Text = AyudaCuit06AV.Evaluar(txtCuit.Text, out Color c);
                lblCuitAyuda.ForeColor = c;
            }
            if (Grilla?.DataSource != null) RecargarGrilla();
        }

        private class ProveedorVm
        {
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public string Cuit { get; set; }
            public string Email { get; set; }
            public string Telefono { get; set; }
            public string Direccion { get; set; }
            public string Estado { get; set; }
            [Browsable(false)] public Proveedor06AV Proveedor { get; set; }
        }
    }
}
