using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using SER.Generador;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class RolesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly RolesBLL06AV _rolesSer = new RolesBLL06AV();
        private readonly FamiliasBLL06AV _familiasSer = new FamiliasBLL06AV();
        private readonly PatentesBLL06AV _patentesSer = new PatentesBLL06AV();

        private List<Rol06AV> _roles = new List<Rol06AV>();
        private Rol06AV _rolPendiente;
        private bool _creando;
        private bool _suspenderSeleccion;   // ← bloquea MostrarSeleccion durante recargas

        public RolesControl()
        {
            InitializeComponent();
            ConectarEventos();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            CargarDatos();

            if (_roles.Count > 0)
            {
                _suspenderSeleccion = true;
                grilla.Rows[0].Selected = true;
                grilla.CurrentCell = grilla.Rows[0].Cells[0];
                _suspenderSeleccion = false;
                MostrarSeleccion();
            }
        }

        private void ConectarEventos()
        {
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();
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
                        ? GestorIdioma06AV.Instancia.Obtener("nuevo_rol")
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

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("titulo_roles");
            lblDescripcion.Text = t.Obtener("descripcion");
            btnNuevo.Text = t.Obtener("nuevo");
            btnGuardar.Text = t.Obtener("guardar");
            btnEliminar.Text = t.Obtener("eliminar");
            lblPatentes.Text = t.Obtener("patentes_disponibles");
            btnAgregarPatente.Text = t.Obtener("agregar_patente");
            lblFamilias.Text = t.Obtener("familias_disponibles");
            btnAgregarFamilia.Text = t.Obtener("agregar_familia");
            btnQuitar.Text = t.Obtener("quitar_seleccionado");
            txtMensaje.Text = t.Obtener("ayuda_roles");

            if (grilla.Columns["Id"] != null) grilla.Columns["Id"].HeaderText = "Id";
            if (grilla.Columns["Descripcion"] != null) grilla.Columns["Descripcion"].HeaderText = t.Obtener("descripcion");
            if (grilla.Columns["Codigo"] != null) grilla.Columns["Codigo"].HeaderText = t.Obtener("codigo");
            if (grilla.Columns["Hijos"] != null) grilla.Columns["Hijos"].Visible = false;
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

        // ── Carga de datos ───────────────────────────────────────────────────

        private void CargarDatos(string idSeleccionar = null)
        {
            _suspenderSeleccion = true;
            try
            {
                _roles = _rolesSer.ObtenerTodos() ?? new List<Rol06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _roles;

                cmbPatentes.DataSource = _patentesSer.ObtenerTodos();
                cmbFamilias.DataSource = _familiasSer.ObtenerTodos();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
            finally
            {
                _suspenderSeleccion = false;
            }

            // Reposicionar DESPUÉS de levantar el flag
            if (idSeleccionar != null)
                SeleccionarPorId(idSeleccionar);
            else if (_roles.Count > 0)
            {
                grilla.Rows[0].Selected = true;
                grilla.CurrentCell = grilla.Rows[0].Cells[0];
                MostrarSeleccion();
            }
        }

        // ── Selección ────────────────────────────────────────────────────────

        private void MostrarSeleccion()
        {
            if (_suspenderSeleccion) return;
            if (_creando) return;

            Rol06AV rol = ObtenerRolSeleccionado();
            if (rol == null) { LimpiarEditor(); return; }

            txtDescripcion.Text = rol.Descripcion;
            DibujarArbol(rol);
        }

        private Rol06AV ObtenerRolSeleccionado()
        {
            if (grilla.CurrentRow == null) return null;
            return grilla.CurrentRow.DataBoundItem as Rol06AV;
        }

        private void SeleccionarPorId(string id)
        {
            _suspenderSeleccion = true;
            try
            {
                foreach (DataGridViewRow row in grilla.Rows)
                {
                    Rol06AV r = row.DataBoundItem as Rol06AV;
                    if (r != null && r.Id == id)
                    {
                        grilla.ClearSelection();
                        row.Selected = true;
                        grilla.CurrentCell = row.Cells[0];
                        break;
                    }
                }
            }
            finally
            {
                _suspenderSeleccion = false;
            }

            // Una sola llamada controlada al final
            MostrarSeleccion();
        }

        // ── ABM ──────────────────────────────────────────────────────────────

        private void Nuevo()
        {
            _creando = true;
            _rolPendiente = new Rol06AV
            {
                Id = "",
                Descripcion = GestorIdioma06AV.Instancia.Obtener("nuevo_rol")
            };

            _suspenderSeleccion = true;
            grilla.ClearSelection();
            _suspenderSeleccion = false;

            LimpiarEditor();
            DibujarArbol(_rolPendiente);
        }

        private void Guardar()
        {
            var t = GestorIdioma06AV.Instancia;

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MostrarError(t.Obtener("val_campo_vacio", t.Obtener("descripcion")));
                return;
            }

            if (_creando && (_rolPendiente == null || !_rolPendiente.Hijos.Any()))
            {
                MostrarError(t.Obtener("rol_sin_hijos"));
                return;
            }

            try
            {
                bool creando = _creando;
                string idFinal;

                if (_creando)
                {
                    Rol06AV nuevo = new Rol06AV
                    {
                        Id = new GeneradorID().GenerarId().Trim(),
                        Descripcion = txtDescripcion.Text.Trim()
                    };
                    _rolesSer.Agregar(nuevo);
                    PersistirHijosPendientes(nuevo.Id);
                    idFinal = nuevo.Id;
                }
                else
                {
                    Rol06AV seleccionado = ObtenerRolSeleccionado();
                    if (seleccionado == null) return;
                    seleccionado.Descripcion = txtDescripcion.Text.Trim();
                    _rolesSer.Modificar(seleccionado);
                    idFinal = seleccionado.Id;
                }

                _creando = false;
                _rolPendiente = null;

                CargarDatos(idFinal);

                MostrarExito(creando
                    ? t.Obtener("rol_creado")
                    : t.Obtener("rol_modificado"));
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

            var t = GestorIdioma06AV.Instancia;
            bool ok = ConfirmacionForm.Mostrar(
                t.Obtener("confirmar_eliminar_rol"),
                t.Obtener("eliminar_rol"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                t.Obtener("eliminar"),
                t.Obtener("cancelar"),
                FindForm());
            if (!ok) return;

            try
            {
                _rolesSer.Eliminar(rol.Id);
                CargarDatos();
                MostrarExito(t.Obtener("rol_eliminado"));
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        // ── Agregar / Quitar hijos ───────────────────────────────────────────

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
            EjecutarAccion(
                () => _rolesSer.AgregarPatente(rol.Id, cmbPatentes.SelectedValue.ToString()),
                rol.Id);
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
            EjecutarAccion(
                () => _rolesSer.AgregarFamilia(rol.Id, cmbFamilias.SelectedValue.ToString()),
                rol.Id);
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
                    MostrarError(GestorIdioma06AV.Instancia.Obtener("solo_hijos_directos_rol"));
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
                MostrarError(GestorIdioma06AV.Instancia.Obtener("solo_hijos_directos_rol"));
        }

        // ── Pendientes (modo Nuevo) ──────────────────────────────────────────

        private void AgregarPendiente(IComponentePermiso06AV componente)
        {
            try
            {
                if (_rolPendiente == null)
                    _rolPendiente = new Rol06AV
                    {
                        Id = new GeneradorID().GenerarId().Trim(),
                        Descripcion = txtDescripcion.Text.Trim()
                    };

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
                if (hijo is Patente06AV patente)
                {
                    _rolesSer.AgregarPatente(idRol, patente.Id);
                    continue;
                }
                if (hijo is Familia06AV familia)
                    _rolesSer.AgregarFamilia(idRol, familia.Id);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void EjecutarAccion(Action accion, string idRol)
        {
            try
            {
                accion();
                CargarDatos(idRol);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void LimpiarEditor()
        {
            txtDescripcion.Text = "";
            arbol.Nodes.Clear();
        }

        private void DibujarArbol(Rol06AV rol)
        {
            arbol.BeginUpdate();
            arbol.Nodes.Clear();
            TreeNode raiz = new TreeNode(rol.Descripcion)
            { Tag = new NodoPermiso("Rol", rol.Id) };
            foreach (IComponentePermiso06AV hijo in rol.Hijos)
                raiz.Nodes.Add(CrearNodo(hijo));
            arbol.Nodes.Add(raiz);
            raiz.ExpandAll();
            arbol.EndUpdate();
        }

        private TreeNode CrearNodo(IComponentePermiso06AV componente)
        {
            if (componente is Patente06AV patente)
                return new TreeNode("P - " + patente.Descripcion)
                { Tag = new NodoPermiso("Patente", patente.Id) };

            Familia06AV familia = (Familia06AV)componente;
            TreeNode nodo = new TreeNode("F - " + familia.Descripcion)
            { Tag = new NodoPermiso("Familia", familia.Id) };
            foreach (IComponentePermiso06AV hijo in familia.Hijos)
                nodo.Nodes.Add(CrearNodo(hijo));
            return nodo;
        }

        private void MostrarExito(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                GestorIdioma06AV.Instancia.Obtener("exito"),
                ConfirmacionForm.TipoConfirmacion.Info,
                FindForm());
        }

        private void MostrarError(string mensaje)
        {
            ConfirmacionForm.MostrarInfo(
                mensaje,
                GestorIdioma06AV.Instancia.Obtener("aviso"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                FindForm());
        }

        private sealed class NodoPermiso
        {
            public NodoPermiso(string tipo, string id) { Tipo = tipo; Id = id; }
            public string Tipo { get; private set; }
            public string Id { get; private set; }
        }
    }
}   