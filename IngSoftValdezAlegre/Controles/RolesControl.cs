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
    public partial class RolesControl : UserControl
    {
        private readonly RolesBLL06AV _rolesSer = new RolesBLL06AV();
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
        private readonly ComboBox cmbFamilias = new ComboBox();
        private readonly Button btnAgregarPatente = new Button();
        private readonly Button btnAgregarFamilia = new Button();
        private readonly Button btnQuitar = new Button();
        private readonly Label lblPatentes = new Label();
        private readonly Label lblFamilias = new Label();
        private readonly TextBox txtMensaje = new TextBox();

        private List<Rol06AV> _roles = new List<Rol06AV>();
        private Rol06AV _rolPendiente;
        private bool _creando;

        public RolesControl()
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
            Controls.Add(lblFamilias);
            Controls.Add(cmbFamilias);
            Controls.Add(btnAgregarFamilia);
            Controls.Add(btnQuitar);
            Controls.Add(txtMensaje);

            lblTitulo.Text = "Roles";
            lblId.Text = "Id";
            lblDescripcion.Text = "Descripcion";
            lblPatentes.Text = "Patentes disponibles";
            lblFamilias.Text = "Familias disponibles";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
            btnAgregarPatente.Text = "Agregar patente";
            btnAgregarFamilia.Text = "Agregar familia";
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
            cmbFamilias.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPatentes.DisplayMember = "Descripcion";
            cmbPatentes.ValueMember = "Id";
            cmbFamilias.DisplayMember = "Descripcion";
            cmbFamilias.ValueMember = "Id";

            arbol.HideSelection = false;
            arbol.FullRowSelect = true;

            txtMensaje.Multiline = true;
            txtMensaje.ReadOnly = true;
            txtMensaje.BorderStyle = BorderStyle.FixedSingle;
            txtMensaje.Text = "Un rol puede tener patentes directas y familias. No puede contener otros roles. El arbol muestra las patentes efectivas expandidas.";

            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();
            btnAgregarPatente.Click += (s, e) => AgregarPatente();
            btnAgregarFamilia.Click += (s, e) => AgregarFamilia();
            btnQuitar.Click += (s, e) => QuitarSeleccionado();
            txtDescripcion.TextChanged += (s, e) =>
            {
                if (_creando && _rolPendiente != null)
                {
                    _rolPendiente.Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text)
                        ? "Nuevo rol"
                        : txtDescripcion.Text.Trim();
                    DibujarArbol(_rolPendiente);
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
            Tema.AplicarBotonAcento(btnAgregarFamilia);
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
            lblFamilias.SetBounds(accionesX, y + 104, accionesW, 20);
            cmbFamilias.SetBounds(accionesX, y + 128, accionesW, 25);
            btnAgregarFamilia.SetBounds(accionesX, y + 160, accionesW, 32);
            btnQuitar.SetBounds(accionesX, y + 210, accionesW, 32);
            txtMensaje.SetBounds(accionesX, y + 258, accionesW, Math.Max(90, alto - y - 266));
        }

        private void CargarDatos()
        {
            try
            {
                _roles = _rolesSer.ObtenerTodos() ?? new List<Rol06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _roles;

                cmbPatentes.DataSource = _patentesSer.ObtenerTodos();
                cmbFamilias.DataSource = _familiasSer.ObtenerTodos();

                if (_roles.Count > 0 && grilla.CurrentRow == null)
                    grilla.Rows[0].Selected = true;

                MostrarSeleccion();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void MostrarSeleccion()
        {
            if (_creando) return;
            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null)
            {
                LimpiarEditor();
                return;
            }

            txtId.Text = rol.Id;
            txtId.ReadOnly = true;
            txtDescripcion.Text = rol.Descripcion;
            DibujarArbol(rol);
        }

        private Rol06AV ObtenerRolSeleccionado()
        {
            if (grilla.CurrentRow == null) return null;
            return grilla.CurrentRow.DataBoundItem as Rol06AV;
        }

        private void Nuevo()
        {
            _creando = true;
            _rolPendiente = new Rol06AV { Id = "", Descripcion = "Nuevo rol" };
            grilla.ClearSelection();
            LimpiarEditor();
            txtId.ReadOnly = false;
            DibujarArbol(_rolPendiente);
            txtId.Focus();
        }

        private void Guardar()
        {
            try
            {
                Rol06AV rol = new Rol06AV { Id = txtId.Text.Trim(), Descripcion = txtDescripcion.Text.Trim() };
                if (_creando)
                {
                    _rolesSer.Agregar(rol);
                    PersistirHijosPendientes(rol.Id);
                }
                else
                {
                    _rolesSer.Modificar(rol);
                }

                _creando = false;
                _rolPendiente = null;
                CargarDatos();
                SeleccionarPorId(rol.Id);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void Eliminar()
        {
            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null) return;

            bool ok = ConfirmacionForm.Mostrar(
                "Desea eliminar el rol seleccionado?",
                "Eliminar rol",
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                "Eliminar",
                "Cancelar",
                FindForm());
            if (!ok) return;

            try
            {
                _rolesSer.Eliminar(rol.Id);
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

            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null) return;
            EjecutarAccion(() => _rolesSer.AgregarPatente(rol.Id, cmbPatentes.SelectedValue.ToString()), rol.Id);
        }

        private void AgregarFamilia()
        {
            if (cmbFamilias.SelectedValue == null) return;
            if (_creando)
            {
                Familia06AV familia = cmbFamilias.SelectedItem as Familia06AV;
                if (familia == null) return;
                AgregarPendiente(familia);
                return;
            }

            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null) return;
            EjecutarAccion(() => _rolesSer.AgregarFamilia(rol.Id, cmbFamilias.SelectedValue.ToString()), rol.Id);
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
                    MostrarError("Solo se pueden quitar hijos directos del rol desde esta pantalla.");
                    return;
                }

                QuitarPendiente(tag);
                return;
            }

            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null) return;

            if (tag.Tipo == "Patente" && arbol.SelectedNode.Parent.Parent == null)
                EjecutarAccion(() => _rolesSer.QuitarPatente(rol.Id, tag.Id), rol.Id);
            else if (tag.Tipo == "Familia" && arbol.SelectedNode.Parent.Parent == null)
                EjecutarAccion(() => _rolesSer.QuitarFamilia(rol.Id, tag.Id), rol.Id);
            else
                MostrarError("Solo se pueden quitar hijos directos del rol desde esta pantalla.");
        }

        private void AgregarPendiente(IComponentePermiso06AV componente)
        {
            try
            {
                if (_rolPendiente == null)
                    _rolPendiente = new Rol06AV { Id = txtId.Text.Trim(), Descripcion = txtDescripcion.Text.Trim() };

                _rolPendiente.Agregar(componente);
                DibujarArbol(_rolPendiente);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void QuitarPendiente(NodoPermiso tag)
        {
            if (_rolPendiente == null) return;

            IComponentePermiso06AV componente = _rolPendiente.Hijos
                .FirstOrDefault(h => h.Id == tag.Id &&
                                     ((tag.Tipo == "Patente" && h is Patente06AV) ||
                                      (tag.Tipo == "Familia" && h is Familia06AV)));
            if (componente != null)
            {
                _rolPendiente.Quitar(componente);
                DibujarArbol(_rolPendiente);
            }
        }

        private void PersistirHijosPendientes(string idRol)
        {
            if (_rolPendiente == null) return;

            foreach (IComponentePermiso06AV hijo in _rolPendiente.Hijos)
            {
                Patente06AV patente = hijo as Patente06AV;
                if (patente != null)
                {
                    _rolesSer.AgregarPatente(idRol, patente.Id);
                    continue;
                }

                Familia06AV familia = hijo as Familia06AV;
                if (familia != null)
                    _rolesSer.AgregarFamilia(idRol, familia.Id);
            }
        }

        private void EjecutarAccion(Action accion, string idRol)
        {
            try
            {
                accion();
                CargarDatos();
                SeleccionarPorId(idRol);
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
                Rol06AV rol = row.DataBoundItem as Rol06AV;
                if (rol != null && rol.Id == id)
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

        private void DibujarArbol(Rol06AV rol)
        {
            arbol.BeginUpdate();
            arbol.Nodes.Clear();
            TreeNode raiz = new TreeNode(rol.Descripcion) { Tag = new NodoPermiso("Rol", rol.Id) };
            foreach (IComponentePermiso06AV hijo in rol.Hijos)
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
