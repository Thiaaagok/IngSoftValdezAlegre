namespace IngSoftValdezAlegre
{
    partial class FRMGestionBackup
    {
        /// <summary>Variable del diseñador necesaria.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Limpiar los recursos que se estén usando.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblIntro = new System.Windows.Forms.Label();
            this.btnGenerar = new System.Windows.Forms.Button();
            this.btnRestaurar = new System.Windows.Forms.Button();
            this.lblEstado = new System.Windows.Forms.Label();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblTitulo
            //
            this.lblTitulo.Location = new System.Drawing.Point(20, 18);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(520, 30);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "lblTitulo";
            //
            // lblIntro
            //
            this.lblIntro.Location = new System.Drawing.Point(20, 52);
            this.lblIntro.Name = "lblIntro";
            this.lblIntro.Size = new System.Drawing.Size(520, 60);
            this.lblIntro.TabIndex = 1;
            this.lblIntro.Text = "lblIntro";
            //
            // btnGenerar
            //
            this.btnGenerar.Location = new System.Drawing.Point(20, 124);
            this.btnGenerar.Name = "btnGenerar";
            this.btnGenerar.Size = new System.Drawing.Size(250, 60);
            this.btnGenerar.TabIndex = 2;
            this.btnGenerar.Text = "btnGenerar";
            this.btnGenerar.UseVisualStyleBackColor = true;
            //
            // btnRestaurar
            //
            this.btnRestaurar.Location = new System.Drawing.Point(290, 124);
            this.btnRestaurar.Name = "btnRestaurar";
            this.btnRestaurar.Size = new System.Drawing.Size(250, 60);
            this.btnRestaurar.TabIndex = 3;
            this.btnRestaurar.Text = "btnRestaurar";
            this.btnRestaurar.UseVisualStyleBackColor = true;
            //
            // lblEstado
            //
            this.lblEstado.Location = new System.Drawing.Point(20, 196);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Size = new System.Drawing.Size(520, 40);
            this.lblEstado.TabIndex = 4;
            this.lblEstado.Text = "";
            //
            // btnCerrar
            //
            this.btnCerrar.Location = new System.Drawing.Point(410, 244);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(130, 34);
            this.btnCerrar.TabIndex = 5;
            this.btnCerrar.Text = "btnCerrar";
            this.btnCerrar.UseVisualStyleBackColor = true;
            //
            // FRMGestionBackup
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 296);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblIntro);
            this.Controls.Add(this.btnGenerar);
            this.Controls.Add(this.btnRestaurar);
            this.Controls.Add(this.lblEstado);
            this.Controls.Add(this.btnCerrar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FRMGestionBackup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FRMGestionBackup";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.Button btnGenerar;
        private System.Windows.Forms.Button btnRestaurar;
        private System.Windows.Forms.Label lblEstado;
        private System.Windows.Forms.Button btnCerrar;
    }
}
