using BLL;
using PCFORGE_ValdezThiago_96VA.Common;
using PCFORGE_ValdezThiago_96VA.Controles;
using SER;
using SER.Excepciones;
using SER.Integridad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public partial class FRMLogin : Form
    {
        public FRMLogin()
        {
            InitializeComponent();
            AplicarTema();
            AplicarIdioma();

            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            HabilitarArrastreVentana(this);
            HabilitarArrastreVentana(panel1);
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

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
                var integridad = new IntegridadBLL06AV();
                ResultadoVerificacion06AV revision = null;
                try { revision = integridad.Verificar(); }
                catch { revision = null; }

                if (revision != null && revision.SinLineaBase)
                {
                    try { integridad.Recalcular(); } catch { }
                }
                else if (revision != null && !revision.EsConsistente)
                {
                    var ti = GestorIdioma06AV.Instancia;

                    if (new UsuariosBLL06AV().TienePatente(login, contrasenia, PatenteEnum06AV.RepararIntegridad))
                    {
                        using (var frm = new FRMReparacionDV(revision))
                            frm.ShowDialog(this);

                        LoginTextBox.Clear();
                        ContraseniaTextBox.Clear();
                        LoginTextBox.Focus();
                        return;
                    }

                    ConfirmacionForm.MostrarInfo(
                        ti.Obtener("login_inconsistencia_usuario"),
                        titulo: ti.Obtener("dv_titulo"),
                        tipo: ConfirmacionForm.TipoConfirmacion.Error,
                        owner: this);
                    Close();
                    Application.Exit();
                    return;
                }

                UsuariosBLL06AV SER = new UsuariosBLL06AV();
                Usuario06AV usuario = SER.Login(login, contrasenia);

                AplicarIdioma();

                var t = GestorIdioma06AV.Instancia;

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
                            UsuarioSesion06AV.Instancia().CerrarSesion();
                            ConfirmacionForm.MostrarInfo(
                                t.Obtener("login_pass_obligatoria"),
                                titulo: t.Obtener("acceso_denegado"),
                                tipo: ConfirmacionForm.TipoConfirmacion.Advertencia,
                                owner: this);
                            return;
                        }

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
            catch (SesionActivaException)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("sesion_activa_bloqueo"),
                    titulo: t.Obtener("acceso_denegado"),
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
            catch (UsuarioAccesoDatosException ex)
            {
                var t = GestorIdioma06AV.Instancia;

                Exception real = ex;
                while (real.InnerException != null) real = real.InnerException;

                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_conexion_tarde") + "\n\n" + real.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (InvalidOperationException ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_permisos_inconsistentes") + "\n\n" + ex.Message,
                    titulo: t.Obtener("error"),
                    tipo: ConfirmacionForm.TipoConfirmacion.Error,
                    owner: this);
            }
            catch (Exception ex)
            {
                var t = GestorIdioma06AV.Instancia;
                ConfirmacionForm.MostrarInfo(
                    t.Obtener("error_conexion_tarde") + "\n\n" + ex.Message,
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
