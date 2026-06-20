using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using SER.Excepciones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class UsuariosControl : UserControl, IIdiomaAplicable06AV
    {
        private enum Modo
        {
            Consulta,
            Anadir,
            Modificar,
            ActivarDesactivar,
            Desbloquear
        }

        private readonly UsuariosBLL06AV _usuariosSer = new UsuariosBLL06AV();
        private readonly RolesBLL06AV _rolesSer = new RolesBLL06AV();

        private List<Usuario06AV> _usuariosCargados = new List<Usuario06AV>();
        private Modo _modo = Modo.Consulta;
        private bool _suspenderEventosRadio = false;

        public UsuariosControl()
        {
            InitializeComponent();
            AplicarTema();
            ConfigurarColumnas();
            AplicarIdioma();
            AjustarLayout();
            Resize += (s, e) => AjustarLayout();

            // Observer: suscribirse al cambio de idioma
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            Disposed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;
        }

        private void AplicarTema()
        {
            Tema.AplicarControl(this);
            Tema.AplicarTitulo(lblTitulo);
            Tema.AplicarSubtitulo(lblFormTitulo);
            Tema.AplicarSubtitulo(lblMensajeTitulo);
            Tema.AplicarGrilla(grilla);

            Tema.AplicarBotonPrimario(btnCrear);
            Tema.AplicarBotonPrimario(btnModificar);
            Tema.AplicarBotonAcento(btnActDesact);
            Tema.AplicarBotonAcento(btnDesbloquear);
            Tema.AplicarBotonPrimario(btnAplicar);
            Tema.AplicarBotonSecundario(btnCancelar);

            txtMensaje.BackColor = Tema.Acero50;
            txtMensaje.ForeColor = Tema.Texto;
            txtMensaje.BorderStyle = BorderStyle.FixedSingle;
            lblCantidad.ForeColor = Tema.TextoSuave;
        }

        private void AjustarLayout()
        {
            int margen = 8;
            int ancho = Math.Max(680, ClientSize.Width);
            int alto = Math.Max(500, ClientSize.Height);
            int altoHeader = 34;
            int anchoAcciones = 118;
            int espacio = 12;
            int altoInferior = 210;

            lblTitulo.Location = new Point(margen, 0);
            radActivos.Location = new Point(Math.Max(210, ancho - 310), 10);
            radTodos.Location = new Point(Math.Max(310, ancho - 200), 10);
            lblCantidad.Location = new Point(Math.Max(420, ancho - 150), 12);

            int grillaY = altoHeader + 4;
            int grillaH = Math.Max(190, alto - grillaY - altoInferior - 16);
            int accionesX = ancho - anchoAcciones - margen;
            int grillaW = Math.Max(420, accionesX - margen - espacio);

            grilla.Location = new Point(margen, grillaY);
            grilla.Size = new Size(grillaW, grillaH);

            Button[] acciones = { btnCrear, btnDesbloquear, btnModificar, btnActDesact, btnAplicar, btnCancelar };
            int botonY = grillaY;
            foreach (Button boton in acciones)
            {
                boton.SetBounds(accionesX, botonY, anchoAcciones, 34);
                botonY += 42;
            }

            int formY = grillaY + grillaH + 20;
            int labelW = 64;
            int inputX = margen + labelW + 6;
            int mensajeW = Math.Min(240, Math.Max(190, ancho / 4));
            int mensajeX = ancho - mensajeW - margen;
            int inputW = Math.Max(260, mensajeX - inputX - 34);
            int rowH = 28;

            lblFormTitulo.Location = new Point(margen, formY);
            lblMensajeTitulo.Location = new Point(mensajeX, formY);

            int y = formY + 30;
            PosicionarCampo(lblDni, txtDni, margen, inputX, y, labelW, inputW);
            y += rowH;
            PosicionarCampo(lblApellido, txtApellido, margen, inputX, y, labelW, inputW);
            y += rowH;
            PosicionarCampo(lblNombre, txtNombre, margen, inputX, y, labelW, inputW);
            y += rowH;
            PosicionarCampo(lblEmail, txtEmail, margen, inputX, y, labelW, inputW);
            y += rowH;
            PosicionarCampo(lblRol, cmbRol, margen, inputX, y, labelW, inputW);
            y += rowH;
            PosicionarCampo(lblLogin, txtLogin, margen, inputX, y, labelW, inputW);

            int checksY = Math.Min(alto - 26, y + 32);
            lblBloqueado.Location = new Point(inputX, checksY);
            chkBloqueado.Location = new Point(inputX + 78, checksY + 2);
            lblActivo.Location = new Point(inputX + 170, checksY);
            chkActivo.Location = new Point(inputX + 222, checksY + 2);

            txtMensaje.SetBounds(mensajeX, formY + 30, mensajeW, Math.Max(78, alto - formY - 42));
        }

        private void PosicionarCampo(Label label, Control input, int labelX, int inputX, int y, int labelW, int inputW)
        {
            label.SetBounds(labelX, y + 4, labelW, 20);
            input.SetBounds(inputX, y, inputW, input is ComboBox ? 25 : 24);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            lblTitulo.Text      = t.Obtener("usuarios");
            radActivos.Text     = t.Obtener("activos");
            radTodos.Text       = t.Obtener("todos");
            lblFormTitulo.Text  = t.Obtener("datos_usuario");
            lblDni.Text         = t.Obtener("dni");
            lblApellido.Text    = t.Obtener("apellido");
            lblNombre.Text      = t.Obtener("nombre");
            lblEmail.Text       = t.Obtener("email");
            lblLogin.Text       = t.Obtener("campo_login");
            lblRol.Text         = t.Obtener("rol");
            lblBloqueado.Text   = t.Obtener("bloqueado");
            lblActivo.Text      = t.Obtener("activo");
            lblMensajeTitulo.Text = t.Obtener("mensaje");
            btnCrear.Text       = t.Obtener("crear");
            btnDesbloquear.Text = t.Obtener("desbloquear");
            btnModificar.Text   = t.Obtener("modificar");
            btnActDesact.Text   = t.Obtener("act_desact");
            btnAplicar.Text     = t.Obtener("aplicar");
            btnCancelar.Text    = t.Obtener("cancelar");

            // Headers de la grilla
            if (grilla.Columns["Dni"]      != null) grilla.Columns["Dni"].HeaderText      = t.Obtener("dni");
            if (grilla.Columns["Apellido"] != null) grilla.Columns["Apellido"].HeaderText = t.Obtener("apellido");
            if (grilla.Columns["Nombre"]   != null) grilla.Columns["Nombre"].HeaderText   = t.Obtener("nombre");
            if (grilla.Columns["Login"]    != null) grilla.Columns["Login"].HeaderText    = t.Obtener("campo_login");
            if (grilla.Columns["Email"]    != null) grilla.Columns["Email"].HeaderText    = t.Obtener("email");
            if (grilla.Columns["Rol"]      != null) grilla.Columns["Rol"].HeaderText      = t.Obtener("rol");
            if (grilla.Columns["Activo"]   != null) grilla.Columns["Activo"].HeaderText   = t.Obtener("activo");
            if (grilla.Columns["Bloqueado"]!= null) grilla.Columns["Bloqueado"].HeaderText= t.Obtener("bloqueado");

            // Actualizar conteo si ya hay datos cargados
            if (_usuariosCargados != null && _usuariosCargados.Count > 0)
                lblCantidad.Text = t.Obtener("numero_usuarios") + " " + _usuariosCargados.Count;

            // Refrescar el texto del recuadro de mensaje según el modo activo
            RefrescarMensajeModo();
        }

        /// <summary>
        /// Reescribe solo el texto de txtMensaje según el modo actual.
        /// Se llama desde AplicarIdioma() para que el recuadro también
        /// se actualice cuando el usuario cambia de idioma.
        /// </summary>
        private void RefrescarMensajeModo()
        {
            var t = GestorIdioma06AV.Instancia;
            switch (_modo)
            {
                case Modo.Consulta:
                    SetMensaje(t.Obtener("modo_consulta_titulo"), t.Obtener("modo_consulta_detalle"));
                    break;
                case Modo.Anadir:
                    SetMensaje(t.Obtener("modo_anadir_titulo"), t.Obtener("modo_anadir_detalle"));
                    break;
                case Modo.Modificar:
                    SetMensaje(t.Obtener("modo_modificar_titulo"), t.Obtener("modo_modificar_detalle"));
                    break;
                case Modo.ActivarDesactivar:
                    SetMensaje(t.Obtener("modo_actdesact_titulo"), t.Obtener("modo_actdesact_detalle"));
                    break;
                case Modo.Desbloquear:
                    SetMensaje(t.Obtener("modo_desbloquear_titulo"), t.Obtener("modo_desbloquear_detalle"));
                    break;
            }
        }

        private void ConfigurarColumnas()
        {
            grilla.AutoGenerateColumns = false;
            grilla.Columns.Clear();

            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dni", HeaderText = "DNI", DataPropertyName = "Dni", FillWeight = 80 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Apellido", HeaderText = "Apellido", DataPropertyName = "Apellido", FillWeight = 120 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", HeaderText = "Nombre", DataPropertyName = "Nombre", FillWeight = 120 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Login", HeaderText = "Login", DataPropertyName = "Login", FillWeight = 110 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", DataPropertyName = "Email", FillWeight = 170 });
            grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rol", HeaderText = "Rol", DataPropertyName = "RolDescripcion", FillWeight = 110 });
            grilla.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Activo", HeaderText = "Activo", DataPropertyName = "Activo", FillWeight = 60 });
            grilla.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Bloqueado", HeaderText = "Bloq.", DataPropertyName = "Bloqueado", FillWeight = 60 });
        }

        private void CargarRoles()
        {
            try
            {
                var roles = _rolesSer.ObtenerTodos();

                cmbRol.DataSource = roles;
                cmbRol.DisplayMember = "Descripcion";
                cmbRol.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("error_cargar_roles"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this.FindForm());
            }
        }


        /// <summary>
        /// Oculta los botones de acción para los que el usuario no tiene patente.
        /// Se llama al cargar el control y cada vez que se vuelve a Modo.Consulta.
        /// </summary>
        private void AplicarPermisosBotones()
        {
            var sesion = UsuarioSesion06AV.Instancia();

            // Visible + habilitado solo si tiene la patente correspondiente
            bool puedeVer        = sesion.TienePermiso(PatenteEnum06AV.VerUsuarios);
            bool puedeCrear      = sesion.TienePermiso(PatenteEnum06AV.CrearUsuarios);
            bool puedeEditar     = sesion.TienePermiso(PatenteEnum06AV.EditarUsuarios);
            bool puedeActDesact  = sesion.TienePermiso(PatenteEnum06AV.ActDesactivarUsuarios);
            bool puedeDesbloquear= sesion.TienePermiso(PatenteEnum06AV.DesbloquearUsuarios);

            // La grilla misma solo se muestra si puede ver usuarios
            grilla.Visible  = puedeVer;

            // Botones de acción: visibles solo si tiene el permiso
            btnCrear.Visible       = puedeCrear;
            btnModificar.Visible   = puedeEditar;
            btnActDesact.Visible   = puedeActDesact;
            btnDesbloquear.Visible = puedeDesbloquear;

            // Si el botón es visible, lo habilitamos; si no tiene permiso, ocultamos
            if (puedeCrear)       HabilitarBoton(btnCrear,       true);
            if (puedeEditar)      HabilitarBoton(btnModificar,   true);
            if (puedeActDesact)   HabilitarBoton(btnActDesact,   true);
            if (puedeDesbloquear) HabilitarBoton(btnDesbloquear, true);
        }

        private void CambiarModo(Modo nuevo)
        {
            _modo = nuevo;
            var t       = GestorIdioma06AV.Instancia;
            var sesion  = UsuarioSesion06AV.Instancia();

            switch (nuevo)
            {
                case Modo.Consulta:
                    SetMensaje(t.Obtener("modo_consulta_titulo"), t.Obtener("modo_consulta_detalle"));
                    SetBotones(crear: true, desbloq: true, modif: true, actDesact: true,
                               aplicar: true, cancelar: false, radios: true);
                    // Ocultar los que no tiene permiso
                    AplicarPermisosBotones();
                    SetFormularioEditable(false, incluirEstado: false);
                    grilla.Enabled = true;
                    txtLogin.ReadOnly = true;
                    break;

                case Modo.Anadir:
                    if (!sesion.TienePermiso(PatenteEnum06AV.CrearUsuarios))
                    {
                        ConfirmacionForm.MostrarInfo(
                            t.Obtener("sin_permiso_crear"),
                            titulo: t.Obtener("permiso_denegado"),
                            tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                            owner: this.FindForm());
                        return;
                    }
                    SetMensaje(t.Obtener("modo_anadir_titulo"), t.Obtener("modo_anadir_detalle"));
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    LimpiarFormulario();
                    SetFormularioEditable(true, incluirEstado: false);
                    txtDni.Focus();
                    txtLogin.ReadOnly = true;
                    break;

                case Modo.Modificar:
                    SetMensaje(t.Obtener("modo_modificar_titulo"), t.Obtener("modo_modificar_detalle"));

                    var seleccionado    = ObtenerUsuarioSeleccionado();
                    bool puedeEditar    = sesion.TienePermiso(PatenteEnum06AV.EditarUsuarios);
                    bool esMiUsuario    = seleccionado?.Dni == sesion.UsuarioActual?.Dni;

                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);

                    txtDni.ReadOnly      = true;
                    txtApellido.ReadOnly = true;
                    txtNombre.ReadOnly   = true;
                    txtLogin.ReadOnly    = true;

                    if (puedeEditar)
                    {
                        // Puede editar cualquier usuario: email + rol
                        txtEmail.ReadOnly = false;
                        cmbRol.Enabled    = true;
                    }
                    else if (esMiUsuario)
                    {
                        // Solo puede editar su propio email, no el rol
                        txtEmail.ReadOnly = false;
                        cmbRol.Enabled    = false;
                    }
                    else
                    {
                        // Sin permiso y no es su propio usuario
                        txtEmail.ReadOnly = true;
                        cmbRol.Enabled    = false;
                    }

                    chkActivo.Enabled    = false;
                    chkBloqueado.Enabled = false;
                    grilla.Enabled       = false;
                    break;

                case Modo.ActivarDesactivar:
                    if (!sesion.TienePermiso(PatenteEnum06AV.ActDesactivarUsuarios))
                    {
                        ConfirmacionForm.MostrarInfo(
                            t.Obtener("sin_permiso_actdesact"),
                            titulo: t.Obtener("permiso_denegado"),
                            tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                            owner: this.FindForm());
                        return;
                    }
                    SetMensaje(t.Obtener("modo_actdesact_titulo"), t.Obtener("modo_actdesact_detalle"));
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    SetFormularioEditable(false, incluirEstado: false);
                    txtLogin.ReadOnly = true;
                    break;

                case Modo.Desbloquear:
                    if (!sesion.TienePermiso(PatenteEnum06AV.DesbloquearUsuarios))
                    {
                        ConfirmacionForm.MostrarInfo(
                            t.Obtener("sin_permiso_desbloquear"),
                            titulo: t.Obtener("permiso_denegado"),
                            tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                            owner: this.FindForm());
                        return;
                    }
                    SetMensaje(t.Obtener("modo_desbloquear_titulo"), t.Obtener("modo_desbloquear_detalle"));
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    SetFormularioEditable(false, incluirEstado: false);
                    txtLogin.ReadOnly = true;
                    break;
            }
        }

        private void SetMensaje(string titulo, string detalle)
        {
            txtMensaje.Text = titulo + Environment.NewLine + Environment.NewLine + detalle;
        }

        private void SetBotones(bool crear, bool desbloq, bool modif, bool actDesact,
                                bool aplicar, bool cancelar, bool radios)
        {
            HabilitarBoton(btnCrear, crear);
            HabilitarBoton(btnDesbloquear, desbloq);
            HabilitarBoton(btnModificar, modif);
            HabilitarBoton(btnActDesact, actDesact);
            HabilitarBoton(btnAplicar, aplicar);
            HabilitarBoton(btnCancelar, cancelar);
            radActivos.Enabled = radios;
            radTodos.Enabled = radios;
        }

        private void HabilitarBoton(Button b, bool habilitado)
        {
            b.Enabled = habilitado;
            if (!habilitado)
            {
                Tema.AplicarBotonDeshabilitado(b);
                return;
            }

            if (b == btnActDesact || b == btnDesbloquear)
            {
                Tema.AplicarBotonAcento(b);
            }
            else if (b == btnCancelar)
            {
                Tema.AplicarBotonSecundario(b);
            }
            else
            {
                Tema.AplicarBotonPrimario(b);
            }
        }

        private void SetFormularioEditable(bool editable, bool incluirEstado)
        {
            txtDni.ReadOnly = !editable;
            txtApellido.ReadOnly = !editable;
            txtNombre.ReadOnly = !editable;
            txtEmail.ReadOnly = !editable;
            cmbRol.Enabled = editable;
            chkBloqueado.Enabled = incluirEstado;
            chkActivo.Enabled = incluirEstado;
        }

        private void LimpiarFormulario()
        {
            txtDni.Text = "";
            txtApellido.Text = "";
            txtNombre.Text = "";
            txtEmail.Text = "";
            txtLogin.Text = "";

            // Seleccionar "UsuarioGeneral" por defecto; si no existe, el primero de la lista
            cmbRol.SelectedValue = "UsuarioGeneral";
            if (cmbRol.SelectedValue == null && cmbRol.Items.Count > 0)
                cmbRol.SelectedIndex = 0;

            chkBloqueado.Checked = false;
            chkActivo.Checked = true;
        }

        private void RecargarGrilla()
        {
            try
            {
                var todos = _usuariosSer.ObtenerTodos() ?? new List<Usuario06AV>();

                var roles = _rolesSer.ObtenerTodos();

                foreach (var usuario in todos)
                {
                    var rol = roles.FirstOrDefault(r => r.Id == usuario.IdRol);

                    if (rol != null)
                    {
                        usuario.RolDescripcion = rol.Descripcion;
                    }
                }

                IEnumerable<Usuario06AV> filtrados = todos;

                if (radActivos.Checked)
                {
                    filtrados = filtrados.Where(u => u.Activo);
                }

                _usuariosCargados = filtrados.ToList();

                grilla.DataSource = null;
                grilla.DataSource = _usuariosCargados;

                lblCantidad.Text = GestorIdioma06AV.Instancia.Obtener("numero_usuarios")
                                  + " " + _usuariosCargados.Count;

                PintarFilasInactivas();
            }
            catch (Exception ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_cargar_usuarios") + "\n" + ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this.FindForm());
            }
        }

        private void PintarFilasInactivas()
        {
            foreach (DataGridViewRow row in grilla.Rows)
            {
                var u = row.DataBoundItem as Usuario06AV;
                if (u == null) continue;
                if (!u.Activo)
                {
                    row.DefaultCellStyle.BackColor = Tema.PeligroSuave;
                    row.DefaultCellStyle.ForeColor = Tema.Peligro;
                }
                else if (u.Bloqueado)
                {
                    row.DefaultCellStyle.BackColor = Tema.AdvertenciaSuave;
                    row.DefaultCellStyle.ForeColor = Tema.Grafito900;
                }
            }
        }

        private void AplicarAlta()
        {
            var t = GestorIdioma06AV.Instancia;
            try
            {
                bool ok = _usuariosSer.CrearUsuario(
                    txtDni.Text.Trim(),
                    txtNombre.Text.Trim(),
                    txtApellido.Text.Trim(),
                    txtEmail.Text.Trim(),
                    cmbRol.SelectedValue?.ToString(), DniOperador());

                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("usuario_creado"),
                        titulo: t.Obtener("listo"),
                        owner: this.FindForm());

                    LimpiarFormulario();
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("error_crear_usuario"),
                        titulo: t.Obtener("aviso"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this.FindForm());
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("aviso"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private void AplicarModificacion()
        {
            var seleccionado = ObtenerUsuarioSeleccionado();
            if (seleccionado == null) return;
            seleccionado.Email = txtEmail.Text.Trim();
            seleccionado.IdRol = cmbRol.SelectedValue?.ToString();
            var t = GestorIdioma06AV.Instancia;

            try
            {
                bool ok = _usuariosSer.EditarUsuario(seleccionado, DniOperador());
                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("usuario_modificado"),
                        titulo: t.Obtener("listo"),
                        owner: this.FindForm());

                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("error_modificar_usuario"),
                        titulo: t.Obtener("aviso"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this.FindForm());
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("aviso"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private void AplicarToggleActivo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;
            var t = GestorIdioma06AV.Instancia;

            string accion = u.Activo ? t.Obtener("accion_desactivar") : t.Obtener("accion_reactivar");

            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_toggle_activo", accion, u.Apellido, u.Nombre),
                titulo: t.Obtener("confirmar_titulo"),
                tipo: u.Activo
                    ? ConfirmacionForm.TipoConfirmacion.Advertencia
                    : ConfirmacionForm.TipoConfirmacion.Pregunta,
                textoSi: u.Activo ? t.Obtener("si_desactivar") : t.Obtener("si_reactivar"),
                textoNo: t.Obtener("cancelar"),
                owner: this.FindForm());

            if (!confirmado) return;

            if (u.Dni == UsuarioSesion06AV.Instancia().UsuarioActual.Dni)
            {
                ConfirmacionForm.MostrarInfo(t.Obtener("no_desactivar_mismo"), owner: this.FindForm());
                return;
            }

            try
            {
                bool ok = u.Activo
                    ? _usuariosSer.DesactivarUsuario(u.Dni, DniOperador())
                    : _usuariosSer.ReactivarUsuario(u.Dni, DniOperador());

                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("operacion_exitosa"),
                        titulo: t.Obtener("listo"),
                        owner: this.FindForm());

                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("aviso"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private void AplicarDesbloqueo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;
            var t = GestorIdioma06AV.Instancia;

            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: t.Obtener("confirmar_desbloqueo", u.Apellido, u.Nombre),
                titulo: t.Obtener("confirmar_desbloqueo_titulo"),
                textoSi: t.Obtener("si_desbloquear"),
                textoNo: t.Obtener("cancelar"),
                owner: this.FindForm());

            if (!confirmado) return;

            try
            {
                bool ok = _usuariosSer.DesbloquearUsuario(u.Dni, DniOperador());
                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("usuario_desbloqueado"),
                        titulo: t.Obtener("listo"),
                        owner: this.FindForm());
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("aviso"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private string DniOperador()
        {
            var sesion = UsuarioSesion06AV.Instancia();
            return sesion?.UsuarioActual?.Dni;
        }

        private void UsuariosControl2_Load(object sender, EventArgs e)
        {
            CargarRoles();
            CambiarModo(Modo.Consulta);  // ya llama AplicarPermisosBotones() internamente
            RecargarGrilla();
        }

        private void radActivos_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspenderEventosRadio) return;
            if (radActivos.Checked) RecargarGrilla();
        }

        private void radTodos_CheckedChanged(object sender, EventArgs e)
        {
            if (_suspenderEventosRadio) return;
            if (radTodos.Checked) RecargarGrilla();
        }

        private void grilla_SelectionChanged(object sender, EventArgs e)
        {
            if (_modo == Modo.Anadir) return;
            var usuario = ObtenerUsuarioSeleccionado();
            if (usuario == null) return;

            txtDni.Text = usuario.Dni;
            txtApellido.Text = usuario.Apellido;
            txtNombre.Text = usuario.Nombre;
            txtEmail.Text = usuario.Email;
            txtLogin.Text = usuario.Login;
            chkActivo.Checked = usuario.Activo;
            chkBloqueado.Checked = usuario.Bloqueado;
            cmbRol.SelectedValue = usuario.IdRol;
        }

        private Usuario06AV ObtenerUsuarioSeleccionado()
        {
            if (grilla.CurrentRow == null) return null;
            return grilla.CurrentRow.DataBoundItem as Usuario06AV;
        }

        private void btnCrear_Click(object sender, EventArgs e) => CambiarModo(Modo.Anadir);

        private void btnModificar_Click(object sender, EventArgs e)
        {
            var t        = GestorIdioma06AV.Instancia;
            var sesion   = UsuarioSesion06AV.Instancia();
            var seleccionado = ObtenerUsuarioSeleccionado();

            if (seleccionado == null)
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("seleccionar_usuario"),
                    titulo: t.Obtener("aviso"),
                    owner: this.FindForm());
                return;
            }

            bool puedeEditar  = sesion.TienePermiso(PatenteEnum06AV.EditarUsuarios);
            bool esMiUsuario  = seleccionado.Dni == sesion.UsuarioActual?.Dni;

            // Puede entrar si tiene la patente de edición O si está editando su propio perfil
            if (!puedeEditar && !esMiUsuario)
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("sin_permiso_modificar"),
                    titulo: t.Obtener("permiso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
                return;
            }

            CambiarModo(Modo.Modificar);
        }

        private void btnActDesact_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            if (ObtenerUsuarioSeleccionado() == null)
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("seleccionar_usuario"),
                    titulo: t.Obtener("aviso"),
                    owner: this.FindForm());
                return;
            }
            CambiarModo(Modo.ActivarDesactivar);
        }

        private void btnDesbloquear_Click(object sender, EventArgs e)
        {
            var t = GestorIdioma06AV.Instancia;
            var u = ObtenerUsuarioSeleccionado();
            if (u == null)
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("seleccionar_usuario"),
                    titulo: t.Obtener("aviso"),
                    owner: this.FindForm());
                return;
            }
            if (!u.Bloqueado)
            {
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("usuario_no_bloqueado"),
                    titulo: t.Obtener("aviso"),
                    owner: this.FindForm());
                return;
            }
            CambiarModo(Modo.Desbloquear);
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
            CambiarModo(Modo.Consulta);
            RecargarGrilla();
        }

        private void btnAplicar_Click(object sender, EventArgs e)
        {
            switch (_modo)
            {
                case Modo.Consulta:
                    RecargarGrilla();
                    break;

                case Modo.Anadir:
                    AplicarAlta();
                    break;

                case Modo.Modificar:
                    AplicarModificacion();
                    break;

                case Modo.ActivarDesactivar:
                    AplicarToggleActivo();
                    break;

                case Modo.Desbloquear:
                    AplicarDesbloqueo();
                    break;
            }
        }
    }
}
