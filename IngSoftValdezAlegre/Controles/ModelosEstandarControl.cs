using BE;
using BLL;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// ABM del catálogo de modelos de computadora estándar (RFN1). Los componentes del
    /// modelo se eligen con el asistente "Armá tu PC" (uno por tipo).
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class ModelosEstandarControl : AbmBaseControl06AV
    {
        private readonly ModelosEstandarBLL06AV _bll = new ModelosEstandarBLL06AV();
        private readonly ComponentesBLL06AV _componentesBLL = new ComponentesBLL06AV();
        private List<ModeloEstandar06AV> _items = new List<ModeloEstandar06AV>();
        private ModeloEstandar06AV _sel;
        private List<Componente06AV> _componentes = new List<Componente06AV>();

        private Label lblNombre, lblDesc, lblComp;
        private ResumenPcControl06AV _resumen;
        private TextBox txtNombre, txtDesc;
        private Button btnArmar;

        public ModelosEstandarControl()
        {
            lblNombre = new Label();
            lblDesc = new Label();
            lblComp = new Label();
            _resumen = new ResumenPcControl06AV();
            txtNombre = new TextBox();
            txtDesc = new TextBox();
            btnArmar = new Button { Width = 190, Height = 32, AutoSize = false };
            btnArmar.Click += (s, e) => AbrirAsistente();
            InicializarAbm();
        }

        protected override string ClaveTitulo => "pcf_modelos_titulo";

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(lblDesc, txtDesc);

            var pnlComp = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0)
            };
            pnlComp.Controls.Add(btnArmar);
            pnlComp.Controls.Add(_resumen);
            AgregarCampo(lblComp, pnlComp);
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            _items = _bll.ObtenerTodos() ?? new List<ModeloEstandar06AV>();
            grilla.DataSource = null;
            grilla.DataSource = _items;
            if (grilla.Columns["Componentes"] != null) grilla.Columns["Componentes"].Visible = false;
        }

        private void AbrirAsistente()
        {
            List<Componente06AV> todos;
            try { todos = _componentesBLL.ObtenerTodos() ?? new List<Componente06AV>(); }
            catch (Exception ex) { MostrarError(ex.Message); return; }
            if (todos.Count == 0) { MostrarError("No hay componentes cargados."); return; }

            using (var dlg = new FRMArmarPc06AV(todos, _componentes))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _componentes = dlg.Seleccionados;
                    ActualizarResumen();
                }
            }
        }

        private void ActualizarResumen()
        {
            _resumen.Mostrar(_componentes);
        }

        protected override void PrepararNuevo()
        {
            _sel = null;
            txtNombre.Clear();
            txtDesc.Clear();
            _componentes = new List<Componente06AV>();
            ActualizarResumen();
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is ModeloEstandar06AV m)) return false;
            _sel = m;
            txtNombre.Text = m.Nombre;
            txtDesc.Text = m.Descripcion;
            _componentes = new List<Componente06AV>(m.Componentes ?? new List<Componente06AV>());
            ActualizarResumen();
            return true;
        }

        protected override bool Guardar(bool editando)
        {
            var modelo = new ModeloEstandar06AV
            {
                Id = editando && _sel != null ? _sel.Id : 0,
                Nombre = txtNombre.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Componentes = _componentes
            };
            if (editando) _bll.Modificar(modelo); else _bll.Crear(modelo);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is ModeloEstandar06AV m))
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (!Confirmar($"¿Eliminar el modelo '{m.Nombre}'?",
                           GestorIdioma06AV.Instancia.Obtener("eliminar"))) return;
            try { _bll.Eliminar(m.Id); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblNombre.Text = t.Obtener("pcf_prov_nombre") + ":";
            lblDesc.Text = t.Obtener("descripcion") + ":";
            lblComp.Text = t.Obtener("pcf_componentes") + ":";
            btnArmar.Text = t.Obtener("pcf_elegir_componentes");
        }
    }
}
