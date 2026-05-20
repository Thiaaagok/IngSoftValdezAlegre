using BE;
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
    public partial class BitacoraControl : UserControl
    {
        private readonly BitacoraSER06AV _bitacoraSer = new BitacoraSER06AV();
        private readonly UsuariosSER06AV _usuariosSer = new UsuariosSER06AV();

        private List<Bitacora06AV> _eventosCargados = new List<Bitacora06AV>();
        private Dictionary<string, Usuario06AV> _usuariosPorDni = new Dictionary<string, Usuario06AV>();

        private const string OPCION_TODOS = "(Todos)";
        public BitacoraControl()
        {
            InitializeComponent();
            ConfigurarColumnas();
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
                    MessageBox.Show("La fecha inicial no puede ser mayor a la final.",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var eventos = _bitacoraSer.ObtenerEventosEntreFechas(desde, hasta) ?? new List<Bitacora06AV>();
                IEnumerable<Bitacora06AV> filtrados = eventos;

                if (!string.IsNullOrWhiteSpace(txtDni.Text))
                {
                    var dnisCoincidentes = new HashSet<string>(_usuariosPorDni.Values
                        .Select(u => u.Dni));

                    if (dnisCoincidentes.Count == 0)
                    {
                        // Nada que mostrar
                        _eventosCargados = new List<Bitacora06AV>();
                        grilla.DataSource = null;
                        grilla.DataSource = _eventosCargados;
                        lblCantidad.Text = "Eventos: 0";
                        return;
                    }

                    filtrados = filtrados.Where(ev => ev.UsuarioDni != null && dnisCoincidentes.Contains(ev.UsuarioDni));
                }

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
                lblCantidad.Text = "Eventos: " + _eventosCargados.Count;

                PintarPorCriticidad();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo aplicar el filtro:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 224, 224);
                        row.DefaultCellStyle.ForeColor = Tema.Peligro;
                        break;
                    case "Alta":
                        row.DefaultCellStyle.BackColor = Tema.Amber50;
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(140, 80, 0);
                        break;
                    case "Media":
                        row.DefaultCellStyle.BackColor = Color.White;
                        break;
                    case "Baja":
                    default:
                        row.DefaultCellStyle.BackColor = Color.White;
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

            MessageBox.Show("PDF generado correctamente.");
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


            MessageBox.Show("Archivo generado correctamente.");
        }

        private void lblLimpiar_Click(object sender, EventArgs e)
        {
            EstablecerRangoPorDefecto();
            cmbModulo.SelectedIndex = 0;
            cmbEvento.SelectedIndex = 0;
            cmbCriticidad.SelectedIndex = 0;
            txtDni.Text = "—";
            AplicarFiltros();
        }

        private void lblFiltrar_Click(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void grilla_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
