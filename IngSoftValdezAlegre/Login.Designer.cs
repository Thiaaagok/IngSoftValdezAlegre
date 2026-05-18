namespace IngSoftValdezAlegre
{
    partial class Login
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.materialLabel2 = new ReaLTaiizor.Controls.MaterialLabel();
            this.Usuario = new ReaLTaiizor.Controls.MaterialLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.LoginTextBox = new System.Windows.Forms.TextBox();
            this.ContraseniaTextBox = new System.Windows.Forms.TextBox();
            this.IniciarSesionBTN = new System.Windows.Forms.Button();
            this.CerrarBTN = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // materialLabel2
            // 
            this.materialLabel2.AutoSize = true;
            this.materialLabel2.BackColor = System.Drawing.Color.RoyalBlue;
            this.materialLabel2.Depth = 0;
            this.materialLabel2.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabel2.Location = new System.Drawing.Point(298, 158);
            this.materialLabel2.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.materialLabel2.Name = "materialLabel2";
            this.materialLabel2.Size = new System.Drawing.Size(103, 19);
            this.materialLabel2.TabIndex = 6;
            this.materialLabel2.Text = "CONTRASEÑA";
            // 
            // Usuario
            // 
            this.Usuario.AutoSize = true;
            this.Usuario.BackColor = System.Drawing.Color.RoyalBlue;
            this.Usuario.Depth = 0;
            this.Usuario.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.Usuario.Location = new System.Drawing.Point(301, 85);
            this.Usuario.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            this.Usuario.Name = "Usuario";
            this.Usuario.Size = new System.Drawing.Size(66, 19);
            this.Usuario.TabIndex = 7;
            this.Usuario.Text = "USUARIO";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.RoyalBlue;
            this.panel1.Location = new System.Drawing.Point(0, -7);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(295, 419);
            this.panel1.TabIndex = 9;
            // 
            // LoginTextBox
            // 
            this.LoginTextBox.Location = new System.Drawing.Point(301, 107);
            this.LoginTextBox.Multiline = true;
            this.LoginTextBox.Name = "LoginTextBox";
            this.LoginTextBox.Size = new System.Drawing.Size(387, 39);
            this.LoginTextBox.TabIndex = 10;
            // 
            // ContraseniaTextBox
            // 
            this.ContraseniaTextBox.Location = new System.Drawing.Point(301, 180);
            this.ContraseniaTextBox.Multiline = true;
            this.ContraseniaTextBox.Name = "ContraseniaTextBox";
            this.ContraseniaTextBox.Size = new System.Drawing.Size(387, 39);
            this.ContraseniaTextBox.TabIndex = 11;
            // 
            // IniciarSesionBTN
            // 
            this.IniciarSesionBTN.Location = new System.Drawing.Point(316, 262);
            this.IniciarSesionBTN.Name = "IniciarSesionBTN";
            this.IniciarSesionBTN.Size = new System.Drawing.Size(354, 48);
            this.IniciarSesionBTN.TabIndex = 12;
            this.IniciarSesionBTN.Text = "INICIAR SESION";
            this.IniciarSesionBTN.UseVisualStyleBackColor = true;
            this.IniciarSesionBTN.Click += new System.EventHandler(this.IniciarSesionBTN_Click);
            // 
            // CerrarBTN
            // 
            this.CerrarBTN.Location = new System.Drawing.Point(316, 316);
            this.CerrarBTN.Name = "CerrarBTN";
            this.CerrarBTN.Size = new System.Drawing.Size(354, 48);
            this.CerrarBTN.TabIndex = 13;
            this.CerrarBTN.Text = "CERRAR";
            this.CerrarBTN.UseVisualStyleBackColor = true;
            this.CerrarBTN.Click += new System.EventHandler(this.CerrarBTN_Click);
            // 
            // Login
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LavenderBlush;
            this.ClientSize = new System.Drawing.Size(695, 408);
            this.ControlBox = false;
            this.Controls.Add(this.CerrarBTN);
            this.Controls.Add(this.IniciarSesionBTN);
            this.Controls.Add(this.ContraseniaTextBox);
            this.Controls.Add(this.LoginTextBox);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.Usuario);
            this.Controls.Add(this.materialLabel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Login";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Load += new System.EventHandler(this.Login_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ReaLTaiizor.Controls.MaterialLabel materialLabel2;
        private ReaLTaiizor.Controls.MaterialLabel Usuario;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox LoginTextBox;
        private System.Windows.Forms.TextBox ContraseniaTextBox;
        private System.Windows.Forms.Button IniciarSesionBTN;
        private System.Windows.Forms.Button CerrarBTN;
    }
}