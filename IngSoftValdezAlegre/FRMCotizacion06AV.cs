using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// Diálogo modal para capturar el costo ofrecido por el proveedor y las
    /// condiciones/observaciones de una cotización (RFN2). El gerente después
    /// aprueba o desaprueba viendo ese precio.
    /// </summary>
    public class FRMCotizacion06AV : Form
    {
        public decimal Costo { get; private set; }
        public string Condiciones { get; private set; }

        private Label lblCosto, lblCond;
        private TextBox txtCosto, txtCond;
        private Button btnAceptar, btnCancelar;

        public FRMCotizacion06AV()
        {
            ConstruirUI();
            Tema.AplicarFormulario(this);
            Tema.AplicarBotonPrimario(btnAceptar);
            Tema.AplicarBotonSecundario(btnCancelar);
            AplicarIdioma();
        }

        private void ConstruirUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(440, 250);

            lblCosto = new Label { AutoSize = true };
            lblCond = new Label { AutoSize = true };
            txtCosto = new TextBox { Width = 260, Text = "0" };
            txtCond = new TextBox { Width = 260, Height = 90, Multiline = true, ScrollBars = ScrollBars.Vertical };

            var tabla = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 16, 16, 8), AutoSize = true
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            void Fila(Label l, Control c)
            {
                int r = tabla.RowCount; tabla.RowCount = r + 1;
                tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                l.Anchor = AnchorStyles.Left; l.Margin = new Padding(3, 9, 6, 3);
                c.Anchor = AnchorStyles.Left; c.Margin = new Padding(3, 6, 3, 6);
                Tema.AplicarEntrada(c);
                tabla.Controls.Add(l, 0, r); tabla.Controls.Add(c, 1, r);
            }
            Fila(lblCosto, txtCosto);
            Fila(lblCond, txtCond);

            btnAceptar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0) };
            btnCancelar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0) };
            btnAceptar.Click += (s, e) => Aceptar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 10, 12, 0)
            };
            barra.Controls.Add(btnAceptar);
            barra.Controls.Add(btnCancelar);

            Controls.Add(tabla);
            Controls.Add(barra);
            AcceptButton = btnAceptar;
            CancelButton = btnCancelar;
        }

        private void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            Text = t.Obtener("pcf_cotizacion");
            lblCosto.Text = t.Obtener("pcf_costo") + ":";
            lblCond.Text = t.Obtener("pcf_condiciones") + ":";
            btnAceptar.Text = t.Obtener("aceptar");
            btnCancelar.Text = t.Obtener("cancelar");
        }

        private void Aceptar()
        {
            if (!decimal.TryParse(txtCosto.Text.Trim(), out decimal costo) || costo < 0)
            {
                ConfirmacionForm.MostrarInfo("El costo debe ser un número válido y no negativo.",
                    GestorIdioma06AV.Instancia.Obtener("aviso"), ConfirmacionForm.TipoConfirmacion.Advertencia, this);
                return;
            }
            Costo = costo;
            Condiciones = txtCond.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
