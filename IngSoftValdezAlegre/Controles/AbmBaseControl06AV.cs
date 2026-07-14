using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    /// <summary>
    /// Base de los ABM de PC Factory con el diseño de "grilla + vistas".
    ///
    ///  • Vista GRILLA: la grilla ocupa toda la pantalla, con una barra
    ///    superior que tiene el título y los botones Nuevo / Editar / Eliminar.
    ///  • Vista FORMULARIO: los campos del registro y una barra inferior con
    ///    Guardar / Volver. "Nuevo" y "Editar" abren el formulario; "Volver"
    ///    (o guardar con éxito) regresa a la grilla.
    ///
    /// Las subclases sólo aportan sus campos y el mapeo con la entidad a través
    /// de los métodos abstractos. Todo el andamiaje (paneles, barras, cambio de
    /// vista, idioma, tema) vive acá para que las 5 pantallas queden idénticas.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public abstract partial class AbmBaseControl06AV : UserControl, IIdiomaAplicable06AV
    {
        protected DataGridView Grilla { get; private set; }
        protected TableLayoutPanel TablaCampos { get; private set; }
        protected bool Editando { get; private set; }

        private Panel _pnlGrilla, _pnlForm;
        private Label _lblTitulo, _lblFormTitulo;
        private Button _btnNuevo, _btnEditar, _btnEliminar, _btnGuardar, _btnVolver;

        /// <summary>
        /// La subclase llama a esto al final de su constructor, DESPUÉS de haber
        /// instanciado sus controles de campo (para que ConstruirCampos los use).
        /// </summary>
        protected void InicializarAbm()
        {
            ConstruirChrome();
            ConstruirCampos(TablaCampos);
            AplicarTema();
            AplicarIdioma();
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            // Observer de tema: repinta el ABM (grilla + formulario) al cambiar claro/oscuro.
            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;

            MostrarGrilla();
            RecargarGrilla();
        }

        // ── Hooks de la subclase ─────────────────────────────────────
        protected abstract string ClaveTitulo { get; }
        protected abstract void ConstruirCampos(TableLayoutPanel tabla);
        protected abstract void CargarDatosEnGrilla(DataGridView grilla);
        protected abstract void PrepararNuevo();
        protected abstract bool CargarSeleccionEnCampos();
        protected abstract bool Guardar(bool editando);
        protected abstract void EliminarSeleccion();
        protected abstract void AplicarIdiomaCampos();

        // ── Construcción de la UI ────────────────────────────────────
        private void ConstruirChrome()
        {
            // --- Vista grilla ---
            Grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None
            };
            Grilla.SelectionChanged += (s, e) => ActualizarBotones();
            Grilla.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) IniciarEdicion(); };

            _lblTitulo = new Label { AutoSize = true, Location = new Point(6, 16) };

            _btnNuevo = NuevoBoton();
            _btnEditar = NuevoBoton();
            _btnEliminar = NuevoBoton();
            _btnNuevo.Click += (s, e) => IniciarNuevo();
            _btnEditar.Click += (s, e) => IniciarEdicion();
            _btnEliminar.Click += (s, e) => EliminarSeleccion();

            var barraGrilla = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraGrilla.Controls.Add(_lblTitulo);
            barraGrilla.Controls.Add(BarraBotones(_btnNuevo, _btnEditar, _btnEliminar));

            _pnlGrilla = new Panel { Dock = DockStyle.Fill };
            _pnlGrilla.Controls.Add(Grilla);        // Fill primero (queda en índice 0 → ocupa lo que resta)
            _pnlGrilla.Controls.Add(barraGrilla);   // borde (Top) después

            // --- Vista formulario ---
            _lblFormTitulo = new Label { AutoSize = true, Location = new Point(6, 16) };
            var barraForm = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraForm.Controls.Add(_lblFormTitulo);

            TablaCampos = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Padding = new Padding(8)
            };
            TablaCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            TablaCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var contCampos = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 8),
                AutoScroll = true
            };
            contCampos.Controls.Add(TablaCampos);

            _btnGuardar = NuevoBoton(120);
            _btnVolver = NuevoBoton(120);
            _btnGuardar.Click += (s, e) => GuardarYVolver();
            _btnVolver.Click += (s, e) => MostrarGrilla();

            var barraFormBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraFormBottom.Controls.Add(BarraBotones(_btnVolver, _btnGuardar));

            _pnlForm = new Panel { Dock = DockStyle.Fill, Visible = false };
            _pnlForm.Controls.Add(contCampos);        // Fill primero
            _pnlForm.Controls.Add(barraForm);         // Top
            _pnlForm.Controls.Add(barraFormBottom);   // Bottom

            Controls.Add(_pnlForm);
            Controls.Add(_pnlGrilla);
        }

        private static Button NuevoBoton(int width = 104) =>
            new Button { Width = width, Height = 32, Margin = new Padding(6, 0, 0, 0) };

        private static FlowLayoutPanel BarraBotones(params Button[] botones)
        {
            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(0, 12, 10, 0)
            };
            flp.Controls.AddRange(botones);
            return flp;
        }

        protected void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(_lblTitulo);
            Tema.AplicarSubtitulo(_lblFormTitulo);
            Tema.AplicarGrilla(Grilla);
            Tema.AplicarBotonSecundario(_btnNuevo);
            Tema.AplicarBotonSecundario(_btnEditar);
            Tema.AplicarBotonPeligro(_btnEliminar);
            Tema.AplicarBotonPrimario(_btnGuardar);
            Tema.AplicarBotonSecundario(_btnVolver);

            _pnlGrilla.BackColor = Tema.FondoApp;
            _pnlForm.BackColor = Tema.FondoApp;
            foreach (Control panel in new[] { _pnlGrilla, _pnlForm })
                foreach (Control hijo in panel.Controls)
                    if (hijo is Panel) hijo.BackColor = Tema.FondoApp;
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            _lblTitulo.Text = t.Obtener(ClaveTitulo);
            _btnNuevo.Text = t.Obtener("nuevo");
            _btnEditar.Text = t.Obtener("editar");
            _btnEliminar.Text = t.Obtener("eliminar");
            _btnGuardar.Text = t.Obtener("guardar");
            _btnVolver.Text = t.Obtener("volver");
            _lblFormTitulo.Text = t.Obtener(Editando ? "editar_registro" : "nuevo_registro");
            AplicarIdiomaCampos();
        }

        // ── Navegación entre vistas ──────────────────────────────────
        protected void RecargarGrilla()
        {
            try { CargarDatosEnGrilla(Grilla); }
            catch (Exception ex) { MostrarError(ex.Message); }
            ActualizarBotones();
        }

        private void IniciarNuevo()
        {
            Editando = false;
            PrepararNuevo();
            _lblFormTitulo.Text = GestorIdioma06AV.Instancia.Obtener("nuevo_registro");
            MostrarFormulario();
        }

        private void IniciarEdicion()
        {
            if (Grilla.CurrentRow == null)
            {
                MostrarError(GestorIdioma06AV.Instancia.Obtener("pcf_seleccione_registro"));
                return;
            }
            Editando = true;
            if (!CargarSeleccionEnCampos()) return;
            _lblFormTitulo.Text = GestorIdioma06AV.Instancia.Obtener("editar_registro");
            MostrarFormulario();
        }

        private void GuardarYVolver()
        {
            bool ok;
            try { ok = Guardar(Editando); }
            catch (Exception ex) { MostrarError(ex.Message); return; }
            if (!ok) return;
            RecargarGrilla();
            MostrarGrilla();
        }

        private void MostrarGrilla()
        {
            _pnlForm.Visible = false;
            _pnlGrilla.Visible = true;
            _pnlGrilla.BringToFront();
            ActualizarBotones();
        }

        private void MostrarFormulario()
        {
            _pnlGrilla.Visible = false;
            _pnlForm.Visible = true;
            _pnlForm.BringToFront();
        }

        private void ActualizarBotones()
        {
            bool hay = Grilla != null && Grilla.CurrentRow != null && Grilla.Rows.Count > 0;
            if (_btnEditar != null) _btnEditar.Enabled = hay;
            if (_btnEliminar != null) _btnEliminar.Enabled = hay;
        }

        // ── Helpers para las subclases ───────────────────────────────
        protected void AgregarCampo(Label etiqueta, Control campo)
        {
            int fila = TablaCampos.RowCount;
            TablaCampos.RowCount = fila + 1;
            TablaCampos.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            etiqueta.AutoSize = true;
            etiqueta.Anchor = AnchorStyles.Left;
            etiqueta.Margin = new Padding(3, 9, 6, 3);

            campo.Anchor = AnchorStyles.Left;
            if (campo is TextBox || campo is ComboBox) campo.Width = 360;
            campo.Margin = new Padding(3, 6, 3, 6);
            Tema.AplicarEntrada(campo);

            TablaCampos.Controls.Add(etiqueta, 0, fila);
            TablaCampos.Controls.Add(campo, 1, fila);
        }

        protected void MostrarError(string mensaje) => ConfirmacionForm.MostrarInfo(
            mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"),
            ConfirmacionForm.TipoConfirmacion.Advertencia, FindForm());

        protected bool Confirmar(string mensaje, string titulo) => ConfirmacionForm.Mostrar(
            mensaje, titulo, ConfirmacionForm.TipoConfirmacion.Advertencia,
            GestorIdioma06AV.Instancia.Obtener("eliminar"),
            GestorIdioma06AV.Instancia.Obtener("cancelar"), FindForm());
    }
}
