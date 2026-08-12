using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    public class FRMNuevoProveedor06AV : Form
    {
        private readonly ProveedoresBLL06AV _bll = new ProveedoresBLL06AV();

        public Proveedor06AV ProveedorCreado { get; private set; }

        private Label lblNombre, lblCuit, lblEmail, lblTel, lblDir;
        private TextBox txtNombre, txtCuit, txtEmail, txtTel, txtDir;
        private Button btnGuardar, btnCancelar;

        public FRMNuevoProveedor06AV()
        {
            ConstruirUI();
            Tema.AplicarFormulario(this);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonSecundario(btnCancelar);
            AplicarIdioma();
        }

        private void ConstruirUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 280);

            lblNombre = new Label(); lblCuit = new Label(); lblEmail = new Label();
            lblTel = new Label(); lblDir = new Label();
            txtNombre = new TextBox { Width = 260 };
            txtCuit = new TextBox { Width = 260 };
            txtEmail = new TextBox { Width = 260 };
            txtTel = new TextBox { Width = 260 };
            txtDir = new TextBox { Width = 260 };

            var tabla = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 16, 16, 8),
                AutoSize = true
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            void Fila(Label l, Control c)
            {
                int r = tabla.RowCount; tabla.RowCount = r + 1;
                tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                l.AutoSize = true; l.Anchor = AnchorStyles.Left; l.Margin = new Padding(3, 9, 6, 3);
                c.Anchor = AnchorStyles.Left; c.Margin = new Padding(3, 6, 3, 6);
                Tema.AplicarEntrada(c);
                tabla.Controls.Add(l, 0, r); tabla.Controls.Add(c, 1, r);
            }
            Fila(lblNombre, txtNombre);
            Fila(lblCuit, txtCuit);
            Fila(lblEmail, txtEmail);
            Fila(lblTel, txtTel);
            Fila(lblDir, txtDir);

            btnGuardar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0) };
            btnCancelar = new Button { Width = 110, Height = 32, Margin = new Padding(6, 0, 0, 0) };
            btnGuardar.Click += (s, e) => Guardar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 10, 12, 0)
            };
            barra.Controls.Add(btnGuardar);
            barra.Controls.Add(btnCancelar);

            Controls.Add(tabla);
            Controls.Add(barra);
            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;
        }

        private void AplicarIdioma()
        {
            var t = GestorIdioma06AV.Instancia;
            Text = t.Obtener("pcf_nuevo_proveedor");
            lblNombre.Text = t.Obtener("pcf_prov_nombre") + ":";
            lblCuit.Text = t.Obtener("cuit") + ":";
            lblEmail.Text = t.Obtener("email") + ":";
            lblTel.Text = t.Obtener("telefono") + ":";
            lblDir.Text = t.Obtener("direccion") + ":";
            btnGuardar.Text = t.Obtener("guardar");
            btnCancelar.Text = t.Obtener("cancelar");
        }

        private void Guardar()
        {
            var p = new Proveedor06AV
            {
                Nombre = txtNombre.Text.Trim(),
                Cuit = txtCuit.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Telefono = txtTel.Text.Trim(),
                Direccion = txtDir.Text.Trim()
            };
            try
            {
                _bll.Crear(p);
                ProveedorCreado = p;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ConfirmacionForm.MostrarInfo(ex.Message, GestorIdioma06AV.Instancia.Obtener("aviso"),
                    ConfirmacionForm.TipoConfirmacion.Advertencia, this);
            }
        }
    }
}
