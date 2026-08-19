using BLL;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.Controles;
using SER;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private Button btnTema;   // toggle de tema claro/oscuro (topbar)

        // Backup automático cada 3 horas (solo administrador).
        private const int IntervaloAutoBackupMs = 3 * 60 * 60 * 1000;
        private System.Windows.Forms.Timer _timerAutoBackup;
        private ToolStripMenuItem gestionBackupsToolStripMenuItem;

        public Usuario06AV Usuario = new Usuario06AV();

        public FRMMain()
        {
            InitializeComponent();
            ConfigurarInterfaz();
            CargarSesionEnEncabezado();
            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            Tema.TemaChanged += RefrescarTema;
            FormClosed += (s, e) => Tema.TemaChanged -= RefrescarTema;

            MostrarControl(new UsuariosControl());

            ConfigurarGestionBackups();
        }

        
        private void ConstruirMenuAgrupado()
        {
            flpModulos.Controls.Clear();

            AgregarGrupo("menu_grp_admin", "\uE713", new List<ItemMenu>
            {
                Item("usuarios", "\uE716", null, false, () => new Controles.UsuariosControl()),
                Item("titulo_roles", "\uE902", PatenteEnum06AV.GestionarRoles, false, () => new Controles.RolesControl()),
                Item("titulo_familias", "\uE8FD", PatenteEnum06AV.GestionarFamilias, false, () => new Controles.FamiliasControl()),
                Item("titulo_patentes", "\uE8D7", PatenteEnum06AV.GestionarPatentes, false, () => new Controles.PatentesControl()),
                Item("bitacora", "\uE9D5", null, true, () => new Controles.BitacoraControl()),
                ItemAccion("backup_menu", "\uE777", null, true, () => { using (var f = new FRMGestionBackup()) f.ShowDialog(this); }),
            });

            AgregarGrupo("menu_grp_maestro", "\uE8F1", new List<ItemMenu>
            {
                Item("pcf_menu_clientes", "\uE716", PatenteEnum06AV.GestionarClientes, false, () => new Controles.ClientesControl()),
                Item("pcf_menu_proveedores", "\uE8D7", PatenteEnum06AV.GestionarProveedores, false, () => new Controles.ProveedoresControl()),
                Item("pcf_menu_componentes", "\uE950", PatenteEnum06AV.GestionarComponentes, false, () => new Controles.ComponentesControl()),
                Item("pcf_menu_modelos", "\uE8A4", PatenteEnum06AV.GestionarModelosEstandar, false, () => new Controles.ModelosEstandarControl()),
            });

            AgregarGrupo("menu_grp_compra", "\uE7BF", new List<ItemMenu>
            {
                Item("pcf_menu_compras", "\uE9D5", PatenteEnum06AV.GestionarCompras, false, () => new Controles.ComprasControl()),
                Item("menu_consultar_stock", "\uE7B8", PatenteEnum06AV.GestionarComponentes, false, () => new Controles.ConsultarStockControl()),
            });

            AgregarGrupo("menu_grp_venta", "\uE719", new List<ItemMenu>
            {
                Item("pcf_menu_ventas", "\uE719", PatenteEnum06AV.GestionarVentas, false, () => new Controles.VentasControl()),
                Item("pcf_menu_entregas", "\uE7B8", PatenteEnum06AV.GestionarEntregas, false, () => new Controles.EntregasControl()),
                Item("pcf_menu_recibos", "\uE8A5", PatenteEnum06AV.GestionarVentas, false, () => new Controles.RecibosControl()),
                Item("menu_facturas", "\uE8A5", PatenteEnum06AV.GestionarVentas, false, () => new Controles.FacturasControl()),
            });

            // Circuito de fabrica (gerente / responsable tecnico).
            AgregarGrupo("menu_grp_produccion", "\uE713", new List<ItemMenu>
            {
                Item("pcf_menu_produccion", "\uE713", PatenteEnum06AV.GestionarProduccion, false, () => new Controles.ProduccionControl()),
                Item("pcf_menu_lineas", "\uE9F5", PatenteEnum06AV.GestionarLineasEnsamblaje, false, () => new Controles.LineasEnsamblajeControl()),
            });

            AgregarGrupo("menu_grp_ayuda", "\uE897", new List<ItemMenu>
            {
                ItemAccion("menu_acerca_de", "\uE946", null, false, MostrarAcercaDe),
            });
        }

        private static ItemMenu Item(string clave, string icono, PatenteEnum06AV? patente, bool soloAdmin, Func<UserControl> crear)
            => new ItemMenu { Clave = clave, Icono = icono, Patente = patente, SoloAdmin = soloAdmin, Crear = crear };

        private static ItemMenu ItemAccion(string clave, string icono, PatenteEnum06AV? patente, bool soloAdmin, Action accion)
            => new ItemMenu { Clave = clave, Icono = icono, Patente = patente, SoloAdmin = soloAdmin, Accion = accion };

        private void AgregarGrupo(string claveHeader, string icono, List<ItemMenu> items)
        {
            var sesion = UsuarioSesion06AV.Instancia();
            var visibles = items.Where(it =>
                (it.Patente == null || sesion.TienePermiso(it.Patente.Value)) &&
                (!it.SoloAdmin || EsAdministrador())).ToList();
            if (visibles.Count == 0) return;

            var grupo = new GrupoSidebar(GestorIdioma06AV.Instancia.Obtener(claveHeader), icono);
            var header = new Button { Tag = grupo };
            ConfigurarBotonGrupo(header);
            header.Click += (s, e) =>
            {
                grupo.Expandido = !grupo.Expandido;
                foreach (var h in grupo.Hijos) h.Visible = grupo.Expandido;
                RenderizarGrupo(header);
            };
            flpModulos.Controls.Add(header);

            foreach (var it in visibles)
            {
                var btn = new Button { Visible = grupo.Expandido };
                ConfigurarBotonModulo(btn, GestorIdioma06AV.Instancia.Obtener(it.Clave), it.Icono);
                var itLocal = it;
                btn.Click += (s, e) =>
                {
                    if (itLocal.Crear != null) { SeleccionarModulo(btn); MostrarControl(itLocal.Crear()); }
                    else itLocal.Accion?.Invoke();
                };
                grupo.Hijos.Add(btn);
                flpModulos.Controls.Add(btn);
            }

            RenderizarGrupo(header);
        }

        private void ConfigurarBotonGrupo(Button btn)
        {
            btn.Height = 40;
            btn.Margin = new Padding(0, 3, 0, 3);   // sin margen horizontal: evita el scroll-x al aparecer el scroll-y
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TabStop = false;
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Tema.FondoElevado;
            btn.ForeColor = Tema.Primario;
            btn.FlatAppearance.MouseOverBackColor = Tema.Acero50;
            btn.FlatAppearance.MouseDownBackColor = Tema.Seleccion;
        }

        private void RenderizarGrupo(Button btn)
        {
            if (!(btn.Tag is GrupoSidebar g)) return;
            btn.Width = AnchoBotonSidebar();
            btn.Padding = _sidebarExpandido ? new Padding(10, 0, 0, 0) : Padding.Empty;
            string chevron = g.Expandido ? "\u25BE  " : "\u25B8  ";
            btn.Text = _sidebarExpandido ? chevron + g.Texto.ToUpperInvariant() : string.Empty;
            btn.TextAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.ImageAlign = _sidebarExpandido ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            btn.TextImageRelation = TextImageRelation.ImageBeforeText;
            Image ant = btn.Image;
            btn.Image = CrearIconoModulo(g.Icono, btn.ForeColor);
            ant?.Dispose();
        }

        // Ancho útil real del área de módulos. Se toma de flpModulos.ClientSize, que
        // YA descuenta la barra de scroll vertical cuando aparece; así los botones
        // nunca sobresalen y no se dispara una barra de scroll horizontal fantasma.
        private int AnchoBotonSidebar() =>
            Math.Max(1, flpModulos.ClientSize.Width);

        /// <summary>Crea (una sola vez) el botón de tema claro/oscuro en la barra superior.</summary>
        private void CrearBotonTema()
        {
            if (btnTema != null) return;

            btnTema = new Button
            {
                Size = new Size(40, 34),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnTema.Click += (s, e) => Tema.ToggleTema();
            ConfigurarBotonTopbar(btnTema, Tema.FondoElevado, Tema.Acero700);
            btnTema.Font = new Font("Segoe UI Symbol", 12f);
            ActualizarTextoBotonTema();

            flpTopActions.Controls.Add(btnTema);
            flpTopActions.Controls.SetChildIndex(btnTema, 0);   // primero (a la izquierda del idioma)
            toolTipMain.SetToolTip(btnTema, GestorIdioma06AV.Instancia.Obtener("cambiar_tema"));
        }

        /// <summary>El botón muestra el modo al que se cambiaría: sol si está oscuro, luna si está claro.</summary>
        private void ActualizarTextoBotonTema()
        {
            if (btnTema != null)
                btnTema.Text = Tema.EsOscuro ? "☀" : "☾";   // ☀ / ☾
        }

        private void RefrescarTema()
        {
            BackColor = Tema.FondoApp;
            pnlTopBar.BackColor = Tema.FondoElevado;
            pnlShell.BackColor = Tema.FondoApp;
            panelPrincipal.BackColor = Tema.FondoApp;
            pnlSidebar.BackColor = Tema.FondoElevado;
            lblSistema.ForeColor = Tema.TextoFuerte;

            ConfigurarBotonTopbar(btnToggleSidebar, Tema.FondoElevado, Tema.Acero700);
            ConfigurarBotonTopbar(opcionesUsuarioBTN, Tema.FondoElevado, Tema.Acero700);
            ConfigurarBotonTopbar(btnIdioma, Tema.PrimarioSuave, Tema.Primario);
            ConfigurarBotonTopbar(btnCerrarSesion, Tema.FondoElevado, Tema.Peligro);
            if (btnTema != null)
            {
                ConfigurarBotonTopbar(btnTema, Tema.FondoElevado, Tema.Acero700);
                btnTema.Font = new Font("Segoe UI Symbol", 12f);
                ActualizarTextoBotonTema();
            }

            ConfigurarPanelUsuario();
            ConfigurarMenuUsuario();

            foreach (Control c in flpModulos.Controls)
                if (c is Button b)
                {
                    if (b.Tag is GrupoSidebar)
                    {
                        b.BackColor = Tema.FondoElevado;
                        b.ForeColor = Tema.Primario;
                        b.FlatAppearance.MouseOverBackColor = Tema.Acero50;
                        RenderizarGrupo(b);
                    }
                    else RenderizarBotonModulo(b);
                }

            if (panelPrincipal.Controls.Count > 0 && panelPrincipal.Controls[0] is UserControl uc)
                Tema.AplicarControl(uc);

            Invalidate(true);
        }

        private void MostrarAcercaDe()
        {
            ConfirmacionForm.MostrarInfo(
                "PC Forge / Cl\u00EDnica\nSistema de gesti\u00F3n de ensamblaje de PC.\nTrabajo Pr\u00E1ctico de Ingenier\u00EDa de Software.",
                GestorIdioma06AV.Instancia.Obtener("menu_grp_ayuda"),
                ConfirmacionForm.TipoConfirmacion.Info, this);
        }

        private void ConfigurarGestionBackups()
        {
            if (!EsAdministrador()) return;

            var t = GestorIdioma06AV.Instancia;

            gestionBackupsToolStripMenuItem = new ToolStripMenuItem(t.Obtener("backup_menu"));
            gestionBackupsToolStripMenuItem.Click += (s, e) =>
            {
                using (var f = new FRMGestionBackup())
                    f.ShowDialog(this);
            };
            ctxMenuUsuario.Items.Add(gestionBackupsToolStripMenuItem);

            _timerAutoBackup = new System.Windows.Forms.Timer { Interval = IntervaloAutoBackupMs };
            _timerAutoBackup.Tick += (s, e) => EjecutarBackupAutomatico();
            _timerAutoBackup.Start();
            FormClosed += (s, e) => _timerAutoBackup?.Stop();
        }

        private void EjecutarBackupAutomatico()
        {
            try
            {
                new IntegridadBLL06AV().RespaldarEnCarpetaPorDefecto();
            }
            catch
            {
            }
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;

            Text = "PC Forge";
            lblSistema.Text = "PC FORGE";
            lblMenuPrincipal.Text = t.Obtener("menu_principal");
            lblSidebarFooter.Text = t.Obtener("sidebar_footer");

            ConstruirMenuAgrupado();

            cambiarContraseñaToolStripMenuItem.Text = t.Obtener("cambiar_contrasenia");
            reloginToolStripMenuItem.Text = t.Obtener("relogin");
            if (gestionBackupsToolStripMenuItem != null)
                gestionBackupsToolStripMenuItem.Text = t.Obtener("backup_menu");
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

            CrearBotonTema();

            ConfigurarPanelUsuario();
            ConfigurarMenuUsuario();
            AjustarSidebar();

            // Cuando el área de módulos cambia de ancho (aparece/desaparece la barra
            // de scroll vertical, o se colapsa el sidebar) reajustamos el ancho de los
            // botones para que sigan encajando y NO aparezca una barra horizontal.
            flpModulos.ClientSizeChanged += (s, e) => AjustarAnchosModulos();

            // La ventana no tiene bordes (FormBorderStyle = None), así que se permite
            // moverla arrastrando la barra superior (y el título), como una barra de título.
            HabilitarArrastreVentana(pnlTopBar);
            HabilitarArrastreVentana(lblSistema);
        }

        // ── Arrastre de la ventana sin bordes ────────────────────────
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        /// <summary>Hace que arrastrar el control indicado mueva toda la ventana,
        /// igual que si se arrastrara la barra de título de una ventana normal.</summary>
        private void HabilitarArrastreVentana(Control control)
        {
            if (control == null) return;
            control.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            };
        }

        private bool _ajustandoAnchos;

        /// <summary>Iguala el ancho de todos los botones del menú al área cliente
        /// actual de flpModulos (que ya descuenta el scroll vertical), evitando el
        /// scroll horizontal. Reentrante-safe.</summary>
        private void AjustarAnchosModulos()
        {
            if (_ajustandoAnchos) return;
            _ajustandoAnchos = true;
            flpModulos.SuspendLayout();
            int ancho = AnchoBotonSidebar();
            foreach (Control c in flpModulos.Controls)
                if (c is Button b) b.Width = ancho;
            flpModulos.ResumeLayout();
            _ajustandoAnchos = false;
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
            btn.Margin = new Padding(0, 3, 0, 3);   // sin margen horizontal: evita el scroll-x al aparecer el scroll-y
            btn.Width = AnchoBotonSidebar();
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
            btn.ForeColor = activo ? Tema.Primario : Tema.Texto;
            btn.FlatAppearance.MouseOverBackColor = activo
                ? Tema.PrimarioSuave
                : Tema.Seleccion;
            btn.FlatAppearance.MouseDownBackColor = Tema.Seleccion;
            btn.Width = AnchoBotonSidebar();

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

            // El control recién mostrado siempre queda en el idioma activo de la sesión.
            if (control is IIdiomaAplicable06AV aplicable)
                aplicable.AplicarIdioma();
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
                    if (btn.Tag is GrupoSidebar) RenderizarGrupo(btn);
                    else RenderizarBotonModulo(btn);
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

        /// <summary>Definición de un ítem del menú agrupado (submenú).</summary>
        private sealed class ItemMenu
        {
            public string Clave;
            public string Icono;
            public PatenteEnum06AV? Patente;   // null = sin gating por patente
            public bool SoloAdmin;             // true = solo para administradores
            public Func<UserControl> Crear;    // control a mostrar
            public Action Accion;              // acción alternativa (p. ej. abrir un diálogo)
        }

        /// <summary>Encabezado desplegable de un grupo del menú lateral.</summary>
        private sealed class GrupoSidebar
        {
            public GrupoSidebar(string texto, string icono) { Texto = texto; Icono = icono; Expandido = false; }
            public string Texto;
            public string Icono;
            public bool Expandido;
            public readonly List<Button> Hijos = new List<Button>();
        }

    }
}
