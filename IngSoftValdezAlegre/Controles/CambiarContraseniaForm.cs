using SER;
using SER.Excepciones;
using System;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Controles
{
    public partial class CambiarContraseniaForm : Form
    {
        private readonly string _dni;

        public CambiarContraseniaForm(string dni)
        {
            InitializeComponent();
            _dni = dni;
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
                var ser = new UsuariosSER06AV();
                bool ok = ser.CambiarContraseña(_dni, actual, nueva);

                if (ok)
                {
                    MessageBox.Show("Contraseña actualizada correctamente.",
                        "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
