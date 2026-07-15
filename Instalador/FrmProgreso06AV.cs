using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Instalador
{
    /// <summary>
    /// Segunda pantalla del asistente: ejecuta la instalación (creación de la base y
    /// scripts) en segundo plano mostrando el progreso y un registro. Al terminar bien,
    /// escribe la cadena de conexión en el config de la aplicación y ofrece abrir el sistema.
    /// </summary>
    public class FrmProgreso06AV : Form
    {
        private static readonly Color Primario = Color.FromArgb(21, 101, 192);
        private static readonly Color Ok = Color.FromArgb(46, 125, 50);
        private static readonly Color Error = Color.FromArgb(198, 40, 40);

        private readonly OpcionesInstalacion06AV _opciones;

        private Label lblTitulo;
        private ProgressBar barra;
        private TextBox txtLog;
        private CheckBox chkAbrir;
        private Button btnFinalizar;

        private string _exeApp;

        /// <summary>True si la instalación terminó correctamente.</summary>
        public bool Exito { get; private set; }

        public FrmProgreso06AV(OpcionesInstalacion06AV opciones)
        {
            _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
            ConstruirUI();
            Shown += async (s, e) => await EjecutarInstalacionAsync();
        }

        private void ConstruirUI()
        {
            Text = "Instalación en curso";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ClientSize = new Size(560, 420);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            lblTitulo = new Label
            {
                Text = "Preparando la base de datos...",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Primario,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            barra = new ProgressBar
            {
                Location = new Point(26, 62),
                Width = 508,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            txtLog = new TextBox
            {
                Location = new Point(26, 96),
                Size = new Size(508, 270),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(245, 245, 245),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };

            chkAbrir = new CheckBox
            {
                Text = "Abrir el sistema al finalizar",
                Checked = true,
                AutoSize = true,
                Location = new Point(26, 380),
                Enabled = false
            };

            btnFinalizar = new Button
            {
                Text = "Finalizar",
                Location = new Point(438, 374),
                Width = 96,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Primario,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnFinalizar.FlatAppearance.BorderSize = 0;
            btnFinalizar.Click += (s, e) => Finalizar();

            Controls.AddRange(new Control[] { lblTitulo, barra, txtLog, chkAbrir, btnFinalizar });
        }

        private void Log(string mensaje)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), mensaje);
                return;
            }
            txtLog.AppendText(mensaje + Environment.NewLine);
        }

        private async Task EjecutarInstalacionAsync()
        {
            try
            {
                await Task.Run(() => new InstaladorBLL06AV(_opciones).Instalar(Log));

                // Dejar la app apuntando a la instancia elegida. Se escribe en TODAS las
                // ubicaciones del ejecutable (junto al Instalador y bin\Debug|Release), para
                // que la cadena elegida valga sin importar qué configuración se ejecute.
                var exes = ConfiguradorApp06AV.LocalizarTodosExeApp();
                _exeApp = exes.Count > 0 ? exes[0] : null;
                if (_exeApp != null)
                {
                    int escritos = 0;
                    foreach (string exe in exes)
                        if (ConfiguradorApp06AV.EscribirCadenaConexion(exe, _opciones.CadenaBaseDatos()))
                            escritos++;
                    Log(escritos > 0
                        ? $"Configuración de la aplicación actualizada ({escritos} ubicación/es)."
                        : "Aviso: no se pudo actualizar la configuración de la aplicación.");

                    // Acceso directo en el Escritorio, automático (sin intervención del usuario).
                    string lnk = ConfiguradorApp06AV.CrearAccesoDirectoEscritorio(_exeApp);
                    Log(lnk != null
                        ? "Acceso directo creado en el Escritorio."
                        : "Aviso: no se pudo crear el acceso directo en el Escritorio.");
                }
                else
                {
                    Log("Aviso: no se encontró el ejecutable del sistema para configurarlo.");
                    chkAbrir.Checked = false;
                }

                Exito = true;
                Log("");
                Log("Instalación finalizada con éxito.");
                lblTitulo.Text = "Instalación completada";
                lblTitulo.ForeColor = Ok;
            }
            catch (Exception ex)
            {
                Exito = false;
                Log("");
                Log("ERROR: " + ex.Message);
                lblTitulo.Text = "La instalación falló";
                lblTitulo.ForeColor = Error;
                chkAbrir.Checked = false;
                btnFinalizar.Text = "Cerrar";
            }
            finally
            {
                barra.Style = ProgressBarStyle.Continuous;
                barra.Value = 100;
                chkAbrir.Enabled = Exito && _exeApp != null;
                btnFinalizar.Enabled = true;
                ControlBox = true;
            }
        }

        private void Finalizar()
        {
            if (Exito && chkAbrir.Checked && _exeApp != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_exeApp)
                    {
                        WorkingDirectory = System.IO.Path.GetDirectoryName(_exeApp),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo abrir el sistema:\n" + ex.Message,
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            DialogResult = Exito ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }
    }
}
