using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class FamiliasControl : UserControl
    {
        private readonly FamiliasBLL06AV _familiasSer = new FamiliasBLL06AV();
        private readonly PatentesBLL06AV _patentesSer = new PatentesBLL06AV();

        private readonly Label lblTitulo = new Label();
        private readonly DataGridView grilla = new DataGridView();
        private readonly Label lblId = new Label();
        private readonly Label lblDescripcion = new Label();
        private readonly TextBox txtId = new TextBox();
        private readonly TextBox txtDescripcion = new TextBox();
        private readonly Button btnNuevo = new Button();
        private readonly Button btnGuardar = new Button();
        private readonly Button btnEliminar = new Button();
        private readonly TreeView arbol = new TreeView();
        private readonly ComboBox cmbPatentes = new ComboBox();
        private readonly ComboBox cmbSubfamilias = new ComboBox();
        private readonly Button btnAgregarPatente = new Button();
        private readonly Button btnAgregarSubfamilia = new Button();
        private readonly Button btnQuitar = new Button();
        private readonly Label lblPatentes = new Label();
        private readonly Label lblSubfamilias = new Label();
        private readonly TextBox txtMensaje = new TextBox();

        private List<Familia06AV> _familias = new List<Familia06AV>();
        private Familia06AV _familiaPendiente;
        private bool _creando;

        public FamiliasControl()
        {
            InitializeComponent();
            ConstruirInterfaz();
            AplicarTema();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();
            CargarDatos();
        }

        private void ConstruirInterfaz()
        {
            Controls.Add(lblTitulo);
            Controls.Add(grilla);
            Controls.Add(lblId);
            Controls.Add(txtId);
            Controls.Add(lblDescripcion);
            Controls.Add(txtDescripcion);
            Controls.Add(btnNuevo);
            Controls.Add(btnGuardar);
            Controls.Add(btnEliminar);
            Controls.Add(arbol);
            Controls.Add(lblPatentes);
            Controls.Add(cmbPatentes);
            Controls.Add(btnAgregarPatente);
            Controls.Add(lblSubfamilias);
            Controls.Add(cmbSubfamilias);
            Controls.Add(btnAgregarSubfamilia);
            Controls.Add(btnQuitar);
            Controls.Add(txtMensaje);

            lblTitulo.Text = "Familias";
            lblId.Text = "Id";
            lblDescripcion.Text = "Descripcion";
            lblPatentes.Text = "Patentes disponibles";
            lblSubfamilias.Text = "Familias disponibles";
            btnNuevo.Text = "Nueva";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
            btnAgregarPatente.Text = "Agregar patente";
            btnAgregarSubfamilia.Text = "Agregar familia";
            btnQuitar.Text = "Quitar seleccionado";

            grilla.AutoGenerateColumns = false;
            grilla.AllowUserToAddRows = false;
            grilla.AllowUserToDeleteRows = false;
            grilla.ReadOnly = true;
            grilla.MultiSelect = false;
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", DataPropertyName = "Id", FillWeight = 85 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripcion", DataPropertyName = "Descripcion", FillWeight = 140 });
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();

            cmbPatentes.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSubfamilias.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPatentes.DisplayMember = "Descripcion";
            cmbPatentes.ValueMember = "Id";
            cmbSubfamilias.DisplayMember = "Descripcion";
            cmbSubfamilias.ValueMember = "Id";

            arbol.HideSelection = false;
            arbol.FullRowSelect = true;

            txtMensaje.Multiline = true;
            txtMensaje.ReadOnly = true;
            txtMensaje.BorderStyle = BorderStyle.FixedSingle;
            txtMensaje.Text = "Una familia puede tener patentes y otras familias. La pantalla bloquea ciclos y patentes repetidas en toda la rama.";

            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();
            btnAgregarPatente.Click += (s, e) => AgregarPatente();
            btnAgregarSubfamilia.Click += (s, e) => AgregarSubfamilia();
            btnQuitar.Click += (s, e) => QuitarSeleccionado();
            txtDescripcion.TextChanged += (s, e) =>
            {
                if (_creando && _familiaPendiente != null)
                {
                    _familiaPendiente.Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text)
                        ? "Nueva familia"
                        : txtDescripcion.Text.Trim();
                    DibujarArbol(_familiaPendiente);
                }
            };
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnNuevo);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonPeligro(btnEliminar);
            Tema.AplicarBotonAcento(btnAgregarPatente);
            Tema.AplicarBotonAcento(btnAgregarSubfamilia);
            Tema.AplicarBotonSecundario(btnQuitar);

            arbol.BackColor = Tema.FondoPanel;
            arbol.ForeColor = Tema.Texto;
            arbol.BorderStyle = BorderStyle.FixedSingle;
            arbol.Font = Tema.FuenteRegular;
            txtMensaje.BackColor = Tema.Acero50;
            txtMensaje.ForeColor = Tema.Texto;
        }

        private void AjustarLayout()
        {
            int margen = 8;
            int ancho = Math.Max(760, ClientSize.Width);
            int alto = Math.Max(520, ClientSize.Height);
            int izquierdaW = Math.Max(260, ancho / 3);
            int derechaX = margen + izquierdaW + 16;
            int derechaW = ancho - derechaX - margen;

            lblTitulo.SetBounds(margen, 0, 240, 34);
            grilla.SetBounds(margen, 42, izquierdaW, alto - 50);

            int y = 42;
            lblId.SetBounds(derechaX, y + 4, 80, 22);
            txtId.SetBounds(derechaX + 92, y, Math.Max(180, derechaW - 340), 25);
            btnNuevo.SetBounds(ancho - margen - 236, y - 1, 72, 30);
            btnGuardar.SetBounds(ancho - margen - 156, y - 1, 72, 30);
            btnEliminar.SetBounds(ancho - margen - 76, y - 1, 76, 30);

            y += 34;
            lblDescripcion.SetBounds(derechaX, y + 4, 86, 22);
            txtDescripcion.SetBounds(derechaX + 92, y, derechaW - 92, 25);

            y += 42;
            int accionesW = Math.Min(260, Math.Max(220, derechaW / 3));
            int arbolW = derechaW - accionesW - 14;
            arbol.SetBounds(derechaX, y, arbolW, alto - y - 8);

            int accionesX = derechaX + arbolW + 14;
            lblPatentes.SetBounds(accionesX, y, accionesW, 20);
            cmbPatentes.SetBounds(accionesX, y + 24, accionesW, 25);
            btnAgregarPatente.SetBounds(accionesX, y + 56, accionesW, 32);
            lblSubfamilias.SetBounds(accionesX, y + 104, accionesW, 20);
            cmbSubfamilias.SetBounds(accionesX, y + 128, accionesW, 25);
            btnAgregarSubfamilia.SetBounds(accionesX, y + 160, accionesW, 32);
            btnQuitar.SetBounds(accionesX, y + 210, accionesW, 32);
            txtMensaje.SetBounds(accionesX, y + 258, accionesW, Math.Max(90, alto - y - 266));
        }

        private void CargarDatos()
        {
            try
            {
                _familias = _familiasSer.ObtenerTodos() ?? new List<Familia06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _familias;

                cmbPatentes.DataSource = _patentesSer.ObtenerTodos();
                CargarComboSubfamilias(null);

                if (_familias.Count > 0 && grilla.CurrentRow == null)
                    grilla.Rows[0].Selected = true;

                MostrarSeleccion();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void CargarComboSubfamilias(string idActual)
        {
            var candidatas = _familias
                .Where(f => !string.Equals(f.Id, idActual, StringComparison.OrdinalIgnoreCase))
                .ToList();
            cmbSubfamilias.DataSource = candidatas;
        }

        private void MostrarSeleccion()
        {
            if (_creando) return;
            Familia06AV familia = ObtenerFamiliaSeleccionada();
            if (familia == null)
            {
                LimpiarEditor();
                return;
            }

            txtId.Text = familia.Id;
            txtId.ReadOnly = true;
            txtDescripcion.Text = familia.Descripcion;
            CargarComboSubfamilias(familia.Id);
            DibujarArbol(familia);
        }

        private Familia06AV ObtenerFamiliaSeleccionada()
        {
            if (grilla.CurrentRow == null) return null;
            return grilla.CurrentRow.DataBoundItem as Familia06AV;
        }

        private void Nuevo()
        {
            _creando = true;
            _familiaPendiente = new Familia06AV { Id = "", Descripcion = "Nueva familia" };
            grilla.ClearSelection();
            LimpiarEditor();
            txtId.ReadOnly = false;
            CargarComboSubfamilias(null);
            DibujarArbol(_familiaPendiente);
            txtId.Focus();
        }

        private void Guardar()
        {
            try
            {
                Familia06AV familia = new Familia06AV { Id = txtId.Text.Trim(), Descripcion = txtDescripcion.Text.Trim() };
                if (_creando)
                {
                    _familiasSer.Agregar(familia);
                    PersistirHijosPendientes(familia.Id);
                }
                else
                {
                    _familiasSer.Modificar(familia);
                }

                _creando = false;
                _familiaPendiente = null;
                CargarDatos();
                SeleccionarPorId(familia.Id);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void Eliminar()
        {
            Familia06AV familia = ObtenerFamiliaSeleccionada();
            if (familia == null) return;

            bool ok = ConfirmacionForm.Mostrar(
                "Desea eliminar la familia seleccionada?",
                "Eliminar familia",
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                "Eliminar",
                "Cancelar",
                FindForm());
            if (!ok) return;

            try
            {
                _familiasSer.Eliminar(familia.Id);
                CargarDatos();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void AgregarPatente()
        {
            if (cmbPatentes.SelectedValue == null) return;
            if (_creando)
            {
                Patente06AV patente = cmbPatentes.SelectedItem as Patente06AV;
                if (patente == null) return;
                AgregarPendiente(patente);
                return;
            }

            Familia06AV familia = ObtenerFamiliaSeleccionada();
            if (familia == null) return;
            EjecutarAccion(() => _familiasSer.AgregarPatente(familia.Id, cmbPatentes.SelectedValue.ToString()), familia.Id);
        }

        private void AgregarSubfamilia()
        {
            if (cmbSubfamilias.SelectedValue == null) return;
            if (_creando)
            {
                Familia06AV subfamilia = cmbSubfamilias.SelectedItem as Familia06AV;
                if (subfamilia == null) return;
                AgregarPendiente(subfamilia);
                return;
            }

            Familia06AV familia = ObtenerFamiliaSeleccionada();
            if (familia == null) return;
            EjecutarAccion(() => _familiasSer.AgregarSubfamilia(familia.Id, cmbSubfamilias.SelectedValue.ToString()), familia.Id);
        }

        private void QuitarSeleccionado()
        {
            if (arbol.SelectedNode == null) return;
            NodoPermiso tag = arbol.SelectedNode.Tag as NodoPermiso;
            if (tag == null || arbol.SelectedNode.Parent == null) return;

            if (_creando)
            {
                if (arbol.SelectedNode.Parent.Parent != null)
                {
                    MostrarError("Solo se pueden quitar hijos directos de la familia seleccionada.");
                    return;
                }

                QuitarPendiente(tag);
                return;
            }

            Familia06AV familia = ObtenerFamiliaSeleccionada();
            if (familia == null) return;

            if (tag.Tipo == "Patente" && arbol.SelectedNode.Parent.Parent == null)
                EjecutarAccion(() => _familiasSer.QuitarPatente(familia.Id, tag.Id), familia.Id);
            else if (tag.Tipo == "Familia" && arbol.SelectedNode.Parent.Parent == null)
                EjecutarAccion(() => _familiasSer.QuitarSubfamilia(familia.Id, tag.Id), familia.Id);
            else
                MostrarError("Solo se pueden quitar hijos directos de la familia seleccionada.");
        }

        private void AgregarPendiente(IComponentePermiso06AV componente)
        {
            try
            {
                if (_familiaPendiente == null)
                    _familiaPendiente = new Familia06AV { Id = txtId.Text.Trim(), Descripcion = txtDescripcion.Text.Trim() };

                _familiaPendiente.Agregar(componente);
                DibujarArbol(_familiaPendiente);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void QuitarPendiente(NodoPermiso tag)
        {
            if (_familiaPendiente == null) return;

            IComponentePermiso06AV componente = _familiaPendiente.Hijos
                .FirstOrDefault(h => h.Id == tag.Id &&
                                     ((tag.Tipo == "Patente" && h is Patente06AV) ||
                                      (tag.Tipo == "Familia" && h is Familia06AV)));
            if (componente != null)
            {
                _familiaPendiente.Quitar(componente);
                DibujarArbol(_familiaPendiente);
            }
        }

        private void PersistirHijosPendientes(string idFamilia)
        {
            if (_familiaPendiente == null) return;

            foreach (IComponentePermiso06AV hijo in _familiaPendiente.Hijos)
            {
                Patente06AV patente = hijo as Patente06AV;
                if (patente != null)
                {
                    _familiasSer.AgregarPatente(idFamilia, patente.Id);
                    continue;
                }

                Familia06AV familia = hijo as Familia06AV;
                if (familia != null)
                    _familiasSer.AgregarSubfamilia(idFamilia, familia.Id);
            }
        }

        private void EjecutarAccion(Action accion, string idFamilia)
        {
            try
            {
                accion();
                CargarDatos();
                SeleccionarPorId(idFamilia);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void SeleccionarPorId(string id)
        {
            foreach (DataGridViewRow row in grilla.Rows)
            {
                Familia06AV familia = row.DataBoundItem as Familia06AV;
                if (familia != null && familia.Id == id)
                {
                    row.Selected = true;
                    grilla.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private void LimpiarEditor()
        {
            txtId.Text = "";
            txtDescripcion.Text = "";
            arbol.Nodes.Clear();
        }

        private void DibujarArbol(Familia06AV familia)
        {
            arbol.BeginUpdate();
            arbol.Nodes.Clear();
            TreeNode raiz = new TreeNode(familia.Descripcion) { Tag = new NodoPermiso("Familia", familia.Id) };
            foreach (IComponentePermiso06AV hijo in familia.Hijos)
                raiz.Nodes.Add(CrearNodo(hijo));
            arbol.Nodes.Add(raiz);
            raiz.ExpandAll();
            arbol.EndUpdate();
        }

        private TreeNode CrearNodo(IComponentePermiso06AV componente)
        {
            Patente06AV patente = componente as Patente06AV;
            if (patente != null)
                return new TreeNode("P - " + patente.Descripcion) { Tag = new NodoPermiso("Patente", patente.Id) };

            Familia06AV familia = componente as Familia06AV;
            TreeNode nodo = new TreeNode("F - " + familia.Descripcion) { Tag = new NodoPermiso("Familia", familia.Id) };
            foreach (IComponentePermiso06AV hijo in familia.Hijos)
                nodo.Nodes.Add(CrearNodo(hijo));
            return nodo;
        }

        private void MostrarError(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                "Aviso",
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                FindForm());
        }

        private sealed class NodoPermiso
        {
            public NodoPermiso(string tipo, string id)
            {
                Tipo = tipo;
                Id = id;
            }

            public string Tipo { get; private set; }
            public string Id { get; private set; }
        }
    }
}
