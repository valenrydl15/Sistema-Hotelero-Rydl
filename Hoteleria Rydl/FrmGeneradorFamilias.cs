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
using Microsoft.Data.SqlClient;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmGeneradorFamilias : Form, Idioma.IObserver
    {
        private readonly PerfilServicio _servicio = new PerfilServicio();

        private enum Modo { Consulta, Generar, Modificar }
        private Modo _modo = Modo.Consulta;
        private Familia _familiaSeleccionada;

        // Listas cargadas
        private List<Permiso> _todosPermisos;
        private List<Familia> _todasFamilias;

        public FrmGeneradorFamilias()
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
            new Traductor().ActualizarTraducciones("FrmGeneradorFamilias", this);
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

            // Combo filtro Permisos/Familias
            cmbFiltro.Items.Clear();
            cmbFiltro.Items.Add("Permisos");
            cmbFiltro.Items.Add("Familias");
            cmbFiltro.SelectedIndex = 0;

            // Lista de familias existentes (panel izquierdo superior)
            lstFamiliasExistentes.Items.Clear();
            foreach (var f in _todasFamilias)
                lstFamiliasExistentes.Items.Add(f);

            ActualizarListaDerecha();
        }

        private void ActualizarListaDerecha()
        {
            lstDisponibles.Items.Clear();
            if (cmbFiltro.SelectedIndex == 0)
            {
                // Mostrar Permisos
                foreach (var p in _todosPermisos)
                    lstDisponibles.Items.Add(p);
            }
            else
            {
                // Mostrar Familias existentes
                foreach (var f in _todasFamilias)
                    lstDisponibles.Items.Add(f);
            }
        }

        private void SetModoConsulta()
        {
            _modo = Modo.Consulta;
            lblMensaje.Text = "Modo Consulta";
            btnGenerar.Enabled = true;
            btnModificar.Enabled = true;
            btnEliminar.Enabled = true;
            btnAplicar.Enabled = false;
            btnSalir.Enabled = true;
            txtNombre.Enabled = false;
            txtNombre.Clear();
            chkSeleccionados.Items.Clear();
        }

        private void SetModoGenerar()
        {
            _modo = Modo.Generar;
            lblMensaje.Text = "Modo Generar — Ingresá el nombre y seleccioná los componentes";
            btnGenerar.Enabled = false;
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
            if (_familiaSeleccionada == null)
            { Msg("Seleccioná una familia de la lista.", esError: true); return; }

            _modo = Modo.Modificar;
            lblMensaje.Text = $"Modo Modificar — Familia: {_familiaSeleccionada.Nombre}";
            btnGenerar.Enabled = false;
            btnModificar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAplicar.Enabled = true;
            btnSalir.Enabled = false;
            txtNombre.Enabled = false;
            txtNombre.Text = _familiaSeleccionada.Nombre;

            // Cargar componentes actuales en el checklist
            var familiaCompleta = _servicio.ObtenerFamiliaCompleta(_familiaSeleccionada.IdFamilia);
            chkSeleccionados.Items.Clear();
            foreach (var p in familiaCompleta.Permisos)
                chkSeleccionados.Items.Add(p, true);
            foreach (var sf in familiaCompleta.SubFamilias)
                chkSeleccionados.Items.Add(sf, true);
        }


        private void btnGenerar_Click(object sender, EventArgs e)
        {
            SetModoGenerar();
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            SetModoModificar();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {

            if (_familiaSeleccionada == null)
            { Msg("Seleccioná una familia para eliminar.", esError: true); return; }

            var confirm = MessageBox.Show(
                $"¿Eliminar la familia '{_familiaSeleccionada.Nombre}'?\n" +
                "No se puede eliminar si está en uso por un rol u otra familia.",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            bool ok = _servicio.EliminarFamilia(_familiaSeleccionada.IdFamilia, out string msg);
            Msg(msg, esError: !ok);
            if (ok) { CargarDatos(); _familiaSeleccionada = null; }
        }

        private void btnAplicar_Click(object sender, EventArgs e)
        {
            // Obtener ids seleccionados en el checklist
            var idsPermisos = new List<int>();
            var idsSubFamilias = new List<int>();

            foreach (var item in chkSeleccionados.CheckedItems)
            {
                if (item is Permiso p) idsPermisos.Add(p.IdPermiso);
                else if (item is Familia f) idsSubFamilias.Add(f.IdFamilia);
            }

            bool ok;
            string msg;

            if (_modo == Modo.Generar)
                ok = _servicio.CrearFamilia(txtNombre.Text, "", idsPermisos, idsSubFamilias, out msg);
            else
                ok = _servicio.ModificarFamilia(_familiaSeleccionada.IdFamilia,
                                                 idsPermisos, idsSubFamilias, out msg);

            Msg(msg, esError: !ok);
            if (ok) { CargarDatos(); SetModoConsulta(); }

        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmbFiltro_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarListaDerecha();
        }

        private void lstFamiliasExistentes_SelectedIndexChanged(object sender, EventArgs e)
        {
            _familiaSeleccionada = lstFamiliasExistentes.SelectedItem as Familia;
        }

        private void chkSeleccionados_DoubleClick(object sender, EventArgs e)
        {
            if (chkSeleccionados.SelectedIndex >= 0)
                chkSeleccionados.Items.RemoveAt(chkSeleccionados.SelectedIndex);
        }

        private void lstDisponibles_DoubleClick(object sender, EventArgs e)
        {
            if (lstDisponibles.SelectedItem == null) return;
            var item = lstDisponibles.SelectedItem;

            // Verificar que no esté ya en la lista
            bool yaEsta = false;
            foreach (var existing in chkSeleccionados.Items)
                if (existing.GetType() == item.GetType() &&
                    ((dynamic)existing).Id == ((dynamic)item).Id)
                { yaEsta = true; break; }

            if (!yaEsta)
                chkSeleccionados.Items.Add(item, true);
        }

        private void btnVerPermisos_Click(object sender, EventArgs e)
        {
            // Funciona sobre el item seleccionado en lstDisponibles (cuando es una familia)
            // o en lstFamiliasExistentes
            Familia fam = null;

            if (lstFamiliasExistentes.SelectedItem is Familia fSel)
                fam = fSel;
            else if (lstDisponibles.SelectedItem is Familia fDisp)
                fam = fDisp;

            if (fam == null)
            { Msg("Seleccioná una familia para ver sus permisos.", esError: true); return; }

            var completa = _servicio.ObtenerFamiliaCompleta(fam.IdFamilia);
            var permisos = completa.ObtenerPermisos().ToList();

            string lista = permisos.Count > 0
                ? string.Join("\n", permisos.Select(p => $"  • {p.Nombre}"))
                : "  (sin permisos asignados)";

            MessageBox.Show(
                $"Permisos de la familia '{fam.Nombre}':\n\n{lista}",
                "Consulta de permisos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            CargarDatos(); SetModoConsulta();
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

    }
}
    

