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
    public partial class FamiliasControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly FamiliasBLL06AV _familiasSer = new FamiliasBLL06AV();
        private readonly PatentesBLL06AV _patentesSer = new PatentesBLL06AV();

        private List<Familia06AV> _familias = new List<Familia06AV>();
        private Familia06AV _familiaPendiente;
        private bool _creando;

        public FamiliasControl()
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
        }

        private void ConectarEventos()
        {
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();
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
                    var t = GestorIdioma06AV.Instancia;
                    _familiaPendiente.Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text)
                        ? t.Obtener("nueva_familia")
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

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            lblTitulo.Text = t.Obtener("titulo_familias");
            lblDescripcion.Text = t.Obtener("descripcion");
            btnNuevo.Text = t.Obtener("nueva");
            btnGuardar.Text = t.Obtener("guardar");
            btnEliminar.Text = t.Obtener("eliminar");
            lblPatentes.Text = t.Obtener("patentes_disponibles");
            btnAgregarPatente.Text = t.Obtener("agregar_patente");
            lblSubfamilias.Text = t.Obtener("subfamilias_disponibles");
            btnAgregarSubfamilia.Text = t.Obtener("agregar_subfamilia");
            btnQuitar.Text = t.Obtener("quitar_seleccionado");
            txtMensaje.Text = t.Obtener("ayuda_familias");
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
            _familiaPendiente = new Familia06AV
            {
                Id = "",
                Descripcion = GestorIdioma06AV.Instancia.Obtener("nueva_familia")
            };
            grilla.ClearSelection();
            LimpiarEditor();
            CargarComboSubfamilias(null);
            DibujarArbol(_familiaPendiente);
        }

        private void Guardar()
        {
            try
            {
                GeneradorID generadorId = new GeneradorID();
                Familia06AV familia = new Familia06AV
                {
                    Id = generadorId.GenerarId(),
                    Descripcion = txtDescripcion.Text.Trim()
                };

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

            var t = GestorIdioma06AV.Instancia;
            bool ok = ConfirmacionForm.Mostrar(
                t.Obtener("confirmar_eliminar_familia"),
                t.Obtener("eliminar_familia"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                t.Obtener("eliminar"),
                t.Obtener("cancelar"),
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
                    MostrarError(GestorIdioma06AV.Instancia.Obtener("solo_hijos_directos_familia"));
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
                MostrarError(GestorIdioma06AV.Instancia.Obtener("solo_hijos_directos_familia"));
        }

        private void AgregarPendiente(IComponentePermiso06AV componente)
        {
            try
            {
                if (_familiaPendiente == null)
                    _familiaPendiente = new Familia06AV
                    {
                        Id = new GeneradorID().GenerarId(),
                        Descripcion = txtDescripcion.Text.Trim()
                    };

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

        private void grilla_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}