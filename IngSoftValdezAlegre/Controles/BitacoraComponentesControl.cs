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
    /// <summary>
    /// Bitácora de Cambios de Componentes: histórico de versiones que arman los
    /// triggers de la base (Componentes_C). La pantalla solo LEE el histórico;
    /// el botón Activar restaura una versión actualizando el componente, y es el
    /// trigger de UPDATE el que vuelve a versionar. Nunca se escribe Componentes_C.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public partial class BitacoraComponentesControl : UserControl, IIdiomaAplicable06AV
    {
        private readonly ComponentesBLL06AV _bll = new ComponentesBLL06AV();
        private List<ComponenteHistorico06AV> _versiones = new List<ComponenteHistorico06AV>();

        private Label lblTitulo, lblCodigo, lblDescripcion, lblFechaIni, lblFechaFin, lblCantidad;
        private TextBox txtCodigo, txtDescripcion;
        private DateTimePicker dtpFechaIni, dtpFechaFin;
        private Button btnAplicar, btnLimpiar, btnActivar, btnSalir;
        private DataGridView grilla;

        public BitacoraComponentesControl()
        {
            ConstruirUI();
            ConfigurarColumnas();
            AplicarTema();
            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            Tema.TemaChanged += AplicarTema;
            Disposed += (s, e) => Tema.TemaChanged -= AplicarTema;

            Cargar();
        }

        // ── Construcción de la interfaz ──────────────────────────────

        private void ConstruirUI()
        {
            lblTitulo = new Label { AutoSize = true, Location = new Point(4, 16) };

            var barraTitulo = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraTitulo.Controls.Add(lblTitulo);

            lblCodigo      = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 0), Margin = new Padding(0, 0, 4, 0) };
            lblDescripcion = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 0), Margin = new Padding(12, 0, 4, 0) };
            lblFechaIni    = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 0), Margin = new Padding(12, 0, 4, 0) };
            lblFechaFin    = new Label { AutoSize = true, Padding = new Padding(0, 8, 0, 0), Margin = new Padding(12, 0, 4, 0) };

            txtCodigo      = new TextBox { Width = 120 };
            txtDescripcion = new TextBox { Width = 200 };

            // ShowCheckBox: destildado = el filtro de fecha no se aplica.
            dtpFechaIni = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Width = 120,
                ShowCheckBox = true, Checked = false
            };
            dtpFechaFin = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Width = 120,
                ShowCheckBox = true, Checked = false
            };

            btnAplicar = NuevoBoton();
            btnLimpiar = NuevoBoton();
            btnAplicar.Click += (s, e) => Cargar();
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            txtCodigo.KeyDown      += FiltroEnter;
            txtDescripcion.KeyDown += FiltroEnter;

            var flpFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = false, Padding = new Padding(6, 10, 6, 6)
            };
            flpFiltros.Controls.AddRange(new Control[]
            {
                lblCodigo, txtCodigo, lblDescripcion, txtDescripcion,
                lblFechaIni, dtpFechaIni, lblFechaFin, dtpFechaFin
            });

            // Los botones van anclados a la derecha, fuera del flow de los filtros:
            // si quedaran dentro se envuelven a una segunda fila y la grilla los tapa.
            var flpFiltroBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 10, 10, 0)
            };
            flpFiltroBotones.Controls.AddRange(new Control[] { btnAplicar, btnLimpiar });

            var barraFiltros = new Panel { Dock = DockStyle.Top, Height = 56 };
            barraFiltros.Controls.Add(flpFiltros);
            barraFiltros.Controls.Add(flpFiltroBotones);

            grilla = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BorderStyle = BorderStyle.None
            };
            grilla.SelectionChanged += (s, e) => ActualizarBotones();

            lblCantidad = new Label { AutoSize = true, Padding = new Padding(0, 10, 0, 0) };

            btnActivar = NuevoBoton(120);
            btnSalir   = NuevoBoton(120);
            btnActivar.Click += (s, e) => ActivarSeleccion();
            btnSalir.Click   += (s, e) => Salir();

            var flpAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 12, 10, 0)
            };
            flpAcciones.Controls.AddRange(new Control[] { btnSalir, btnActivar });

            var barraAcciones = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            barraAcciones.Controls.Add(flpAcciones);
            barraAcciones.Controls.Add(lblCantidad);
            lblCantidad.Location = new Point(8, 16);

            Controls.Add(grilla);
            Controls.Add(barraAcciones);
            Controls.Add(barraFiltros);
            Controls.Add(barraTitulo);
        }

        private static Button NuevoBoton(int width = 104) =>
            new Button
            {
                Width = width, Height = 32, FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0)
            };

        private void FiltroEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Cargar();
        }

        private void ConfigurarColumnas()
        {
            grilla.AutoGenerateColumns = false;
            grilla.Columns.Clear();
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "CodigoComponente", DataPropertyName = "CodigoComponente", FillWeight = 85 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Fecha", DataPropertyName = "Fecha", FillWeight = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hora", DataPropertyName = "Hora", FillWeight = 70 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", DataPropertyName = "Descripcion", FillWeight = 190 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", DataPropertyName = "Tipo", FillWeight = 100 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Marca", DataPropertyName = "Marca", FillWeight = 90 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modelo", DataPropertyName = "Modelo", FillWeight = 90 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PrecioUnitario", DataPropertyName = "PrecioUnitario", FillWeight = 80,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Stock", DataPropertyName = "Stock", FillWeight = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            grilla.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StockMinimo", DataPropertyName = "StockMinimo", FillWeight = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            grilla.Columns.Add(new DataGridViewCheckBoxColumn { Name = "BajaLogica", DataPropertyName = "BajaLogica", FillWeight = 80 });
            grilla.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Activo", DataPropertyName = "Activo", FillWeight = 70 });
        }

        // ── Datos ────────────────────────────────────────────────────

        private void Cargar()
        {
            try
            {
                _versiones = _bll.ObtenerBitacora(
                    txtCodigo.Text,
                    txtDescripcion.Text,
                    dtpFechaIni.Checked ? dtpFechaIni.Value.Date : (DateTime?)null,
                    dtpFechaFin.Checked ? dtpFechaFin.Value.Date : (DateTime?)null)
                    ?? new List<ComponenteHistorico06AV>();

                grilla.DataSource = null;
                grilla.DataSource = _versiones;
                PintarVigentes();
                ActualizarContador();
            }
            catch (Exception ex)
            {
                MostrarAviso(ex.Message);
            }
            ActualizarBotones();
        }

        private void LimpiarFiltros()
        {
            txtCodigo.Clear();
            txtDescripcion.Clear();
            dtpFechaIni.Checked = false;
            dtpFechaFin.Checked = false;
            Cargar();
        }

        private void PintarVigentes()
        {
            foreach (DataGridViewRow fila in grilla.Rows)
            {
                if (!(fila.DataBoundItem is ComponenteHistorico06AV v)) continue;
                if (v.Activo)
                {
                    fila.DefaultCellStyle.ForeColor = Tema.Exito;
                    fila.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                }
                else if (v.BajaLogica)
                {
                    fila.DefaultCellStyle.ForeColor = Tema.TextoSuave;
                }
            }
        }

        private ComponenteHistorico06AV VersionSeleccionada =>
            grilla.CurrentRow?.DataBoundItem as ComponenteHistorico06AV;

        private void ActivarSeleccion()
        {
            var t = GestorIdioma06AV.Instancia;
            var version = VersionSeleccionada;

            if (version == null) { MostrarAviso(t.Obtener("pcf_seleccione_registro")); return; }
            if (version.Activo)  { MostrarAviso(t.Obtener("pcf_bitacora_ya_vigente")); return; }

            string mensaje = string.Format(t.Obtener("pcf_bitacora_confirmar_activar"),
                                           version.CodigoComponente,
                                           version.FechaHora.ToString("dd/MM/yyyy HH:mm:ss"));

            bool ok = ConfirmacionForm.Mostrar(
                mensaje, t.Obtener("pcf_bitacora_activar"),
                ConfirmacionForm.TipoConfirmacion.Pregunta,
                t.Obtener("aceptar"), t.Obtener("cancelar"), FindForm());
            if (!ok) return;

            try
            {
                _bll.ActivarHistorico(version);
                Cargar();
                MostrarAviso(t.Obtener("pcf_bitacora_activada"), ConfirmacionForm.TipoConfirmacion.Info);
            }
            catch (Exception ex)
            {
                MostrarAviso(ex.Message);
            }
        }

        private void Salir()
        {
            var main = FindForm() as FRMMain;
            if (main != null) main.MostrarControl(new ComponentesControl());
        }

        private void ActualizarBotones()
        {
            var version = VersionSeleccionada;
            btnActivar.Enabled = version != null && !version.Activo;
        }

        private void ActualizarContador()
        {
            lblCantidad.Text = GestorIdioma06AV.Instancia.Obtener("pcf_bitacora_versiones")
                               + " " + _versiones.Count;
        }

        private void MostrarAviso(string mensaje,
            ConfirmacionForm.TipoConfirmacion tipo = ConfirmacionForm.TipoConfirmacion.Advertencia) =>
            ConfirmacionForm.MostrarInfo(mensaje, GestorIdioma06AV.Instancia.Obtener("aviso"), tipo, FindForm());

        // ── Tema e idioma ────────────────────────────────────────────

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);
            Tema.AplicarBotonPrimario(btnAplicar);
            Tema.AplicarBotonSecundario(btnLimpiar);
            Tema.AplicarBotonAcento(btnActivar);
            Tema.AplicarBotonSecundario(btnSalir);
            Tema.AplicarEntrada(txtCodigo);
            Tema.AplicarEntrada(txtDescripcion);
            Tema.AplicarEntrada(dtpFechaIni);
            Tema.AplicarEntrada(dtpFechaFin);

            lblCantidad.ForeColor = Tema.TextoSuave;
            foreach (Control hijo in Controls)
                if (hijo is Panel) hijo.BackColor = Tema.FondoApp;

            if (grilla.DataSource != null) PintarVigentes();
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            lblTitulo.Text      = t.Obtener("pcf_bitacora_comp_titulo");
            lblCodigo.Text      = t.Obtener("codigo") + ":";
            lblDescripcion.Text = t.Obtener("descripcion") + ":";
            lblFechaIni.Text    = t.Obtener("fecha_desde") + ":";
            lblFechaFin.Text    = t.Obtener("fecha_hasta") + ":";
            btnAplicar.Text     = t.Obtener("filtrar");
            btnLimpiar.Text     = t.Obtener("limpiar");
            btnActivar.Text     = t.Obtener("pcf_bitacora_activar");
            btnSalir.Text       = t.Obtener("volver");

            void H(string col, string clave)
            {
                if (grilla.Columns[col] != null) grilla.Columns[col].HeaderText = t.Obtener(clave);
            }
            H("CodigoComponente", "pcf_col_codigo");
            H("Fecha", "pcf_bitacora_col_fecha");
            H("Hora", "pcf_bitacora_col_hora");
            H("Descripcion", "pcf_col_descripcion");
            H("Tipo", "tipo");
            H("Marca", "marca");
            H("Modelo", "modelo");
            H("PrecioUnitario", "precio");
            H("Stock", "pcf_col_stock");
            H("StockMinimo", "pcf_bitacora_col_minimo");
            H("BajaLogica", "pcf_bitacora_col_baja");
            H("Activo", "pcf_bitacora_col_vigente");

            ActualizarContador();
        }
    }
}
