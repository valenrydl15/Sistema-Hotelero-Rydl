namespace GUI_HRS_480
{
    partial class FrmGeneradorFamilias
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
            
            lstFamiliasExistentes = new ListBox();
            chkSeleccionados = new CheckedListBox();
            txtNombre = new TextBox();
            btnGenerar = new Button();
            btnModificar = new Button();
            btnEliminar = new Button();
            btnAplicar = new Button();
            btnSalir = new Button();
            lblNombre = new Label();
            lblFamiliasExistentes = new Label();
            lblSeleccionados = new Label();
            lblFiltro = new Label();
            lblTitulo = new Label();
            pnlHeader = new Panel();
            cmbFiltro = new ComboBox();
            lblDisponibles = new Label();
            lstDisponibles = new ListBox();
            btnVerPermisos = new Button();
            lblMensaje = new Label();
            btnCancelar = new Button();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // lstFamiliasExistentes
            // 
            lstFamiliasExistentes.FormattingEnabled = true;
            lstFamiliasExistentes.Location = new Point(12, 125);
            lstFamiliasExistentes.Name = "lstFamiliasExistentes";
            lstFamiliasExistentes.Size = new Size(433, 144);
            lstFamiliasExistentes.TabIndex = 0;
            lstFamiliasExistentes.SelectedIndexChanged += lstFamiliasExistentes_SelectedIndexChanged;
            // 
            // chkSeleccionados
            // 
            chkSeleccionados.CheckOnClick = true;
            chkSeleccionados.FormattingEnabled = true;
            chkSeleccionados.Location = new Point(12, 305);
            chkSeleccionados.Name = "chkSeleccionados";
            chkSeleccionados.Size = new Size(433, 224);
            chkSeleccionados.TabIndex = 1;
            chkSeleccionados.DoubleClick += chkSeleccionados_DoubleClick;
                  // 
            // txtNombre
            // 
            txtNombre.Enabled = false;
            txtNombre.Location = new Point(710, 18);
            txtNombre.Name = "txtNombre";
            txtNombre.Size = new Size(125, 27);
            txtNombre.TabIndex = 3;
            // 
            // btnGenerar
            // 
            btnGenerar.Location = new Point(861, 18);
            btnGenerar.Name = "btnGenerar";
            btnGenerar.Size = new Size(117, 34);
            btnGenerar.TabIndex = 4;
            btnGenerar.Text = "Generar";
            btnGenerar.UseVisualStyleBackColor = true;
            btnGenerar.Click += btnGenerar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(581, 390);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(117, 34);
            btnModificar.TabIndex = 5;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnEliminar
            // 
            btnEliminar.Location = new Point(581, 453);
            btnEliminar.Name = "btnEliminar";
            btnEliminar.Size = new Size(117, 34);
            btnEliminar.TabIndex = 6;
            btnEliminar.Text = "Eliminar";
            btnEliminar.UseVisualStyleBackColor = true;
            btnEliminar.Click += btnEliminar_Click;
            // 
            // btnAplicar
            // 
            btnAplicar.Location = new Point(581, 500);
            btnAplicar.Name = "btnAplicar";
            btnAplicar.Size = new Size(117, 34);
            btnAplicar.TabIndex = 7;
            btnAplicar.Text = "Aplicar";
            btnAplicar.UseVisualStyleBackColor = true;
            btnAplicar.Click += btnAplicar_Click;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(581, 556);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(117, 34);
            btnSalir.TabIndex = 8;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // lblNombre
            // 
            lblNombre.AutoSize = true;
            lblNombre.Location = new Point(619, 18);
            lblNombre.Name = "lblNombre";
            lblNombre.Size = new Size(67, 20);
            lblNombre.TabIndex = 10;
            lblNombre.Text = "Nombre:";
            // 
            // lblFamiliasExistentes
            // 
            lblFamiliasExistentes.AutoSize = true;
            lblFamiliasExistentes.Location = new Point(17, 87);
            lblFamiliasExistentes.Name = "lblFamiliasExistentes";
            lblFamiliasExistentes.Size = new Size(131, 20);
            lblFamiliasExistentes.TabIndex = 11;
            lblFamiliasExistentes.Text = "Familias Existentes\r\n";
            // 
            // lblSeleccionados
            // 
            lblSeleccionados.AutoSize = true;
            lblSeleccionados.Location = new Point(78, 282);
            lblSeleccionados.Name = "lblSeleccionados";
            lblSeleccionados.Size = new Size(198, 20);
            lblSeleccionados.TabIndex = 12;
            lblSeleccionados.Text = "Componentes seleccionados";
            // 
            // lblFiltro
            // 
            lblFiltro.AutoSize = true;
            lblFiltro.Location = new Point(910, 87);
            lblFiltro.Name = "lblFiltro";
            lblFiltro.Size = new Size(120, 20);
            lblFiltro.TabIndex = 13;
            lblFiltro.Text = "Familia-Permisos";
            // 
            // lblTitulo
            // 
            lblTitulo.Location = new Point(98, 13);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(155, 33);
            lblTitulo.TabIndex = 14;
            lblTitulo.Text = "Gestion de Familias";
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Location = new Point(12, 12);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(397, 60);
            pnlHeader.TabIndex = 15;
            // 
            // cmbFiltro
            // 
            cmbFiltro.FormattingEnabled = true;
            cmbFiltro.Location = new Point(910, 110);
            cmbFiltro.Name = "cmbFiltro";
            cmbFiltro.Size = new Size(151, 28);
            cmbFiltro.TabIndex = 16;
            cmbFiltro.SelectedIndexChanged += cmbFiltro_SelectedIndexChanged;
            // 
            // lblDisponibles
            // 
            lblDisponibles.AutoSize = true;
            lblDisponibles.Location = new Point(740, 217);
            lblDisponibles.Name = "lblDisponibles";
            lblDisponibles.Size = new Size(256, 20);
            lblDisponibles.TabIndex = 17;
            lblDisponibles.Text = "Disponibles (doble clic para agregar)";
            // 
            // lstDisponibles
            // 
            lstDisponibles.FormattingEnabled = true;
            lstDisponibles.Location = new Point(740, 254);
            lstDisponibles.Name = "lstDisponibles";
            lstDisponibles.Size = new Size(426, 264);
            lstDisponibles.TabIndex = 18;
            lstDisponibles.DoubleClick += lstDisponibles_DoubleClick;
            // 
            // btnVerPermisos
            // 
            btnVerPermisos.Location = new Point(1178, 235);
            btnVerPermisos.Name = "btnVerPermisos";
            btnVerPermisos.Size = new Size(117, 34);
            btnVerPermisos.TabIndex = 19;
            btnVerPermisos.Text = "Ver Permisos";
            btnVerPermisos.UseVisualStyleBackColor = true;
            btnVerPermisos.Click += btnVerPermisos_Click;
            // 
            // lblMensaje
            // 
            lblMensaje.AutoSize = true;
            lblMensaje.Location = new Point(1037, 25);
            lblMensaje.Name = "lblMensaje";
            lblMensaje.Size = new Size(110, 20);
            lblMensaje.TabIndex = 20;
            lblMensaje.Text = "Modo Consulta";
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(1168, 553);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(117, 34);
            btnCancelar.TabIndex = 21;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // FrmGeneradorFamilias
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1324, 599);
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
            Controls.Add(btnGenerar);
            Controls.Add(txtNombre);
            Controls.Add(chkSeleccionados);
            Controls.Add(lstFamiliasExistentes);
            MaximizeBox = false;
            Name = "FrmGeneradorFamilias";
            Text = "FrmGeneradorFamilias";
            pnlHeader.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox lstFamiliasExistentes;
        private CheckedListBox chkSeleccionados;
       
        private TextBox txtNombre;
        private Button btnGenerar;
        private Button btnModificar;
        private Button btnEliminar;
        private Button btnAplicar;
        private Button btnSalir;
        private Label lblNombre;
        private Label lblFamiliasExistentes;
        private Label lblSeleccionados;
        private Label lblFiltro;
        private Label lblTitulo;
        private Panel pnlHeader;
        private ComboBox cmbFiltro;
        private Label lblDisponibles;
        private ListBox lstDisponibles;
        private Button btnVerPermisos;
        private Label lblMensaje;
        private Button btnCancelar;

       
        }
}