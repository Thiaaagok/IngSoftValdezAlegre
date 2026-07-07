namespace IngSoftValdezAlegre
{
    partial class FRMReparacionDV
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
            this.lblDetalle = new System.Windows.Forms.Label();
            this.txtDetalle = new System.Windows.Forms.TextBox();
            this.btnRecalcular = new System.Windows.Forms.Button();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnSalir = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblTitulo
            //
            this.lblTitulo.Location = new System.Drawing.Point(20, 18);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(480, 30);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "lblTitulo";
            //
            // lblIntro
            //
            this.lblIntro.Location = new System.Drawing.Point(20, 52);
            this.lblIntro.Name = "lblIntro";
            this.lblIntro.Size = new System.Drawing.Size(480, 44);
            this.lblIntro.TabIndex = 1;
            this.lblIntro.Text = "lblIntro";
            //
            // lblDetalle
            //
            this.lblDetalle.Location = new System.Drawing.Point(20, 100);
            this.lblDetalle.Name = "lblDetalle";
            this.lblDetalle.Size = new System.Drawing.Size(480, 20);
            this.lblDetalle.TabIndex = 2;
            this.lblDetalle.Text = "lblDetalle";
            //
            // txtDetalle
            //
            this.txtDetalle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDetalle.Location = new System.Drawing.Point(20, 122);
            this.txtDetalle.Multiline = true;
            this.txtDetalle.Name = "txtDetalle";
            this.txtDetalle.ReadOnly = true;
            this.txtDetalle.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetalle.Size = new System.Drawing.Size(480, 160);
            this.txtDetalle.TabIndex = 3;
            //
            // btnRecalcular
            //
            this.btnRecalcular.Location = new System.Drawing.Point(20, 300);
            this.btnRecalcular.Name = "btnRecalcular";
            this.btnRecalcular.Size = new System.Drawing.Size(480, 34);
            this.btnRecalcular.TabIndex = 4;
            this.btnRecalcular.Text = "btnRecalcular";
            this.btnRecalcular.UseVisualStyleBackColor = true;
            //
            // btnRestore
            //
            this.btnRestore.Location = new System.Drawing.Point(20, 342);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(480, 34);
            this.btnRestore.TabIndex = 5;
            this.btnRestore.Text = "btnRestore";
            this.btnRestore.UseVisualStyleBackColor = true;
            //
            // btnSalir
            //
            this.btnSalir.Location = new System.Drawing.Point(20, 384);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Size = new System.Drawing.Size(480, 26);
            this.btnSalir.TabIndex = 6;
            this.btnSalir.Text = "btnSalir";
            this.btnSalir.UseVisualStyleBackColor = true;
            //
            // FRMReparacionDV
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 420);
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.lblIntro);
            this.Controls.Add(this.lblDetalle);
            this.Controls.Add(this.txtDetalle);
            this.Controls.Add(this.btnRecalcular);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.btnSalir);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FRMReparacionDV";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FRMReparacionDV";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.Label lblDetalle;
        private System.Windows.Forms.TextBox txtDetalle;
        private System.Windows.Forms.Button btnRecalcular;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnSalir;
    }
}
