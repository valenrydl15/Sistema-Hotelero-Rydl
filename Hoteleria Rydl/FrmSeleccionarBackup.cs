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
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmSeleccionarBackup : Form
    {

        private readonly List<string> _backups;
        private readonly DigitoVerificadorServicio _dvServicio;

        
        public FrmSeleccionarBackup(List<string> backups, DigitoVerificadorServicio dvServicio)
        {
            _backups = backups;
            _dvServicio = dvServicio;
            InitializeComponent();
            Tema.Aplicar(this);
            CargarBackups();
        }

        private void CargarBackups()
        {
            _lstBackups.Items.Clear();
            foreach (var b in _backups)
                _lstBackups.Items.Add(Path.GetFileName(b));

            if (_lstBackups.Items.Count > 0)
                _lstBackups.SelectedIndex = 0;
        }
        private void _btnRestore_Click(object sender, EventArgs e)
        {
            if (_lstBackups.SelectedItem == null)
            {
                MessageBox.Show("Seleccioná un backup de la lista.",
                    "⚠ Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nombreSeleccionado = _lstBackups.SelectedItem.ToString();
            string rutaCompleta = _backups
                .FirstOrDefault(r => Path.GetFileName(r) == nombreSeleccionado);

            if (rutaCompleta == null)
            {
                MessageBox.Show("No se encontró el archivo de backup.",
                    "❌ Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Restaurar desde:\n'{nombreSeleccionado}'?\n\n" +
                "⚠ Esta acción reemplazará la base de datos completa.\n" +
                "TODOS los cambios posteriores al backup se perderán.\n\n" +
                "¿Confirmás el restore?",
                "⚠ Confirmar Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnRestore.Enabled = false;
                _btnCancelar.Enabled = false;
                _lblEstado.Text = "Ejecutando restore... (puede tardar unos segundos)";
                _lblEstado.ForeColor = Color.FromArgb(180, 120, 0);
                Application.DoEvents();

                // Restore sin lista de inconsistencias (restore manual, no hay inconsistencias)
                Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
                System.Threading.Thread.Sleep(1000);

                _dvServicio.EjecutarRestore(rutaCompleta, new List<InconsistenciaDV>());

                _lblEstado.Text = "✔ Restore ejecutado correctamente.";
                _lblEstado.ForeColor = Color.FromArgb(30, 130, 70);

                MessageBox.Show(
                    "✔ Base de datos restaurada correctamente.\n\n" +
                    "Se recomienda cerrar y volver a abrir la aplicación.",
                    "Restore exitoso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblEstado.Text = "❌ Error durante el restore.";
                _lblEstado.ForeColor = Color.FromArgb(200, 50, 50);
                MessageBox.Show(
                    $"Error durante el restore:\n{ex.Message}\n\n" +
                    "Cerrá todas las ventanas de SSMS e intentá nuevamente.",
                    "❌ Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnRestore.Enabled = true;
                _btnCancelar.Enabled = true;
            }
        }
        

        private void _btnCancelar_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
