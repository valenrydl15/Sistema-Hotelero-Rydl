namespace GUI_HRS_480
{
    partial class FrmBitacora
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
            btnSalir = new Button();
            lblTitulo = new Label();
            dgv = new DataGridView();
            lblNombreLabel = new Label();
            lblNombreVal = new Label();
            lblApellidoLabel = new Label();
            lblApellidoVal = new Label();
            lblFilLogin = new Label();
            lblFIlFechaFin = new Label();
            lblFilFechaIni = new Label();
            lblFilModulo = new Label();
            lblFilEvento = new Label();
            lblFilCriticidad = new Label();
            cmbLogin = new ComboBox();
            cmbModulo = new ComboBox();
            cmbEvento = new ComboBox();
            cmbCriticidad = new ComboBox();
            dtpFechaIni = new DateTimePicker();
            dtpFechaFin = new DateTimePicker();
            btnLimpiar = new Button();
            btnAplicar = new Button();
            btnImprimir = new Button();
            colNumero = new DataGridViewTextBoxColumn();
            colLogin = new DataGridViewTextBoxColumn();
            colFecha = new DataGridViewTextBoxColumn();
            colHora = new DataGridViewTextBoxColumn();
            colModulo = new DataGridViewTextBoxColumn();
            colEvento = new DataGridViewTextBoxColumn();
            colCriticidad = new DataGridViewTextBoxColumn();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(22, 55, 95);
            pnlHeader.Controls.Add(btnSalir);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.ForeColor = Color.White;
            pnlHeader.Location = new Point(12, 12);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(870, 102);
            pnlHeader.TabIndex = 0;
            // 
            // btnSalir
            // 
            btnSalir.ForeColor = Color.Black;
            btnSalir.Location = new Point(737, 34);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(94, 29);
            btnSalir.TabIndex = 1;
            btnSalir.Text = "SALIR";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click_1;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Georgia", 22.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitulo.Location = new Point(32, 19);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(451, 43);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "BITACORA DE EVENTOS";
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new DataGridViewColumn[] { colNumero, colLogin, colFecha, colHora, colModulo, colEvento, colCriticidad });
            dgv.Location = new Point(12, 120);
            dgv.MultiSelect = false;
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.Size = new Size(870, 259);
            dgv.TabIndex = 1;
            dgv.SelectionChanged += dgv_SelectionChanged_1;
            // 
            // lblNombreLabel
            // 
            lblNombreLabel.AutoSize = true;
            lblNombreLabel.Location = new Point(224, 412);
            lblNombreLabel.Name = "lblNombreLabel";
            lblNombreLabel.Size = new Size(71, 20);
            lblNombreLabel.TabIndex = 2;
            lblNombreLabel.Text = "Nombre: ";
            // 
            // lblNombreVal
            // 
            lblNombreVal.AutoSize = true;
            lblNombreVal.Location = new Point(312, 412);
            lblNombreVal.Name = "lblNombreVal";
            lblNombreVal.Size = new Size(0, 20);
            lblNombreVal.TabIndex = 3;
            // 
            // lblApellidoLabel
            // 
            lblApellidoLabel.AutoSize = true;
            lblApellidoLabel.Location = new Point(501, 412);
            lblApellidoLabel.Name = "lblApellidoLabel";
            lblApellidoLabel.Size = new Size(69, 20);
            lblApellidoLabel.TabIndex = 4;
            lblApellidoLabel.Text = "Apellido:";
            // 
            // lblApellidoVal
            // 
            lblApellidoVal.AutoSize = true;
            lblApellidoVal.Location = new Point(598, 412);
            lblApellidoVal.Name = "lblApellidoVal";
            lblApellidoVal.Size = new Size(0, 20);
            lblApellidoVal.TabIndex = 5;
            // 
            // lblFilLogin
            // 
            lblFilLogin.AutoSize = true;
            lblFilLogin.Location = new Point(13, 465);
            lblFilLogin.Name = "lblFilLogin";
            lblFilLogin.Size = new Size(46, 20);
            lblFilLogin.TabIndex = 6;
            lblFilLogin.Text = "Login";
            // 
            // lblFIlFechaFin
            // 
            lblFIlFechaFin.AutoSize = true;
            lblFIlFechaFin.Location = new Point(501, 462);
            lblFIlFechaFin.Name = "lblFIlFechaFin";
            lblFIlFechaFin.Size = new Size(82, 20);
            lblFIlFechaFin.TabIndex = 7;
            lblFIlFechaFin.Text = "Fecha Final";
            // 
            // lblFilFechaIni
            // 
            lblFilFechaIni.AutoSize = true;
            lblFilFechaIni.Location = new Point(253, 462);
            lblFilFechaIni.Name = "lblFilFechaIni";
            lblFilFechaIni.Size = new Size(87, 20);
            lblFilFechaIni.TabIndex = 8;
            lblFilFechaIni.Text = "Fecha Inicio";
            // 
            // lblFilModulo
            // 
            lblFilModulo.AutoSize = true;
            lblFilModulo.Location = new Point(13, 528);
            lblFilModulo.Name = "lblFilModulo";
            lblFilModulo.Size = new Size(61, 20);
            lblFilModulo.TabIndex = 9;
            lblFilModulo.Text = "Modulo";
            // 
            // lblFilEvento
            // 
            lblFilEvento.AutoSize = true;
            lblFilEvento.Location = new Point(258, 533);
            lblFilEvento.Name = "lblFilEvento";
            lblFilEvento.Size = new Size(54, 20);
            lblFilEvento.TabIndex = 10;
            lblFilEvento.Text = "Evento";
            // 
            // lblFilCriticidad
            // 
            lblFilCriticidad.AutoSize = true;
            lblFilCriticidad.Location = new Point(501, 531);
            lblFilCriticidad.Name = "lblFilCriticidad";
            lblFilCriticidad.Size = new Size(73, 20);
            lblFilCriticidad.TabIndex = 11;
            lblFilCriticidad.Text = "Criticidad";
            // 
            // cmbLogin
            // 
            cmbLogin.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLogin.FormattingEnabled = true;
            cmbLogin.Location = new Point(80, 462);
            cmbLogin.Name = "cmbLogin";
            cmbLogin.Size = new Size(151, 28);
            cmbLogin.TabIndex = 12;
            // 
            // cmbModulo
            // 
            cmbModulo.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbModulo.FormattingEnabled = true;
            cmbModulo.Location = new Point(80, 525);
            cmbModulo.Name = "cmbModulo";
            cmbModulo.Size = new Size(151, 28);
            cmbModulo.TabIndex = 13;
            cmbModulo.SelectedIndexChanged += cmbModulo_SelectedIndexChanged_1;
            // 
            // cmbEvento
            // 
            cmbEvento.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEvento.FormattingEnabled = true;
            cmbEvento.Location = new Point(312, 528);
            cmbEvento.Name = "cmbEvento";
            cmbEvento.Size = new Size(151, 28);
            cmbEvento.TabIndex = 14;
            // 
            // cmbCriticidad
            // 
            cmbCriticidad.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCriticidad.FormattingEnabled = true;
            cmbCriticidad.Location = new Point(589, 528);
            cmbCriticidad.Name = "cmbCriticidad";
            cmbCriticidad.Size = new Size(151, 28);
            cmbCriticidad.TabIndex = 15;
            // 
            // dtpFechaIni
            // 
            dtpFechaIni.Checked = false;
            dtpFechaIni.Format = DateTimePickerFormat.Short;
            dtpFechaIni.Location = new Point(346, 460);
            dtpFechaIni.Name = "dtpFechaIni";
            dtpFechaIni.ShowCheckBox = true;
            dtpFechaIni.Size = new Size(149, 27);
            dtpFechaIni.TabIndex = 16;
            // 
            // dtpFechaFin
            // 
            dtpFechaFin.Checked = false;
            dtpFechaFin.Format = DateTimePickerFormat.Short;
            dtpFechaFin.Location = new Point(589, 458);
            dtpFechaFin.Name = "dtpFechaFin";
            dtpFechaFin.ShowCheckBox = true;
            dtpFechaFin.Size = new Size(149, 27);
            dtpFechaFin.TabIndex = 17;
            // 
            // btnLimpiar
            // 
            btnLimpiar.Location = new Point(160, 592);
            btnLimpiar.Name = "btnLimpiar";
            btnLimpiar.Size = new Size(94, 29);
            btnLimpiar.TabIndex = 18;
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.UseVisualStyleBackColor = true;
            btnLimpiar.Click += btnLimpiar_Click_1;
            // 
            // btnAplicar
            // 
            btnAplicar.Location = new Point(346, 592);
            btnAplicar.Name = "btnAplicar";
            btnAplicar.Size = new Size(94, 29);
            btnAplicar.TabIndex = 19;
            btnAplicar.Text = "Aplicar";
            btnAplicar.UseVisualStyleBackColor = true;
            btnAplicar.Click += btnAplicar_Click_1;
            // 
            // btnImprimir
            // 
            btnImprimir.Location = new Point(525, 592);
            btnImprimir.Name = "btnImprimir";
            btnImprimir.Size = new Size(94, 29);
            btnImprimir.TabIndex = 20;
            btnImprimir.Text = "Imprimir";
            btnImprimir.UseVisualStyleBackColor = true;
            btnImprimir.Click += btnImprimir_Click_1;
            // 
            // colNumero
            // 
            colNumero.HeaderText = "Numero";
            colNumero.MinimumWidth = 6;
            colNumero.Name = "colNumero";
            colNumero.ReadOnly = true;
            // 
            // colLogin
            // 
            colLogin.FillWeight = 75.08021F;
            colLogin.HeaderText = "Login";
            colLogin.MinimumWidth = 6;
            colLogin.Name = "colLogin";
            colLogin.ReadOnly = true;
            // 
            // colFecha
            // 
            colFecha.FillWeight = 75.08021F;
            colFecha.HeaderText = "Fecha";
            colFecha.MinimumWidth = 6;
            colFecha.Name = "colFecha";
            colFecha.ReadOnly = true;
            // 
            // colHora
            // 
            colHora.FillWeight = 75.08021F;
            colHora.HeaderText = "Hora";
            colHora.MinimumWidth = 6;
            colHora.Name = "colHora";
            colHora.ReadOnly = true;
            // 
            // colModulo
            // 
            colModulo.FillWeight = 75.08021F;
            colModulo.HeaderText = "Modulo";
            colModulo.MinimumWidth = 6;
            colModulo.Name = "colModulo";
            colModulo.ReadOnly = true;
            // 
            // colEvento
            // 
            colEvento.FillWeight = 75.08021F;
            colEvento.HeaderText = "Evento";
            colEvento.MinimumWidth = 6;
            colEvento.Name = "colEvento";
            colEvento.ReadOnly = true;
            // 
            // colCriticidad
            // 
            colCriticidad.FillWeight = 224.598938F;
            colCriticidad.HeaderText = "Criticidad";
            colCriticidad.MinimumWidth = 6;
            colCriticidad.Name = "colCriticidad";
            colCriticidad.ReadOnly = true;
            // 
            // FrmBitacora
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(990, 633);
            Controls.Add(btnImprimir);
            Controls.Add(btnAplicar);
            Controls.Add(btnLimpiar);
            Controls.Add(dtpFechaFin);
            Controls.Add(dtpFechaIni);
            Controls.Add(cmbCriticidad);
            Controls.Add(cmbEvento);
            Controls.Add(cmbModulo);
            Controls.Add(cmbLogin);
            Controls.Add(lblFilCriticidad);
            Controls.Add(lblFilEvento);
            Controls.Add(lblFilModulo);
            Controls.Add(lblFilFechaIni);
            Controls.Add(lblFIlFechaFin);
            Controls.Add(lblFilLogin);
            Controls.Add(lblApellidoVal);
            Controls.Add(lblApellidoLabel);
            Controls.Add(lblNombreVal);
            Controls.Add(lblNombreLabel);
            Controls.Add(dgv);
            Controls.Add(pnlHeader);
            Name = "FrmBitacora";
            Text = "FrmBitacora";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Button btnSalir;
        private Label lblTitulo;
        private DataGridView dgv;
        private Label lblNombreLabel;
        private Label lblNombreVal;
        private Label lblApellidoLabel;
        private Label lblApellidoVal;
        private Label lblFilLogin;
        private Label lblFIlFechaFin;
        private Label lblFilFechaIni;
        private Label lblFilModulo;
        private Label lblFilEvento;
        private Label lblFilCriticidad;
        private ComboBox cmbLogin;
        private ComboBox cmbModulo;
        private ComboBox cmbEvento;
        private ComboBox cmbCriticidad;
        private DateTimePicker dtpFechaIni;
        private DateTimePicker dtpFechaFin;
        private Button btnLimpiar;
        private Button btnAplicar;
        private Button btnImprimir;
        private DataGridViewTextBoxColumn colNumero;
        private DataGridViewTextBoxColumn colLogin;
        private DataGridViewTextBoxColumn colFecha;
        private DataGridViewTextBoxColumn colHora;
        private DataGridViewTextBoxColumn colModulo;
        private DataGridViewTextBoxColumn colEvento;
        private DataGridViewTextBoxColumn colCriticidad;
    }
}