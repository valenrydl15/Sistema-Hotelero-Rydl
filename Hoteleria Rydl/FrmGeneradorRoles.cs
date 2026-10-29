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
    public partial class FrmGeneradorRoles : Form, Idioma.IObserver
    {
        private readonly PerfilServicio _servicio = new PerfilServicio();

        private enum Modo { Consulta, Crear, Modificar }
        private Modo _modo = Modo.Consulta;
        private RolPerfil _rolSeleccionado;

        private List<Permiso> _todosPermisos;
        private List<Familia> _todasFamilias;
        private List<Rol> _todosRoles;
        public FrmGeneradorRoles()
        {
            InitializeComponent();
            Tema.Aplicar(this);
            CargarDatos();
            SetModoConsulta();
            Idioma.GestorTraducciones.Instancia.AgregarForm(this);
            ActualizarTraducciones();
        }

        public void ActualizarTraducciones()
        {
            new Traductor().ActualizarTraducciones("FrmGeneradorRoles", this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }
        private void CargarDatos()
        {
            _todosPermisos = _servicio.ObtenerPermisos();
            _todasFamilias = _servicio.ObtenerFamilias();
            _todosRoles = _servicio.ObtenerRoles();

            // Lista de roles existentes
            lstRolesExistentes.Items.Clear();
            foreach (var r in _todosRoles)
                lstRolesExistentes.Items.Add(r);

            // Filtro
            cmbFiltro.Items.Clear();
            cmbFiltro.Items.Add("Permisos");
            cmbFiltro.Items.Add("Familias");
            cmbFiltro.SelectedIndex = 0;

            ActualizarListaDerecha();
        }
        private void ActualizarListaDerecha()
        {
            lstDisponibles.Items.Clear();
            if (cmbFiltro.SelectedIndex == 0)
                foreach (var p in _todosPermisos) lstDisponibles.Items.Add(p);
            else
                foreach (var f in _todasFamilias) lstDisponibles.Items.Add(f);
        }
        private void SetModoConsulta()
        {
            _modo = Modo.Consulta;
            lblMensaje.Text = "Modo Consulta";
            btnCrear.Enabled = true;
            btnModificar.Enabled = true;
            btnEliminar.Enabled = true;
            btnAplicar.Enabled = false;
            btnSalir.Enabled = true;
            txtNombre.Enabled = false;
            txtNombre.Clear();
            chkSeleccionados.Items.Clear();
        }

        private void SetModoCrear()
        {
            _modo = Modo.Crear;
            lblMensaje.Text = "Modo Crear — Ingresá el nombre y seleccioná componentes";
            btnCrear.Enabled = false;
            btnModificar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAplicar.Enabled = true;
            btnSalir.Enabled = false;
            txtNombre.Enabled = true;
            txtNombre.Clear();
            txtNombre.Focus();
            chkSeleccionados.Items.Clear();
        }

        private void SetModoModificar()
        {
            if (_rolSeleccionado == null)
            { Msg("Seleccioná un rol de la lista.", esError: true); return; }

            _modo = Modo.Modificar;
            lblMensaje.Text = $"Modo Modificar — Rol: {_rolSeleccionado.Nombre}";
            btnCrear.Enabled = false;
            btnModificar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAplicar.Enabled = true;
            btnSalir.Enabled = false;
            txtNombre.Enabled = false;
            txtNombre.Text = _rolSeleccionado.Nombre;

            // Cargar componentes actuales
            var rolCompleto = _servicio.ObtenerRolCompleto(_rolSeleccionado.IdRol);
            chkSeleccionados.Items.Clear();
            foreach (var p in rolCompleto.PermisosDirectos)
                chkSeleccionados.Items.Add(p, true);
            foreach (var f in rolCompleto.Familias)
                chkSeleccionados.Items.Add(f, true);
        }
        private void btnCrear_Click(object sender, EventArgs e)
        {
            SetModoCrear();
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            SetModoModificar();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_rolSeleccionado == null)
            { Msg("Seleccioná un rol para eliminar.", esError: true); return; }

            if (_rolSeleccionado.Nombre == "Basico")
            { Msg("❌ No se puede eliminar el rol Básico.", esError: true); return; }

            // Verificar si está en uso
            bool enUso = _servicio.ObtenerRolCompleto(_rolSeleccionado.IdRol) != null;

            var confirm = MessageBox.Show(
                $"¿Eliminar el rol '{_rolSeleccionado.Nombre}'?\n\n" +
                "⚠ Si hay usuarios con este rol, pasarán automáticamente al rol 'Básico'.\n\n" +
                "¿Confirmás la eliminación?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            bool ok = _servicio.EliminarRol(
                _rolSeleccionado.IdRol, out string msg, out int afectados);
            Msg(msg, esError: !ok);

            if (ok && afectados > 0)
                MessageBox.Show(
                    $"ℹ {afectados} usuario/s fueron movidos al rol 'Básico'.",
                    "Usuarios afectados",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            if (ok) { CargarDatos(); _rolSeleccionado = null; SetModoConsulta(); }
        }

        private void btnAplicar_Click(object sender, EventArgs e)
        {
            var idsPermisos = new List<int>();
            var idsFamilias = new List<int>();

            foreach (var item in chkSeleccionados.CheckedItems)
            {
                if (item is Permiso p) idsPermisos.Add(p.IdPermiso);
                else if (item is Familia f) idsFamilias.Add(f.IdFamilia);
            }

            bool ok; string msg;

            if (_modo == Modo.Crear)
                ok = _servicio.CrearRol(txtNombre.Text, "", idsPermisos, idsFamilias, out msg);
            else
                ok = _servicio.ModificarRol(_rolSeleccionado.IdRol, idsPermisos, idsFamilias, out msg);

            Msg(msg, esError: !ok);
            if (ok) { CargarDatos(); SetModoConsulta(); }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void lstDisponibles_DoubleClick(object sender, EventArgs e)
        {
            if (lstDisponibles.SelectedItem == null) return;
            var item = lstDisponibles.SelectedItem;

            bool yaEsta = false;
            foreach (var ex in chkSeleccionados.Items)
                if (ex.GetType() == item.GetType() &&
                    ((dynamic)ex).Id == ((dynamic)item).Id)
                { yaEsta = true; break; }

            if (!yaEsta) chkSeleccionados.Items.Add(item, true);
        }

        private void chkSeleccionados_DoubleClick(object sender, EventArgs e)
        {
            if (chkSeleccionados.SelectedIndex >= 0)
                chkSeleccionados.Items.RemoveAt(chkSeleccionados.SelectedIndex);
        }

        private void btnVerPermisos_Click(object sender, EventArgs e)
        {
            Familia fam = null;
            if (lstDisponibles.SelectedItem is Familia fD) fam = fD;
            else if (lstRolesExistentes.SelectedItem is Rol r)
            {
                // Si seleccionó un rol, mostramos todos sus permisos desenrollados
                var rolCompleto = _servicio.ObtenerRolCompleto(r.IdRol);
                if (rolCompleto != null)
                {
                    var perms = rolCompleto.ObtenerPermisos().ToList();
                    string lista2 = perms.Count > 0
                        ? string.Join("\n", perms.Select(p => $"  • {p.Nombre}"))
                        : "  (sin permisos)";
                    MessageBox.Show($"Permisos del rol '{r.Nombre}':\n\n{lista2}",
                        "Consulta de permisos del rol",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (fam == null) { Msg("Seleccioná una familia o rol para consultar.", esError: true); return; }

            var famCompleta = _servicio.ObtenerFamiliaCompleta(fam.IdFamilia);
            var permsFam = famCompleta.ObtenerPermisos().ToList();
            string listaFam = permsFam.Count > 0
                ? string.Join("\n", permsFam.Select(p => $"  • {p.Nombre}"))
                : "  (sin permisos)";
            MessageBox.Show($"Permisos de la familia '{fam.Nombre}':\n\n{listaFam}",
                "Consulta de permisos de familia",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lstRolesExistentes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRolesExistentes.SelectedItem is Rol r)
                _rolSeleccionado = new RolPerfil { IdRol = r.IdRol, Nombre = r.Nombre };
        }

        private void cmbFiltro_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarListaDerecha();
        }

        private void Msg(string texto, bool esError)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = esError
                ? Color.FromArgb(200, 50, 50)
                : Color.FromArgb(30, 130, 70);

            if (esError)
            {
                MessageBox.Show(
                    texto,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void btnCancelar_Click(object sender, EventArgs e)
        {
            CargarDatos(); SetModoConsulta();
        }

       
    }
}
