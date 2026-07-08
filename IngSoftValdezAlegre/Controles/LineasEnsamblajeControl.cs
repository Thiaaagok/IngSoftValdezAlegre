using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>ABM de Líneas de Ensamblaje (PC Factory). Id autonumérico (oculto).</summary>
    public class LineasEnsamblajeControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly LineasEnsamblajeBLL06AV _bll = new LineasEnsamblajeBLL06AV();
        private List<LineaEnsamblaje06AV> _items = new List<LineaEnsamblaje06AV>();
        private int _idEditando;

        private Label lblTitulo;
        private DataGridView grilla;
        private Label lblNombre, lblDesc;
        private TextBox txtNombre, txtDesc;
        private CheckBox chkDisponible;
        private Button btnNuevo, btnGuardar, btnEliminar;

        public LineasEnsamblajeControl()
        {
            ConstruirUI();
            AplicarTema();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
            CargarDatos();
        }

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true };
            grilla = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
            };
            grilla.SelectionChanged += (s, e) => MostrarSeleccion();

            lblNombre = new Label { AutoSize = true };
            lblDesc = new Label { AutoSize = true };
            txtNombre = new TextBox();
            txtDesc = new TextBox();
            chkDisponible = new CheckBox { Text = "Disponible", AutoSize = true, Checked = true };

            btnNuevo = new Button { Text = "Nuevo" };
            btnGuardar = new Button { Text = "Guardar" };
            btnEliminar = new Button { Text = "Eliminar" };
            btnNuevo.Click += (s, e) => Nuevo();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            Controls.AddRange(new Control[]
            {
                lblTitulo, grilla,
                lblNombre, txtNombre, lblDesc, txtDesc, chkDisponible,
                btnNuevo, btnGuardar, btnEliminar
            });
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonSecundario(btnNuevo);
            Tema.AplicarBotonPeligro(btnEliminar);
        }

        public void AplicarIdioma()
        {
            lblTitulo.Text = "Gestión de Líneas de Ensamblaje";
            lblNombre.Text = "Nombre:";
            lblDesc.Text = "Descripción:";
            chkDisponible.Text = "Disponible";
            btnNuevo.Text = "Nuevo";
            btnGuardar.Text = "Guardar";
            btnEliminar.Text = "Eliminar";
        }

        private void AjustarLayout()
        {
            int margen = 12;
            int ancho = Math.Max(720, ClientSize.Width);
            int alto = Math.Max(420, ClientSize.Height);
            lblTitulo.SetBounds(margen, margen, 360, 30);

            int grillaW = (int)(ancho * 0.55);
            grilla.SetBounds(margen, 52, grillaW - margen, alto - 64);

            int fx = grillaW + margen, etiqW = 90, campoX = fx + etiqW;
            int campoW = ancho - campoX - margen, y = 60, paso = 40;
            void Fila(Control l, Control c) { l.SetBounds(fx, y + 3, etiqW, 22); c.SetBounds(campoX, y, campoW, 26); y += paso; }
            Fila(lblNombre, txtNombre);
            Fila(lblDesc, txtDesc);
            chkDisponible.SetBounds(campoX, y, 160, 24); y += paso;

            y += 8;
            btnNuevo.SetBounds(campoX, y, 100, 34);
            btnGuardar.SetBounds(campoX + 108, y, 100, 34);
            btnEliminar.SetBounds(campoX + 216, y, 100, 34);
        }

        private void CargarDatos()
        {
            try
            {
                _items = _bll.ObtenerTodas() ?? new List<LineaEnsamblaje06AV>();
                grilla.DataSource = null;
                grilla.DataSource = _items;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarSeleccion()
        {
            if (grilla.CurrentRow?.DataBoundItem is LineaEnsamblaje06AV l)
            {
                _idEditando = l.Id;
                txtNombre.Text = l.Nombre;
                txtDesc.Text = l.Descripcion;
                chkDisponible.Checked = l.Disponible;
            }
        }

        private void Nuevo()
        {
            _idEditando = 0;
            txtNombre.Clear(); txtDesc.Clear();
            chkDisponible.Checked = true;
            txtNombre.Focus();
        }

        private void Guardar()
        {
            var l = new LineaEnsamblaje06AV
            {
                Id = _idEditando,
                Nombre = txtNombre.Text.Trim(),
                Descripcion = txtDesc.Text.Trim(),
                Disponible = chkDisponible.Checked
            };

            try
            {
                if (_idEditando > 0) _bll.Modificar(l); else _bll.Crear(l);
                CargarDatos(); Nuevo();
                ConfirmacionForm.MostrarInfo("Línea guardada correctamente.",
                    "Líneas de ensamblaje", ConfirmacionForm.TipoConfirmacion.Info, FindForm());
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void Eliminar()
        {
            if (_idEditando <= 0) { MostrarError("Seleccioná una línea de la lista."); return; }
            bool ok = ConfirmacionForm.Mostrar($"¿Eliminar la línea {txtNombre.Text}?",
                "Eliminar línea", ConfirmacionForm.TipoConfirmacion.Advertencia,
                "Eliminar", "Cancelar", FindForm());
            if (!ok) return;
            try { _bll.Eliminar(_idEditando); CargarDatos(); Nuevo(); }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());
    }
}
