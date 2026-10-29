using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmLogin : Form, Idioma.IObserver
    {
        private readonly AutenticacionServicio _servicio = new AutenticacionServicio();
        public FrmLogin()
        {
            InitializeComponent();
            Tema.Aplicar(this);
            Idioma.GestorTraducciones.Instancia.AgregarForm(this);
            ActualizarTraducciones();

        }
        public void ActualizarTraducciones()
        {
            new Traductor().ActualizarTraducciones("FrmLogin", this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }


        private void btnIngresar_Click_1(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string clave = txtClave.Text;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(clave))
            {
                Mensaje("Completá usuario y contraseña.", false);
                return;
            }

            var resultado = _servicio.Login(usuario, clave, out var usuarioLogueado);

            switch (resultado)
            {
                case ResultadoLogin.Exitoso:
                    AbrirMenu();
                    break;

                case ResultadoLogin.DebeChangiarClave:
                    MessageBox.Show("Debés cambiar tu contraseña antes de continuar.",
                        "Cambio requerido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AbrirCambioForzado();
                    break;

                // ── INCONSISTENCIA DE DÍGITO VERIFICADOR ──────────
                case ResultadoLogin.InconsistenciaDV:
                    if (usuarioLogueado?.NombreRol == "Administrador")
                    {
                        // Admin: abrir formulario de reparación
                        var frmRep = new FrmReparacionDV(
                            _servicio.UltimaVerificacionDV,
                            usuarioLogueado);
                        frmRep.ShowDialog(this);

                        // Si resolvió el problema, intentar login de nuevo automáticamente
                        Mensaje("Inconsistencia resuelta. Iniciá sesión nuevamente.",
                                       ok: false);
                    }
                    else
                    {
                        // Usuario normal: solo ve el cartel de advertencia
                        MessageBox.Show(
                            "⚠ Se detectó una inconsistencia en los datos del sistema.\n\n" +
                            "Por favor, contactá al Administrador para resolver el problema\n" +
                            "antes de poder acceder al sistema.",
                            "Inconsistencia detectada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
                        Mensaje("Acceso bloqueado. Contactá al administrador.", ok: true);
                    }
                    break;
                

                case ResultadoLogin.UsuarioNoEncontrado:
                    Mensaje("Usuario no encontrado.", false);
                    break;

                case ResultadoLogin.ContrasenaIncorrecta:
                    Mensaje("Contraseña incorrecta.", false);
                    break;

                case ResultadoLogin.CuentaBloqueada:
                    Mensaje("Cuenta bloqueada. Contactá al administrador.", false);
                    break;

                case ResultadoLogin.UsuarioInactivo:
                    Mensaje("Cuenta inactiva. Contactá al administrador.", false);
                    break;

                case ResultadoLogin.SesionYaActiva:
                    Mensaje("Este usuario ya tiene sesión activa en el sistema.", false);
                    break;
            }
        }
        private void AbrirMenu()
        {
            var menu = new FrmMenuPrincipal();
            menu.FormClosed += (s, e) => this.Show();
            this.Hide();
            menu.Show();
        }

        private void AbrirCambioForzado()
        {
            var sesion = SesionManager.Instancia.UsuarioActual;
            using var frm = new FrmCambioClave(sesion.NombreUsuario, forzado: true);
            frm.ShowDialog(this);


            _servicio.Logout(SesionManager.Instancia.UsuarioActual?.IdUsuario ?? 0);
            txtUsuario.Clear();
            txtClave.Clear();
            Mensaje("Contraseña actualizada. Iniciá sesión nuevamente.", ok: false);
        }

        private void Mensaje(string texto, bool ok)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok
                ? System.Drawing.Color.FromArgb(30, 130, 70)
                : System.Drawing.Color.FromArgb(200, 50, 50);
        }




        private void btnSalir_Click_1(object sender, EventArgs e)
             => Application.Exit();

      
    }
}

   

