using BE;
using BLL;
using BLL.Armado;
using SER;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA.Controles
{
    [DesignerCategory("Code")]
    public class ReglasCompatibilidadControl06AV : AbmBaseControl06AV
    {
        private sealed class FilaRegla
        {
            [Browsable(false)]
            public ReglaCompatibilidad06AV Regla { get; set; }
            public string Nombre { get; set; }
            public string Condicion { get; set; }
            public string Accion { get; set; }
            public string Estado { get; set; }
            public string Origen { get; set; }
        }

        private readonly CompatibilidadBLL06AV _bll = new CompatibilidadBLL06AV();
        private ReglaCompatibilidad06AV _sel;

        private Label lblAviso, lblNombre, lblNivel, lblPiezaA, lblDatoA, lblOperador, lblPiezaB, lblDatoB, lblValor, lblMensaje,
                      lblVistaTit, lblVista, lblAyudaMensaje;
        private TextBox txtNombre, txtValor, txtMensaje;
        private CheckBox chkActiva;
        private ComboBox cboNivel, cboPiezaA, cboDatoA, cboOperador, cboPiezaB, cboDatoB;
        private FlowLayoutPanel pnlMensaje;

        public ReglasCompatibilidadControl06AV()
        {
            lblAviso = new Label { AutoSize = true, MaximumSize = new Size(560, 0), Padding = new Padding(6, 4, 6, 4) };
            lblNombre = new Label(); lblNivel = new Label(); lblPiezaA = new Label(); lblDatoA = new Label();
            lblOperador = new Label(); lblPiezaB = new Label(); lblDatoB = new Label(); lblValor = new Label();
            lblMensaje = new Label(); lblVistaTit = new Label();
            lblVista = new Label { AutoSize = true, MaximumSize = new Size(560, 0), Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
            lblAyudaMensaje = new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            txtNombre = new TextBox { MaxLength = 150 };
            txtValor = new TextBox { MaxLength = 6 };
            txtMensaje = new TextBox { Width = 360, MaxLength = 300 };
            chkActiva = new CheckBox { AutoSize = true, Checked = true };
            cboNivel = Combo(); cboPiezaA = Combo(); cboDatoA = Combo(); cboOperador = Combo(); cboPiezaB = Combo(); cboDatoB = Combo();
            pnlMensaje = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
            pnlMensaje.Controls.Add(txtMensaje);
            pnlMensaje.Controls.Add(lblAyudaMensaje);

            foreach (ComboBox c in new[] { cboPiezaA, cboDatoA, cboOperador, cboPiezaB, cboDatoB })
                c.SelectedIndexChanged += (s, e) => ActualizarVista();
            InicializarAbm();
            LlenarCombos();
        }

        protected override string ClaveTitulo => "cmp_titulo";

        private static ComboBox Combo() => new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Value", ValueMember = "Key" };

        protected override void ConstruirCampos(TableLayoutPanel tabla)
        {
            AgregarCampo(new Label { Text = string.Empty }, lblAviso);
            AgregarCampo(lblNombre, txtNombre);
            AgregarCampo(new Label { Text = string.Empty }, chkActiva);
            AgregarCampo(lblNivel, cboNivel);
            AgregarCampo(lblPiezaA, cboPiezaA);
            AgregarCampo(lblDatoA, cboDatoA);
            AgregarCampo(lblOperador, cboOperador);
            AgregarCampo(lblPiezaB, cboPiezaB);
            AgregarCampo(lblDatoB, cboDatoB);
            AgregarCampo(lblValor, txtValor);
            AgregarCampo(lblMensaje, pnlMensaje);
            AgregarCampo(lblVistaTit, lblVista);
        }

        private void LlenarCombos()
        {
            var t = GestorIdioma06AV.Instancia;
            Cargar(cboNivel, new[]
            {
                Par(NivelReglaCompatibilidad06AV.Incompatible, t.Obtener("cmp_nivel_bloquea")),
                Par(NivelReglaCompatibilidad06AV.Advertencia, t.Obtener("cmp_nivel_advierte"))
            });
            var tipos = Enum.GetValues(typeof(TipoComponente06AV)).Cast<TipoComponente06AV>()
                            .Select(x => Par(x, NombreTipo(x))).ToList();
            Cargar(cboPiezaA, tipos);
            Cargar(cboPiezaB, tipos);
            var datos = Enum.GetValues(typeof(AtributoCompatibilidad06AV)).Cast<AtributoCompatibilidad06AV>()
                            .Select(x => Par(x, NombreAtributo(x))).ToList();
            Cargar(cboDatoA, datos);
            Cargar(cboDatoB, datos);
            Cargar(cboOperador, Enum.GetValues(typeof(OperadorCompatibilidad06AV)).Cast<OperadorCompatibilidad06AV>()
                                    .Select(x => Par(x, NombreOperador(x))).ToList());
        }

        private static KeyValuePair<T, string> Par<T>(T clave, string texto) => new KeyValuePair<T, string>(clave, texto);

        private static void Cargar<T>(ComboBox c, IEnumerable<KeyValuePair<T, string>> items)
        {
            object anterior = c.SelectedValue;
            c.DataSource = items.ToList();
            if (anterior != null) c.SelectedValue = anterior;
        }

        public static string NombreTipo(TipoComponente06AV t) => GestorIdioma06AV.Instancia.Obtener("cmp_tipo_" + t);
        public static string NombreAtributo(AtributoCompatibilidad06AV a) => GestorIdioma06AV.Instancia.Obtener("cmp_attr_" + a);
        public static string NombreOperador(OperadorCompatibilidad06AV o) => GestorIdioma06AV.Instancia.Obtener("cmp_op_" + o);

        private static string NombreRegla(ReglaCompatibilidad06AV r)
        {
            if (!r.Sistema) return r.Nombre;
            string clave = "cmpr_" + r.Codigo, texto = GestorIdioma06AV.Instancia.Obtener(clave);
            return texto == clave ? r.Nombre : texto;
        }

        private static string Condicion(ReglaCompatibilidad06AV r)
        {
            var t = GestorIdioma06AV.Instancia;
            if (r.Sistema) return t.Obtener("cmp_condicion_programada");
            if (!r.TipoA.HasValue || !r.TipoB.HasValue || !r.AtributoA.HasValue || !r.AtributoB.HasValue || !r.Operador.HasValue)
                return "";
            return t.Obtener("cmp_vista", NombreAtributo(r.AtributoA.Value), NombreTipo(r.TipoA.Value),
                             NombreOperador(r.Operador.Value), NombreAtributo(r.AtributoB.Value), NombreTipo(r.TipoB.Value));
        }

        protected override void CargarDatosEnGrilla(DataGridView grilla)
        {
            var t = GestorIdioma06AV.Instancia;
            List<ReglaCompatibilidad06AV> reglas = _bll.ObtenerReglas() ?? new List<ReglaCompatibilidad06AV>();
            grilla.DataSource = null;
            grilla.DataSource = reglas.Select(r => new FilaRegla
            {
                Regla = r,
                Nombre = NombreRegla(r),
                Condicion = Condicion(r),
                Accion = t.Obtener(r.Nivel == NivelReglaCompatibilidad06AV.Advertencia ? "cmp_nivel_advierte" : "cmp_nivel_bloquea"),
                Estado = t.Obtener(r.Activa ? "cmp_estado_activa" : "cmp_estado_inactiva"),
                Origen = t.Obtener(r.Sistema ? "cmp_origen_sistema" : "cmp_origen_propia")
            }).ToList();

            var encabezados = new Dictionary<string, string>
            {
                { "Nombre", "cmp_col_nombre" }, { "Condicion", "cmp_col_condicion" }, { "Accion", "cmp_col_nivel" },
                { "Estado", "cmp_col_estado" }, { "Origen", "cmp_col_origen" }
            };
            foreach (DataGridViewColumn col in grilla.Columns)
                if (encabezados.TryGetValue(col.DataPropertyName, out string clave)) col.HeaderText = t.Obtener(clave);
            if (grilla.Columns["Nombre"] != null) grilla.Columns["Nombre"].FillWeight = 180;
            if (grilla.Columns["Condicion"] != null) grilla.Columns["Condicion"].FillWeight = 200;
        }

        protected override void PrepararNuevo()
        {
            _sel = null;
            txtNombre.Clear();
            txtValor.Clear();
            txtMensaje.Clear();
            chkActiva.Checked = true;
            cboNivel.SelectedValue = NivelReglaCompatibilidad06AV.Incompatible;
            cboPiezaA.SelectedValue = TipoComponente06AV.Refrigeracion;
            cboDatoA.SelectedValue = AtributoCompatibilidad06AV.Socket;
            cboOperador.SelectedValue = OperadorCompatibilidad06AV.Contiene;
            cboPiezaB.SelectedValue = TipoComponente06AV.Procesador;
            cboDatoB.SelectedValue = AtributoCompatibilidad06AV.Socket;
            ModoSistema(false, null);
            ActualizarVista();
        }

        protected override bool CargarSeleccionEnCampos()
        {
            if (!(Grilla.CurrentRow?.DataBoundItem is FilaRegla f)) return false;
            ReglaCompatibilidad06AV r = f.Regla;
            _sel = r;
            txtNombre.Text = NombreRegla(r);
            chkActiva.Checked = r.Activa;
            cboNivel.SelectedValue = r.Nivel;
            if (r.TipoA.HasValue) cboPiezaA.SelectedValue = r.TipoA.Value;
            if (r.AtributoA.HasValue) cboDatoA.SelectedValue = r.AtributoA.Value;
            if (r.Operador.HasValue) cboOperador.SelectedValue = r.Operador.Value;
            if (r.TipoB.HasValue) cboPiezaB.SelectedValue = r.TipoB.Value;
            if (r.AtributoB.HasValue) cboDatoB.SelectedValue = r.AtributoB.Value;
            txtValor.Text = r.Valor.HasValue ? r.Valor.Value.ToString("0.##") : "";
            txtMensaje.Text = r.Mensaje ?? "";
            ModoSistema(r.Sistema, r.Codigo);
            ActualizarVista();
            return true;
        }

        private void ModoSistema(bool sistema, string codigo)
        {
            var t = GestorIdioma06AV.Instancia;
            bool conValor = sistema && (codigo == "fuente_limite" || codigo == "ram_slots");
            txtNombre.ReadOnly = sistema;
            foreach (Control c in new Control[] { cboPiezaA, cboDatoA, cboOperador, cboPiezaB, cboDatoB, pnlMensaje,
                                                  lblPiezaA, lblDatoA, lblOperador, lblPiezaB, lblDatoB, lblMensaje,
                                                  lblVistaTit, lblVista })
                c.Visible = !sistema;
            lblValor.Visible = txtValor.Visible = conValor;
            lblValor.Text = codigo == "ram_slots" ? t.Obtener("cmp_lbl_valor_ranuras") : t.Obtener("cmp_lbl_valor_margen");
            lblAviso.Visible = sistema;
            lblAviso.Text = sistema ? t.Obtener("cmp_sistema_aviso") : "";
        }

        private void ActualizarVista()
        {
            if (!(cboPiezaA.SelectedValue is TipoComponente06AV ta) || !(cboPiezaB.SelectedValue is TipoComponente06AV tb) ||
                !(cboDatoA.SelectedValue is AtributoCompatibilidad06AV da) || !(cboDatoB.SelectedValue is AtributoCompatibilidad06AV db) ||
                !(cboOperador.SelectedValue is OperadorCompatibilidad06AV op))
            {
                lblVista.Text = "";
                return;
            }
            lblVista.Text = GestorIdioma06AV.Instancia.Obtener("cmp_vista_completa",
                NombreTipo(ta), NombreTipo(tb), NombreAtributo(da), NombreTipo(ta), NombreOperador(op), NombreAtributo(db), NombreTipo(tb));
        }

        protected override bool Guardar(bool editando)
        {
            var t = GestorIdioma06AV.Instancia;
            decimal? valor = null;
            if (txtValor.Visible && txtValor.Text.Trim().Length > 0)
            {
                if (!decimal.TryParse(txtValor.Text.Trim(), out decimal v))
                { MostrarError(t.Obtener("pcf_esp_numero_invalido", txtValor.Text.Trim())); return false; }
                valor = v;
            }

            bool sistema = editando && _sel != null && _sel.Sistema;
            var r = new ReglaCompatibilidad06AV
            {
                Id = editando && _sel != null ? _sel.Id : 0,
                Codigo = editando && _sel != null ? _sel.Codigo : null,
                Sistema = sistema,
                Nombre = sistema ? _sel.Nombre : txtNombre.Text.Trim(),
                Activa = chkActiva.Checked,
                Nivel = cboNivel.SelectedValue is NivelReglaCompatibilidad06AV n ? n : NivelReglaCompatibilidad06AV.Incompatible,
                Valor = sistema ? valor : null,
                TipoA = sistema ? null : cboPiezaA.SelectedValue as TipoComponente06AV?,
                AtributoA = sistema ? null : cboDatoA.SelectedValue as AtributoCompatibilidad06AV?,
                Operador = sistema ? null : cboOperador.SelectedValue as OperadorCompatibilidad06AV?,
                TipoB = sistema ? null : cboPiezaB.SelectedValue as TipoComponente06AV?,
                AtributoB = sistema ? null : cboDatoB.SelectedValue as AtributoCompatibilidad06AV?,
                Mensaje = sistema ? null : (txtMensaje.Text.Trim().Length == 0 ? null : txtMensaje.Text.Trim())
            };
            _bll.Guardar(r);
            return true;
        }

        protected override void EliminarSeleccion()
        {
            var t = GestorIdioma06AV.Instancia;
            if (!(Grilla.CurrentRow?.DataBoundItem is FilaRegla f))
            {
                MostrarError(t.Obtener("pcf_seleccione_registro"));
                return;
            }
            if (f.Regla.Sistema)
            {
                MostrarError(t.Obtener("cmp_no_eliminar_sistema"));
                return;
            }
            if (!Confirmar(t.Obtener("cmp_confirmar_eliminar", f.Nombre), t.Obtener("eliminar"))) return;
            try { _bll.Eliminar(f.Regla); RecargarGrilla(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        protected override void AplicarIdiomaCampos()
        {
            var t = GestorIdioma06AV.Instancia;
            lblNombre.Text = t.Obtener("cmp_lbl_nombre");
            chkActiva.Text = t.Obtener("cmp_lbl_activa");
            lblNivel.Text = t.Obtener("cmp_lbl_nivel");
            lblPiezaA.Text = t.Obtener("cmp_lbl_pieza_a");
            lblDatoA.Text = t.Obtener("cmp_lbl_dato");
            lblOperador.Text = t.Obtener("cmp_lbl_operador");
            lblPiezaB.Text = t.Obtener("cmp_lbl_pieza_b");
            lblDatoB.Text = t.Obtener("cmp_lbl_dato");
            lblMensaje.Text = t.Obtener("cmp_lbl_mensaje");
            lblAyudaMensaje.Text = t.Obtener("cmp_ayuda_mensaje");
            lblVistaTit.Text = t.Obtener("cmp_lbl_vista");
            lblAviso.BackColor = Tema.PrimarioSuave;
            lblAviso.ForeColor = Tema.Texto;
            lblAyudaMensaje.ForeColor = Tema.TextoSuave;
            lblAyudaMensaje.Font = Tema.FuenteMini;
            lblVista.ForeColor = Tema.Primario;
            if (cboNivel.Items.Count > 0) LlenarCombos();
        }
    }
}
