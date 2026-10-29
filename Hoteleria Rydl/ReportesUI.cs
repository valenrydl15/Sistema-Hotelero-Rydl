using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GUI_HRS_480
{
    /// <summary>Utilidades de pantalla compartidas por los reportes PDF.</summary>
    internal static class ReportesUI
    {
        /// <summary>Pide dónde guardar el PDF. Devuelve null si el usuario cancela.</summary>
        public static string PedirRuta(string nombreSugerido)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Exportar a PDF";
                dlg.Filter = "Documento PDF (*.pdf)|*.pdf";
                dlg.DefaultExt = "pdf";
                dlg.AddExtension = true;
                dlg.OverwritePrompt = true;
                dlg.FileName = nombreSugerido;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
            }
        }

        /// <summary>Pregunta si se quiere abrir el PDF recién generado y lo abre con el visor predeterminado.</summary>
        public static void OfrecerAbrir(IWin32Window padre, string ruta, string mensaje)
        {
            var r = MessageBox.Show(padre, mensaje + "\n\n¿Querés abrirlo ahora?", "PDF generado",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (r != DialogResult.Yes) return;
            try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show(padre, "No se pudo abrir el archivo: " + ex.Message, "PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
