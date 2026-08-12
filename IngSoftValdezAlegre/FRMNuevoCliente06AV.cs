using BE;
using BLL;
using IngSoftValdezAlegre.Common;
using SER;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IngSoftValdezAlegre
{
    /// <summary>
    /// Diálogo modal para dar de alta un cliente sin salir del proceso de Venta
    /// (CU02 "Registrar cliente", escenario alternativo 2.1 del CU01). Si el alta
    /// sale bien, expone el cliente creado en <see cref="ClienteCreado"/> y cierra con OK.
    /// </summary>
    public class FRMNuevoCliente06AV : Form
    {
        private readonly ClientesBLL06AV _bll = new ClientesBLL06AV();

        /// <summary>Cliente recién creado (solo válido si DialogResult == OK).</summary>
        public Cliente06AV ClienteCreado { get; private set; }

        private Label lblDni, lblNombre, lblApellido, lblTel, lblDir;
        private TextBox txtDni, txtNombre, txtApellido, txtTel, txtDir;
        private Button btnGuardar, btnCancelar;

        public FRMNuevoCliente06AV(string dniSugerido = null)
        {
            ConstruirUI();
            Tema.AplicarFormulario(this);
            Tema.AplicarBotonPrimario(btnGuardar);
            Tema.AplicarBotonSecundario(btnCancelar);
            AplicarIdioma();
            if (!string.IsNullOrWhiteSpace(dniSugerido)) txtDni.Text = dniSugerido;
        }

        private void ConstruirUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 280);

            lblDni = new Label(); lblNombre = new Label(); lblApellido = new Label();
            lblTel = new Label(); lblDir = new Label();
            txtDni = new TextBox { Width = 260, MaxLength = 20 };
            txtNombre = new TextBox { Width = 260 };
            txtApellido = new TextBox { Width = 260 };
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
            Fila(lblDni, txtDni);
            Fila(lblNombre, txtNombre);
            Fila(lblApellido, txtApellido);
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
            Text = t.Obtener("pcf_nuevo_cliente");
            lblDni.Text = t.Obtener("dni") + ":";
            lblNombre.Text = t.Obtener("nombre") + ":";
            lblApellido.Text = t.Obtener("apellido") + ":";
            lblTel.Text = t.Obtener("telefono") + ":";
            lblDir.Text = t.Obtener("direccion") + ":";
            btnGuardar.Text = t.Obtener("guardar");
            btnCancelar.Text = t.Obtener("cancelar");
        }

        private void Guardar()
        {
            var c = new Cliente06AV
            {
                Dni = txtDni.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                Telefono = txtTel.Text.Trim(),
                Direccion = txtDir.Text.Trim()
            };
            try
            {
                _bll.Crear(c);
                ClienteCreado = c;
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
