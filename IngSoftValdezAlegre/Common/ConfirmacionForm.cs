using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IngSoftValdezAlegre.Common
{
    public partial class ConfirmacionForm : Form
    {
        public enum TipoConfirmacion
        {
            Pregunta,
            Advertencia,
            Info,
            Error
        }

        public ConfirmacionForm()
        {
            InitializeComponent();
            AplicarTemaBase();
            btnSi.DialogResult = DialogResult.Yes;
            btnNo.DialogResult = DialogResult.No;
            this.AcceptButton = btnSi;
            this.CancelButton = btnNo;
            btnSi.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };
            btnNo.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
        }

        /// <summary>
        /// El ajuste de botones se hace acá, cuando el formulario ya se mostró y aplicó
        /// el auto-escalado por fuente/DPI. Si se hiciera antes de ShowDialog, en pantallas
        /// con escala (125%/150%) el escalado posterior movía los botones y se superponían.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AjustarBotones();
        }

        private void AplicarTemaBase()
        {
            Tema.AplicarFormulario(this);
            BackColor = Tema.FondoElevado;
            lblMensaje.ForeColor = Tema.Texto;
            lblMensaje.Font = Tema.FuenteRegular;
            lblTitulo.ForeColor = Tema.TextoInvertido;
            lblTitulo.Font = Tema.FuenteTitulo;
            Tema.AplicarBotonSecundario(btnNo);
        }

        public static bool Mostrar(
            string mensaje,
            string titulo = "Confirmar",
            TipoConfirmacion tipo = TipoConfirmacion.Pregunta,
            string textoSi = "Aceptar",
            string textoNo = "Cancelar",
            IWin32Window owner = null)
        {
            using (var f = new ConfirmacionForm())
            {
                f.Text = titulo;
                f.lblTitulo.Text = titulo;
                f.lblMensaje.Text = mensaje;
                f.btnSi.Text = textoSi;
                f.btnNo.Text = textoNo;
                f.AplicarTipo(tipo);

                var r = owner != null ? f.ShowDialog(owner) : f.ShowDialog();
                return r == DialogResult.Yes;
            }
        }

        public static void MostrarInfo(
            string mensaje,
            string titulo = "Información",
            TipoConfirmacion tipo = TipoConfirmacion.Info,
            IWin32Window owner = null)
        {
            using (var f = new ConfirmacionForm())
            {
                f.Text = titulo;
                f.lblTitulo.Text = titulo;
                f.lblMensaje.Text = mensaje;

                f.btnNo.Visible = false;
                f.btnNo.Enabled = false;

                f.btnSi.Text = "Aceptar";

                f.AplicarTipo(tipo);

                if (owner != null) f.ShowDialog(owner); else f.ShowDialog();
            }
        }

        private void AplicarTipo(TipoConfirmacion tipo)
        {
            Color colorHeader;
            switch (tipo)
            {
                case TipoConfirmacion.Advertencia:
                    colorHeader = Tema.Advertencia;
                    Tema.AplicarBotonAcento(btnSi);
                    break;
                case TipoConfirmacion.Info:
                    colorHeader = Tema.Primario;
                    Tema.AplicarBotonPrimario(btnSi);
                    break;
                case TipoConfirmacion.Error:
                    colorHeader = Tema.Peligro;
                    Tema.AplicarBotonPeligro(btnSi);
                    break;
                default:
                    colorHeader = Tema.Primario;
                    Tema.AplicarBotonPrimario(btnSi);
                    break;
            }

            pnlTitulo.BackColor = colorHeader;
            lblTitulo.BackColor = colorHeader;
        }

        /// <summary>
        /// Ajusta el ancho de los botones al texto que tienen (para que no se corte,
        /// p. ej. "Elegir archivo y restaurar"), ensancha el formulario si hace falta
        /// y centra el grupo de botones dejando el "Sí/Confirmar" a la derecha.
        /// </summary>
        private void AjustarBotones()
        {
            const int margen = 40;       // margen a cada lado del formulario
            const int separacion = 12;   // espacio entre los dos botones
            const int minAncho = 100;    // ancho mínimo de un botón
            const int padding = 30;      // relleno horizontal dentro del botón

            btnSi.AutoSize = false;
            btnNo.AutoSize = false;

            int anchoSi = Math.Max(minAncho,
                TextRenderer.MeasureText(btnSi.Text, btnSi.Font).Width + padding);
            btnSi.Width = anchoSi;

            int anchoNo = 0;
            if (btnNo.Visible)
            {
                anchoNo = Math.Max(minAncho,
                    TextRenderer.MeasureText(btnNo.Text, btnNo.Font).Width + padding);
                btnNo.Width = anchoNo;
            }

            // Ancho total que ocupan los botones (con separación si hay dos).
            int anchoGrupo = anchoSi + (btnNo.Visible ? anchoNo + separacion : 0);

            // Ensanchar el formulario si el grupo no entra con sus márgenes.
            int anchoNecesario = anchoGrupo + margen * 2;
            if (ClientSize.Width < anchoNecesario)
                ClientSize = new System.Drawing.Size(anchoNecesario, ClientSize.Height);

            // El mensaje aprovecha el nuevo ancho.
            lblMensaje.Width = ClientSize.Width - lblMensaje.Left - margen;

            // Alinear a la derecha: btnSi (confirmar) pegado al margen derecho y btnNo
            // (cancelar) a su izquierda, con separación. Así nunca se superponen.
            int y = btnSi.Top;
            int derecha = ClientSize.Width - margen;
            btnSi.Location = new System.Drawing.Point(derecha - anchoSi, y);
            if (btnNo.Visible)
                btnNo.Location = new System.Drawing.Point(btnSi.Left - separacion - anchoNo, y);
        }
    }
}
