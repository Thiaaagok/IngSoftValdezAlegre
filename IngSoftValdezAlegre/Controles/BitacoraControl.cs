using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using SER.Exportacion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class BitacoraControl : UserControl, IIdiomaAplicable
    {
        private readonly BitacoraBLL06AV _bitacoraSer = new BitacoraBLL06AV();
        private readonly UsuariosBLL06AV _usuariosSer = new UsuariosBLL06AV();

        private List<Bitacora06AV> _eventosCargados = new List<Bitacora06AV>();
        private Dictionary<string, Usuario06AV> _usuariosPorDni = new Dictionary<string, Usuario06AV>();

        private const string OPCION_TODOS = "(Todos)";

        public BitacoraControl()
        {
            InitializeComponent();
            AplicarTema();
            ConfigurarColumnas();
            AplicarIdioma();

            // Observer: suscribirse al cambio de idioma
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarGrilla(grilla);

            Tema.AplicarBotonPrimario(lblFiltrar);
            Tema.AplicarBotonSecundario(lblLimpiar);
            Tema.AplicarBotonAcento(lblImprimirPDF);
            Tema.AplicarBotonAcento(lblImprimirEXCEL);

            lblCantidad.ForeColor = Tema.TextoSuave;
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text        = t.Obtener("bitacora_titulo");
            lblFiltrar.Text       = t.Obtener("filtrar");
            lblLimpiar.Text       = t.Obtener("limpiar");
            lblImprimirPDF.Text   = t.Obtener("imprimir_pdf");
            lblImprimirEXCEL.Text = t.Obtener("imprimir_excel");
            lblFechaDesde.Text    = t.Obtener("fecha_desde");
            lblFechaHasta.Text    = t.Obtener("fecha_hasta");
            lblCriticidad.Text    = t.Obtener("criticidad");
            lblEvento.Text        = t.Obtener("evento");
            lblModulo.Text        = t.Obtener("modulo");
            lblDni.Text           = t.Obtener("nombre");
            lblApellido.Text      = t.Obtener("apellido");

            // Headers de la grilla
            if (grilla.Columns["UsuarioDni"]  != null) grilla.Columns["UsuarioDni"].HeaderText  = t.Obtener("dni");
            if (grilla.Columns["Fecha"]       != null) grilla.Columns["Fecha"].HeaderText       = "Fecha";
            if (grilla.Columns["Hora"]        != null) grilla.Columns["Hora"].HeaderText        = "Hora";
            if (grilla.Columns["Modulo"]      != null) grilla.Columns["Modulo"].HeaderText      = t.Obtener("modulo");
            if (grilla.Columns["Descripcion"] != null) grilla.Columns["Descripcion"].HeaderText = t.Obtener("evento");
            if (grilla.Columns["Criticidad"]  != null) grilla.Columns["Criticidad"].HeaderText  = t.Obtener("criticidad");

            // Actualizar conteo si ya hay datos
            if (_eventosCargados != null && _eventosCargados.Count > 0)
                lblCantidad.Text = t.Obtener("numero_eventos") + " " + _eventosCargados.Count;
        }

        private void BitacoraControl2_Load(object sender, EventArgs e)
        {
            CargarUsuarios();
            CargarCombos();
            EstablecerRangoPorDefecto();
            AplicarFiltros();
        }
        // -------------------- Setup --------------------

        private void ConfigurarColumnas()
        {
            grilla.AutoGenerateColumns = false;
            grilla.Columns.Clear();
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "UsuarioDni", HeaderText = "DNI", DataPropertyName = "UsuarioDni", FillWeight = 70 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Fecha", DataPropertyName = "Fecha", FillWeight = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hora", HeaderText = "Hora", DataPropertyName = "Fecha", FillWeight = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "HH:mm:ss" } });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Modulo", HeaderText = "Módulo", DataPropertyName = "Modulo", FillWeight = 100 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Evento", DataPropertyName = "Descripcion", FillWeight = 220 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Criticidad", HeaderText = "Criticidad", DataPropertyName = "Criticidad", FillWeight = 90 });
        }

        private void CargarUsuarios()
        {
            try
            {
                var todos = _usuariosSer.ObtenerTodos() ?? new List<Usuario06AV>();
                _usuariosPorDni = todos
                    .Where(u => !string.IsNullOrEmpty(u.Dni))
                    .GroupBy(u => u.Dni)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            catch
            {
                _usuariosPorDni = new Dictionary<string, Usuario06AV>();
            }
        }

        private void CargarCombos()
        {
            cmbModulo.Items.Clear();
            cmbModulo.Items.Add(OPCION_TODOS);
            foreach (var m in Enum.GetNames(typeof(ModuloBitacora)))
            {
                cmbModulo.Items.Add(m);
            }
            cmbModulo.SelectedIndex = 0;

            cmbEvento.Items.Clear();
            cmbEvento.Items.Add(OPCION_TODOS);
            foreach (var c in Enum.GetNames(typeof(CategoriaBitacora)))
            {
                cmbEvento.Items.Add(c);
            }
            cmbEvento.SelectedIndex = 0;

            cmbCriticidad.Items.Clear();
            cmbCriticidad.Items.Add(OPCION_TODOS);
            foreach (var c in Enum.GetNames(typeof(CriticidadBitacora)))
            {
                cmbCriticidad.Items.Add(c);
            }
            cmbCriticidad.SelectedIndex = 0;
        }

        private void EstablecerRangoPorDefecto()
        {
            dtpFechaIni.Value = DateTime.Now.Date.AddDays(-3);
            dtpFechaFin.Value = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
        }

        private void AplicarFiltros()
        {
            try
            {
                var desde = dtpFechaIni.Value.Date;
                var hasta = dtpFechaFin.Value.Date.AddDays(1).AddSeconds(-1);

                if (desde > hasta)
                {
                    var t = GestorIdioma06AV.Instancia;
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("fecha_inicial_mayor"),
                        titulo: t.Obtener("aviso"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this.FindForm());
                    return;
                }

                var eventos = _bitacoraSer.ObtenerEventosEntreFechas(desde, hasta) ?? new List<Bitacora06AV>();
                IEnumerable<Bitacora06AV> filtrados = eventos;

                if (cmbModulo.SelectedItem != null && cmbModulo.SelectedItem.ToString() != OPCION_TODOS)
                {
                    string mod = cmbModulo.SelectedItem.ToString();
                    filtrados = filtrados.Where(ev => string.Equals(ev.Modulo, mod, StringComparison.OrdinalIgnoreCase));
                }

                if (cmbEvento.SelectedItem != null && cmbEvento.SelectedItem.ToString() != OPCION_TODOS)
                {
                    string cat = cmbEvento.SelectedItem.ToString();
                    filtrados = filtrados.Where(ev => string.Equals(ev.Categoria, cat, StringComparison.OrdinalIgnoreCase));
                }

                if (cmbCriticidad.SelectedItem != null && cmbCriticidad.SelectedItem.ToString() != OPCION_TODOS)
                {
                    string crit = cmbCriticidad.SelectedItem.ToString();
                    filtrados = filtrados.Where(ev => string.Equals(ev.Criticidad, crit, StringComparison.OrdinalIgnoreCase));
                }

                _eventosCargados = filtrados.OrderByDescending(ev => ev.Fecha).ToList();
                grilla.DataSource = null;
                grilla.DataSource = _eventosCargados;
                lblCantidad.Text = GestorIdioma06AV.Instancia.Obtener("numero_eventos")
                                  + " " + _eventosCargados.Count;

                PintarPorCriticidad();
            }
            catch (Exception ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_filtro") + "\n" + ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this.FindForm());
            }
        }

        private void PintarPorCriticidad()
        {
            foreach (DataGridViewRow row in grilla.Rows)
            {
                var ev = row.DataBoundItem as Bitacora06AV;
                if (ev == null) continue;

                switch (ev.Criticidad)
                {
                    case "Critica":
                        row.DefaultCellStyle.BackColor = Tema.PeligroSuave;
                        row.DefaultCellStyle.ForeColor = Tema.Peligro;
                        break;
                    case "Alta":
                        row.DefaultCellStyle.BackColor = Tema.AdvertenciaSuave;
                        row.DefaultCellStyle.ForeColor = Tema.Naranja600;
                        break;
                    case "Media":
                        row.DefaultCellStyle.BackColor = Tema.FondoElevado;
                        row.DefaultCellStyle.ForeColor = Tema.Texto;
                        break;
                    case "Baja":
                    default:
                        row.DefaultCellStyle.BackColor = Tema.FondoElevado;
                        row.DefaultCellStyle.ForeColor = Tema.Texto;
                        break;
                }
            }
        }

        private void lblImprimirPDF_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Bitacora_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var columnas = new Dictionary<string, Func<Bitacora06AV, object>>
            {
                { "DNI",        b => b.UsuarioDni },
                { "Fecha",      b => b.Fecha.ToString("dd/MM/yyyy") },
                { "Hora",       b => b.Fecha.ToString("HH:mm:ss") },
                { "Módulo",     b => b.Modulo },
                { "Evento",     b => b.Descripcion },
                { "Criticidad", b => b.Criticidad }
            };

            var exp = new ExportacionPDF();
            exp.Exportar(_eventosCargados, columnas, sfd.FileName, "Bitácora de eventos");

            MessageBox.Show(GestorIdioma06AV.Instancia.Obtener("pdf_generado"));
        }

        private void lblImprimirEXCEL_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"Bitacora_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var columnas = new Dictionary<string, Func<Bitacora06AV, object>>
            {
                { "DNI",        b => "\t" + b.UsuarioDni },
                { "Fecha",      b => b.Fecha.ToString("dd/MM/yyyy") },
                { "Hora",       b => b.Fecha.ToString("HH:mm:ss") },
                { "Módulo",     b => b.Modulo },
                { "Evento",     b => b.Descripcion },
                { "Criticidad", b => b.Criticidad }
            };

            var exp = new ExportacionEXCEL();
            exp.Exportar(_eventosCargados, columnas, sfd.FileName);

            MessageBox.Show(GestorIdioma06AV.Instancia.Obtener("archivo_generado"));
        }

        private void lblLimpiar_Click(object sender, EventArgs e)
        {
            EstablecerRangoPorDefecto();
            cmbModulo.SelectedIndex = 0;
            cmbEvento.SelectedIndex = 0;
            cmbCriticidad.SelectedIndex = 0;
            AplicarFiltros();
        }

        private void lblFiltrar_Click(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void grilla_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void grilla_SelectionChanged(object sender, EventArgs e)
        {
            var ev = grilla.CurrentRow?.DataBoundItem as Bitacora06AV;

            if (ev == null || string.IsNullOrEmpty(ev.UsuarioDni))
            {
                txtApellido.Text = "";
                txtNombre.Text = "";
                return;
            }

            if (_usuariosPorDni.TryGetValue(ev.UsuarioDni, out var usuario))
            {
                txtApellido.Text = usuario.Apellido;
                txtNombre.Text = usuario.Nombre;
            }
            else
            {
                txtApellido.Text = "(no encontrado)";
                txtNombre.Text = "(no encontrado)";
            }
        }
    }
}
