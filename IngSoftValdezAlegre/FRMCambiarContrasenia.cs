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

            // Observer: suscribirse al cambio de idioma
            GestorIdioma06AV.Instancia.IdiomaChanged += AplicarIdioma;
            FormClosed += (s, e) => GestorIdioma06AV.Instancia.IdiomaChanged -= AplicarIdioma;

            if (_esObligatorio)
            {
                var t = GestorIdioma06AV.Instancia;
                btnCancelar.Visible = false;
                this.ControlBox = false;
                this.Text = t.Obtener("cambiar_contrasenia") + t.Obtener("cambiar_contrasenia_obligatorio_sufijo");
                lblMensaje.ForeColor = Tema.Peligro;
                lblMensaje.Text = t.Obtener("cambiar_pass_obligatorio_msg");
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
            var t = GestorIdioma06AV.Instancia;
            lblMensaje.ForeColor = Tema.Peligro;
            lblMensaje.Text = "";

            string actual = txtActual.Text;
            string nueva = txtNueva.Text;
            string repetir = txtRepetir.Text;

            if (string.IsNullOrWhiteSpace(actual) ||
                string.IsNullOrWhiteSpace(nueva) ||
                string.IsNullOrWhiteSpace(repetir))
            {
                lblMensaje.Text = t.Obtener("completar_tres_campos");
                return;
            }

            if (nueva != repetir)
            {
                lblMensaje.Text = t.Obtener("pass_no_coincide");
                return;
            }

            if (nueva == actual)
            {
                lblMensaje.Text = t.Obtener("val_contrasenias_iguales");
                return;
            }

            try
            {
                var ser = new UsuariosBLL06AV();
                bool ok = ser.CambiarContraseña(_dni, actual, nueva);

                if (ok)
                {
                    ContraseniaCambiada = true;

                    ConfirmacionForm.MostrarInfo(
                        _esObligatorio
                            ? t.Obtener("pass_actualizada_obligatorio")
                            : t.Obtener("pass_actualizada_voluntario"),
                        t.Obtener("listo"));

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblMensaje.Text = t.Obtener("error_actualizar_pass");
                }
            }
            catch (ContraseniaInvalidaException)
            {
                lblMensaje.Text = t.Obtener("pass_actual_incorrecta");
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
                lblMensaje.Text = t.Obtener("error_conexion_reintentar");
            }
        }
    }
}
