using BE;
using IngSoftValdezAlegre.Common;
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
    public partial class FRMMain : Form
    {
        private const int SidebarAnchoExpandido = 220;
        private const int SidebarAnchoColapsado = 60;
        private bool _sidebarExpandido = true;

        public Usuario06AV Usuario = new Usuario06AV();

        public FRMMain()
        {
            InitializeComponent();
            ConfigurarBotonesDelDesigner();
            lblUsuario.Text = UsuarioSesion06AV.Instancia().NombreCompleto();
            if (!UsuarioSesion06AV.Instancia().TieneRol("Administrador"))
            {
                bitacoraBTN.Enabled = false;
                bitacoraBTN.Visible = false;
            }
        }

        private void MostrarControl(UserControl control)
        {
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

        private void ConfigurarBotonesDelDesigner()
        {
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
            using (var f = new FRMCambiarContrasenia(Usuario.Dni))
            {
                f.ShowDialog(this);
            }
        }

        private void cerrarSesiónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: "¿Estás seguro que querés cerrar sesión?",
                titulo: "Cerrar sesión",
                textoSi: "Sí, cerrar",
                textoNo: "Cancelar",
                owner: this);

            if (!confirmado) return;

            this.Hide();
            UsuarioSesion06AV.Instancia().CerrarSesion();
            var login = new FRMLogin();
            login.FormClosed += (s, args) => this.Close();
            login.Show();
        }
    }
}
