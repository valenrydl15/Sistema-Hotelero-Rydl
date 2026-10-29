using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmGestionUsuarios : Form, Idioma.IObserver
    {
        private readonly UsuarioServicio _servicio = new UsuarioServicio();

        
        private enum Modo { Consulta, Anadir, Modificar, Desbloquear }
        private Modo _modo = Modo.Consulta;

        
        private Usuario _seleccionado;

        
        private bool _soloActivos = true;

        // ¿El usuario en sesión puede asignar roles?
        private bool _puedeAsignarRoles;
        public FrmGestionUsuarios()
        {
            InitializeComponent();
            Tema.Aplicar(this);
            _puedeAsignarRoles = _servicio.UsuarioActualPuedeGestionarPerfiles();
            CargarRoles();
            ActualizarGrillaSuperior();
            SetModoConsulta();

            Idioma.GestorTraducciones.Instancia.AgregarForm(this);

            ActualizarTraducciones();
        }
        public void ActualizarTraducciones()
        {
            new Traductor()
                .ActualizarTraducciones(
                    "FrmGestionUsuarios",
                    this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }

        
        private void ActualizarGrillaSuperior()
        {
            var lista = _servicio.ObtenerTodos(_soloActivos);
            CargarEnGrilla(lista);
            lblNumUsuarios.Text = $"Número de Usuarios: {dgvSuperior.Rows.Count}";
        }

      

        private void CargarEnGrilla(List<Usuario> lista)
        {
            dgvSuperior.Rows.Clear();
            foreach (var u in lista)
            {
                int idx = dgvSuperior.Rows.Add(
                    u.DNI, u.Apellido, u.Nombre, u.Email ?? "", u.NombreUsuario, u.NombreRol);
                dgvSuperior.Rows[idx].Tag = u;

                if (!u.Activo)
                    dgvSuperior.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 180, 180);
                if (u.Bloqueado)
                    dgvSuperior.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(160, 0, 0);
            }
            if (dgvSuperior.Rows.Count > 0)
                dgvSuperior.Rows[0].Selected = true;
        }

        private void CargarRoles()
        {
            // Carga TODOS los roles: los predefinidos + los creados en Generador de Roles
            var roles = _servicio.ObtenerRoles();

            cmbFilRol.Items.Clear();
            cmbFilRol.Items.Add(new Rol { IdRol = 0, Nombre = "(Todos)" });
            cmbFilRol.Items.AddRange(roles.ToArray());
            cmbFilRol.SelectedIndex = 0;

            cmbRolEdicion.Items.Clear();
            cmbRolEdicion.Items.AddRange(roles.ToArray());
            if (cmbRolEdicion.Items.Count > 0) cmbRolEdicion.SelectedIndex = 0;
        }


        private void dgvSuperior_SelectionChanged_1(object sender, EventArgs e)
        {
            if (_modo != Modo.Consulta) return;
            if (dgvSuperior.CurrentRow == null) return;

            _seleccionado = dgvSuperior.CurrentRow.Tag as Usuario;
            if (_seleccionado == null) return;

            lblNombreValor.Text = _seleccionado.Nombre;
            lblApellidoValor.Text = _seleccionado.Apellido;
            txtFilDNI.Text = _seleccionado.DNI;
            txtFilApellidos.Text = _seleccionado.Apellido;
            txtFilNombres.Text = _seleccionado.Nombre;
            txtFilLogin.Text = _seleccionado.NombreUsuario;

            foreach (var item in cmbFilRol.Items)
                if (item is Rol r && r.IdRol == _seleccionado.IdRol)
                { cmbFilRol.SelectedItem = item; break; }
        }

        // ============================================================
        // MODOS
        // ============================================================
        private void SetModoConsulta()
        {
            _modo = Modo.Consulta;
            MostrarMensaje("Modo Consulta", esError: false);
            SetBotones(crear: true, desbloquear: true, modificar: true,
                       actDesact: true, aplicar: false, cancelar: false, salir: true);
            pnlEdicion.Visible = false;
        }

        private void SetModoAnadir()
        {
            _modo = Modo.Anadir;
            MostrarMensaje("Modo Añadir — Completá los datos y presioná Aplicar.", esError: false);
            SetBotones(crear: false, desbloquear: false, modificar: false,
                       actDesact: false, aplicar: true, cancelar: true, salir: false);
            LimpiarEdicion();

            // En alta, el combo de rol siempre habilitado (se asigna al crear)
            cmbRolEdicion.Enabled = true;
            pnlEdicion.Visible = true;
            txtEdNombre.Focus();
        }

        private void SetModoModificar()
        {
            if (_seleccionado == null)
            { MostrarMensaje("Seleccioná un usuario primero.", esError: true); return; }

            _modo = Modo.Modificar;
            MostrarMensaje("Modo Modificar — Editá Email y/o Rol, luego presioná Aplicar.", esError: false);
            SetBotones(crear: false, desbloquear: false, modificar: false,
                       actDesact: false, aplicar: true, cancelar: true, salir: false);

            txtEdDNI.Text = _seleccionado.DNI;
            txtEdNombre.Text = _seleccionado.Nombre;
            txtEdApellido.Text = _seleccionado.Apellido;
            txtEdEmail.Text = _seleccionado.Email ?? "";

            // Seleccionar el rol actual en el combo
            foreach (var item in cmbRolEdicion.Items)
                if (item is Rol r && r.IdRol == _seleccionado.IdRol)
                { cmbRolEdicion.SelectedItem = item; break; }

            // Solo lectura para campos no modificables
            txtEdDNI.Enabled = false;
            txtEdNombre.Enabled = false;
            txtEdApellido.Enabled = false;
            txtEdEmail.Enabled = true;

            // El combo de rol solo es editable si tiene permiso
            cmbRolEdicion.Enabled = _puedeAsignarRoles;

            if (!_puedeAsignarRoles)
                MostrarMensaje("Podés modificar el email. Para cambiar el rol necesitás el permiso GestionPerfiles.",
                               esError: false);

            pnlEdicion.Visible = true;
            txtEdEmail.Focus();
        }


        private void SetModoDesbloquear()
        {
            if (_seleccionado == null) { MostrarMensaje("Seleccioná un usuario primero.", esError: true); return; }
            if (!_seleccionado.Bloqueado) { MostrarMensaje("El usuario seleccionado no está bloqueado.", esError: true); return; }

            _modo = Modo.Desbloquear;
            MostrarMensaje($"Modo Desbloquear — Se reseteará la clave de {_seleccionado.NombreUsuario}. Presioná Aplicar.", esError: false);
            SetBotones(crear: false, desbloquear: false, modificar: false,
                       actDesact: false, aplicar: true, cancelar: true, salir: false);
            pnlEdicion.Visible = false;
        }

        private void SetBotones(bool crear, bool desbloquear, bool modificar,
                                  bool actDesact, bool aplicar, bool cancelar, bool salir)
        {
            btnCrear.Enabled = crear;
            btnDesbloquear.Enabled = desbloquear;
            btnModificar.Enabled = modificar;
            btnActDesact.Enabled = actDesact;
            btnAplicar.Enabled = aplicar;
            btnCancelar.Enabled = cancelar;
            btnSalir.Enabled = salir;
        }

        private void LimpiarEdicion()
        {
            txtEdDNI.Clear(); txtEdNombre.Clear();
            txtEdApellido.Clear(); txtEdEmail.Clear();
            if (cmbRolEdicion.Items.Count > 0) cmbRolEdicion.SelectedIndex = 0;
            txtEdDNI.Enabled = true;
            txtEdNombre.Enabled = true;
            txtEdApellido.Enabled = true;
            txtEdEmail.Enabled = true;
            cmbRolEdicion.Enabled = true;
        }

        // ============================================================
        // BOTONES PRINCIPALES
        // ============================================================
        private void btnCrear_Click_1(object sender, EventArgs e) => SetModoAnadir();

        private void btnDesbloquear_Click_1(object sender, EventArgs e) => SetModoDesbloquear();

        private void btnModificar_Click_1(object sender, EventArgs e) =>  SetModoModificar();

        private void btnActDesact_Click_1(object sender, EventArgs e)
        {
            if (_seleccionado == null) { MostrarMensaje("Seleccioná un usuario.", esError: true); return; }
            bool nuevo = !_seleccionado.Activo;
            if (MessageBox.Show($"¿{(nuevo ? "Activar" : "Desactivar")} a {_seleccionado.NombreUsuario}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            bool ok = _servicio.SetActivo(_seleccionado.IdUsuario, nuevo, out string msg);
            MostrarMensaje(msg, esError: !ok);
            if (ok) ActualizarGrillaSuperior();
        }

        private void btnAplicar_Click_1(object sender, EventArgs e)
        {
            switch (_modo)
            {
                case Modo.Anadir: EjecutarAlta(); break;
                case Modo.Modificar: EjecutarModificacion(); break;
                case Modo.Desbloquear: EjecutarDesbloqueo(); break;
            }
        }

        private void btnCancelar_Click_1(object sender, EventArgs e)
        {
            SetModoConsulta();
            ActualizarGrillaSuperior();
        }

        private void btnSalir_Click_1(object sender, EventArgs e) => Close();

        // ============================================================
        // OPERACIONES
        // ============================================================
        private void EjecutarAlta()
        {
            var rol = cmbRolEdicion.SelectedItem as Rol;
            bool ok = _servicio.Alta(
                txtEdNombre.Text, txtEdApellido.Text,
                txtEdDNI.Text, txtEdEmail.Text,
                rol?.IdRol ?? 1, out string msg);

            MostrarMensaje(msg, esError: !ok);
            if (ok) { ActualizarGrillaSuperior(); SetModoConsulta(); }
        }

        private void EjecutarModificacion()
        {
            bool algoCambio = false;
            string msgFinal = "";

            // 1. Modificar email (siempre permitido)
            bool okEmail = _servicio.ModificarEmail(
                _seleccionado.IdUsuario, txtEdEmail.Text, out string msgEmail);
            algoCambio = okEmail;
            msgFinal = msgEmail;

            // 2. Asignar nuevo rol (solo si tiene permiso y cambió el rol)
            var rolSeleccionado = cmbRolEdicion.SelectedItem as Rol;
            if (_puedeAsignarRoles && rolSeleccionado != null &&
                rolSeleccionado.IdRol != _seleccionado.IdRol)
            {
                bool okRol = _servicio.AsignarRol(
                    _seleccionado.IdUsuario, rolSeleccionado.IdRol, out string msgRol);
                algoCambio = algoCambio || okRol;
                msgFinal += "\n" + msgRol;
            }

            MostrarMensaje(msgFinal.Trim(), esError: !algoCambio);
            if (algoCambio) { ActualizarGrillaSuperior(); SetModoConsulta(); }
        }

        private void EjecutarDesbloqueo()
        {
            bool ok = _servicio.Desbloquear(_seleccionado.IdUsuario, out string msg);
            MostrarMensaje(msg, esError: !ok);
            if (ok) { ActualizarGrillaSuperior(); SetModoConsulta(); }
        }

        // ============================================================
        // FILTROS / MARCADORES
        // ============================================================
        private void rdActivos_CheckedChanged_1(object sender, EventArgs e)
        {
            if (rdActivos.Checked) { _soloActivos = true; ActualizarGrillaSuperior(); }
        }

        private void rdTodos_CheckedChanged_1(object sender, EventArgs e)
        {
            if (rdTodos.Checked) { _soloActivos = false; ActualizarGrillaSuperior(); }
        }

        private void btnAplicarFiltro_Click_1(object sender, EventArgs e)
        {
            var rol = cmbFilRol.SelectedItem as Rol;
            var lista = _servicio.ObtenerFiltrado(
                txtFilDNI.Text, txtFilApellidos.Text, txtFilNombres.Text,
                txtFilLogin.Text,
                (rol != null && rol.IdRol != 0) ? (int?)rol.IdRol : null,
                _soloActivos);
            CargarEnGrilla(lista);
            lblNumUsuarios.Text = $"Número de Usuarios: {dgvSuperior.Rows.Count}";
            rdActivos.Checked = false;
            rdTodos.Checked = false;
        }

        private void MostrarMensaje(string texto, bool esError)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = esError
                ? Color.FromArgb(180, 0, 0)
                : Color.FromArgb(20, 110, 50);
        }


    }
}

