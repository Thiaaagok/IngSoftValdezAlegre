using BE;
using IngSoftValdezAlegre.Common;
using Microsoft.VisualBasic;
using SER;
using SER.Excepciones;
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
    public partial class UsuariosControl : UserControl
    {
        private enum Modo
        {
            Consulta,
            Anadir,
            Modificar,
            ActivarDesactivar,
            Desbloquear
        }

        private readonly UsuariosSER06AV _usuariosSer = new UsuariosSER06AV();
        private readonly RolesSER06AV _rolesSer = new RolesSER06AV();

        private List<Usuario06AV> _usuariosCargados = new List<Usuario06AV>();
        private Modo _modo = Modo.Consulta;
        private bool _suspenderEventosRadio = false;

        public UsuariosControl()
        {
            InitializeComponent();
            ConfigurarColumnas();
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
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: "Error al cargar roles",
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this.FindForm());
            }
        }


        private void CambiarModo(Modo nuevo)
        {
            _modo = nuevo;

            switch (nuevo)
            {
                case Modo.Consulta:
                    SetMensaje("Modo Consulta",
                        "Unicamente para ver los usuario");
                    SetBotones(crear: true, desbloq: true, modif: true, actDesact: true,
                               aplicar: true, cancelar: false, radios: true);
                    SetFormularioEditable(false, incluirEstado: false);
                    grilla.Enabled = true;
                    break;

                case Modo.Anadir:
                    SetMensaje("Modo Añadir",
                        "Completá DNI, Apellido, Nombre, Email y Rol. El sistema genera el " +
                        "Login y una contraseña inicial. Presioná Aplicar para crear.");
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    LimpiarFormulario();
                    SetFormularioEditable(true, incluirEstado: false);
                    txtDni.Focus();
                    break;

                case Modo.Modificar:
                    SetMensaje(
                        "Modo Modificar",
                        "Solo se puede modificar el Email y el Rol del usuario.");

                    bool esAdmin = UsuarioSesion06AV.Instancia().TieneRol("Administrador");
                    var seleccionado = ObtenerUsuarioSeleccionado();
                    bool esMiPropioUsuario = seleccionado?.Dni == UsuarioSesion06AV.Instancia().UsuarioActual.Dni;

                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                        aplicar: true, cancelar: true, radios: false);

                    txtDni.ReadOnly = true;
                    txtApellido.ReadOnly = true;
                    txtNombre.ReadOnly = true;
                    txtLogin.ReadOnly = true;

                    if (esAdmin)
                    {
                        txtEmail.ReadOnly = false;
                        cmbRol.Enabled = true;
                    }
                    else if (esMiPropioUsuario)
                    {
                        txtEmail.ReadOnly = false;
                        cmbRol.Enabled = false;
                    }
                    else
                    {
                        txtEmail.ReadOnly = true;
                        cmbRol.Enabled = false;
                    }

                    chkActivo.Enabled = false;
                    chkBloqueado.Enabled = false;
                    grilla.Enabled = false;
                    break;

                case Modo.ActivarDesactivar:
                    SetMensaje("Modo Activar/Desactivar",
                        "Vas a alternar el estado Activo/Inactivo del usuario seleccionado. " +
                        "Presioná Aplicar para confirmar.");
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    SetFormularioEditable(false, incluirEstado: false);
                    break;

                case Modo.Desbloquear:
                    SetMensaje("Modo Desbloquear",
                        "Seleccioná un usuario bloqueado en la grilla y presioná Aplicar para desbloquearlo.");
                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                               aplicar: true, cancelar: true, radios: false);
                    SetFormularioEditable(false, incluirEstado: false);
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
            b.BackColor = habilitado ? Tema.Primario : Tema.Gris400;
        }

        private void SetFormularioEditable(bool editable, bool incluirEstado)
        {
            txtDni.ReadOnly = !editable;
            txtApellido.ReadOnly = !editable;
            txtNombre.ReadOnly = !editable;
            txtEmail.ReadOnly = !editable;
            txtLogin.ReadOnly = !editable;
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
            if (cmbRol.Items.Count > 0) cmbRol.SelectedIndex = 0;
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

                lblCantidad.Text =
                    "Número de usuarios: " + _usuariosCargados.Count;

                PintarFilasInactivas();
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(
                    "No se pudo cargar la lista de usuarios.\n" + ex.Message,
                    titulo: "Error",
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
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                    row.DefaultCellStyle.ForeColor = Tema.Peligro;
                }
                else if (u.Bloqueado)
                {
                    row.DefaultCellStyle.BackColor = Tema.Amber50;
                }
            }
        }

        private void AplicarAlta()
        {
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
                        "Usuario creado correctamente.",
                        titulo: "Listo",
                        owner: this.FindForm());

                    LimpiarFormulario();
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    ConfirmacionForm.MostrarInfo(
                        "No se pudo crear el usuario.",
                        titulo: "Aviso",
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this.FindForm());
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: "Aviso",
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

            try
            {
                bool ok = _usuariosSer.EditarUsuario(seleccionado, DniOperador());
                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        "Usuario modificado correctamente.",
                        titulo: "Listo",
                        owner: this.FindForm());

                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    ConfirmacionForm.MostrarInfo(
                        "No se pudo modificar el usuario.",
                        titulo: "Aviso",
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this.FindForm());
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: "Aviso",
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private void AplicarToggleActivo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;

            string accion = u.Activo ? "desactivar" : "reactivar";

            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: $"¿Confirmás {accion} al usuario {u.Apellido}, {u.Nombre}?",
                titulo: "Confirmar",
                tipo: u.Activo
                    ? ConfirmacionForm.TipoConfirmacion.Advertencia
                    : ConfirmacionForm.TipoConfirmacion.Pregunta,
                textoSi: u.Activo ? "Sí, desactivar" : "Sí, reactivar",
                textoNo: "Cancelar",
                owner: this.FindForm());

            if (!confirmado) return;

            if(u.Dni == UsuarioSesion06AV.Instancia().UsuarioActual.Dni)
            {
                ConfirmacionForm.MostrarInfo("No se puede desactivar a uno mismo");
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
                        "Operación realizada correctamente.",
                        titulo: "Listo",
                        owner: this.FindForm());

                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: "Aviso",
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
            }
        }

        private void AplicarDesbloqueo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;

            bool confirmado = ConfirmacionForm.Mostrar(
                mensaje: $"¿Desbloquear al usuario {u.Apellido}, {u.Nombre}?",
                titulo: "Confirmar desbloqueo",
                textoSi: "Sí, desbloquear",
                textoNo: "Cancelar",
                owner: this.FindForm());

            if (!confirmado) return;

            try
            {
                bool ok = _usuariosSer.DesbloquearUsuario(u.Dni, DniOperador());
                if (ok)
                {
                    ConfirmacionForm.MostrarInfo(
                        "Usuario desbloqueado correctamente.",
                        titulo: "Listo",
                        owner: this.FindForm());

                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: "Aviso",
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
            CambiarModo(Modo.Consulta);
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
            var seleccionado = ObtenerUsuarioSeleccionado();
            if (seleccionado == null)
            {
                ConfirmacionForm.MostrarInfo(
                    "Seleccioná un usuario en la grilla.",
                    titulo: "Aviso",
                    owner: this.FindForm());
                return;
            }

            bool esAdmin = UsuarioSesion06AV.Instancia().TieneRol("Administrador");
            bool esMiPropioUsuario = seleccionado.Dni == UsuarioSesion06AV.Instancia().UsuarioActual.Dni;

            if (!esAdmin && !esMiPropioUsuario)
            {
                ConfirmacionForm.MostrarInfo(
                    "No tenés permisos para modificar a otros usuarios.\nSolo podés modificar tu propio email.",
                    titulo: "Permiso denegado",
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this.FindForm());
                return;
            }

            CambiarModo(Modo.Modificar);
        }

        private void btnActDesact_Click(object sender, EventArgs e)
        {
            if (ObtenerUsuarioSeleccionado() == null)
            {
                ConfirmacionForm.MostrarInfo(
                    "Seleccioná un usuario en la grilla.",
                    titulo: "Aviso",
                    owner: this.FindForm());
                return;
            }
            CambiarModo(Modo.ActivarDesactivar);
        }

        private void btnDesbloquear_Click(object sender, EventArgs e)
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null)
            {
                ConfirmacionForm.MostrarInfo(
                    "Seleccioná un usuario en la grilla.",
                    titulo: "Aviso",
                    owner: this.FindForm());
                return;
            }
            if (!u.Bloqueado)
            {
                ConfirmacionForm.MostrarInfo(
                    "El usuario seleccionado no está bloqueado.",
                    titulo: "Aviso",
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
