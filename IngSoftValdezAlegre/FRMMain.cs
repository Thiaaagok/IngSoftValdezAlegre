using BLL;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.Controles;
using SER;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    public partial class FRMMain : Form
    {
        private const int SidebarAnchoExpandido = 220;
        private const int SidebarAnchoColapsado = 64;
        private bool _sidebarExpandido = true;
        private Button _moduloActivo;
        private ToolStripMenuItem cambiarIdiomaToolStripMenuItem;
        private ContextMenuStrip cmsIdiomas;

        public Usuario06AV Usuario = new Usuario06AV();

        public FRMMain()
        {
            InitializeComponent();
            ConfigurarInterfaz();
            CargarSesionEnEncabezado();
            AplicarIdioma();

            // Observer: suscribirse al cambio de idioma
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            var sesion = UsuarioSesion06AV.Instancia();

            if (!sesion.TienePermiso(PatenteEnum06AV.GestionarRoles))
            {
                rolesBTN.Enabled = false;
                rolesBTN.Visible = false;
            }

            if (!sesion.TienePermiso(PatenteEnum06AV.GestionarFamilias))
            {
                familiasBTN.Enabled = false;
                familiasBTN.Visible = false;
            }

            if (!sesion.TienePermiso(PatenteEnum06AV.GestionarPatentes))
            {
                patentesBTN.Enabled = false;
                patentesBTN.Visible = false;
            }

            if (!EsAdministrador())
            {
                bitacoraBTN.Enabled = false;
                bitacoraBTN.Visible = false;
            }

            SeleccionarModulo(usuariosBTN);
            MostrarControl(new UsuariosControl());
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            Text = "PC Forge / Clinica";
            lblSistema.Text = "PC FORGE/CLINICA";
            lblMenuPrincipal.Text = t.Obtener("menu_principal");
            lblSidebarFooter.Text = t.Obtener("sidebar_footer");

            ConfigurarBotonModulo(usuariosBTN, t.Obtener("usuarios"), "\uE716");
            ConfigurarBotonModulo(rolesBTN, t.Obtener("titulo_roles"), "\uE8D7");
            ConfigurarBotonModulo(familiasBTN, t.Obtener("titulo_familias"), "\uE902");
            ConfigurarBotonModulo(patentesBTN, t.Obtener("titulo_patentes"), "\uE8A4");
            ConfigurarBotonModulo(bitacoraBTN, t.Obtener("bitacora"), "\uE9D5");

            cambiarContraseñaToolStripMenuItem.Text = t.Obtener("cambiar_contrasenia");
            reloginToolStripMenuItem.Text = t.Obtener("relogin");
            btnCerrarSesion.Text = t.Obtener("cerrar_sesion");
            btnIdioma.Text = t.IdiomaActual;

            if (cambiarIdiomaToolStripMenuItem == null)
            {
                cambiarIdiomaToolStripMenuItem = new ToolStripMenuItem
                {
                    Name = "cambiarIdiomaToolStripMenuItem",
                    Text = t.Obtener("cambiar_idioma")
                };
                cambiarIdiomaToolStripMenuItem.Click += cambiarIdiomaToolStripMenuItem_Click;
            }
            else
            {
                cambiarIdiomaToolStripMenuItem.Text = t.Obtener("cambiar_idioma");
            }

            ConstruirMenuIdiomas();

            toolTipMain.SetToolTip(btnIdioma, t.Obtener("cambiar_idioma"));
            toolTipMain.SetToolTip(btnCerrarSesion, t.Obtener("cerrar_sesion"));
            toolTipMain.SetToolTip(opcionesUsuarioBTN, t.Obtener("usuario"));
        }

        private void ConfigurarInterfaz()
        {
            DoubleBuffered = true;

            pnlTopBar.BackColor = Tema.FondoElevado;
            pnlShell.BackColor = Tema.FondoApp;
            panelPrincipal.BackColor = Tema.FondoApp;
            pnlSidebar.BackColor = Tema.FondoElevado;
            panelPrincipal.Padding = new Padding(16);

            lblSistema.Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold);
            lblSistema.ForeColor = Tema.TextoFuerte;
            lblSistema.Width = 220;
            pnlTopBar.Paint += DibujarLineaInferior;
            pnlSidebar.Paint += DibujarLineaDerecha;

            ConfigurarBotonTopbar(btnToggleSidebar, Tema.FondoElevado, Tema.Acero700);
            ConfigurarBotonTopbar(opcionesUsuarioBTN, Tema.FondoElevado, Tema.Acero700);
            ConfigurarBotonTopbar(btnIdioma, Tema.PrimarioSuave, Tema.Primario);
            ConfigurarBotonTopbar(btnCerrarSesion, Tema.FondoElevado, Tema.Peligro);

            ConfigurarPanelUsuario();
            ConfigurarMenuUsuario();
            AjustarSidebar();
        }

        private void ConfigurarPanelUsuario()
        {
            panel4.BackColor = Tema.FondoElevado;
            lblAvatar.BackColor = Tema.Primario;
            lblAvatar.ForeColor = Tema.TextoInvertido;
            lblAvatar.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            lblUsuario.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            lblRol.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblUsuario.ForeColor = Tema.Texto;
            lblRol.ForeColor = Tema.TextoSuave;
            AplicarAvatarCircular();
        }

        private void ConfigurarMenuUsuario()
        {
            ctxMenuUsuario.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            ctxMenuUsuario.BackColor = Tema.FondoElevado;
            ctxMenuUsuario.ForeColor = Tema.Texto;
            ctxMenuUsuario.RenderMode = ToolStripRenderMode.System;
            ctxMenuUsuario.Padding = new Padding(4);
        }

        private void ConfigurarBotonTopbar(Button btn, Color fondo, Color texto)
        {
            btn.BackColor = fondo;
            btn.ForeColor = texto;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Tema.Borde;
            btn.FlatAppearance.MouseOverBackColor = Tema.Acero100;
            btn.FlatAppearance.MouseDownBackColor = Tema.Borde;
            btn.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ConfigurarBotonModulo(Button btn, string texto, string icono)
        {
            btn.Tag = new ModuloSidebar(texto, icono);
            btn.Width = Math.Max(44, pnlSidebar.Width - pnlSidebar.Padding.Left - pnlSidebar.Padding.Right);
            btn.Height = 42;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.ImageAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.TextImageRelation = TextImageRelation.ImageBeforeText;
            btn.TextAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.Padding = _sidebarExpandido ? new Padding(16, 0, 0, 0) : Padding.Empty;
            btn.UseVisualStyleBackColor = false;
            btn.TabStop = false;

            RenderizarBotonModulo(btn);
            toolTipMain.SetToolTip(btn, texto);
        }

        private void RenderizarBotonModulo(Button btn)
        {
            if (!(btn.Tag is ModuloSidebar modulo))
            {
                return;
            }

            bool activo = btn == _moduloActivo;
            btn.Text = _sidebarExpandido ? modulo.Texto : string.Empty;
            btn.TextAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.ImageAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.Padding = _sidebarExpandido ? new Padding(16, 0, 0, 0) : Padding.Empty;
            btn.BackColor = activo ? Tema.PrimarioSuave : Tema.FondoElevado;
            btn.ForeColor = activo ? Tema.Primario : Tema.Acero700;
            btn.FlatAppearance.MouseOverBackColor = activo
                ? Tema.PrimarioSuave
                : Tema.Acero50;
            btn.FlatAppearance.MouseDownBackColor = Tema.Seleccion;
            btn.Width = Math.Max(44, pnlSidebar.Width - pnlSidebar.Padding.Left - pnlSidebar.Padding.Right);

            Image imagenAnterior = btn.Image;
            btn.Image = CrearIconoModulo(modulo.Icono, btn.ForeColor);
            imagenAnterior?.Dispose();
        }

        private Image CrearIconoModulo(string icono, Color color)
        {
            var bitmap = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (var fuente = new Font("Segoe MDL2 Assets", 11.5f, FontStyle.Regular))
            {
                g.Clear(Color.Transparent);
                TextRenderer.DrawText(
                    g,
                    icono,
                    fuente,
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            return bitmap;
        }

        private void AplicarAvatarCircular()
        {
            lblAvatar.Region?.Dispose();
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, lblAvatar.Width - 1, lblAvatar.Height - 1);
                lblAvatar.Region = new Region(path);
            }
        }

        private void DibujarLineaInferior(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Tema.Borde))
            {
                e.Graphics.DrawLine(pen, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            }
        }

        private void DibujarLineaDerecha(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Tema.Borde))
            {
                e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            }
        }

        private void SeleccionarModulo(Button boton)
        {
            _moduloActivo = boton;

            foreach (Control c in flpModulos.Controls)
            {
                if (c is Button btn)
                {
                    RenderizarBotonModulo(btn);
                }
            }
        }

        private void MostrarControl(UserControl control)
        {
            foreach (Control c in panelPrincipal.Controls)
            {
                c.Dispose();
            }

            panelPrincipal.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelPrincipal.Controls.Add(control);
        }

        private void bitacoraBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(bitacoraBTN);
            MostrarControl(new BitacoraControl());
        }

        private void usuariosBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(usuariosBTN);
            MostrarControl(new UsuariosControl());
        }

        private void rolesBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(rolesBTN);
            MostrarControl(new RolesControl());
        }

        private void familiasBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(familiasBTN);
            MostrarControl(new FamiliasControl());
        }

        private void patentesBTN_Click(object sender, EventArgs e)
        {
            SeleccionarModulo(patentesBTN);
            MostrarControl(new PatentesControl());
        }

        private void btnToggleSidebar_Click(object sender, EventArgs e)
        {
            _sidebarExpandido = !_sidebarExpandido;
            AjustarSidebar();
        }

        private void AjustarSidebar()
        {
            pnlSidebar.Width = _sidebarExpandido ? SidebarAnchoExpandido : SidebarAnchoColapsado;
            pnlSidebar.Padding = _sidebarExpandido
                ? new Padding(14, 20, 10, 14)
                : new Padding(10, 20, 10, 14);
            lblMenuPrincipal.Visible = _sidebarExpandido;
            lblSidebarFooter.Visible = _sidebarExpandido;
            btnToggleSidebar.Text = _sidebarExpandido ? "≡" : ">";

            foreach (Control c in flpModulos.Controls)
            {
                if (c is Button btn)
                {
                    RenderizarBotonModulo(btn);
                }
            }
        }

        private void opcionesUsuarioBTN_Click(object sender, EventArgs e)
        {
            ctxMenuUsuario.Show(panel4, new Point(panel4.Width - ctxMenuUsuario.Width, panel4.Height + 2));
        }

        // Hay más de dos idiomas disponibles (ES/EN/PT), así que el botón y el ítem
        // de menú despliegan un listado en vez de alternar entre dos opciones fijas.
        private void btnIdioma_Click(object sender, EventArgs e)
        {
            cmsIdiomas.Show(btnIdioma, new Point(0, btnIdioma.Height));
        }

        private void cambiarIdiomaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmsIdiomas.Show(Cursor.Position);
        }

        /// <summary>
        /// Crea, una sola vez, el menú con todos los idiomas en SER.GestorIdioma06AV.IdiomasDisponibles.
        /// Si se agrega un idioma nuevo (otro .json + otra constante), aparece solo, sin tocar este método.
        /// </summary>
        private void ConstruirMenuIdiomas()
        {
            if (cmsIdiomas != null) return;

            cmsIdiomas = new ContextMenuStrip();

            var nombres = new System.Collections.Generic.Dictionary<string, string>
            {
                { GestorIdioma06AV.ES, "Español" },
                { GestorIdioma06AV.EN, "English" },
                { GestorIdioma06AV.PT, "Português" }
            };

            foreach (string codigo in GestorIdioma06AV.IdiomasDisponibles)
            {
                string texto = nombres.TryGetValue(codigo, out var n) ? n : codigo;
                var item = new ToolStripMenuItem(texto) { Tag = codigo };
                item.Click += (s, e) => CambiarIdiomaA(((ToolStripMenuItem)s).Tag.ToString());
                cmsIdiomas.Items.Add(item);
            }
        }

        private void CambiarIdiomaA(string nuevo)
        {
            var t = GestorIdioma06AV.Instancia;
            if (t.IdiomaActual == nuevo) return;

            try
            {
                t.CambiarIdioma(nuevo);

                ConfirmacionForm.MostrarInfo(
                    t.Obtener("idioma_cambiado_sesion"),
                    titulo: t.Obtener("idioma"),
                    owner: this);
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            cerrarSesiónToolStripMenuItem_Click(sender, e);
        }

        private void cambiarContraseñaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var usuarioActual = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuarioActual == null)
            {
                CerrarSesionYVolverALogin();
                return;
            }

            using (var f = new FRMCambiarContrasenia(usuarioActual.Dni))
            {
                f.ShowDialog(this);

                if (f.ContraseniaCambiada)
                {
                    CerrarSesionYVolverALogin();
                }
            }
        }

        private void CerrarSesionYVolverALogin()
        {
            UsuarioSesion06AV.Instancia().CerrarSesion();
            var login = new FRMLogin();
            login.Show();
            Close();
        }

        /// <summary>
        /// Se llama cuando se le agregan o quitan patentes/familias al rol que
        /// tiene asignado el usuario de la sesión actual. Las patentes efectivas
        /// quedaron cacheadas en UsuarioSesion06AV al momento del login, así que
        /// hay que cerrar sesión y obligar a loguearse de nuevo para que se
        /// recalculen (CargarPatentes se llama otra vez dentro de Login).
        /// </summary>
        public void ForzarReloginPorCambioDeRol()
        {
            var t = GestorIdioma06AV.Instancia;
            ConfirmacionForm.MostrarInfo(
                t.Obtener("rol_modificado_relogin"),
                titulo: t.Obtener("aviso"),
                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                owner: this);

            CerrarSesionYVolverALogin();
        }

        /// <summary>
        /// "Relogin": es como cerrar sesión a nivel de pantalla, pero a propósito
        /// NO se llama a UsuarioSesion06AV.Instancia().CerrarSesion(). El singleton
        /// se queda con el usuario, rol y patentes cargados. Sirve para demostrar
        /// que el singleton retiene su estado: cualquier intento de login posterior
        /// (propio o de otro usuario) va a ser rechazado por UsuariosBLL06AV.Login,
        /// que verifica si ya hay una sesión activa antes de autenticar a nadie.
        /// </summary>
        private void reloginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_relogin"),
                titulo: t.Obtener("relogin"),
                textoSi: t.Obtener("si_relogin"),
                textoNo: t.Obtener("cancelar"),
                owner: this);

            if (!confirmado) return;

            Hide();

            var login = new FRMLogin();
            login.FormClosed += (s, args) => Close();
            login.Show();
        }

        private void cerrarSesiónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_cerrar_sesion"),
                titulo: t.Obtener("cerrar_sesion"),
                textoSi: t.Obtener("si_cerrar"),
                textoNo: t.Obtener("cancelar"),
                owner: this);

            if (!confirmado) return;

            Hide();

            var usuarioActual = UsuarioSesion06AV.Instancia().UsuarioActual;
            if (usuarioActual != null)
            {
                // Persistir idioma elegido durante la sesión
                try
                {
                    new UsuariosBLL06AV().CambiarIdioma(usuarioActual.Dni, t.IdiomaActual);
                }
                catch
                {
                    // Si falla la persistencia del idioma, no bloqueamos el logout
                }

                // Registrar evento de logout en bitácora
                new BitacoraBLL06AV().Logout(usuarioActual.Dni);
            }

            UsuarioSesion06AV.Instancia().CerrarSesion();
            var login = new FRMLogin();
            login.FormClosed += (s, args) => Close();
            login.Show();
        }

        private void CargarSesionEnEncabezado()
        {
            var sesion = UsuarioSesion06AV.Instancia();
            Usuario = sesion.UsuarioActual ?? new Usuario06AV();

            lblUsuario.Text = sesion.UsuarioActual != null
                ? sesion.NombreCompleto()
                : GestorIdioma06AV.Instancia.Obtener("usuario");
            lblRol.Text = sesion.Rol?.Descripcion ?? string.Empty;
            lblAvatar.Text = ObtenerIniciales(sesion.UsuarioActual);
        }

        private bool EsAdministrador()
        {
            var rol = UsuarioSesion06AV.Instancia().Rol;
            return rol != null &&
                   string.Equals(rol.Descripcion, "Administrador", StringComparison.OrdinalIgnoreCase);
        }

        private string ObtenerIniciales(Usuario06AV usuario)
        {
            if (usuario == null)
            {
                return "US";
            }

            string nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? "U" : usuario.Nombre.Trim().Substring(0, 1);
            string apellido = string.IsNullOrWhiteSpace(usuario.Apellido) ? "S" : usuario.Apellido.Trim().Substring(0, 1);
            return (nombre + apellido).ToUpperInvariant();
        }

        private sealed class ModuloSidebar
        {
            public ModuloSidebar(string texto, string icono)
            {
                Texto = texto;
                Icono = icono;
            }

            public string Texto { get; }
            public string Icono { get; }
        }

    }
}
