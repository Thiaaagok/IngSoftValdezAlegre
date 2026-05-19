using BE;
using IngSoftValdezAlegre.Controles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    public partial class MainForm : Form
    {
        private const int SidebarAnchoExpandido = 220;
        private const int SidebarAnchoColapsado = 60;
        private bool _sidebarExpandido = true;

        public Usuario06AV Usuario = new Usuario06AV();

        public MainForm()
        {
            InitializeComponent();
            ConfigurarBotonesDelDesigner();
            Usuario = UsuarioSesion06AV.Instancia().UsuarioActual;
        }

        // ============ NAVEGACIÓN ============

        private void MostrarControl(UserControl control)
        {
            // Liberar el control anterior si había uno
            foreach (Control c in panelPrincipal.Controls)
                c.Dispose();

            panelPrincipal.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelPrincipal.Controls.Add(control);
        }

        private void bitacoraBTN_Click(object sender, EventArgs e)
        {
            MostrarControl(new BitacoraControl());
        }

        private void usuariosBTN_Click(object sender, EventArgs e)
        {
            MostrarControl(new UsuariosControl());
        }

        // ============ SIDEBAR ============

        private void ConfigurarBotonesDelDesigner()
        {
            // Asigna Tag a los botones creados desde el designer
            // para que el toggle pueda mostrar/ocultar texto
            AsignarTag(usuariosBTN, "Usuarios");
            AsignarTag(bitacoraBTN, "Bitácora");
        }

        private void AsignarTag(Button btn, string texto)
        {
            btn.Tag = texto;
            btn.Text = texto;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(15, 0, 0, 0);
        }

        private void btnToggleSidebar_Click(object sender, EventArgs e)
        {
            _sidebarExpandido = !_sidebarExpandido;
            pnlSidebar.Width = _sidebarExpandido ? SidebarAnchoExpandido : SidebarAnchoColapsado;

            foreach (Control c in flpModulos.Controls)
            {
                if (c is Button btn && btn.Tag is string textoOriginal)
                {
                    btn.Width = pnlSidebar.Width - 5;
                    if (_sidebarExpandido)
                    {
                        btn.Text = textoOriginal;
                        btn.TextAlign = ContentAlignment.MiddleLeft;
                    }
                    else
                    {
                        // Mostrar solo las primeras 2 letras (o un ícono si usás iconos)
                        btn.Text = textoOriginal.Substring(0, Math.Min(2, textoOriginal.Length));
                        btn.TextAlign = ContentAlignment.MiddleCenter;
                    }
                }
            }
        }

        private void opcionesUsuarioBTN_Click(object sender, EventArgs e)
        {
            ctxMenuUsuario.Show(opcionesUsuarioBTN, new Point(0, opcionesUsuarioBTN.Height));
        }

        private void cambiarContraseñaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var f = new CambiarContraseniaForm(Usuario.Dni))
            {
                f.ShowDialog(this);
            }
        }

        private void cerrarSesiónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var r = MessageBox.Show("¿Cerrar sesión?", "Confirmar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            this.Hide();
            var login = new Login();
            login.FormClosed += (s, args) => this.Close();
            login.Show();
        }
    }
}
