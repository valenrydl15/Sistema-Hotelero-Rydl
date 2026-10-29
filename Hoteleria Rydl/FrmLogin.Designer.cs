
namespace GUI_HRS_480
{
    partial class FrmLogin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            lblSubtitulo = new Label();
            lblHotel = new Label();
            lblUsuario = new Label();
            lblClave = new Label();
            lblInfo = new Label();
            lblMensaje = new Label();
            txtUsuario = new TextBox();
            txtClave = new TextBox();
            btnIngresar = new Button();
            btnSalir = new Button();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader (panel de marca, a la izquierda)
            // 
            pnlHeader.BackColor = Color.FromArgb(20, 45, 80);
            pnlHeader.Controls.Add(lblSubtitulo);
            pnlHeader.Controls.Add(lblHotel);
            pnlHeader.Dock = DockStyle.Left;
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(380, 560);
            pnlHeader.TabIndex = 0;
            // 
            // lblHotel
            // 
            lblHotel.AutoSize = true;
            lblHotel.Font = new Font("Georgia", 30F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHotel.ForeColor = Color.White;
            lblHotel.Location = new Point(36, 190);
            lblHotel.Name = "lblHotel";
            lblHotel.TabIndex = 0;
            lblHotel.Text = "HOTEL RYDL";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitulo.ForeColor = Color.FromArgb(201, 162, 39);
            lblSubtitulo.Location = new Point(40, 252);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "Sistema de Gestion Hotelera";
            // 
            // lblUsuario
            // 
            lblUsuario.AutoSize = true;
            lblUsuario.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsuario.ForeColor = Color.FromArgb(110, 122, 142);
            lblUsuario.Location = new Point(450, 170);
            lblUsuario.Name = "lblUsuario";
            lblUsuario.TabIndex = 2;
            lblUsuario.Text = "Usuario";
            // 
            // txtUsuario
            // 
            txtUsuario.Font = new Font("Segoe UI", 11F);
            txtUsuario.Location = new Point(450, 194);
            txtUsuario.Name = "txtUsuario";
            txtUsuario.Size = new Size(380, 32);
            txtUsuario.TabIndex = 0;
            // 
            // lblClave
            // 
            lblClave.AutoSize = true;
            lblClave.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblClave.ForeColor = Color.FromArgb(110, 122, 142);
            lblClave.Location = new Point(450, 246);
            lblClave.Name = "lblClave";
            lblClave.TabIndex = 3;
            lblClave.Text = "Contraseña";
            // 
            // txtClave
            // 
            txtClave.Font = new Font("Segoe UI", 11F);
            txtClave.Location = new Point(450, 270);
            txtClave.Name = "txtClave";
            txtClave.Size = new Size(380, 32);
            txtClave.TabIndex = 1;
            txtClave.UseSystemPasswordChar = true;
            // 
            // lblMensaje
            // 
            lblMensaje.AutoSize = false;
            lblMensaje.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMensaje.ForeColor = Color.FromArgb(192, 57, 43);
            lblMensaje.Location = new Point(450, 312);
            lblMensaje.Name = "lblMensaje";
            lblMensaje.Size = new Size(380, 40);
            lblMensaje.TabIndex = 5;
            // 
            // btnIngresar
            // 
            btnIngresar.Location = new Point(450, 362);
            btnIngresar.Name = "btnIngresar";
            btnIngresar.Size = new Size(380, 46);
            btnIngresar.TabIndex = 2;
            btnIngresar.Text = "INGRESAR";
            btnIngresar.UseVisualStyleBackColor = true;
            btnIngresar.Click += btnIngresar_Click_1;
            // 
            // btnSalir
            // 
            btnSalir.Location = new Point(450, 418);
            btnSalir.Name = "btnSalir";
            btnSalir.Size = new Size(380, 40);
            btnSalir.TabIndex = 3;
            btnSalir.Text = "SALIR";
            btnSalir.UseVisualStyleBackColor = true;
            btnSalir.Click += btnSalir_Click_1;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = false;
            lblInfo.Font = new Font("Segoe UI", 8.5F);
            lblInfo.ForeColor = Color.FromArgb(140, 150, 168);
            lblInfo.Location = new Point(450, 486);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(380, 36);
            lblInfo.TabIndex = 4;
            lblInfo.Text = "Usuario: DNI + Nombre  | Contraseña: DNI + Apellido";
            // 
            // FrmLogin
            // 
            AcceptButton = btnIngresar;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(243, 245, 249);
            ClientSize = new Size(900, 560);
            Controls.Add(btnSalir);
            Controls.Add(btnIngresar);
            Controls.Add(txtClave);
            Controls.Add(txtUsuario);
            Controls.Add(lblMensaje);
            Controls.Add(lblInfo);
            Controls.Add(lblClave);
            Controls.Add(lblUsuario);
            Controls.Add(pnlHeader);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hotel Rydl - Iniciar sesión";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Label lblSubtitulo;
        private Label lblHotel;
        private Label lblUsuario;
        private Label lblClave;
        private Label lblInfo;
        private Label lblMensaje;
        private TextBox txtUsuario;
        private TextBox txtClave;
        private Button btnIngresar;
        private Button btnSalir;
    }
}
