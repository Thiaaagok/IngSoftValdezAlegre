using IngSoftValdezAlegre.Common;
using SER;
using SER.Excepciones;
using System;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class FRMCambiarContrasenia : Form
    {
        private readonly string _dni;
        private readonly bool _esObligatorio;

        public bool ContraseniaCambiada { get; private set; } = false;

        public FRMCambiarContrasenia(string dni, bool esObligatorio = false)
        {
            InitializeComponent();
            AplicarTema();
            if (String.IsNullOrEmpty(dni))
                dni = UsuarioSesion06AV.Instancia().UsuarioActual.Dni;

            _dni = dni;
            _esObligatorio = esObligatorio;

            AplicarIdioma();

            if (_esObligatorio)
            {
                btnCancelar.Visible = false;
                this.ControlBox = false;
                this.Text = GestorIdioma06AV.Instancia.Obtener("cambiar_contrasenia") + " (obligatorio)";
                lblMensaje.ForeColor = Tema.Peligro;
                lblMensaje.Text = "Por seguridad, debés cambiar tu contraseña antes de continuar.";
            }
        }

        private void AplicarTema()
        {
            Tema.AplicarFormulario(this);
            BackColor = Tema.FondoElevado;
            pnlTitulo.BackColor = Tema.Grafito900;
            lblTitulo.BackColor = Tema.Grafito900;
            lblTitulo.ForeColor = Tema.TextoInvertido;
            lblTitulo.Font = Tema.FuenteTitulo;

            lblActual.ForeColor = Tema.Texto;
            lblNueva.ForeColor = Tema.Texto;
            lblRepetir.ForeColor = Tema.Texto;
            lblMensaje.ForeColor = Tema.Peligro;

            txtActual.BorderStyle = BorderStyle.FixedSingle;
            txtNueva.BorderStyle = BorderStyle.FixedSingle;
            txtRepetir.BorderStyle = BorderStyle.FixedSingle;

            Tema.AplicarBotonPrimario(btnAceptar);
            Tema.AplicarBotonSecundario(btnCancelar);
        }

        public void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            this.Text          = t.Obtener("cambiar_contrasenia");
            lblTitulo.Text     = t.Obtener("cambiar_contrasenia");
            lblActual.Text     = t.Obtener("contrasenia_actual");
            lblNueva.Text      = t.Obtener("nueva_contrasenia");
            lblRepetir.Text    = t.Obtener("repetir_contrasenia");
            btnAceptar.Text    = t.Obtener("aceptar");
            btnCancelar.Text   = t.Obtener("cancelar");
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            lblMensaje.ForeColor = Tema.Peligro;
            lblMensaje.Text = "";

            string actual = txtActual.Text;
            string nueva = txtNueva.Text;
            string repetir = txtRepetir.Text;

            if (string.IsNullOrWhiteSpace(actual) ||
                string.IsNullOrWhiteSpace(nueva) ||
                string.IsNullOrWhiteSpace(repetir))
            {
                lblMensaje.Text = "Completá los tres campos.";
                return;
            }

            if (nueva != repetir)
            {
                lblMensaje.Text = "La nueva contraseña y la repetición no coinciden.";
                return;
            }

            if (nueva == actual)
            {
                lblMensaje.Text = "La nueva contraseña debe ser distinta a la actual.";
                return;
            }

            try
            {
                var ser = new UsuariosBLL06AV();
                bool ok = ser.CambiarContraseña(_dni, actual, nueva);

                if (ok)
                {
                    ContraseniaCambiada = true;

                    if (_esObligatorio)
                    {
                        ConfirmacionForm.MostrarInfo(
                            "Contraseña actualizada correctamente.\nYa podés ingresar al sistema.",
                            "Listo");
                    }
                    else
                    {
                        ConfirmacionForm.MostrarInfo(
                            "Contraseña actualizada correctamente.\nDebés iniciar sesión nuevamente.",
                            "Listo");
          
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblMensaje.Text = "No se pudo actualizar la contraseña.";
                }
            }
            catch (ContraseniaInvalidaException)
            {
                lblMensaje.Text = "La contraseña actual es incorrecta.";
            }
            catch (UsuarioValidacionException ex)
            {
                lblMensaje.Text = ex.Message;
            }
            catch (UsuarioEstadoInvalidoException ex)
            {
                lblMensaje.Text = ex.Message;
            }
            catch (UsuarioAccesoDatosException)
            {
                lblMensaje.Text = "Error de conexión. Intentá de nuevo.";
            }
        }
    }
}
