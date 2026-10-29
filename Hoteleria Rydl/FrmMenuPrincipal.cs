using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SERVICIOS_HRS_480;
using SEGURIDAD_HRS_480;
using BLL_HRS_480;
using System.IO;

namespace GUI_HRS_480
{
    public partial class FrmMenuPrincipal : Form, Idioma.IObserver
    {
        private readonly AutenticacionServicio _servicio = new AutenticacionServicio();
        private readonly DigitoVerificadorServicio _dvServicio = new DigitoVerificadorServicio();
        public FrmMenuPrincipal()
        {
            InitializeComponent();
            AplicarEstilos();
            Idioma.GestorTraducciones.Instancia.AgregarForm(this);

            ActualizarTraducciones();
        }
        public void ActualizarTraducciones()
        {
            // Se parte siempre del texto base para que el rol no se acumule
            lblBienvenida.Text = _textoBienvenida;

            new Traductor()
                .ActualizarTraducciones(
                    "FrmMenuPrincipal",
                    this);

            _textoBienvenida = lblBienvenida.Text;

            ConfigurarSegunRol();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }
        // Estilo visual (colores y fuentes compartidos en Tema.cs)
        private void AplicarEstilos()
        {
            foreach (var b in new[] { btnCheckin, btnInsumos, btnUsuarios, btnBitacora, btnGenerarFamilia,
                                      btnGenerarRoles, menuAdminBackup, menuAdminRestore,
                                      btnCambiarClave, btnCambiarIdioma, btnReeLogin })
                Tema.BotonLateral(b);

            // En el FlowLayoutPanel el Dock no aplica: el ancho lo da el tamaño
            foreach (var b in new[] { btnCheckin, btnInsumos, btnUsuarios, btnBitacora, btnGenerarFamilia,
                                      btnGenerarRoles, menuAdminBackup, menuAdminRestore,
                                      btnCambiarClave, btnCambiarIdioma, btnReeLogin })
            {
                b.Dock = DockStyle.None;
                b.Size = new Size(270, 40);
            }

            foreach (var l in new[] { lblSecOperacion, lblSecAdmin, lblSecCuenta })
            {
                l.ForeColor = Color.FromArgb(120, 140, 175);
                l.Font = Tema.Fuente(8f, FontStyle.Bold);
                l.TextAlign = ContentAlignment.BottomLeft;
                l.Padding = new Padding(24, 0, 0, 6);
            }

            // Cerrar sesión: botón rojo suave en la base de la barra
            Tema.BotonLateral(btnCerrarSesion);
            btnCerrarSesion.Dock = DockStyle.Fill;
            btnCerrarSesion.BackColor = Color.FromArgb(13, 31, 58);
            btnCerrarSesion.ForeColor = Color.FromArgb(255, 170, 160);

            Tema.BotonDorado(btnAccesoRecepcion);
            pnlAccesoRapido.Paint += (s, e) =>
            {
                using (var pen = new Pen(Tema.Borde))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlAccesoRapido.Width - 1, pnlAccesoRapido.Height - 1);
                using (var br = new SolidBrush(Tema.Dorado))
                    e.Graphics.FillRectangle(br, 0, 0, 5, pnlAccesoRapido.Height);
            };

