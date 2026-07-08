using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Instalador
{
    /// <summary>
    /// Primera pantalla del asistente de instalación: "Configuración inicial - Base de datos".
    /// Permite elegir/detectar la instancia de SQL Server (incluida LocalDB) y las
    /// credenciales, prueba la conexión y devuelve las opciones de instalación.
    /// </summary>
    public class FrmConexion06AV : Form
    {
        private static readonly Color Primario = Color.FromArgb(21, 101, 192);
        private static readonly Color Texto = Color.FromArgb(33, 33, 33);
        private static readonly Color TextoSuave = Color.FromArgb(110, 110, 110);

        private ComboBox cboInstancia;
        private Button btnDetectar;
        private Label lblEstado;
        private RadioButton rbWindows;
        private RadioButton rbSql;
        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Label lblUsuario;
        private Label lblPassword;
        private TextBox txtBaseDatos;
        private Button btnContinuar;
        private Button btnCancelar;

        /// <summary>Opciones elegidas (válidas solo si el resultado fue OK).</summary>
        public OpcionesInstalacion06AV Opciones { get; private set; }

        public FrmConexion06AV()
        {
            ConstruirUI();
            Load += (s, e) => DetectarInstancias();
        }

        private void ConstruirUI()
        {
            Text = "Configuración inicial - Base de datos";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 420);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var lblTitulo = new Label
            {
                Text = "Configurar conexión a la base de datos",
                Font = new Font("Segoe UI Semibold", 15.75f, FontStyle.Bold),
                ForeColor = Primario,
                AutoSize = true,
                Location = new Point(24, 22)
            };

            var lblIntro = new Label
            {
                Text = "Elegí la instancia de SQL Server donde querés instalar / conectarte a la base.",
                ForeColor = TextoSuave,
                AutoSize = true,
                Location = new Point(26, 58)
            };

            // ── Instancia ────────────────────────────────────────────
            var lblInstancia = new Label
            {
                Text = "Instancia:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 100)
            };
            cboInstancia = new ComboBox
            {
                Location = new Point(30, 122),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            btnDetectar = new Button
            {
                Text = "Detectar",
                Location = new Point(422, 121),
                Width = 108,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnDetectar.FlatAppearance.BorderColor = Color.FromArgb(189, 189, 189);
            btnDetectar.Click += (s, e) => DetectarInstancias();

            lblEstado = new Label
            {
                Text = "",
                ForeColor = TextoSuave,
                AutoSize = true,
                Location = new Point(30, 152)
            };

            // ── Base de datos ────────────────────────────────────────
            var lblBase = new Label
            {
                Text = "Base de datos:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 186)
            };
            txtBaseDatos = new TextBox
            {
                Location = new Point(30, 208),
                Width = 380,
                Text = "IngSoftValdezAlegre"
            };

            // ── Autenticación ────────────────────────────────────────
            var lblAuth = new Label
            {
                Text = "Autenticación:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(26, 246)
            };
            rbWindows = new RadioButton
            {
                Text = "Windows",
                Checked = true,
                AutoSize = true,
                Location = new Point(30, 268)
            };
            rbSql = new RadioButton
            {
                Text = "SQL Server",
                AutoSize = true,
                Location = new Point(140, 268)
            };
            rbWindows.CheckedChanged += (s, e) => ActualizarAuth();
            rbSql.CheckedChanged += (s, e) => ActualizarAuth();

            lblUsuario = new Label
            {
                Text = "Usuario:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(30, 300)
            };
            txtUsuario = new TextBox { Location = new Point(90, 297), Width = 150, Text = "sa" };

            lblPassword = new Label
            {
                Text = "Contraseña:",
                ForeColor = Texto,
                AutoSize = true,
                Location = new Point(260, 300)
            };
            txtPassword = new TextBox
            {
                Location = new Point(345, 297),
                Width = 165,
                UseSystemPasswordChar = true
            };

            // ── Botones ──────────────────────────────────────────────
            btnContinuar = new Button
            {
                Text = "Continuar",
                Location = new Point(322, 366),
                Width = 108,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Primario,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnContinuar.FlatAppearance.BorderSize = 0;
            btnContinuar.Click += (s, e) => Continuar();

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(438, 366),
                Width = 96,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Texto,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(189, 189, 189);

            Controls.AddRange(new Control[]
            {
                lblTitulo, lblIntro, lblInstancia, cboInstancia, btnDetectar, lblEstado,
                lblBase, txtBaseDatos, lblAuth, rbWindows, rbSql,
                lblUsuario, txtUsuario, lblPassword, txtPassword,
                btnContinuar, btnCancelar
            });

            AcceptButton = btnContinuar;
            CancelButton = btnCancelar;
            ActualizarAuth();
        }

        private void ActualizarAuth()
        {
            bool sql = rbSql.Checked;
            lblUsuario.Enabled = txtUsuario.Enabled = sql;
            lblPassword.Enabled = txtPassword.Enabled = sql;
        }

        private void DetectarInstancias()
        {
            Cursor = Cursors.WaitCursor;
            btnDetectar.Enabled = false;
            lblEstado.Text = "Buscando instancias...";
            Application.DoEvents();

            try
            {
                List<string> instancias = InstaladorDAL06AV.DetectarInstancias();
                string seleccion = cboInstancia.Text;

                cboInstancia.Items.Clear();
                cboInstancia.Items.AddRange(instancias.ToArray());

                if (!string.IsNullOrWhiteSpace(seleccion) && instancias.Contains(seleccion))
                    cboInstancia.Text = seleccion;
                else if (cboInstancia.Items.Count > 0)
                    cboInstancia.SelectedIndex = 0;

                lblEstado.Text = $"Se detectaron {instancias.Count} instancia(s).";
            }
            catch (Exception ex)
            {
                lblEstado.Text = "No se pudieron detectar instancias: " + ex.Message;
            }
            finally
            {
                btnDetectar.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void Continuar()
        {
            string instancia = (cboInstancia.Text ?? "").Trim();
            if (instancia.Length == 0)
            {
                MessageBox.Show(this, "Elegí o escribí una instancia de SQL Server.",
                    "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string baseDatos = (txtBaseDatos.Text ?? "").Trim();
            if (baseDatos.Length == 0) baseDatos = "IngSoftValdezAlegre";

            var op = new OpcionesInstalacion06AV
            {
                Servidor = instancia,
                BaseDatos = baseDatos,
                SeguridadIntegrada = rbWindows.Checked
            };
            if (rbSql.Checked)
            {
                op.Usuario = txtUsuario.Text;
                op.Contrasenia = txtPassword.Text;
            }

            // Probar la conexión antes de avanzar.
            Cursor = Cursors.WaitCursor;
            btnContinuar.Enabled = false;
            lblEstado.Text = "Probando conexión...";
            Application.DoEvents();
            try
            {
                new InstaladorDAL06AV(op).ProbarConexion();
            }
            catch (Exception ex)
            {
                lblEstado.Text = "";
                MessageBox.Show(this,
                    "No se pudo conectar a la instancia indicada:\n\n" + ex.Message,
                    "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnContinuar.Enabled = true;
                Cursor = Cursors.Default;
                return;
            }

            Cursor = Cursors.Default;
            Opciones = op;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
