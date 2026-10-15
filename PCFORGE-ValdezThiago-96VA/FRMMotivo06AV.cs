using PCFORGE_ValdezThiago_96VA.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PCFORGE_ValdezThiago_96VA
{
    public class FRMMotivo06AV : Form
    {
        private const int Minimo = 5;
        private const int Maximo = 250;

        private readonly Label _lblMensaje, _lblMotivo, _lblContador;
        private readonly TextBox _txtMotivo;
        private readonly Button _btnAceptar, _btnCancelar;

        public string Motivo => _txtMotivo.Text.Trim();

        private FRMMotivo06AV(string titulo, string mensaje, string textoAceptar, bool peligro)
        {
            var t = GestorIdioma06AV.Instancia;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 268);
            Text = titulo;

            _lblMensaje = new Label
            {
                Location = new Point(18, 16), Size = new Size(424, 54), AutoSize = false, Text = mensaje
            };
            _lblMotivo = new Label { Location = new Point(18, 76), AutoSize = true, Text = t.Obtener("pcf_motivo") };
            _txtMotivo = new TextBox
            {
                Location = new Point(18, 96), Size = new Size(424, 84), Multiline = true,
                ScrollBars = ScrollBars.Vertical, MaxLength = Maximo
            };
            _lblContador = new Label { Location = new Point(18, 184), Size = new Size(424, 18), AutoSize = false, TextAlign = ContentAlignment.MiddleRight };
            _txtMotivo.TextChanged += (s, e) => ActualizarContador();

            _btnAceptar = new Button { Size = new Size(150, 34), Location = new Point(292, 218), FlatStyle = FlatStyle.Flat, Text = textoAceptar };
            _btnCancelar = new Button { Size = new Size(110, 34), Location = new Point(174, 218), FlatStyle = FlatStyle.Flat, Text = t.Obtener("cancelar") };
            _btnAceptar.Click += (s, e) => Aceptar();
            _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { _lblMensaje, _lblMotivo, _txtMotivo, _lblContador, _btnAceptar, _btnCancelar });
            CancelButton = _btnCancelar;

            Tema.AplicarFormulario(this);
            _lblMensaje.Font = Tema.FuenteRegular;
            _lblMensaje.ForeColor = Tema.Texto;
            _lblMotivo.Font = Tema.FuenteMini;
            _lblMotivo.ForeColor = Tema.TextoSuave;
            _lblContador.Font = Tema.FuenteMini;
            Tema.AplicarEntrada(_txtMotivo);
            if (peligro) Tema.AplicarBotonPeligro(_btnAceptar); else Tema.AplicarBotonPrimario(_btnAceptar);
            Tema.AplicarBotonSecundario(_btnCancelar);
            ActualizarContador();
        }

        private void ActualizarContador()
        {
            int n = Motivo.Length;
            _lblContador.Text = n + " / " + Maximo;
            _lblContador.ForeColor = n < Minimo ? Tema.Advertencia : Tema.TextoSuave;
        }

        private void Aceptar()
        {
            if (Motivo.Length < Minimo)
            {
                ConfirmacionForm.MostrarInfo(GestorIdioma06AV.Instancia.Obtener("pcf_motivo_corto", Minimo),
                    GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                _txtMotivo.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        public static string Pedir(IWin32Window owner, string titulo, string mensaje, string textoAceptar, bool peligro = true)
        {
            using (var f = new FRMMotivo06AV(titulo, mensaje, textoAceptar, peligro))
            {
                var r = owner != null ? f.ShowDialog(owner) : f.ShowDialog();
                return r == DialogResult.OK ? f.Motivo : null;
            }
        }
    }
}
