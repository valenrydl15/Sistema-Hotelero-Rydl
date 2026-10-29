using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;



namespace GUI_HRS_480
{
    public partial class FrmCambioClave : Form, Idioma.IObserver    
    {
        private readonly bool _forzado;
        private readonly string _nombreUsuarioPrecargado;
        private readonly AutenticacionServicio _servicio = new AutenticacionServicio();

        
        public FrmCambioClave() : this(null, false) { }

        
        public FrmCambioClave(string nombreUsuario, bool forzado)
        {
            _forzado = forzado;
            _nombreUsuarioPrecargado = nombreUsuario;
            InitializeComponent();
            Tema.Aplicar(this);
            Idioma.GestorTraducciones.Instancia.AgregarForm(this);

            ActualizarTraducciones();

        }
        public void ActualizarTraducciones()
        {
            new Traductor()
                .ActualizarTraducciones(
                    "FrmCambioClave",
                    this);
        }
                protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }


        private void btnAceptar_Click_1(object sender, EventArgs e)
        {
            string nombreUsuario = txtNombreUsuario.Text.Trim();
            string actual = txtActual.Text;
            string nueva = txtNueva.Text;
            string confirma = txtConfirma.Text;

            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(actual)
                || string.IsNullOrEmpty(nueva) || string.IsNullOrEmpty(confirma))
            {
                Msg("Completá todos los campos.", esError: true);
                return;
            }

            ResultadoCambioClave res;

            if (_forzado)
            {
                
                var u = SesionManager.Instancia.UsuarioActual;
                res = _servicio.CambiarContrasenaForzada(u, actual, nueva, confirma);
            }
            else
            {
                
                res = _servicio.CambiarContrasena(nombreUsuario, actual, nueva, confirma);
            }

            switch (res)
            {
                case ResultadoCambioClave.Exitoso:
                    MessageBox.Show("✔ Contraseña actualizada correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                    break;

                case ResultadoCambioClave.UsuarioNoEncontrado:
                    Msg($"Usuario '{nombreUsuario}' no encontrado.\nVerificá el nombre de usuario.", esError: true);
                    txtNombreUsuario.Focus();
                    break;

                case ResultadoCambioClave.ClaveActualIncorrecta:
                    Msg("La contraseña actual es incorrecta.", esError: true);
                    txtActual.Clear(); txtActual.Focus();
                    break;

                case ResultadoCambioClave.NuevaIgualActual:
                    Msg("La nueva contraseña no puede ser igual a la actual.", esError: true);
                    txtNueva.Clear(); txtConfirma.Clear(); txtNueva.Focus();
                    break;

                case ResultadoCambioClave.ConfirmacionNoCoincide:
                    Msg("La confirmación no coincide con la nueva contraseña.", esError: true);
                    txtConfirma.Clear(); txtConfirma.Focus();
                    break;
            }
        }

        private void btnCancelar_Click_1(object sender, EventArgs e)
        {
            if (_forzado)
            {
                Msg("Debés cambiar tu contraseña para poder continuar.", esError: true);
                return;
            }
            DialogResult = DialogResult.Cancel;
            Close();
        }
        private void Msg(string t, bool esError)
        {
            lblMsg.Text = t;
            lblMsg.ForeColor = esError
                ? System.Drawing.Color.FromArgb(200, 50, 50)
                : System.Drawing.Color.FromArgb(30, 130, 70);
        }

        private void FrmCambioClave_Load(object sender, EventArgs e)
        {
            txtNombreUsuario.Text = _nombreUsuarioPrecargado ?? "";

            if (_forzado)
            {
                lblTitulo.Text = "⚠  Cambio de contraseña obligatorio";
                txtNombreUsuario.Enabled = false;  
                ControlBox = false; 
            }
            else
            {
                
                txtNombreUsuario.Enabled = true;
            }
        }
    }

}

