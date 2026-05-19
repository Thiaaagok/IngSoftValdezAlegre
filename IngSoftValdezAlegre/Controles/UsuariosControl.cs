using BE;
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

        // -------------------- Carga inicial --------------------

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
                MessageBox.Show(ex.Message);
            }
        }

        // -------------------- Manejo de modos --------------------

        private void CambiarModo(Modo nuevo)
        {
            _modo = nuevo;

            switch (nuevo)
            {
                case Modo.Consulta:
                    SetMensaje("Modo Consulta",
                        "Unicamente para manera de ver");
                    SetBotones(crear: true, desbloq: true, modif: true, actDesact: true,
                               aplicar: true, cancelar: false, radios: true);
                    SetFormularioEditable(false, incluirEstado: false);
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

                    SetBotones(crear: false, desbloq: false, modif: false, actDesact: false,
                        aplicar: true, cancelar: true, radios: false);

                    txtDni.ReadOnly = true;
                    txtApellido.ReadOnly = true;
                    txtNombre.ReadOnly = true;
                    txtLogin.ReadOnly = true;

                    txtEmail.ReadOnly = false;
                    cmbRol.Enabled = true;

                    chkActivo.Enabled = false;
                    chkBloqueado.Enabled = false;

                    break;

                case Modo.ActivarDesactivar:
                    SetMensaje("Modo Eliminar",
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

        // -------------------- Carga de grilla --------------------

        private void RecargarGrilla()
        {
            try
            {
                var todos = _usuariosSer.ObtenerTodos()
                             ?? new List<Usuario06AV>();

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
                MessageBox.Show(
                    "No se pudo cargar la lista de usuarios.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                    MessageBox.Show("Usuario creado.", "Listo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarFormulario();
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    MessageBox.Show("No se pudo crear el usuario.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (UsuarioException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AplicarModificacion()
        {
            var seleccionado = ObtenerUsuarioSeleccionado();
            if (seleccionado == null) return;

            seleccionado.Nombre = txtNombre.Text.Trim();
            seleccionado.Apellido = txtApellido.Text.Trim();
            seleccionado.Email = txtEmail.Text.Trim();
            seleccionado.IdRol = cmbRol.SelectedValue?.ToString();

            try
            {
                bool ok = _usuariosSer.EditarUsuario(seleccionado, DniOperador());
                if (ok)
                {
                    MessageBox.Show("Usuario modificado.", "Listo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
                else
                {
                    MessageBox.Show("No se pudo modificar el usuario.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (UsuarioException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AplicarToggleActivo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;

            string accion = u.Activo ? "desactivar" : "reactivar";
            var resp = MessageBox.Show($"¿Confirmás {accion} al usuario {u.Apellido}, {u.Nombre}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resp != DialogResult.Yes) return;

            try
            {
                bool ok = u.Activo
                    ? _usuariosSer.DesactivarUsuario(u.Dni, DniOperador())
                    : _usuariosSer.ReactivarUsuario(u.Dni, DniOperador());

                if (ok)
                {
                    MessageBox.Show("Listo.", "OK",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AplicarDesbloqueo()
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null) return;

            var resp = MessageBox.Show($"¿Desbloquear al usuario {u.Apellido}, {u.Nombre}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resp != DialogResult.Yes) return;

            try
            {
                bool ok = _usuariosSer.DesbloquearUsuario(u.Dni, DniOperador());
                if (ok)
                {
                    MessageBox.Show("Usuario desbloqueado.", "Listo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CambiarModo(Modo.Consulta);
                    RecargarGrilla();
                }
            }
            catch (UsuarioException ex)
            {
                MessageBox.Show(ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (_modo == Modo.Anadir) return; // en alta no queremos sobrescribir lo que está escribiendo
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
            if (ObtenerUsuarioSeleccionado() == null)
            {
                MessageBox.Show("Seleccioná un usuario en la grilla.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CambiarModo(Modo.Modificar);
        }

        private void btnActDesact_Click(object sender, EventArgs e)
        {
            if (ObtenerUsuarioSeleccionado() == null)
            {
                MessageBox.Show("Seleccioná un usuario en la grilla.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CambiarModo(Modo.ActivarDesactivar);
        }

        private void btnDesbloquear_Click(object sender, EventArgs e)
        {
            var u = ObtenerUsuarioSeleccionado();
            if (u == null)
            {
                MessageBox.Show("Seleccioná un usuario en la grilla.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!u.Bloqueado)
            {
                MessageBox.Show("El usuario seleccionado no está bloqueado.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
