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
    public partial class Login : Form
    {
        public Login()
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
                SER.Login(login, contrasenia);

                MainForm formPrincipal = new MainForm();
                formPrincipal.FormClosed += (s, args) =>
                {
                    //if (formPrincipal.CerrarSesionSolicitado)
                    //{
                    //    LoginTextBox.Text = string.Empty;
                    //    ContraseniaTextBox.Text = string.Empty;
                    //    LoginTextBox.Focus();
                    //    this.Show();
                    //}
                    //else
                    //{
                    //    this.Close();
                    //}
                };
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
