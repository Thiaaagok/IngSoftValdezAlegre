using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Líneas de Ensamblaje (PC Factory). Id autonumérico (oculto).</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class LineasEnsamblajeControl : AbmBaseControl06AV
    {
        private readonly LineasEnsamblajeBLL06AV _bll = new LineasEnsamblajeBLL06AV();
        private List<LineaEnsamblaje06AV> _items = new List<LineaEnsamblaje06AV>();
        private LineaEnsamblaje06AV _sel;

        private Label lblNombre, lblDesc;
        private TextBox txtNombre, txtDesc;
        private CheckBox chkDisponible;

        public LineasEnsamblajeControl()
        {
            lblNombre = new Label(); lblDesc = new Label();
            txtNombre = new TextBox(); txtDesc = new TextBox();
            chkDisponible = new CheckBox { AutoSize = true, Checked = true };
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_lineas_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(lblDesc, txtDesc);
            AgregarCampo(new Label { Text = string.Empty }, chkDisponible);
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodas() ?? new List<LineaEnsamblaje06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
        }

        protected override void PrepararNuevo()
        {
            _sel = null;
            txtNombre.Clear(); txtDesc.Clear();
            chkDisponible.Checked = true;
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is LineaEnsamblaje06AV l)) return false;
            _sel = l;
            txtNombre.Text = l.Nombre;
            txtDesc.Text = l.Descripcion;
            chkDisponible.Checked = l.Disponible;
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            var l = new LineaEnsamblaje06AV
            {
                Id = editando && _sel != null ? _sel.Id : 0,
                Nombre = txtNombre.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Disponible = chkDisponible.Checked
            };
            if (editando) _bll.Modificar(l); else _bll.Crear(l);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is LineaEnsamblaje06AV l))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (!Confirmar($"¿Eliminar la línea {l.Nombre}?",
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(l.Id); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblNombre.Text = t.Obtener("pcf_prov_nombre") + ":";
            lblDesc.Text = t.Obtener("descripcion") + ":";
            chkDisponible.Text = t.Obtener("disponible");
        }
    }
}
