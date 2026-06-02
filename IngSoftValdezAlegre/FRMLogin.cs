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
            AplicarTema();
            AplicarIdioma();
        }

        private void AplicarTema()
        {
            Tema.AplicarFormulario(this);

            BackColor = Tema.FondoApp;
            panel1.BackColor = Tema.Grafito900;

            Usuario.BackColor = Tema.FondoApp;
            Usuario.ForeColor = Tema.Texto;
            Usuario.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
            materialLabel2.BackColor = Tema.FondoApp;
            materialLabel2.ForeColor = Tema.Texto;
            materialLabel2.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);

            LoginTextBox.BackColor = Tema.FondoElevado;
            LoginTextBox.ForeColor = Tema.Texto;
            LoginTextBox.Font = new Font("Segoe UI", 13f, FontStyle.Regular);
            LoginTextBox.BorderStyle = BorderStyle.FixedSingle;
            ContraseniaTextBox.BackColor = Tema.FondoElevado;
            ContraseniaTextBox.ForeColor = Tema.Texto;
            ContraseniaTextBox.Font = new Font("Segoe UI", 13f, FontStyle.Regular);
            ContraseniaTextBox.BorderStyle = BorderStyle.FixedSingle;

            Tema.AplicarBotonPrimario(IniciarSesionBTN);
            Tema.AplicarBotonAcento(CerrarBTN);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            this.Text                  = t.Obtener("login");
            Usuario.Text               = t.Obtener("usuario_label");
            materialLabel2.Text        = t.Obtener("contrasenia_label");
            IniciarSesionBTN.Text      = t.Obtener("iniciar_sesion");
            CerrarBTN.Text             = t.Obtener("cerrar").ToUpperInvariant();
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
                UsuariosBLL06AV SER = new UsuariosBLL06AV();
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

                        // Cambió la contraseña → cerramos sesión y lo obligamos a loguearse de nuevo
                        UsuarioSesion06AV.Instancia().CerrarSesion();
                        ConfirmacionForm.MostrarInfo(
                            "Tu contraseña fue actualizada. Iniciá sesión nuevamente con tu nueva contraseña.",
                            titulo: "Contraseña actualizada",
                            tipo: ConfirmacionForm.TipoConfirmacion.Info,
                            owner: this);

                        LoginTextBox.Clear();
                        ContraseniaTextBox.Clear();
                        LoginTextBox.Focus();
                        return;
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