            lblFecha.Text = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy",
                                new System.Globalization.CultureInfo("es-AR"));
        }

        private void ConfigurarSegunRol()
        {
            var u = SesionManager.Instancia.UsuarioActual;
            if (u == null) return;

            // El texto de lblBienvenida viene del JSON de idioma; no se acumula
            lblBienvenida.Text = $"{_textoBienvenida}  {u.NombreRol}";
            lblNombreUsuario.Text = $"{u.Nombre} {u.Apellido}";

            bool esAdmin = ControlAcceso.EsAdministrador();
            bool puedeRecepcion = ControlAcceso.PuedeOperarRecepcion();

            // Administración: solo Administrador
            lblSecAdmin.Visible = esAdmin;
            btnUsuarios.Visible = esAdmin;
            btnBitacora.Visible = esAdmin;
            btnGenerarFamilia.Visible = esAdmin;
            btnGenerarRoles.Visible = esAdmin;
            menuAdminBackup.Visible = esAdmin;
            menuAdminRestore.Visible = esAdmin;

            // Recepción (Check-IN / Check-OUT): Recepcionista y Administrador
            btnCheckin.Visible = puedeRecepcion;
            pnlAccesoRapido.Visible = puedeRecepcion;
            // Reposición de insumos: Administrador, Encargado de Compras y Encargado de Almacén
            btnInsumos.Visible = ControlAcceso.PuedeAccederReposicion();
            lblSecOperacion.Visible = puedeRecepcion || btnInsumos.Visible;
        }

        private string _textoBienvenida = "Bienvenido/a | Rol:";

        private void btnUsuarios_Click_1(object sender, EventArgs e)
        => new FrmGestionUsuarios().ShowDialog(this);

        private void btnBitacora_Click_1(object sender, EventArgs e)
         => new FrmBitacora().ShowDialog(this);

        private void btnCambiarClave_Click_1(object sender, EventArgs e)
        {
            var u = SesionManager.Instancia.UsuarioActual;

            using var frm = new FrmCambioClave(u.NombreUsuario, forzado: false);
            frm.ShowDialog(this);
        }

        private void btnCheckin_Click_1(object sender, EventArgs e)
        {
            // Doble control: además de ocultarse el botón, se valida el rol al abrir
            if (!ControlAcceso.PuedeOperarRecepcion())
            {
                MessageBox.Show("No tenés permiso para acceder a Recepción.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var frm = new FrmRecepcion())
                frm.ShowDialog(this);
        }

        private void btnInsumos_Click_1(object sender, EventArgs e)
        {
            // Doble control: además de ocultarse el botón, se valida el rol al abrir
            if (!ControlAcceso.PuedeAccederReposicion())
            {
                MessageBox.Show("No tenés permiso para acceder a Reposición de insumos.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var frm = new FrmReposicion())
                frm.ShowDialog(this);
        }


        private void btnCerrarSesion_Click_1(object sender, EventArgs e)
        {
            var primera = MessageBox.Show(
                "¿Estás seguro de que queres cerrar la sesión?",
                "Cerrar sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (primera != DialogResult.Yes) return;

            // Segunda confirmación (reconfirmación)
            var segunda = MessageBox.Show(
                "Esta acción cerrará tu sesión actual y tendrás que\n" +
                "volver a iniciar sesión para acceder al sistema.\n\n" +
                "¿Confirmás el cierre de sesión?",
                "Confirmar cierre de sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (segunda != DialogResult.Yes) return;

            // Ejecutar logout
            var u = SesionManager.Instancia.UsuarioActual;
            _servicio.Logout(u.IdUsuario);
            Close();
        }

        private void FrmMenuPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SesionManager.Instancia.HaySesionActiva)
                _servicio.Logout(SesionManager.Instancia.UsuarioActual.IdUsuario);
        }

        private void btnReeLogin_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Ya existe una sesión activa en el sistema.\n\n" +
                "No es posible iniciar una nueva sesión mientras haya una en curso.\n" +
                "Para cambiar de usuario, primero cerrá la sesión actual.",
                "Relogin no permitido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void btnCambiarIdioma_Click(object sender, EventArgs e)
        {
            FrmIdiomas frm = new FrmIdiomas();
            frm.ShowDialog();
        }

        private void GenerarJson_Click(object sender, EventArgs e)
        {
            Traductor t = new Traductor();

            t.ExportarTextosAJson(new FrmLogin(), "FrmLogin");
            t.ExportarTextosAJson(new FrmCambioClave("", false), "FrmCambioClave");
            t.ExportarTextosAJson(new FrmGestionUsuarios(), "FrmGestionUsuarios");
            t.ExportarTextosAJson(new FrmBitacora(), "FrmBitacora");
            t.ExportarTextosAJson(new FrmGeneradorFamilias(), "FrmGeneradorFamilias");
            t.ExportarTextosAJson(new FrmGeneradorRoles(), "FrmGeneradorRoles");
            t.ExportarTextosAJson(this, "FrmMenuPrincipal");

            MessageBox.Show("JSON generados correctamente");
        }

        private void btnGenerarFamilia_Click(object sender, EventArgs e)
        {
            FrmGeneradorFamilias frm = new FrmGeneradorFamilias();
            frm.ShowDialog();
        }

        private void btnGenerarRoles_Click(object sender, EventArgs e)
        {
            FrmGeneradorRoles frm = new FrmGeneradorRoles();
            frm.ShowDialog();
        }

        private void menuAdminBackup_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "¿Generar un backup de la base de datos ahora?\n\n" +
                "El archivo .bak se guardará en la carpeta de backups configurada.",
                "Confirmar Backup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                string rutaArchivo = _dvServicio.EjecutarBackup();
                _dvServicio.GuardarBackupEstadoCorrecto("Backup manual del administrador");

                MessageBox.Show(
                    $"✔ Backup generado correctamente.\n\n" +
                    $"Archivo: {Path.GetFileName(rutaArchivo)}\n" +
                    $"Ubicación: {Path.GetDirectoryName(rutaArchivo)}",
                    "Backup exitoso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error al generar el backup:\n\n{ex.Message}",
                    "Error de Backup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void menuAdminRestore_Click(object sender, EventArgs e)
        {
            // Obtener backups disponibles
            var backups = _dvServicio.ObtenerBackupsDisponibles();

            if (backups.Count == 0)
            {
                MessageBox.Show(
                    "No hay backups disponibles para restaurar.\n\n" +
                    "Generá un backup primero desde el menú Admin → Backup.",
                    "Sin backups",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Mostrar formulario de selección de backup
            using var frm = new FrmSeleccionarBackup(backups, _dvServicio);
            frm.ShowDialog(this);
        }

    }
}


