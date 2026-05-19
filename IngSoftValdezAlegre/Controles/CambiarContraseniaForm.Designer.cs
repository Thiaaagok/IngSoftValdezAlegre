namespace IngSoftValdezAlegre.Controles
{
    partial class CambiarContraseniaForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Panel pnlTitulo;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblActual;
        private System.Windows.Forms.TextBox txtActual;
        private System.Windows.Forms.Label lblNueva;
        private System.Windows.Forms.TextBox txtNueva;
        private System.Windows.Forms.Label lblRepetir;
        private System.Windows.Forms.TextBox txtRepetir;
        private System.Windows.Forms.Label lblMensaje;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Button btnAceptar;

        private void InitializeComponent()
        {
            this.pnlTitulo = new System.Windows.Forms.Panel();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblActual = new System.Windows.Forms.Label();
            this.txtActual = new System.Windows.Forms.TextBox();
            this.lblNueva = new System.Windows.Forms.Label();
            this.txtNueva = new System.Windows.Forms.TextBox();
            this.lblRepetir = new System.Windows.Forms.Label();
            this.txtRepetir = new System.Windows.Forms.TextBox();
            this.lblMensaje = new System.Windows.Forms.Label();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.btnAceptar = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // pnlTitulo
            this.pnlTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTitulo.Height = 56;
            this.pnlTitulo.BackColor = Tema.Primario;
            this.pnlTitulo.Controls.Add(this.lblTitulo);

            // lblTitulo
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Text = "Cambiar contraseña";
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI Semibold", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.Location = new System.Drawing.Point(24, 16);

            // lblActual
            this.lblActual.AutoSize = true;
            this.lblActual.Text = "Contraseña actual";
            this.lblActual.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblActual.ForeColor = Tema.Texto;
            this.lblActual.Location = new System.Drawing.Point(24, 80);

            // txtActual
            this.txtActual.Location = new System.Drawing.Point(24, 102);
            this.txtActual.Size = new System.Drawing.Size(372, 25);
            this.txtActual.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtActual.UseSystemPasswordChar = true;
            this.txtActual.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // lblNueva
            this.lblNueva.AutoSize = true;
            this.lblNueva.Text = "Nueva contraseña";
            this.lblNueva.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblNueva.ForeColor = Tema.Texto;
            this.lblNueva.Location = new System.Drawing.Point(24, 142);

            // txtNueva
            this.txtNueva.Location = new System.Drawing.Point(24, 164);
            this.txtNueva.Size = new System.Drawing.Size(372, 25);
            this.txtNueva.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtNueva.UseSystemPasswordChar = true;
            this.txtNueva.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // lblRepetir
            this.lblRepetir.AutoSize = true;
            this.lblRepetir.Text = "Repetir nueva contraseña";
            this.lblRepetir.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblRepetir.ForeColor = Tema.Texto;
            this.lblRepetir.Location = new System.Drawing.Point(24, 204);

            // txtRepetir
            this.txtRepetir.Location = new System.Drawing.Point(24, 226);
            this.txtRepetir.Size = new System.Drawing.Size(372, 25);
            this.txtRepetir.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtRepetir.UseSystemPasswordChar = true;
            this.txtRepetir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // lblMensaje
            this.lblMensaje.Location = new System.Drawing.Point(24, 262);
            this.lblMensaje.Size = new System.Drawing.Size(372, 32);
            this.lblMensaje.Text = "";
            this.lblMensaje.ForeColor = Tema.Peligro;
            this.lblMensaje.Font = new System.Drawing.Font("Segoe UI", 9F);

            // btnCancelar
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.Location = new System.Drawing.Point(206, 308);
            this.btnCancelar.Size = new System.Drawing.Size(90, 36);
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.FlatAppearance.BorderColor = Tema.Borde;
            this.btnCancelar.FlatAppearance.BorderSize = 1;
            this.btnCancelar.BackColor = System.Drawing.Color.White;
            this.btnCancelar.ForeColor = Tema.Texto;
            this.btnCancelar.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancelar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);

            // btnAceptar
            this.btnAceptar.Text = "Aceptar";
            this.btnAceptar.Location = new System.Drawing.Point(306, 308);
            this.btnAceptar.Size = new System.Drawing.Size(90, 36);
            this.btnAceptar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.FlatAppearance.BorderSize = 0;
            this.btnAceptar.BackColor = Tema.Primario;
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnAceptar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);

            // CambiarContraseniaForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 360);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.BackColor = System.Drawing.Color.White;
            this.Text = "Cambiar contraseña";
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.lblMensaje);
            this.Controls.Add(this.txtRepetir);
            this.Controls.Add(this.lblRepetir);
            this.Controls.Add(this.txtNueva);
            this.Controls.Add(this.lblNueva);
            this.Controls.Add(this.txtActual);
            this.Controls.Add(this.lblActual);
            this.Controls.Add(this.pnlTitulo);
            this.AcceptButton = this.btnAceptar;
            this.CancelButton = this.btnCancelar;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
