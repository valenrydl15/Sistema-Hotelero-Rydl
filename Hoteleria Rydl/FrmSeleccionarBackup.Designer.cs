namespace GUI_HRS_480
{
    partial class FrmSeleccionarBackup
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
            _btnRestore = new Button();
            _btnCancelar = new Button();
            _lstBackups = new ListBox();
            _blTitulo = new Label();
            _lblDescripcion = new Label();
            _lblEstado = new Label();
            SuspendLayout();
            // 
            // _btnRestore
            // 
            _btnRestore.Location = new Point(417, 37);
            _btnRestore.Name = "_btnRestore";
            _btnRestore.Size = new Size(155, 52);
            _btnRestore.TabIndex = 0;
            _btnRestore.Text = "Restaurar";
            _btnRestore.UseVisualStyleBackColor = true;
            _btnRestore.Click += _btnRestore_Click;
            // 
            // _btnCancelar
            // 
            _btnCancelar.Location = new Point(730, 320);
            _btnCancelar.Name = "_btnCancelar";
            _btnCancelar.Size = new Size(155, 52);
            _btnCancelar.TabIndex = 1;
            _btnCancelar.Text = "Cancelar";
            _btnCancelar.UseVisualStyleBackColor = true;
            _btnCancelar.Click += _btnCancelar_Click;
            // 
            // _lstBackups
            // 
            _lstBackups.FormattingEnabled = true;
            _lstBackups.Location = new Point(283, 155);
            _lstBackups.Name = "_lstBackups";
            _lstBackups.Size = new Size(262, 184);
            _lstBackups.TabIndex = 2;
            // 
            // _blTitulo
            // 
            _blTitulo.AutoSize = true;
            _blTitulo.Location = new Point(39, 37);
            _blTitulo.Name = "_blTitulo";
            _blTitulo.Size = new Size(179, 20);
            _blTitulo.TabIndex = 3;
            _blTitulo.Text = "Restore de Base de Datos";
            // 
            // _lblDescripcion
            // 
            _lblDescripcion.AutoSize = true;
            _lblDescripcion.Location = new Point(594, 53);
            _lblDescripcion.Name = "_lblDescripcion";
            _lblDescripcion.Size = new Size(416, 20);
            _lblDescripcion.TabIndex = 4;
            _lblDescripcion.Text = "Seleccioná el backup al que querés restaurar la base de datos";
            // 
            // _lblEstado
            // 
            _lblEstado.AutoSize = true;
            _lblEstado.Location = new Point(681, 169);
            _lblEstado.Name = "_lblEstado";
            _lblEstado.Size = new Size(50, 20);
            _lblEstado.TabIndex = 5;
            _lblEstado.Text = "label1";
            // 
            // FrmSeleccionarBackup
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1095, 520);
            Controls.Add(_lblEstado);
            Controls.Add(_lblDescripcion);
            Controls.Add(_blTitulo);
            Controls.Add(_lstBackups);
            Controls.Add(_btnCancelar);
            Controls.Add(_btnRestore);
            Name = "FrmSeleccionarBackup";
            Text = "FrmSeleccionarBackup";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button _btnRestore;
        private Button _btnCancelar;
        private ListBox _lstBackups;
        private Label _blTitulo;
        private Label _lblDescripcion;
        private Label _lblEstado;
    }
}