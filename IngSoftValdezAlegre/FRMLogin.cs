using BE;
using IngSoftValdezAlegre.Common;
using IngSoftValdezAlegre.Controles;
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

namespace IngSoftValdezAlegre
{
    public partial class FRMLogin : Form
    {
        public FRMLogin()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }

        private void IniciarSesionBTN_Click(object sender, EventArgs e)
        {
            string login = LoginTextBox.Text;
            string contrasenia = ContraseniaTextBox.Text;

            try
            {
                UsuariosSER06AV SER = new UsuariosSER06AV();
                Usuario06AV usuario = SER.Login(login, contrasenia);

                // Si tiene que cambiar contraseña (primer login o post-desbloqueo)
                if (usuario.DebeCambiarContrasenia)
                {
                    ConfirmacionForm.MostrarInfo(
                        "Por seguridad, debés cambiar tu contraseña antes de continuar.",
                        titulo: "Cambio de contraseña requerido",
                        tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                        owner: this);

                    using (var f = new FRMCambiarContrasenia(usuario.Dni, esObligatorio: true))
                    {
                        f.ShowDialog(this);

                        if (!f.ContraseniaCambiada)
                        {
                            // No cambió → no lo dejamos entrar
                            UsuarioSesion06AV.Instancia().CerrarSesion();
                            ConfirmacionForm.MostrarInfo(
                                "Debés cambiar tu contraseña para poder ingresar al sistema.",
                                titulo: "Acceso denegado",
                                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                                owner: this);
                            return;
                        }
                    }
                }

                FRMMain formPrincipal = new FRMMain();
                formPrincipal.Show();
                this.Hide();
            }
            catch (UsuarioValidacionException ex)
            {
                MessageBox.Show(ex.Message, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (UsuarioNoEncontradoException)
            {
                MessageBox.Show("Login o contraseña incorrectos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ContraseniaInvalidaException)
            {
                MessageBox.Show("Login o contraseña incorrectos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Bloqueado")
            {
                MessageBox.Show("El usuario esta bloqueado. Contacte a un administrador.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Inactivo")
            {
                MessageBox.Show("El usuario esta inactivo. Contacte a un administrador.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UsuarioAccesoDatosException)
            {
                MessageBox.Show("Error de conexion. Intenta mas tarde.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CerrarBTN_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

    }
}
