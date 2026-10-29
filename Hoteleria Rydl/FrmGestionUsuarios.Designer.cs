namespace GUI_HRS_480
{
    partial class FrmGestionUsuarios
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
            pnlHeader = new Panel();
            lblTituloForm = new Label();
            rdTodos = new RadioButton();
            rdActivos = new RadioButton();
            lblNumUsuarios = new Label();
            dgvSuperior = new DataGridView();
            colDNI = new DataGridViewTextBoxColumn();
            colApellidos = new DataGridViewTextBoxColumn();
            colNombres = new DataGridViewTextBoxColumn();
            colEmail = new DataGridViewTextBoxColumn();
            colLogin = new DataGridViewTextBoxColumn();
            colRol = new DataGridViewTextBoxColumn();
            lblFilDNI = new Label();
            lblFilApellidos = new Label();
            lblFilNombres = new Label();
            lblFilLogin = new Label();
            lblFilRol = new Label();
            txtFilDNI = new TextBox();
            txtFilApellidos = new TextBox();
            txtFilNombres = new TextBox();
            txtFilLogin = new TextBox();
            cmbFilRol = new ComboBox();
            lblNombreLabel = new Label();
            lblNombreValor = new Label();
            lblApellidoLabel = new Label();
            lblApellidoValor = new Label();
            lblMensajeLabel = new Label();
            lblMensaje = new Label();
            btnAplicarFiltro = new Button();
            pnlEdicion = new Panel();
            txtEdEmail = new TextBox();
            label1 = new Label();
            cmbRolEdicion = new ComboBox();
            lblEdRol = new Label();
            txtEdNombre = new TextBox();
            txtEdApellido = new TextBox();
            txtEdDNI = new TextBox();
            lblEdNombre = new Label();
            lblEdApellido = new Label();
            lblEdDNI = new Label();
            btnCrear = new Button();
            btnDesbloquear = new Button();
            btnModificar = new Button();
            btnActDesact = new Button();
            btnAplicar = new Button();
            btnCancelar = new Button();
            btnSalir = new Button();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSuperior).BeginInit();
            pnlEdicion.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(22, 55, 95);
            pnlHeader.Controls.Add(lblTituloForm);
            pnlHeader.ForeColor = Color.White;
            pnlHeader.Location = new Point(23, 12);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(280, 94);
            pnlHeader.TabIndex = 1;
            // 
            // lblTituloForm
            // 
            lblTituloForm.AutoSize = true;
            lblTituloForm.Font = new Font("Georgia", 22.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTituloForm.Location = new Point(35, 20);
            lblTituloForm.Name = "lblTituloForm";
            lblTituloForm.Size = new Size(210, 43);
            lblTituloForm.TabIndex = 0;
            lblTituloForm.Text = "USUARIOS";
            // 
            // rdTodos
            // 
            rdTodos.AutoSize = true;
            rdTodos.Location = new Point(426, 12);
            rdTodos.Name = "rdTodos";
            rdTodos.Size = new Size(70, 24);
            rdTodos.TabIndex = 2;
            rdTodos.TabStop = true;
            rdTodos.Text = "Todos";
            rdTodos.UseVisualStyleBackColor = true;
            rdTodos.CheckedChanged += rdTodos_CheckedChanged_1;
            // 
            // rdActivos
            // 
            rdActivos.AutoSize = true;
            rdActivos.Location = new Point(320, 12);
            rdActivos.Name = "rdActivos";
            rdActivos.Size = new Size(78, 24);
            rdActivos.TabIndex = 3;
            rdActivos.TabStop = true;
            rdActivos.Text = "Activos";
            rdActivos.UseVisualStyleBackColor = true;
            rdActivos.CheckedChanged += rdActivos_CheckedChanged_1;
            // 
            // lblNumUsuarios
            // 
            lblNumUsuarios.AutoSize = true;
            lblNumUsuarios.Location = new Point(632, 17);
            lblNumUsuarios.Name = "lblNumUsuarios";
            lblNumUsuarios.Size = new Size(149, 20);
            lblNumUsuarios.TabIndex = 4;
            lblNumUsuarios.Text = "Numero de usuarios: ";
            // 
            // dgvSuperior
            // 
            dgvSuperior.AllowUserToAddRows = false;
            dgvSuperior.AllowUserToDeleteRows = false;
            dgvSuperior.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSuperior.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSuperior.Columns.AddRange(new DataGridViewColumn[] { colDNI, colApellidos, colNombres, colEmail, colLogin, colRol });
            dgvSuperior.Location = new Point(35, 115);
            dgvSuperior.MultiSelect = false;
            dgvSuperior.Name = "dgvSuperior";
            dgvSuperior.ReadOnly = true;
            dgvSuperior.RowHeadersVisible = false;
            dgvSuperior.RowHeadersWidth = 51;
            dgvSuperior.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSuperior.Size = new Size(806, 181);
            dgvSuperior.TabIndex = 5;
            dgvSuperior.SelectionChanged += dgvSuperior_SelectionChanged_1;
            // 
            // colDNI
            // 
            colDNI.HeaderText = "DNI";
            colDNI.MinimumWidth = 6;
            colDNI.Name = "colDNI";
            colDNI.ReadOnly = true;
            // 
            // colApellidos
            // 
            colApellidos.HeaderText = "Apellidos";
            colApellidos.MinimumWidth = 6;
            colApellidos.Name = "colApellidos";
            colApellidos.ReadOnly = true;
            // 
            // colNombres
            // 
            colNombres.HeaderText = "Nombres";
            colNombres.MinimumWidth = 6;
            colNombres.Name = "colNombres";
            colNombres.ReadOnly = true;
            // 
            // colEmail
            // 
            colEmail.HeaderText = "Email";
            colEmail.MinimumWidth = 6;
            colEmail.Name = "colEmail";
            colEmail.ReadOnly = true;
            // 
            // colLogin
            // 
            colLogin.HeaderText = "Login";
            colLogin.MinimumWidth = 6;
            colLogin.Name = "colLogin";
            colLogin.ReadOnly = true;
            // 
            // colRol
            // 
            colRol.HeaderText = "Rol";
            colRol.MinimumWidth = 6;
            colRol.Name = "colRol";
            colRol.ReadOnly = true;
            // 
            // lblFilDNI
            // 
            lblFilDNI.AutoSize = true;
            lblFilDNI.Location = new Point(23, 334);
            lblFilDNI.Name = "lblFilDNI";
            lblFilDNI.Size = new Size(35, 20);
            lblFilDNI.TabIndex = 6;
            lblFilDNI.Text = "DNI";
            // 
            // lblFilApellidos
            // 
            lblFilApellidos.AutoSize = true;
            lblFilApellidos.Location = new Point(23, 368);
            lblFilApellidos.Name = "lblFilApellidos";
            lblFilApellidos.Size = new Size(72, 20);
            lblFilApellidos.TabIndex = 7;
            lblFilApellidos.Text = "Apellidos";
            // 
            // lblFilNombres
            // 
            lblFilNombres.AutoSize = true;
            lblFilNombres.Location = new Point(23, 402);
            lblFilNombres.Name = "lblFilNombres";
            lblFilNombres.Size = new Size(70, 20);
            lblFilNombres.TabIndex = 8;
            lblFilNombres.Text = "Nombres";
            // 
            // lblFilLogin
            // 
            lblFilLogin.AutoSize = true;
            lblFilLogin.Location = new Point(23, 435);
            lblFilLogin.Name = "lblFilLogin";
            lblFilLogin.Size = new Size(46, 20);
            lblFilLogin.TabIndex = 9;
            lblFilLogin.Text = "Login";
            // 
            // lblFilRol
            // 
            lblFilRol.AutoSize = true;
            lblFilRol.Location = new Point(23, 464);
            lblFilRol.Name = "lblFilRol";
            lblFilRol.Size = new Size(31, 20);
            lblFilRol.TabIndex = 10;
            lblFilRol.Text = "Rol";
            // 
            // txtFilDNI
            // 
            txtFilDNI.Location = new Point(101, 331);
            txtFilDNI.Name = "txtFilDNI";
            txtFilDNI.Size = new Size(125, 27);
            txtFilDNI.TabIndex = 11;
            // 
            // txtFilApellidos
            // 
            txtFilApellidos.Location = new Point(101, 364);
            txtFilApellidos.Name = "txtFilApellidos";
            txtFilApellidos.Size = new Size(125, 27);
            txtFilApellidos.TabIndex = 12;
            // 
            // txtFilNombres
            // 
            txtFilNombres.Location = new Point(101, 395);
            txtFilNombres.Name = "txtFilNombres";
            txtFilNombres.Size = new Size(125, 27);
            txtFilNombres.TabIndex = 13;
            // 
            // txtFilLogin
            // 
            txtFilLogin.Location = new Point(101, 431);
            txtFilLogin.Name = "txtFilLogin";
            txtFilLogin.Size = new Size(125, 27);
            txtFilLogin.TabIndex = 14;
            // 
            // cmbFilRol
            // 
            cmbFilRol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilRol.FormattingEnabled = true;
            cmbFilRol.Location = new Point(101, 464);
            cmbFilRol.Name = "cmbFilRol";
            cmbFilRol.Size = new Size(125, 28);
            cmbFilRol.TabIndex = 15;
            // 
            // lblNombreLabel
            // 
            lblNombreLabel.AutoSize = true;
            lblNombreLabel.Location = new Point(23, 510);
            lblNombreLabel.Name = "lblNombreLabel";
            lblNombreLabel.Size = new Size(64, 20);
            lblNombreLabel.TabIndex = 16;
            lblNombreLabel.Text = "Nombre";
            // 
            // lblNombreValor
            // 
            lblNombreValor.AutoSize = true;
            lblNombreValor.Location = new Point(101, 510);
            lblNombreValor.Name = "lblNombreValor";
            lblNombreValor.Size = new Size(0, 20);
            lblNombreValor.TabIndex = 17;
            // 
            // lblApellidoLabel
            // 
            lblApellidoLabel.AutoSize = true;
            lblApellidoLabel.Location = new Point(23, 541);
            lblApellidoLabel.Name = "lblApellidoLabel";
            lblApellidoLabel.Size = new Size(66, 20);
            lblApellidoLabel.TabIndex = 18;
            lblApellidoLabel.Text = "Apellido";
            // 
            // lblApellidoValor
            // 
            lblApellidoValor.AutoSize = true;
            lblApellidoValor.Location = new Point(101, 541);
            lblApellidoValor.Name = "lblApellidoValor";
            lblApellidoValor.Size = new Size(0, 20);
            lblApellidoValor.TabIndex = 19;
            // 
            // lblMensajeLabel
            // 
            lblMensajeLabel.AutoSize = true;
            lblMensajeLabel.Location = new Point(232, 329);
            lblMensajeLabel.Name = "lblMensajeLabel";
            lblMensajeLabel.Size = new Size(64, 20);
            lblMensajeLabel.TabIndex = 20;
            lblMensajeLabel.Text = "Mensaje";
            // 
            // lblMensaje
            // 
            lblMensaje.AutoSize = true;
            lblMensaje.BackColor = Color.White;
            lblMensaje.Location = new Point(232, 376);
            lblMensaje.Name = "lblMensaje";
            lblMensaje.Size = new Size(108, 20);
            lblMensaje.TabIndex = 21;
            lblMensaje.Text = "Modo consulta";
            // 
            // btnAplicarFiltro
            // 
            btnAplicarFiltro.Location = new Point(280, 510);
            btnAplicarFiltro.Name = "btnAplicarFiltro";
            btnAplicarFiltro.Size = new Size(127, 53);
            btnAplicarFiltro.TabIndex = 22;
            btnAplicarFiltro.Text = "APLICAR FILTRO";
            btnAplicarFiltro.UseVisualStyleBackColor = true;
            btnAplicarFiltro.Click += btnAplicarFiltro_Click_1;
            // 
            // pnlEdicion
            // 
            pnlEdicion.BackColor = Color.FromArgb(220, 228, 240);
            pnlEdicion.Controls.Add(txtEdEmail);
            pnlEdicion.Controls.Add(label1);
            pnlEdicion.Controls.Add(cmbRolEdicion);
            pnlEdicion.Controls.Add(lblEdRol);
            pnlEdicion.Controls.Add(txtEdNombre);
            pnlEdicion.Controls.Add(txtEdApellido);
            pnlEdicion.Controls.Add(txtEdDNI);
            pnlEdicion.Controls.Add(lblEdNombre);
            pnlEdicion.Controls.Add(lblEdApellido);
            pnlEdicion.Controls.Add(lblEdDNI);
            pnlEdicion.Location = new Point(576, 359);
            pnlEdicion.Name = "pnlEdicion";
            pnlEdicion.Size = new Size(305, 221);
            pnlEdicion.TabIndex = 23;
            pnlEdicion.Visible = false;
            // 
            // txtEdEmail
            // 
            txtEdEmail.Location = new Point(88, 120);
            txtEdEmail.Name = "txtEdEmail";
            txtEdEmail.Size = new Size(125, 27);
            txtEdEmail.TabIndex = 27;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 122);
            label1.Name = "label1";
            label1.Size = new Size(46, 20);
            label1.TabIndex = 26;
            label1.Text = "Email";
            // 
            // cmbRolEdicion
            // 
            cmbRolEdicion.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRolEdicion.FormattingEnabled = true;
            cmbRolEdicion.Location = new Point(88, 153);
            cmbRolEdicion.Name = "cmbRolEdicion";
            cmbRolEdicion.Size = new Size(125, 28);
            cmbRolEdicion.TabIndex = 25;
            // 
            // lblEdRol
            // 
            lblEdRol.AutoSize = true;
            lblEdRol.Location = new Point(14, 153);
            lblEdRol.Name = "lblEdRol";
            lblEdRol.Size = new Size(31, 20);
            lblEdRol.TabIndex = 24;
            lblEdRol.Text = "Rol";
            // 
            // txtEdNombre
            // 
            txtEdNombre.Location = new Point(88, 85);
            txtEdNombre.Name = "txtEdNombre";
            txtEdNombre.Size = new Size(125, 27);
            txtEdNombre.TabIndex = 19;
            // 
            // txtEdApellido
            // 
            txtEdApellido.Location = new Point(88, 54);
            txtEdApellido.Name = "txtEdApellido";
            txtEdApellido.Size = new Size(125, 27);
            txtEdApellido.TabIndex = 18;
            // 
            // txtEdDNI
            // 
            txtEdDNI.Location = new Point(88, 21);
            txtEdDNI.Name = "txtEdDNI";
            txtEdDNI.Size = new Size(125, 27);
            txtEdDNI.TabIndex = 17;
            // 
            // lblEdNombre
            // 
            lblEdNombre.AutoSize = true;
            lblEdNombre.Location = new Point(10, 89);
            lblEdNombre.Name = "lblEdNombre";
            lblEdNombre.Size = new Size(64, 20);
            lblEdNombre.TabIndex = 16;
            lblEdNombre.Text = "Nombre";
            // 
            // lblEdApellido
            // 
            lblEdApellido.AutoSize = true;
            lblEdApellido.Location = new Point(10, 58);
            lblEdApellido.Name = "lblEdApellido";
            lblEdApellido.Size = new Size(66, 20);
            lblEdApellido.TabIndex = 15;
            lblEdApellido.Text = "Apellido";
            // 
            // lblEdDNI
            // 
            lblEdDNI.AutoSize = true;
            lblEdDNI.Location = new Point(10, 24);
            lblEdDNI.Name = "lblEdDNI";
            lblEdDNI.Size = new Size(35, 20);
            lblEdDNI.TabIndex = 14;
            lblEdDNI.Text = "DNI";
            // 
            // btnCrear
            // 
            btnCrear.Location = new Point(887, 30);
            btnCrear.Name = "btnCrear";
            btnCrear.Size = new Size(138, 70);
            btnCrear.TabIndex = 24;
            btnCrear.Text = "CREAR";
            btnCrear.UseVisualStyleBackColor = true;
            btnCrear.Click += btnCrear_Click_1;
            // 
            // btnDesbloquear
            // 
            btnDesbloquear.Location = new Point(887, 106);
            btnDesbloquear.Name = "btnDesbloquear";
            btnDesbloquear.Size = new Size(138, 70);
            btnDesbloquear.TabIndex = 25;
            btnDesbloquear.Text = "DESBLOQUEAR";
            btnDesbloquear.UseVisualStyleBackColor = true;
            btnDesbloquear.Click += btnDesbloquear_Click_1;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(887, 191);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(138, 70);
            btnModificar.TabIndex = 26;
            btnModificar.Text = "MODIFICAR";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click_1;
            // 
            // btnActDesact
            // 
            btnActDesact.Location = new Point(887, 267);
            btnActDesact.Name = "btnActDesact";
            btnActDesact.Size = new Size(138, 70);
            btnActDesact.TabIndex = 27;
            btnActDesact.Text = "ACTIVAR / DESACTIVAR";
            btnActDesact.UseVisualStyleBackColor = true;
            btnActDesact.Click += btnActDesact_Click_1;
            // 
            // btnAplicar
            // 
            btnAplicar.Location = new Point(887, 351);
            btnAplicar.Name = "btnAplicar";
            btnAplicar.Size = new Size(138, 70);
            btnAplicar.TabIndex = 28;
            btnAplicar.Text = "APLICAR";
            btnAplicar.UseVisualStyleBackColor = true;
            btnAplicar.Click += btnAplicar_Click_1;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(887, 427);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(138, 70);
            btnCancelar.TabIndex = 29;
            btnCancelar.Text = "CANCELAR";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click_1;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(887, 510);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(138, 70);
            btnSalir.TabIndex = 30;
            btnSalir.Text = "SALIR";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click_1;
            // 
            // FrmGestionUsuarios
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1057, 584);
            Controls.Add(btnSalir);
            Controls.Add(btnCancelar);
            Controls.Add(btnAplicar);
            Controls.Add(btnActDesact);
            Controls.Add(btnModificar);
            Controls.Add(btnDesbloquear);
            Controls.Add(btnCrear);
            Controls.Add(pnlEdicion);
            Controls.Add(btnAplicarFiltro);
            Controls.Add(lblMensaje);
            Controls.Add(lblMensajeLabel);
            Controls.Add(lblApellidoValor);
            Controls.Add(lblApellidoLabel);
            Controls.Add(lblNombreValor);
            Controls.Add(lblNombreLabel);
            Controls.Add(cmbFilRol);
            Controls.Add(txtFilLogin);
            Controls.Add(txtFilNombres);
            Controls.Add(txtFilApellidos);
            Controls.Add(txtFilDNI);
            Controls.Add(lblFilRol);
            Controls.Add(lblFilLogin);
            Controls.Add(lblFilNombres);
            Controls.Add(lblFilApellidos);
            Controls.Add(lblFilDNI);
            Controls.Add(dgvSuperior);
            Controls.Add(lblNumUsuarios);
            Controls.Add(rdActivos);
            Controls.Add(rdTodos);
            Controls.Add(pnlHeader);
            Name = "FrmGestionUsuarios";
            Text = "FrmGestionUsuarios";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSuperior).EndInit();
            pnlEdicion.ResumeLayout(false);
            pnlEdicion.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Label lblTituloForm;
        private RadioButton rdTodos;
        private RadioButton rdActivos;
        private Label lblNumUsuarios;
        private DataGridView dgvSuperior;
        private Label lblFilDNI;
        private Label lblFilApellidos;
        private Label lblFilNombres;
        private Label lblFilLogin;
        private Label lblFilRol;
        private TextBox txtFilDNI;
        private TextBox txtFilApellidos;
        private TextBox txtFilNombres;
        private TextBox txtFilLogin;
        private ComboBox cmbFilRol;
        private Label lblNombreLabel;
        private Label lblNombreValor;
        private Label lblApellidoLabel;
        private Label lblApellidoValor;
        private Label lblMensajeLabel;
        private Label lblMensaje;
        private Button btnAplicarFiltro;
        private Panel pnlEdicion;
        private TextBox txtEdNombre;
        private TextBox txtEdApellido;
        private TextBox txtEdDNI;
        private Label lblEdNombre;
        private Label lblEdApellido;
        private Label lblEdDNI;
        private ComboBox cmbRolEdicion;
        private Label lblEdRol;
        private Button btnCrear;
        private Button btnDesbloquear;
        private Button btnModificar;
        private Button btnActDesact;
        private Button btnAplicar;
        private Button btnCancelar;
        private Button btnSalir;
        private TextBox txtEdEmail;
        private Label label1;
        private DataGridViewTextBoxColumn colDNI;
        private DataGridViewTextBoxColumn colApellidos;
        private DataGridViewTextBoxColumn colNombres;
        private DataGridViewTextBoxColumn colEmail;
        private DataGridViewTextBoxColumn colLogin;
        private DataGridViewTextBoxColumn colRol;
    }
}