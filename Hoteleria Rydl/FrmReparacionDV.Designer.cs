namespace GUI_HRS_480
{
    partial class FrmReparacionDV
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
            btnRecalcular = new Button();
            btnRestore = new Button();
            btnSalir = new Button();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            lblCantidad = new Label();
            lblOpcion1 = new Label();
            lblOpcion2 = new Label();
            lblBackupLabel = new Label();
            lblEstado = new Label();
            cmbBackups = new ComboBox();
            lblLateralTitulo = new Label();
            lblLateralDesc = new Label();
            lstInconsistencias = new ListBox();
            SuspendLayout();
            // 
            // btnRecalcular
            // 
            btnRecalcular.Location = new Point(465, 99);
            btnRecalcular.Name = "btnRecalcular";
            btnRecalcular.Size = new Size(184, 78);
            btnRecalcular.TabIndex = 0;
            btnRecalcular.Text = "Recalcular";
            btnRecalcular.UseVisualStyleBackColor = true;
            btnRecalcular.Click += btnRecalcular_Click;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(465, 208);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(184, 78);
            btnRestore.TabIndex = 1;
            btnRestore.Text = "Restore";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(465, 316);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(184, 78);
            btnSalir.TabIndex = 2;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Location = new Point(25, 20);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(265, 20);
            lblTitulo.TabIndex = 3;
            lblTitulo.Text = "Inconsistencias en el Digito Verificador";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Location = new Point(25, 80);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Size = new Size(348, 40);
            lblSubtitulo.TabIndex = 4;
            lblSubtitulo.Text = "Se detectó una alteración en los datos del sistema. \r\nSeleccioná una opción de reparación.";
            // 
            // lblCantidad
            // 
            lblCantidad.AutoSize = true;
            lblCantidad.Location = new Point(700, 50);
            lblCantidad.Name = "lblCantidad";
            lblCantidad.Size = new Size(21, 20);
            lblCantidad.TabIndex = 5;
            lblCantidad.Text = "\"\"";
            // 
            // lblOpcion1
            // 
            lblOpcion1.AutoSize = true;
            lblOpcion1.Location = new Point(700, 119);
            lblOpcion1.Name = "lblOpcion1";
            lblOpcion1.Size = new Size(77, 20);
            lblOpcion1.TabIndex = 6;
            lblOpcion1.Text = "Recalcular";
            // 
            // lblOpcion2
            // 
            lblOpcion2.AutoSize = true;
            lblOpcion2.Location = new Point(700, 222);
            lblOpcion2.Name = "lblOpcion2";
            lblOpcion2.Size = new Size(59, 20);
            lblOpcion2.TabIndex = 7;
            lblOpcion2.Text = "Restore";
            // 
            // lblBackupLabel
            // 
            lblBackupLabel.AutoSize = true;
            lblBackupLabel.Location = new Point(696, 316);
            lblBackupLabel.Name = "lblBackupLabel";
            lblBackupLabel.Size = new Size(135, 20);
            lblBackupLabel.TabIndex = 8;
            lblBackupLabel.Text = "Backup a Restaurar";
            // 
            // lblEstado
            // 
            lblEstado.AutoSize = true;
            lblEstado.Location = new Point(734, 425);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new Size(21, 20);
            lblEstado.TabIndex = 9;
            lblEstado.Text = "\"\"";
            // 
            // cmbBackups
            // 
            cmbBackups.FormattingEnabled = true;
            cmbBackups.Location = new Point(936, 379);
            cmbBackups.Name = "cmbBackups";
            cmbBackups.Size = new Size(151, 28);
            cmbBackups.TabIndex = 10;
            // 
            // lblLateralTitulo
            // 
            lblLateralTitulo.AutoSize = true;
            lblLateralTitulo.Location = new Point(1070, 20);
            lblLateralTitulo.Name = "lblLateralTitulo";
            lblLateralTitulo.Size = new Size(180, 20);
            lblLateralTitulo.TabIndex = 11;
            lblLateralTitulo.Text = "Detalle de Inconsistencias";
            // 
            // lblLateralDesc
            // 
            lblLateralDesc.AutoSize = true;
            lblLateralDesc.Location = new Point(978, 67);
            lblLateralDesc.Name = "lblLateralDesc";
            lblLateralDesc.Size = new Size(413, 20);
            lblLateralDesc.TabIndex = 12;
            lblLateralDesc.Text = "Tablas con diferencias entre el DV almacenado y el calculado";
            // 
            // lstInconsistencias
            // 
            lstInconsistencias.FormattingEnabled = true;
            lstInconsistencias.Location = new Point(1017, 119);
            lstInconsistencias.Name = "lstInconsistencias";
            lstInconsistencias.SelectionMode = SelectionMode.None;
            lstInconsistencias.Size = new Size(340, 204);
            lstInconsistencias.TabIndex = 13;
            // 
            // FrmReparacionDV
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1391, 506);
            Controls.Add(lstInconsistencias);
            Controls.Add(lblLateralDesc);
            Controls.Add(lblLateralTitulo);
            Controls.Add(cmbBackups);
            Controls.Add(lblEstado);
            Controls.Add(lblBackupLabel);
            Controls.Add(lblOpcion2);
            Controls.Add(lblOpcion1);
            Controls.Add(lblCantidad);
            Controls.Add(lblSubtitulo);
            Controls.Add(lblTitulo);
            Controls.Add(btnSalir);
            Controls.Add(btnRestore);
            Controls.Add(btnRecalcular);
            Name = "FrmReparacionDV";
            Text = "FrmReparacionDV";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnRecalcular;
        private Button btnRestore;
        private Button btnSalir;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblCantidad;
        private Label lblOpcion1;
        private Label lblOpcion2;
        private Label lblBackupLabel;
        private Label lblEstado;
        private ComboBox cmbBackups;
        private Label lblLateralTitulo;
        private Label lblLateralDesc;
        private ListBox lstInconsistencias;

    }
}