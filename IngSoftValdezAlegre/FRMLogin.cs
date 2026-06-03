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

                // Después del login el idioma ya está cargado → refrescamos labels
                AplicarIdioma();

                var t = GestorIdioma06AV.Instancia;

                // Si tiene que cambiar contraseña (primer login o post-desbloqueo)
                if (usuario.DebeCambiarContrasenia)
                {
                    ConfirmacionForm.MostrarInfo(
                        t.Obtener("login_cambiar_pass_requerido"),
                        titulo: t.Obtener("cambio_pass_requerido_titulo"),
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
                                t.Obtener("login_pass_obligatoria"),
                                titulo: t.Obtener("acceso_denegado"),
                                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                                owner: this);
                            return;
                        }

                        // Cambió la contraseña → cerramos sesión y lo obligamos a loguearse de nuevo
                        UsuarioSesion06AV.Instancia().CerrarSesion();
                        ConfirmacionForm.MostrarInfo(
                            t.Obtener("login_pass_actualizada"),
                            titulo: t.Obtener("pass_actualizada_titulo"),
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
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    ex.Message,
                    titulo: t.Obtener("validacion"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                    owner: this);
            }
            catch (UsuarioNoEncontradoException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("login_o_pass_incorrectos"),
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (ContraseniaInvalidaException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("login_o_pass_incorrectos"),
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Bloqueado")
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("usuario_bloqueado_contacte"),
                    titulo: t.Obtener("acceso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioEstadoInvalidoException ex) when (ex.EstadoActual == "Inactivo")
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("usuario_inactivo_contacte"),
                    titulo: t.Obtener("acceso_denegado"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (UsuarioAccesoDatosException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_conexion_tarde"),
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
        }

        private void CerrarBTN_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

    }
}
