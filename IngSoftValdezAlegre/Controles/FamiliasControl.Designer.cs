namespace IngSoftValdezAlegre.Controles
{
    partial class FamiliasControl
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblTitulo;
    private System.Windows.Forms.DataGridView grilla;
    private System.Windows.Forms.DataGridViewTextBoxColumn colId;
    private System.Windows.Forms.DataGridViewTextBoxColumn colDescripcion;
    private System.Windows.Forms.Label lblDescripcion;
    private System.Windows.Forms.TextBox txtDescripcion;
    private System.Windows.Forms.Button btnNuevo;
    private System.Windows.Forms.Button btnGuardar;
    private System.Windows.Forms.Button btnEliminar;
    private System.Windows.Forms.TreeView arbol;
    private System.Windows.Forms.ComboBox cmbPatentes;
    private System.Windows.Forms.ComboBox cmbSubfamilias;
    private System.Windows.Forms.Button btnAgregarPatente;
    private System.Windows.Forms.Button btnAgregarSubfamilia;
    private System.Windows.Forms.Button btnQuitar;
    private System.Windows.Forms.Label lblPatentes;
    private System.Windows.Forms.Label lblSubfamilias;
    private System.Windows.Forms.TextBox txtMensaje;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Código generado por el Diseñador de componentes

    private void InitializeComponent()
    {
            this.lblTitulo = new System.Windows.Forms.Label();
            this.grilla = new System.Windows.Forms.DataGridView();
            this.lblDescripcion = new System.Windows.Forms.Label();
            this.txtDescripcion = new System.Windows.Forms.TextBox();
            this.btnNuevo = new System.Windows.Forms.Button();
            this.btnGuardar = new System.Windows.Forms.Button();
            this.btnEliminar = new System.Windows.Forms.Button();
            this.arbol = new System.Windows.Forms.TreeView();
            this.cmbPatentes = new System.Windows.Forms.ComboBox();
            this.cmbSubfamilias = new System.Windows.Forms.ComboBox();
            this.btnAgregarPatente = new System.Windows.Forms.Button();
            this.btnAgregarSubfamilia = new System.Windows.Forms.Button();
            this.btnQuitar = new System.Windows.Forms.Button();
            this.lblPatentes = new System.Windows.Forms.Label();
            this.lblSubfamilias = new System.Windows.Forms.Label();
            this.txtMensaje = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitulo
            // 
            this.lblTitulo.Location = new System.Drawing.Point(8, 0);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(240, 34);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Familias";
            // 
            // grilla
            // 
            this.grilla.AllowUserToAddRows = false;
            this.grilla.AllowUserToDeleteRows = false;
            this.grilla.Location = new System.Drawing.Point(8, 42);
            this.grilla.MultiSelect = false;
            this.grilla.Name = "grilla";
            this.grilla.ReadOnly = true;
            this.grilla.Size = new System.Drawing.Size(260, 470);
            this.grilla.TabIndex = 1;
            // 
            // lblDescripcion
            // 
            this.lblDescripcion.Location = new System.Drawing.Point(284, 80);
            this.lblDescripcion.Name = "lblDescripcion";
            this.lblDescripcion.Size = new System.Drawing.Size(86, 22);
            this.lblDescripcion.TabIndex = 4;
            this.lblDescripcion.Text = "Descripcion";
            // 
            // txtDescripcion
            // 
            this.txtDescripcion.Location = new System.Drawing.Point(376, 76);
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.Size = new System.Drawing.Size(380, 20);
            this.txtDescripcion.TabIndex = 5;
            // 
            // btnNuevo
            // 
            this.btnNuevo.Location = new System.Drawing.Point(284, 42);
            this.btnNuevo.Name = "btnNuevo";
            this.btnNuevo.Size = new System.Drawing.Size(72, 30);
            this.btnNuevo.TabIndex = 6;
            this.btnNuevo.Text = "Nueva";
            this.btnNuevo.UseVisualStyleBackColor = true;
            // 
            // btnGuardar
            // 
            this.btnGuardar.Location = new System.Drawing.Point(362, 42);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new System.Drawing.Size(72, 30);
            this.btnGuardar.TabIndex = 7;
            this.btnGuardar.Text = "Guardar";
            this.btnGuardar.UseVisualStyleBackColor = true;
            // 
            // btnEliminar
            // 
            this.btnEliminar.Location = new System.Drawing.Point(440, 42);
            this.btnEliminar.Name = "btnEliminar";
            this.btnEliminar.Size = new System.Drawing.Size(76, 30);
            this.btnEliminar.TabIndex = 8;
            this.btnEliminar.Text = "Eliminar";
            this.btnEliminar.UseVisualStyleBackColor = true;
            // 
            // arbol
            // 
            this.arbol.FullRowSelect = true;
            this.arbol.HideSelection = false;
            this.arbol.Location = new System.Drawing.Point(284, 118);
            this.arbol.Name = "arbol";
            this.arbol.Size = new System.Drawing.Size(440, 394);
            this.arbol.TabIndex = 9;
            // 
            // cmbPatentes
            // 
            this.cmbPatentes.DisplayMember = "Descripcion";
            this.cmbPatentes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPatentes.Location = new System.Drawing.Point(732, 142);
            this.cmbPatentes.Name = "cmbPatentes";
            this.cmbPatentes.Size = new System.Drawing.Size(220, 21);
            this.cmbPatentes.TabIndex = 11;
            this.cmbPatentes.ValueMember = "Id";
            // 
            // cmbSubfamilias
            // 
            this.cmbSubfamilias.DisplayMember = "Descripcion";
            this.cmbSubfamilias.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSubfamilias.Location = new System.Drawing.Point(732, 246);
            this.cmbSubfamilias.Name = "cmbSubfamilias";
            this.cmbSubfamilias.Size = new System.Drawing.Size(220, 21);
            this.cmbSubfamilias.TabIndex = 14;
            this.cmbSubfamilias.ValueMember = "Id";
            // 
            // btnAgregarPatente
            // 
            this.btnAgregarPatente.Location = new System.Drawing.Point(732, 174);
            this.btnAgregarPatente.Name = "btnAgregarPatente";
            this.btnAgregarPatente.Size = new System.Drawing.Size(220, 32);
            this.btnAgregarPatente.TabIndex = 12;
            this.btnAgregarPatente.Text = "Agregar patente";
            this.btnAgregarPatente.UseVisualStyleBackColor = true;
            // 
            // btnAgregarSubfamilia
            // 
            this.btnAgregarSubfamilia.Location = new System.Drawing.Point(732, 278);
            this.btnAgregarSubfamilia.Name = "btnAgregarSubfamilia";
            this.btnAgregarSubfamilia.Size = new System.Drawing.Size(220, 32);
            this.btnAgregarSubfamilia.TabIndex = 15;
            this.btnAgregarSubfamilia.Text = "Agregar familia";
            this.btnAgregarSubfamilia.UseVisualStyleBackColor = true;
            // 
            // btnQuitar
            // 
            this.btnQuitar.Location = new System.Drawing.Point(732, 328);
            this.btnQuitar.Name = "btnQuitar";
            this.btnQuitar.Size = new System.Drawing.Size(220, 32);
            this.btnQuitar.TabIndex = 16;
            this.btnQuitar.Text = "Quitar seleccionado";
            this.btnQuitar.UseVisualStyleBackColor = true;
            // 
            // lblPatentes
            // 
            this.lblPatentes.Location = new System.Drawing.Point(732, 118);
            this.lblPatentes.Name = "lblPatentes";
            this.lblPatentes.Size = new System.Drawing.Size(220, 20);
            this.lblPatentes.TabIndex = 10;
            this.lblPatentes.Text = "Patentes disponibles";
            // 
            // lblSubfamilias
            // 
            this.lblSubfamilias.Location = new System.Drawing.Point(732, 222);
            this.lblSubfamilias.Name = "lblSubfamilias";
            this.lblSubfamilias.Size = new System.Drawing.Size(220, 20);
            this.lblSubfamilias.TabIndex = 13;
            this.lblSubfamilias.Text = "Familias disponibles";
            // 
            // txtMensaje
            // 
            this.txtMensaje.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtMensaje.Location = new System.Drawing.Point(732, 376);
            this.txtMensaje.Multiline = true;
            this.txtMensaje.Name = "txtMensaje";
            this.txtMensaje.ReadOnly = true;
            this.txtMensaje.Size = new System.Drawing.Size(220, 136);
            this.txtMensaje.TabIndex = 17;
            this.txtMensaje.Text = "Una familia puede tener patentes y otras familias. La pantalla bloquea ciclos y p" +
    "atentes repetidas en toda la rama.";
            // 
            // FamiliasControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTitulo);
            this.Controls.Add(this.grilla);
            this.Controls.Add(this.lblDescripcion);
            this.Controls.Add(this.txtDescripcion);
            this.Controls.Add(this.btnNuevo);
            this.Controls.Add(this.btnGuardar);
            this.Controls.Add(this.btnEliminar);
            this.Controls.Add(this.arbol);
            this.Controls.Add(this.lblPatentes);
            this.Controls.Add(this.cmbPatentes);
            this.Controls.Add(this.btnAgregarPatente);
            this.Controls.Add(this.lblSubfamilias);
            this.Controls.Add(this.cmbSubfamilias);
            this.Controls.Add(this.btnAgregarSubfamilia);
            this.Controls.Add(this.btnQuitar);
            this.Controls.Add(this.txtMensaje);
            this.Name = "FamiliasControl";
            this.Size = new System.Drawing.Size(976, 528);
            ((System.ComponentModel.ISupportInitialize)(this.grilla)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion
}
}