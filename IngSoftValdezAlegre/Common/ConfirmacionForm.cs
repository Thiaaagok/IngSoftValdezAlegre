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

        private void AjustarBotones()
        {
            const int margen = 40;
            const int separacion = 12;
            const int minAncho = 100;
            const int padding = 30;

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

            int anchoGrupo = anchoSi + (btnNo.Visible ? anchoNo + separacion : 0);

            int anchoNecesario = anchoGrupo + margen * 2;
            if (ClientSize.Width < anchoNecesario)
                ClientSize = new System.Drawing.Size(anchoNecesario, ClientSize.Height);

            lblMensaje.Width = ClientSize.Width - lblMensaje.Left - margen;

            int y = btnSi.Top;
            int derecha = ClientSize.Width - margen;
            btnSi.Location = new System.Drawing.Point(derecha - anchoSi, y);
            if (btnNo.Visible)
                btnNo.Location = new System.Drawing.Point(btnSi.Left - separacion - anchoNo, y);
        }
    }
}
