namespace IngSoftValdezAlegre
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.panelPrincipal = new System.Windows.Forms.Panel();
            this.usuariosBTN = new System.Windows.Forms.Button();
            this.bitacoraBTN = new System.Windows.Forms.Button();
            this.pnlTopBar = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.lblUsuario = new System.Windows.Forms.Label();
            this.opcionesUsuarioBTN = new System.Windows.Forms.Button();
            this.lblSistema = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.ctxMenuUsuario = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cambiarContraseñaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cerrarSesiónToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flpModulos = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlTopBar.SuspendLayout();
            this.panel4.SuspendLayout();
            this.ctxMenuUsuario.SuspendLayout();
            this.flpModulos.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelPrincipal
            // 
            this.panelPrincipal.Location = new System.Drawing.Point(131, 38);
            this.panelPrincipal.Name = "panelPrincipal";
            this.panelPrincipal.Size = new System.Drawing.Size(1304, 577);
            this.panelPrincipal.TabIndex = 1;
            // 
            // usuariosBTN
            // 
            this.usuariosBTN.Location = new System.Drawing.Point(3, 3);
            this.usuariosBTN.Name = "usuariosBTN";
            this.usuariosBTN.Size = new System.Drawing.Size(85, 30);
            this.usuariosBTN.TabIndex = 0;
            this.usuariosBTN.Text = "Usuarios";
            this.usuariosBTN.UseVisualStyleBackColor = true;
            this.usuariosBTN.Click += new System.EventHandler(this.usuariosBTN_Click);
            // 
            // bitacoraBTN
            // 
            this.bitacoraBTN.Location = new System.Drawing.Point(3, 39);
            this.bitacoraBTN.Name = "bitacoraBTN";
            this.bitacoraBTN.Size = new System.Drawing.Size(85, 33);
            this.bitacoraBTN.TabIndex = 2;
            this.bitacoraBTN.Text = "Bitacora";
            this.bitacoraBTN.UseVisualStyleBackColor = true;
            this.bitacoraBTN.Click += new System.EventHandler(this.bitacoraBTN_Click);
            // 
            // pnlTopBar
            // 
            this.pnlTopBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(101)))), ((int)(((byte)(192)))));
            this.pnlTopBar.Controls.Add(this.panel4);
            this.pnlTopBar.Controls.Add(this.lblSistema);
            this.pnlTopBar.Controls.Add(this.panel2);
            this.pnlTopBar.Location = new System.Drawing.Point(-1, 0);
            this.pnlTopBar.Name = "pnlTopBar";
            this.pnlTopBar.Size = new System.Drawing.Size(1436, 39);
            this.pnlTopBar.TabIndex = 0;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.lblUsuario);
            this.panel4.Controls.Add(this.opcionesUsuarioBTN);
            this.panel4.Location = new System.Drawing.Point(1294, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(134, 37);
            this.panel4.TabIndex = 1;
            // 
            // lblUsuario
            // 
            this.lblUsuario.AutoSize = true;
            this.lblUsuario.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUsuario.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblUsuario.Location = new System.Drawing.Point(3, 9);
            this.lblUsuario.Name = "lblUsuario";
            this.lblUsuario.Size = new System.Drawing.Size(64, 21);
            this.lblUsuario.TabIndex = 0;
            this.lblUsuario.Text = "Usuario";
            // 
            // opcionesUsuarioBTN
            // 
            this.opcionesUsuarioBTN.Location = new System.Drawing.Point(108, 5);
            this.opcionesUsuarioBTN.Name = "opcionesUsuarioBTN";
            this.opcionesUsuarioBTN.Size = new System.Drawing.Size(23, 27);
            this.opcionesUsuarioBTN.TabIndex = 0;
            this.opcionesUsuarioBTN.Text = "▼";
            this.opcionesUsuarioBTN.UseVisualStyleBackColor = true;
            this.opcionesUsuarioBTN.Click += new System.EventHandler(this.opcionesUsuarioBTN_Click);
            // 
            // lblSistema
            // 
            this.lblSistema.AutoSize = true;
            this.lblSistema.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSistema.ForeColor = System.Drawing.SystemColors.Control;
            this.lblSistema.Location = new System.Drawing.Point(1, 3);
            this.lblSistema.Name = "lblSistema";
            this.lblSistema.Size = new System.Drawing.Size(209, 32);
            this.lblSistema.TabIndex = 0;
            this.lblSistema.Text = "PCFORGE/CLINICA";
            // 
            // panel2
            // 
            this.panel2.Location = new System.Drawing.Point(0, 39);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(128, 665);
            this.panel2.TabIndex = 0;
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.Location = new System.Drawing.Point(4, 41);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(128, 418);
            this.pnlSidebar.TabIndex = 0;
            // 
            // ctxMenuUsuario
            // 
            this.ctxMenuUsuario.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cambiarContraseñaToolStripMenuItem,
            this.cerrarSesiónToolStripMenuItem});
            this.ctxMenuUsuario.Name = "ctxMenuUsuario";
            this.ctxMenuUsuario.Size = new System.Drawing.Size(183, 48);
            // 
            // cambiarContraseñaToolStripMenuItem
            // 
            this.cambiarContraseñaToolStripMenuItem.Name = "cambiarContraseñaToolStripMenuItem";
            this.cambiarContraseñaToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.cambiarContraseñaToolStripMenuItem.Text = "Cambiar Contraseña";
            this.cambiarContraseñaToolStripMenuItem.Click += new System.EventHandler(this.cambiarContraseñaToolStripMenuItem_Click);
            // 
            // cerrarSesiónToolStripMenuItem
            // 
            this.cerrarSesiónToolStripMenuItem.Name = "cerrarSesiónToolStripMenuItem";
            this.cerrarSesiónToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.cerrarSesiónToolStripMenuItem.Text = "Cerrar Sesión";
            this.cerrarSesiónToolStripMenuItem.Click += new System.EventHandler(this.cerrarSesiónToolStripMenuItem_Click);
            // 
            // flpModulos
            // 
            this.flpModulos.AutoScroll = true;
            this.flpModulos.Controls.Add(this.usuariosBTN);
            this.flpModulos.Controls.Add(this.bitacoraBTN);
            this.flpModulos.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpModulos.Location = new System.Drawing.Point(-1, 41);
            this.flpModulos.Name = "flpModulos";
            this.flpModulos.Size = new System.Drawing.Size(133, 576);
            this.flpModulos.TabIndex = 0;
            this.flpModulos.WrapContents = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1435, 615);
            this.ControlBox = false;
            this.Controls.Add(this.flpModulos);
            this.Controls.Add(this.pnlSidebar);
            this.Controls.Add(this.pnlTopBar);
            this.Controls.Add(this.panelPrincipal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "MainForm";
            this.pnlTopBar.ResumeLayout(false);
            this.pnlTopBar.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.ctxMenuUsuario.ResumeLayout(false);
            this.flpModulos.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelPrincipal;
        private System.Windows.Forms.Button usuariosBTN;
        private System.Windows.Forms.Button bitacoraBTN;
        private System.Windows.Forms.Panel pnlTopBar;
        private System.Windows.Forms.Label lblSistema;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label lblUsuario;
        private System.Windows.Forms.Button opcionesUsuarioBTN;
        private System.Windows.Forms.ContextMenuStrip ctxMenuUsuario;
        private System.Windows.Forms.ToolStripMenuItem cambiarContraseñaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cerrarSesiónToolStripMenuItem;
        private System.Windows.Forms.FlowLayoutPanel flpModulos;
    }
}