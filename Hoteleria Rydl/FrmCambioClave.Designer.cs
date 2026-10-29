namespace GUI_HRS_480   
{
    partial class FrmCambioClave
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
            lblTitulo = new Label();
            lblActual = new Label();
            lblNueva = new Label();
            txtActual = new TextBox();
            txtNueva = new TextBox();
            lblConfirma = new Label();
            txtConfirma = new TextBox();
            lblMsg = new Label();
            btnAceptar = new Button();
            btnCancelar = new Button();
            txtNombreUsuario = new TextBox();
            label1 = new Label();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(22, 55, 95);
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(800, 157);
            pnlHeader.TabIndex = 0;
            // 
            // lblTitulo
            // 
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Georgia", 22.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Location = new Point(219, 50);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Size = new Size(358, 43);
            lblTitulo.TabIndex = 0;
            lblTitulo.Text = "Cambiar Contraseña";
            // 
            // lblActual
            // 
            lblActual.AutoSize = true;
            lblActual.Location = new Point(25, 270);
            lblActual.Name = "lblActual";
            lblActual.Size = new Size(127, 20);
            lblActual.TabIndex = 1;
            lblActual.Text = "Contraseña actual";
            // 
            // lblNueva
            // 
            lblNueva.AutoSize = true;
            lblNueva.Location = new Point(26, 344);
            lblNueva.Name = "lblNueva";
            lblNueva.Size = new Size(126, 20);
            lblNueva.TabIndex = 2;
            lblNueva.Text = "Contraseña nueva";
            // 
            // txtActual
            // 
            txtActual.Location = new Point(237, 263);
            txtActual.Name = "txtActual";
            txtActual.Size = new Size(172, 27);
            txtActual.TabIndex = 3;
            txtActual.UseSystemPasswordChar = true;
            // 
            // txtNueva
            // 
            txtNueva.Location = new Point(237, 341);
            txtNueva.Name = "txtNueva";
            txtNueva.Size = new Size(172, 27);
            txtNueva.TabIndex = 4;
            txtNueva.UseSystemPasswordChar = true;
            // 
            // lblConfirma
            // 
            lblConfirma.AutoSize = true;
            lblConfirma.Location = new Point(26, 394);
            lblConfirma.Name = "lblConfirma";
            lblConfirma.Size = new Size(194, 20);
            lblConfirma.TabIndex = 5;
            lblConfirma.Text = "Confirmar nueva contraseña";
            // 
            // txtConfirma
            // 
            txtConfirma.Location = new Point(237, 391);
            txtConfirma.Name = "txtConfirma";
            txtConfirma.Size = new Size(172, 27);
            txtConfirma.TabIndex = 6;
            txtConfirma.UseSystemPasswordChar = true;
            // 
            // lblMsg
            // 
            lblMsg.AutoSize = true;
            lblMsg.Location = new Point(477, 187);
            lblMsg.Name = "lblMsg";
            lblMsg.Size = new Size(0, 20);
            lblMsg.TabIndex = 7;
            // 
            // btnAceptar
            // 
            btnAceptar.Location = new Point(659, 177);
            btnAceptar.Name = "btnAceptar";
            btnAceptar.Size = new Size(108, 52);
            btnAceptar.TabIndex = 8;
            btnAceptar.Text = "ACEPTAR";
            btnAceptar.UseVisualStyleBackColor = true;
            btnAceptar.Click += btnAceptar_Click_1;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(659, 250);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(108, 52);
            btnCancelar.TabIndex = 9;
            btnCancelar.Text = "CANCELAR";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click_1;
            // 
            // txtNombreUsuario
            // 
            txtNombreUsuario.Location = new Point(237, 212);
            txtNombreUsuario.Name = "txtNombreUsuario";
            txtNombreUsuario.Size = new Size(172, 27);
            txtNombreUsuario.TabIndex = 10;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 215);
            label1.Name = "label1";
            label1.Size = new Size(102, 20);
            label1.TabIndex = 11;
            label1.Text = "Login-Usuario";
            // 
            // FrmCambioClave
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label1);
            Controls.Add(txtNombreUsuario);
            Controls.Add(btnCancelar);
            Controls.Add(btnAceptar);
            Controls.Add(lblMsg);
            Controls.Add(txtConfirma);
            Controls.Add(lblConfirma);
            Controls.Add(txtNueva);
            Controls.Add(txtActual);
            Controls.Add(lblNueva);
            Controls.Add(lblActual);
            Controls.Add(pnlHeader);
            Name = "FrmCambioClave";
            Text = "FrmCambioClave";
            Load += FrmCambioClave_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Label lblTitulo;
        private Label lblActual;
        private Label lblNueva;
        private TextBox txtActual;
        private TextBox txtNueva;
        private Label lblConfirma;
        private TextBox txtConfirma;
        private Label lblMsg;
        private Button btnAceptar;
        private Button btnCancelar;
        private TextBox txtNombreUsuario;
        private Label label1;
    }
}