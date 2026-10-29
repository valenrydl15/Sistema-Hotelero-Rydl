namespace GUI_HRS_480
{
    partial class FrmIdiomas
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
            cboIdioma = new ComboBox();
            btnAceptar = new Button();
            btnSalir = new Button();
            SuspendLayout();
            // 
            // cboIdioma
            // 
            cboIdioma.FormattingEnabled = true;
            cboIdioma.Location = new Point(140, 77);
            cboIdioma.Name = "cboIdioma";
            cboIdioma.Size = new Size(226, 28);
            cboIdioma.TabIndex = 0;
            // 
            // btnAceptar
            // 
            btnAceptar.ForeColor = Color.Black;
            btnAceptar.Location = new Point(439, 68);
            btnAceptar.Name = "btnAceptar";
            btnAceptar.Size = new Size(134, 44);
            btnAceptar.TabIndex = 1;
            btnAceptar.Text = "Aceptar";
            btnAceptar.UseVisualStyleBackColor = true;
            btnAceptar.Click += btnAceptar_Click;
            // 
            // btnSalir
            // 
            btnSalir.ForeColor = Color.Black;
            btnSalir.Location = new Point(439, 143);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(134, 44);
            btnSalir.TabIndex = 2;
            btnSalir.Text = "Salir";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click;
            // 
            // FrmIdiomas
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnSalir);
            Controls.Add(btnAceptar);
            Controls.Add(cboIdioma);
            ForeColor = Color.Black;
            Name = "FrmIdiomas";
            Text = "FrmIdiomas";
            ResumeLayout(false);
        }

        #endregion

        private ComboBox cboIdioma;
        private Button btnAceptar;
        private Button btnSalir;
    }
}