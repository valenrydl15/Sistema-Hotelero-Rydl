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
    public partial class FrmReparacionDV : Form
    {
        private readonly ResultadoVerificacionDV _resultado;
        private readonly Usuario _admin;
        private readonly DigitoVerificadorServicio _dvServicio = new DigitoVerificadorServicio();
        public FrmReparacionDV(ResultadoVerificacionDV resultado, Usuario admin)
        {
            _resultado = resultado;
            _admin = admin;
            InitializeComponent();
            Tema.Aplicar(this);
            CargarInconsistencias();
        }

        private void CargarInconsistencias()
        {
            lstInconsistencias.Items.Clear();
            foreach (var inc in _resultado.Inconsistencias)
                lstInconsistencias.Items.Add(inc.ResumenCorto);

            lblCantidad.Text =
                $"Se detectaron {_resultado.Inconsistencias.Count} inconsistencia/s:";

            // Cargar backups disponibles en el combo de restore
            cmbBackups.Items.Clear();
            var backups = _dvServicio.ObtenerBackupsDisponibles();
            foreach (var b in backups)
                cmbBackups.Items.Add(Path.GetFileName(b));

            if (cmbBackups.Items.Count > 0)
                cmbBackups.SelectedIndex = 0;
            else
            {
                cmbBackups.Items.Add("(No hay backups disponibles)");
                cmbBackups.SelectedIndex = 0;
                btnRestore.Enabled = false;
            }
        }

        private void btnRecalcular_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
               "¿Recalcular el dígito verificador?\n\n" +
               "Esto tomará el estado ACTUAL de la base de datos como válido,\n" +
               "descartando la inconsistencia detectada.\n\n" +
               "¿Confirmar?",
               "Confirmar recalculado",
               MessageBoxButtons.YesNo,
               MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                btnRecalcular.Enabled = false;
                lblEstado.Text = "Recalculando...";
                lblEstado.ForeColor = Color.FromArgb(180, 120, 0);
                Application.DoEvents();

                _dvServicio.Recalcular(_resultado.Inconsistencias);

                lblEstado.Text = "✔ Recalculado correctamente.";
                lblEstado.ForeColor = Color.FromArgb(30, 130, 70);

                MessageBox.Show(
                    "✔ Dígito Verificador recalculado.\n" +
                    "El sistema ahora está en un estado consistente.\n\n" +
                    "Por favor, iniciá sesión nuevamente.",
                    "Recalculado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblEstado.Text = "❌ Error al recalcular.";
                lblEstado.ForeColor = Color.FromArgb(200, 50, 50);
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRecalcular.Enabled = true;
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (cmbBackups.SelectedItem == null ||
        cmbBackups.SelectedItem.ToString() == "(No hay backups disponibles)")
            {
                MessageBox.Show("No hay backups disponibles para restaurar.",
                    "Sin backups", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Restaurar desde el backup:\n'{cmbBackups.SelectedItem}'?\n\n" +
                "⚠ Esta acción reemplazará la base de datos completa.\n" +
                "TODOS los cambios posteriores al backup se perderán.\n\n" +
                "¿Confirmar restore?",
                "Confirmar restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop);

            if (confirm != DialogResult.Yes) return;

            try
            {
                btnRestore.Enabled = false;
                lblEstado.Text = "Ejecutando restore...";
                lblEstado.ForeColor = Color.FromArgb(180, 120, 0);
                Application.DoEvents();

                // ✅ CORRECTO — la ruta completa ya viene del servicio
                // ObtenerBackupsDisponibles() devuelve rutas completas
                // El combo muestra solo el nombre, pero necesitamos la ruta completa
                var backupsCompletos = _dvServicio.ObtenerBackupsDisponibles();
                string nombreSeleccionado = cmbBackups.SelectedItem.ToString();

                // Buscar la ruta completa que corresponde al nombre seleccionado
                string rutaCompleta = backupsCompletos
                    .FirstOrDefault(r => Path.GetFileName(r) == nombreSeleccionado);

                if (rutaCompleta == null)
                {
                    MessageBox.Show("No se encontró el archivo de backup.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnRestore.Enabled = true;
                    return;
                }

                _dvServicio.EjecutarRestore(rutaCompleta, _resultado.Inconsistencias);

                lblEstado.Text = "✔ Restore ejecutado correctamente.";
                lblEstado.ForeColor = Color.FromArgb(30, 130, 70);

                MessageBox.Show(
                    "✔ Base de datos restaurada correctamente.\n\n" +
                    "Por favor, iniciá sesión nuevamente.",
                    "Restore exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblEstado.Text = "❌ Error durante el restore.";
                lblEstado.ForeColor = Color.FromArgb(200, 50, 50);
                MessageBox.Show(
                    $"Error durante el restore:\n{ex.Message}\n\n" +
                    "Verificá que SQL Server esté activo y cerrá todas las\n" +
                    "ventanas de SSMS antes de hacer el restore.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRestore.Enabled = true;
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Saliendo sin resolver la inconsistencia.\n" +
                "El sistema permanecerá bloqueado hasta que un administrador\n" +
                "resuelva el problema.",
                "Sin resolución",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.Cancel;
            Close();

        }
    }
}
