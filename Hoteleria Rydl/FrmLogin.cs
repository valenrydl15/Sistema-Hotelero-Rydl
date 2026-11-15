using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmLogin : Form, Idioma.IObserver
    {
        private readonly AutenticacionServicio _servicio = new AutenticacionServicio();
        // Los mensajes del login (incluido el del Dígito Verificador) se limpian solos
        private readonly System.Windows.Forms.Timer _timerMensaje = new System.Windows.Forms.Timer { Interval = 7000 };
        public FrmLogin()
        {
            InitializeComponent();
            Tema.Aplicar(this);
            _timerMensaje.Tick += (s, e) => LimpiarMensaje();
            txtUsuario.TextChanged += (s, e) => LimpiarMensaje();
            txtClave.TextChanged += (s, e) => LimpiarMensaje();
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
                    txtClave.Clear();   // antes del mensaje: borrar la clave limpia el mensaje
                    if (usuarioLogueado?.NombreRol == "Administrador")
                    {
                        // Admin: abrir formulario de reparación (recalcular o restaurar un backup)
                        LimpiarMensaje();
                        using (var frmRep = new FrmReparacionDV(_servicio.UltimaVerificacionDV, usuarioLogueado))
                        {
                            var r = frmRep.ShowDialog(this);
                            Mensaje(r == DialogResult.OK
                                        ? "Inconsistencia resuelta. Iniciá sesión nuevamente."
                                        : "La inconsistencia sigue pendiente de resolución.",
                                    ok: r == DialogResult.OK);
                        }
                    }
                    else
                    {
                        // Usuario no administrador: sin carteles de error, solo el aviso de contactar al administrador
                        Mensaje("El sistema no está disponible en este momento. Contactá al administrador para que lo solucione.", ok: false);
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

        private void LimpiarMensaje()
        {
            _timerMensaje.Stop();
            lblMensaje.Text = "";
        }

        private void Mensaje(string texto, bool ok)
        {
            _timerMensaje.Stop();
            _timerMensaje.Start();   // se limpia solo a los pocos segundos o cuando el usuario vuelve a escribir
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok
                ? System.Drawing.Color.FromArgb(30, 130, 70)
                : System.Drawing.Color.FromArgb(200, 50, 50);
        }




        private void btnSalir_Click_1(object sender, EventArgs e)
             => Application.Exit();

      
    }
}

   

