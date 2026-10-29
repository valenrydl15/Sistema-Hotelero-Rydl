namespace GUI_HRS_480
{
    partial class FrmMenuPrincipal
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            pnlNav = new FlowLayoutPanel();
            pnlLogo = new Panel();
            lblSecOperacion = new Label();
            btnCheckin = new Button();
            btnInsumos = new Button();
            lblSecAdmin = new Label();
            btnUsuarios = new Button();
            btnBitacora = new Button();
            btnGenerarFamilia = new Button();
            btnGenerarRoles = new Button();
            menuAdminBackup = new Button();
            menuAdminRestore = new Button();
            lblSecCuenta = new Label();
            btnCambiarClave = new Button();
            btnCambiarIdioma = new Button();
            btnReeLogin = new Button();
            pnlInferior = new Panel();
            btnCerrarSesion = new Button();
            lblSubtitulo = new Label();
            lblHotel = new Label();
            pnlPrincipal = new Panel();
            pnlBanner = new Panel();
            lblBienvenida = new Label();
            lblNombreUsuario = new Label();
            lblFecha = new Label();
            pnlAccesoRapido = new Panel();
            lblAccesoTitulo = new Label();
            lblAccesoTexto = new Label();
            btnAccesoRecepcion = new Button();
            GenerarJson = new Button();
            pnlHeader.SuspendLayout();
            pnlNav.SuspendLayout();
            pnlInferior.SuspendLayout();
            pnlPrincipal.SuspendLayout();
            pnlBanner.SuspendLayout();
            pnlAccesoRapido.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader  (barra lateral)
            // 
            pnlHeader.BackColor = Color.FromArgb(20, 45, 80);
            pnlHeader.Controls.Add(pnlNav);
            pnlHeader.Controls.Add(pnlInferior);
            pnlHeader.Controls.Add(pnlLogo);
            pnlHeader.Dock = DockStyle.Left;
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(270, 700);
            pnlHeader.TabIndex = 0;
            // 
            // pnlLogo
            // 
            pnlLogo.Controls.Add(lblSubtitulo);
            pnlLogo.Controls.Add(lblHotel);
            pnlLogo.Dock = DockStyle.Top;
            pnlLogo.Name = "pnlLogo";
            pnlLogo.Size = new Size(270, 96);
            pnlLogo.TabIndex = 4;
            // 
            // lblHotel
            // 
            lblHotel.AutoSize = true;
            lblHotel.Font = new Font("Georgia", 20F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHotel.ForeColor = Color.White;
            lblHotel.Location = new Point(22, 22);
            lblHotel.Name = "lblHotel";
            lblHotel.Size = new Size(190, 38);
            lblHotel.TabIndex = 0;
            lblHotel.Text = "HOTEL RYDL";
            // 
            // lblSubtitulo
            // 
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 8.5F);
            lblSubtitulo.ForeColor = Color.FromArgb(201, 162, 39);
            lblSubtitulo.Location = new Point(25, 62);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.TabIndex = 1;
            lblSubtitulo.Text = "SISTEMA DE GESTIÓN HOTELERA";
            // 
            // pnlNav
            // 
            pnlNav.AutoScroll = false;
            pnlNav.Dock = DockStyle.Fill;
            pnlNav.FlowDirection = FlowDirection.TopDown;
            pnlNav.Name = "pnlNav";
            pnlNav.Padding = new Padding(0, 6, 0, 0);
            pnlNav.TabIndex = 2;
            pnlNav.WrapContents = false;
            pnlNav.Controls.Add(lblSecOperacion);
            pnlNav.Controls.Add(btnCheckin);
            pnlNav.Controls.Add(btnInsumos);
            pnlNav.Controls.Add(lblSecAdmin);
            pnlNav.Controls.Add(btnUsuarios);
            pnlNav.Controls.Add(btnBitacora);
            pnlNav.Controls.Add(btnGenerarFamilia);
            pnlNav.Controls.Add(btnGenerarRoles);
            pnlNav.Controls.Add(menuAdminBackup);
            pnlNav.Controls.Add(menuAdminRestore);
            pnlNav.Controls.Add(lblSecCuenta);
            pnlNav.Controls.Add(btnCambiarClave);
            pnlNav.Controls.Add(btnCambiarIdioma);
            pnlNav.Controls.Add(btnReeLogin);
            // 
            // Secciones
            // 
            lblSecOperacion.AutoSize = false;
            lblSecOperacion.Name = "lblSecOperacion";
            lblSecOperacion.Size = new Size(270, 30);
            lblSecOperacion.Text = "OPERACIÓN";
            lblSecOperacion.Margin = new Padding(0);
            lblSecAdmin.AutoSize = false;
            lblSecAdmin.Name = "lblSecAdmin";
            lblSecAdmin.Size = new Size(270, 30);
            lblSecAdmin.Text = "ADMINISTRACIÓN";
            lblSecAdmin.Margin = new Padding(0);
            lblSecCuenta.AutoSize = false;
            lblSecCuenta.Name = "lblSecCuenta";
            lblSecCuenta.Size = new Size(270, 30);
            lblSecCuenta.Text = "CUENTA";
            lblSecCuenta.Margin = new Padding(0);
            // 
            // Botones de navegación
            // 
            btnCheckin.Name = "btnCheckin";
            btnCheckin.Size = new Size(270, 40);
            btnCheckin.Margin = new Padding(0);
            btnCheckin.TabIndex = 3;
            btnCheckin.Text = "Check-In / Check-Out";
            btnCheckin.Click += btnCheckin_Click_1;
            btnInsumos.Name = "btnInsumos";
            btnInsumos.Size = new Size(270, 40);
            btnInsumos.Margin = new Padding(0);
            btnInsumos.TabIndex = 4;
            btnInsumos.Text = "Gestion de Insumos";
            btnInsumos.Click += btnInsumos_Click_1;
            btnUsuarios.Name = "btnUsuarios";
            btnUsuarios.Size = new Size(270, 40);
            btnUsuarios.Margin = new Padding(0);
            btnUsuarios.TabIndex = 5;
            btnUsuarios.Text = "Gestion de Usuarios";
            btnUsuarios.Click += btnUsuarios_Click_1;
            btnBitacora.Name = "btnBitacora";
            btnBitacora.Size = new Size(270, 40);
            btnBitacora.Margin = new Padding(0);
            btnBitacora.TabIndex = 6;
            btnBitacora.Text = "Bitacora de Eventos";
            btnBitacora.Click += btnBitacora_Click_1;
            btnGenerarFamilia.Name = "btnGenerarFamilia";
            btnGenerarFamilia.Size = new Size(270, 40);
            btnGenerarFamilia.Margin = new Padding(0);
            btnGenerarFamilia.TabIndex = 7;
            btnGenerarFamilia.Text = "Generar Familias";
            btnGenerarFamilia.Click += btnGenerarFamilia_Click;
            btnGenerarRoles.Name = "btnGenerarRoles";
            btnGenerarRoles.Size = new Size(270, 40);
            btnGenerarRoles.Margin = new Padding(0);
            btnGenerarRoles.TabIndex = 8;
            btnGenerarRoles.Text = "Generar Roles";
            btnGenerarRoles.Click += btnGenerarRoles_Click;
            menuAdminBackup.Name = "menuAdminBackup";
            menuAdminBackup.Size = new Size(270, 40);
            menuAdminBackup.Margin = new Padding(0);
            menuAdminBackup.TabIndex = 9;
            menuAdminBackup.Text = "BackUP";
            menuAdminBackup.Click += menuAdminBackup_Click;
            menuAdminRestore.Name = "menuAdminRestore";
            menuAdminRestore.Size = new Size(270, 40);
            menuAdminRestore.Margin = new Padding(0);
            menuAdminRestore.TabIndex = 10;
            menuAdminRestore.Text = "Restore";
            menuAdminRestore.Click += menuAdminRestore_Click;
            btnCambiarClave.Name = "btnCambiarClave";
            btnCambiarClave.Size = new Size(270, 40);
            btnCambiarClave.Margin = new Padding(0);
            btnCambiarClave.TabIndex = 11;
            btnCambiarClave.Text = "Cambiar Contraseña";
            btnCambiarClave.Click += btnCambiarClave_Click_1;
            btnCambiarIdioma.Name = "btnCambiarIdioma";
            btnCambiarIdioma.Size = new Size(270, 40);
            btnCambiarIdioma.Margin = new Padding(0);
            btnCambiarIdioma.TabIndex = 12;
            btnCambiarIdioma.Text = "Cambiar Idioma";
            btnCambiarIdioma.Click += btnCambiarIdioma_Click;
            btnReeLogin.Name = "btnReeLogin";
            btnReeLogin.Size = new Size(270, 40);
            btnReeLogin.Margin = new Padding(0);
            btnReeLogin.TabIndex = 13;
            btnReeLogin.Text = "ReeLogin";
            btnReeLogin.Click += btnReeLogin_Click;
            // 
            // pnlInferior (Cerrar sesión)
            // 
            pnlInferior.Controls.Add(btnCerrarSesion);
            pnlInferior.Dock = DockStyle.Bottom;
            pnlInferior.Name = "pnlInferior";
            pnlInferior.Size = new Size(270, 70);
            pnlInferior.TabIndex = 3;
            btnCerrarSesion.Dock = DockStyle.Fill;
            btnCerrarSesion.Name = "btnCerrarSesion";
            btnCerrarSesion.TabIndex = 14;
            btnCerrarSesion.Text = "Cerrar Sesion";
            btnCerrarSesion.Click += btnCerrarSesion_Click_1;
            // 
            // pnlPrincipal
            // 
            pnlPrincipal.BackColor = Color.FromArgb(243, 245, 249);
            pnlPrincipal.Controls.Add(GenerarJson);
            pnlPrincipal.Controls.Add(pnlAccesoRapido);
            pnlPrincipal.Controls.Add(pnlBanner);
            pnlPrincipal.Dock = DockStyle.Fill;
            pnlPrincipal.Name = "pnlPrincipal";
            pnlPrincipal.Padding = new Padding(36);
            pnlPrincipal.TabIndex = 1;
            // 
            // pnlBanner
            // 
            pnlBanner.BackColor = Color.FromArgb(20, 45, 80);
            pnlBanner.Controls.Add(lblBienvenida);
            pnlBanner.Controls.Add(lblNombreUsuario);
            pnlBanner.Controls.Add(lblFecha);
            pnlBanner.Dock = DockStyle.Top;
            pnlBanner.Name = "pnlBanner";
            pnlBanner.Size = new Size(800, 170);
            pnlBanner.TabIndex = 0;
            lblBienvenida.AutoSize = true;
            lblBienvenida.Font = new Font("Segoe UI", 11F);
            lblBienvenida.ForeColor = Color.FromArgb(201, 162, 39);
            lblBienvenida.Location = new Point(34, 28);
            lblBienvenida.Name = "lblBienvenida";
            lblBienvenida.TabIndex = 0;
            lblNombreUsuario.AutoSize = true;
            lblNombreUsuario.Font = new Font("Georgia", 26F);
            lblNombreUsuario.ForeColor = Color.White;
            lblNombreUsuario.Location = new Point(30, 56);
            lblNombreUsuario.Name = "lblNombreUsuario";
            lblNombreUsuario.TabIndex = 1;
            lblFecha.AutoSize = true;
            lblFecha.Font = new Font("Segoe UI", 10F);
            lblFecha.ForeColor = Color.FromArgb(205, 214, 230);
            lblFecha.Location = new Point(34, 118);
            lblFecha.Name = "lblFecha";
            lblFecha.TabIndex = 2;
            // 
            // pnlAccesoRapido
            // 
            pnlAccesoRapido.BackColor = Color.White;
            pnlAccesoRapido.Controls.Add(lblAccesoTitulo);
            pnlAccesoRapido.Controls.Add(lblAccesoTexto);
            pnlAccesoRapido.Controls.Add(btnAccesoRecepcion);
            pnlAccesoRapido.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlAccesoRapido.Location = new Point(36, 236);
            pnlAccesoRapido.Name = "pnlAccesoRapido";
            pnlAccesoRapido.Size = new Size(758, 170);
            pnlAccesoRapido.TabIndex = 1;
            lblAccesoTitulo.AutoSize = true;
            lblAccesoTitulo.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblAccesoTitulo.ForeColor = Color.FromArgb(20, 45, 80);
            lblAccesoTitulo.Location = new Point(26, 20);
            lblAccesoTitulo.Name = "lblAccesoTitulo";
            lblAccesoTitulo.Text = "Recepción";
            lblAccesoTexto.AutoSize = false;
            lblAccesoTexto.Font = new Font("Segoe UI", 9.5F);
            lblAccesoTexto.ForeColor = Color.FromArgb(110, 122, 142);
            lblAccesoTexto.Location = new Point(28, 54);
            lblAccesoTexto.Name = "lblAccesoTexto";
            lblAccesoTexto.Size = new Size(466, 40);
            lblAccesoTexto.Text = "Registrá el ingreso y la salida de huéspedes desde una única pantalla: habitación, cliente y cobro.";
            btnAccesoRecepcion.Location = new Point(28, 104);
            btnAccesoRecepcion.Name = "btnAccesoRecepcion";
            btnAccesoRecepcion.Size = new Size(240, 42);
            btnAccesoRecepcion.TabIndex = 0;
            btnAccesoRecepcion.Text = "Abrir Recepción  →";
            btnAccesoRecepcion.Click += btnCheckin_Click_1;
            // 
            // GenerarJson (herramienta interna, oculta)
            // 
            GenerarJson.Location = new Point(36, 420);
            GenerarJson.Name = "GenerarJson";
            GenerarJson.Size = new Size(113, 32);
            GenerarJson.TabIndex = 2;
            GenerarJson.Text = "GenerarJson";
            GenerarJson.Visible = false;
            GenerarJson.Click += GenerarJson_Click;
            // 
            // FrmMenuPrincipal
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 760);
            Controls.Add(pnlPrincipal);
            Controls.Add(pnlHeader);
            MinimumSize = new Size(900, 720);
            Name = "FrmMenuPrincipal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hotel Rydl";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlNav.ResumeLayout(false);
            pnlInferior.ResumeLayout(false);
            pnlPrincipal.ResumeLayout(false);
            pnlBanner.ResumeLayout(false);
            pnlBanner.PerformLayout();
            pnlAccesoRapido.ResumeLayout(false);
            pnlAccesoRapido.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHeader;
        private FlowLayoutPanel pnlNav;
        private Panel pnlInferior;
        private Panel pnlLogo;
        private Panel pnlPrincipal;
        private Panel pnlBanner;
        private Panel pnlAccesoRapido;
        private Label lblSubtitulo;
        private Label lblSecOperacion;
        private Label lblSecAdmin;
        private Label lblSecCuenta;
        private Label lblBienvenida;
        private Label lblNombreUsuario;
        private Label lblFecha;
        private Label lblAccesoTitulo;
        private Label lblAccesoTexto;
        private Label lblHotel;
        private Button btnUsuarios;
        private Button btnBitacora;
        private Button btnCambiarClave;
        private Button btnCheckin;
        private Button btnInsumos;
        private Button btnCerrarSesion;
        private Button btnReeLogin;
        private Button btnCambiarIdioma;
        private Button GenerarJson;
        private Button btnGenerarFamilia;
        private Button btnGenerarRoles;
        private Button menuAdminBackup;
        private Button menuAdminRestore;
        private Button btnAccesoRecepcion;
    }
}
