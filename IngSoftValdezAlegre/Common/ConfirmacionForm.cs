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

        private static readonly Color AzulPrincipal = Color.FromArgb(40, 103, 178);
        private static readonly Color RojoAdvertencia = Color.FromArgb(200, 50, 60);

        public ConfirmacionForm()
        {
            InitializeComponent();
            btnSi.DialogResult = DialogResult.Yes;
            btnNo.DialogResult = DialogResult.No;
            this.AcceptButton = btnSi;
            this.CancelButton = btnNo;
            btnSi.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };
            btnNo.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };
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
                f.btnSi.Anchor = AnchorStyles.Top | AnchorStyles.Right;

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
                    colorHeader = RojoAdvertencia;
                    btnSi.BackColor = RojoAdvertencia;
                    break;
                case TipoConfirmacion.Info:
                    colorHeader = AzulPrincipal;
                    btnSi.BackColor = AzulPrincipal;
                    break;
                case TipoConfirmacion.Error:
                    colorHeader = RojoAdvertencia;
                    btnSi.BackColor = RojoAdvertencia;
                    break;
                default:
                    colorHeader = AzulPrincipal;
                    btnSi.BackColor = AzulPrincipal;
                    break;
            }
        }
    }
}
