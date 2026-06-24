using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class PatentesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly PatentesBLL06AV _patentesSer = new PatentesBLL06AV();

        private List<Patente06AV> _patentes = new List<Patente06AV>();
        private bool _creando;
        private bool _suspenderSeleccion;

        public PatentesControl()
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
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnNuevo);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonPeligro(btnEliminar);

            txtMensaje.BackColor = Tema.Acero50;
            txtMensaje.ForeColor = Tema.Texto;
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text = t.Obtener("titulo_patentes");
            lblId.Text = t.Obtener("id");
            lblDescripcion.Text = t.Obtener("descripcion");
            btnNuevo.Text = t.Obtener("nuevo");
            btnGuardar.Text = t.Obtener("guardar");
            btnEliminar.Text = t.Obtener("eliminar");
            txtMensaje.Text = t.Obtener("ayuda_patentes");

            if (grilla.Columns["Id"] != null) grilla.Columns["Id"].HeaderText = t.Obtener("id");
            if (grilla.Columns["Descripcion"] != null) grilla.Columns["Descripcion"].HeaderText = t.Obtener("descripcion");
        }

        private void AjustarLayout()
        {
            int margen = 8;
            int ancho = Math.Max(560, ClientSize.Width);
            int alto = Math.Max(420, ClientSize.Height);
            int izquierdaW = Math.Max(240, ancho / 3);
            int derechaX = margen + izquierdaW + 16;
            int derechaW = ancho - derechaX - margen;

            lblTitulo.SetBounds(margen, 0, 240, 34);
            grilla.SetBounds(margen, 42, izquierdaW, alto - 50);

            int y = 42;
            lblId.SetBounds(derechaX, y + 4, 86, 22);
            txtId.SetBounds(derechaX + 92, y, derechaW - 92, 25);

            y += 38;
            lblDescripcion.SetBounds(derechaX, y + 4, 86, 22);
            txtDescripcion.SetBounds(derechaX + 92, y, derechaW - 92, 25);

            y += 42;
            btnNuevo.SetBounds(derechaX, y, 72, 30);
            btnGuardar.SetBounds(derechaX + 78, y, 72, 30);
            btnEliminar.SetBounds(derechaX + 156, y, 76, 30);

            y += 48;
            txtMensaje.SetBounds(derechaX, y, derechaW, Math.Max(90, alto - y - 8));
        }

        // ── Carga de datos ───────────────────────────────────────────────────

        private void CargarDatos(string idSeleccionar = null)
        {
            _suspenderSeleccion = true;
            try
            {
                _patentes = _patentesSer.ObtenerTodos() ?? new List<Patente06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _patentes;
                AplicarIdioma();
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
            finally
            {
                _suspenderSeleccion = false;
            }

            if (idSeleccionar != null)
                SeleccionarPorId(idSeleccionar);
            else if (_patentes.Count > 0)
            {
                grilla.Rows[0].Selected = true;
                grilla.CurrentCell = grilla.Rows[0].Cells[0];
                MostrarSeleccion();
            }
            else
            {
                LimpiarEditor();
            }
        }

        // ── Selección ────────────────────────────────────────────────────────

        private void MostrarSeleccion()
        {
            if (_suspenderSeleccion) return;
            if (_creando) return;

            Patente06AV patente = ObtenerPatenteSeleccionada();
            if (patente == null)
            {
                LimpiarEditor();
                return;
            }

            txtId.Text = patente.Id;
            txtId.ReadOnly = true;
            txtDescripcion.Text = patente.Descripcion;
        }

        private Patente06AV ObtenerPatenteSeleccionada()
        {
            if (grilla.CurrentRow == null) return null;
            return grilla.CurrentRow.DataBoundItem as Patente06AV;
        }

        private void SeleccionarPorId(string id)
        {
            _suspenderSeleccion = true;
            try
            {
                foreach (DataGridViewRow row in grilla.Rows)
                {
                    Patente06AV p = row.DataBoundItem as Patente06AV;
                    if (p != null && p.Id == id)
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

            MostrarSeleccion();
        }

        // ── ABM ──────────────────────────────────────────────────────────────

        private void Nuevo()
        {
            _creando = true;

            _suspenderSeleccion = true;
            grilla.ClearSelection();
            _suspenderSeleccion = false;

            txtId.Text = "";
            txtId.ReadOnly = false;
            txtDescripcion.Text = "";
            txtId.Focus();
        }

        private void Guardar()
        {
            var t = GestorIdioma06AV.Instancia;

            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MostrarError(t.Obtener("val_campo_vacio", t.Obtener("id")));
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MostrarError(t.Obtener("val_campo_vacio", t.Obtener("descripcion")));
                return;
            }

            try
            {
                bool creando = _creando;
                string idFinal = txtId.Text.Trim();

                if (_creando)
                {
                    Patente06AV nueva = new Patente06AV
                    {
                        Id = idFinal,
                        Descripcion = txtDescripcion.Text.Trim()
                    };
                    _patentesSer.Agregar(nueva);
                }
                else
                {
                    Patente06AV seleccionada = ObtenerPatenteSeleccionada();
                    if (seleccionada == null) return;
                    idFinal = seleccionada.Id;
                    seleccionada.Descripcion = txtDescripcion.Text.Trim();
                    _patentesSer.Modificar(seleccionada);
                }

                _creando = false;
                CargarDatos(idFinal);

                MostrarExito(creando
                    ? t.Obtener("patente_creada")
                    : t.Obtener("patente_modificada"));
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void Eliminar()
        {
            Patente06AV patente = ObtenerPatenteSeleccionada();
            if (patente == null) return;

            var t = GestorIdioma06AV.Instancia;
            bool ok = ConfirmacionForm.Mostrar(
                t.Obtener("confirmar_eliminar_patente"),
                t.Obtener("eliminar_patente"),
                ConfirmacionForm.TipoConfirmacion.Advertencia,
                t.Obtener("eliminar"),
                t.Obtener("cancelar"),
                FindForm());
            if (!ok) return;

            try
            {
                _patentesSer.Eliminar(patente.Id);
                CargarDatos();
                MostrarExito(t.Obtener("patente_eliminada"));
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void LimpiarEditor()
        {
            txtId.Text = "";
            txtId.ReadOnly = false;
            txtDescripcion.Text = "";
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
    }
}
