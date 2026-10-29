namespace GUI_HRS_480
{
    partial class FrmGeneradorRoles
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
            btnCancelar = new Button();
            lblMensaje = new Label();
            btnVerPermisos = new Button();
            lstDisponibles = new ListBox();
            lblDisponibles = new Label();
            cmbFiltro = new ComboBox();
            pnlHeader = new Panel();
            lblTitulo = new Label();
            lblFiltro = new Label();
            lblSeleccionados = new Label();
            lblFamiliasExistentes = new Label();
            lblNombre = new Label();
            btnSalir = new Button();
            btnAplicar = new Button();
            btnEliminar = new Button();
            btnModificar = new Button();
            btnCrear = new Button();
            txtNombre = new TextBox();
            chkSeleccionados = new CheckedListBox();
            lstRolesExistentes = new ListBox();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(1220, 547);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(117, 34);
            btnCancelar.TabIndex = 41;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // lblMensaje
            // 
            lblMensaje.AutoSize = true;
            lblMensaje.Location = new Point(1220, 19);
            lblMensaje.Name = "lblMensaje";
            lblMensaje.Size = new Size(110, 20);
            lblMensaje.TabIndex = 40;
            lblMensaje.Text = "Modo Consulta";
            // 
            // btnVerPermisos
            // 
            btnVerPermisos.Location = new Point(1230, 229);
            btnVerPermisos.Name = "btnVerPermisos";
            btnVerPermisos.Size = new Size(117, 34);
            btnVerPermisos.TabIndex = 39;
            btnVerPermisos.Text = "Ver Permisos";
            btnVerPermisos.UseVisualStyleBackColor = true;
            btnVerPermisos.Click += btnVerPermisos_Click;
            // 
            // lstDisponibles
            // 
            lstDisponibles.FormattingEnabled = true;
            lstDisponibles.Location = new Point(792, 248);
            lstDisponibles.Name = "lstDisponibles";
            lstDisponibles.Size = new Size(426, 264);
            lstDisponibles.TabIndex = 38;
            lstDisponibles.DoubleClick += lstDisponibles_DoubleClick;
            // 
            // lblDisponibles
            // 
            lblDisponibles.AutoSize = true;
            lblDisponibles.Location = new Point(792, 211);
            lblDisponibles.Name = "lblDisponibles";
            lblDisponibles.Size = new Size(256, 20);
            lblDisponibles.TabIndex = 37;
            lblDisponibles.Text = "Disponibles (doble clic para agregar)";
            // 
            // cmbFiltro
            // 
            cmbFiltro.FormattingEnabled = true;
            cmbFiltro.Location = new Point(962, 104);
            cmbFiltro.Name = "cmbFiltro";
            cmbFiltro.Size = new Size(151, 28);
            cmbFiltro.TabIndex = 36;
            cmbFiltro.SelectedIndexChanged += cmbFiltro_SelectedIndexChanged;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Location = new Point(64, 6);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(397, 60);
            pnlHeader.TabIndex = 35;
            // 
            // lblTitulo
            // 
            lblTitulo.Location = new Point(66, 13);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(155, 33);
            lblTitulo.TabIndex = 14;
            lblTitulo.Text = "Gestion de Roles";
            // 
            // lblFiltro
            // 
            lblFiltro.AutoSize = true;
            lblFiltro.Location = new Point(962, 81);
            lblFiltro.Name = "lblFiltro";
            lblFiltro.Size = new Size(120, 20);
            lblFiltro.TabIndex = 34;
            lblFiltro.Text = "Familia-Permisos";
            // 
            // lblSeleccionados
            // 
            lblSeleccionados.AutoSize = true;
            lblSeleccionados.Location = new Point(130, 276);
            lblSeleccionados.Name = "lblSeleccionados";
            lblSeleccionados.Size = new Size(198, 20);
            lblSeleccionados.TabIndex = 33;
            lblSeleccionados.Text = "Componentes seleccionados";
            // 
            // lblFamiliasExistentes
            // 
            lblFamiliasExistentes.AutoSize = true;
            lblFamiliasExistentes.Location = new Point(69, 81);
            lblFamiliasExistentes.Name = "lblFamiliasExistentes";
            lblFamiliasExistentes.Size = new Size(114, 20);
            lblFamiliasExistentes.TabIndex = 32;
            lblFamiliasExistentes.Text = "Roles Existentes\r\n";
            // 
            // lblNombre
            // 
            lblNombre.AutoSize = true;
            lblNombre.Location = new Point(671, 12);
            lblNombre.Name = "lblNombre";
            lblNombre.Size = new Size(67, 20);
            lblNombre.TabIndex = 31;
            lblNombre.Text = "Nombre:";
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(633, 550);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(117, 34);
            btnSalir.TabIndex = 29;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // btnAplicar
            // 
            btnAplicar.Location = new Point(633, 494);
            btnAplicar.Name = "btnAplicar";
            btnAplicar.Size = new Size(117, 34);
            btnAplicar.TabIndex = 28;
            btnAplicar.Text = "Aplicar";
            btnAplicar.UseVisualStyleBackColor = true;
            btnAplicar.Click += btnAplicar_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(633, 447);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(117, 34);
            btnEliminar.TabIndex = 27;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(633, 384);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(117, 34);
            btnModificar.TabIndex = 26;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnCrear
            // 
            btnCrear.Location = new Point(913, 12);
            btnCrear.Name = "btnCrear";
            btnCrear.Size = new Size(117, 34);
            btnCrear.TabIndex = 25;
            btnCrear.Text = "Crear";
            btnCrear.UseVisualStyleBackColor = true;
            btnCrear.Click += btnCrear_Click;
            // 
            // txtNombre
            // 
            txtNombre.Enabled = false;
            txtNombre.Location = new Point(762, 12);
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new Size(125, 27);
            txtNombre.TabIndex = 24;
            // 
            // chkSeleccionados
            // 
            chkSeleccionados.CheckOnClick = true;
            chkSeleccionados.FormattingEnabled = true;
            chkSeleccionados.Location = new Point(64, 299);
            chkSeleccionados.Name = "chkSeleccionados";
            chkSeleccionados.Size = new Size(433, 224);
            chkSeleccionados.TabIndex = 23;
            
            chkSeleccionados.DoubleClick += chkSeleccionados_DoubleClick;
            // 
            // lstRolesExistentes
            // 
            lstRolesExistentes.FormattingEnabled = true;
            lstRolesExistentes.Location = new Point(64, 119);
            lstRolesExistentes.Name = "lstRolesExistentes";
            lstRolesExistentes.Size = new Size(433, 144);
            lstRolesExistentes.TabIndex = 22;
            lstRolesExistentes.SelectedIndexChanged += lstRolesExistentes_SelectedIndexChanged;
            // 
            // FrmGeneradorRoles
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1411, 590);
            Controls.Add(btnCancelar);
            Controls.Add(lblMensaje);
            Controls.Add(btnVerPermisos);
            Controls.Add(lstDisponibles);
            Controls.Add(lblDisponibles);
            Controls.Add(cmbFiltro);
            Controls.Add(pnlHeader);
            Controls.Add(lblFiltro);
            Controls.Add(lblSeleccionados);
            Controls.Add(lblFamiliasExistentes);
            Controls.Add(lblNombre);
            Controls.Add(btnSalir);
            Controls.Add(btnAplicar);
            Controls.Add(btnEliminar);
            Controls.Add(btnModificar);
            Controls.Add(btnCrear);
            Controls.Add(txtNombre);
            Controls.Add(chkSeleccionados);
            Controls.Add(lstRolesExistentes);
            Name = "FrmGeneradorRoles";
            Text = "FrmGeneradorRoles";
            pnlHeader.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnCancelar;
        private Label lblMensaje;
        private Button btnVerPermisos;
        private ListBox lstDisponibles;
        private Label lblDisponibles;
        private ComboBox cmbFiltro;
        private Panel pnlHeader;
        private Label lblTitulo;
        private Label lblFiltro;
        private Label lblSeleccionados;
        private Label lblFamiliasExistentes;
        private Label lblNombre;
        private Button btnSalir;
        private Button btnAplicar;
        private Button btnEliminar;
        private Button btnModificar;
        private Button btnCrear;
        private TextBox txtNombre;
        private CheckedListBox chkSeleccionados;
        private ListBox lstRolesExistentes;
    }
}