using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SEGURIDAD_HRS_480;

namespace GUI_HRS_480
{
    public class Traductor
    {
        public Dictionary<string, string> traducciones;
        public Traductor()
        {
            traducciones = new Dictionary<string, string>();
        }
        public void ExportarTextosAJson(Form formulario, string nombreArchivo)
        {
            var traducciones = new Dictionary<string, string>();

            void Recorrer(Control parent)
            {
                foreach (Control ctrl in parent.Controls)
                {
                    if (ctrl is Label lbl)
                        traducciones[lbl.Name] = lbl.Text;

                    else if (ctrl is TextBox txt)
                        traducciones[txt.Name] = txt.Text;

                    else if (ctrl is Button btn)
                        traducciones[btn.Name] = btn.Text;

                    else if (ctrl is RadioButton rb)
                        traducciones[rb.Name] = rb.Text;

                    else if (ctrl is DataGridView dgv)
                    {
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            string clave = $"{dgv.Name}_Col{i + 1}";
                            traducciones[clave] = dgv.Columns[i].HeaderText;
                        }
                    }

                    if (ctrl.HasChildren)
                        Recorrer(ctrl);
                }
            }

            Recorrer(formulario);

            string carpetaDestino = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Traducciones");

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            string ruta = Path.Combine(
                carpetaDestino,
                $"{nombreArchivo}espanol.json");

            File.WriteAllText(
                ruta,
                JsonConvert.SerializeObject(
                    traducciones,
                    Newtonsoft.Json.Formatting.Indented));

            MessageBox.Show($"JSON generado:\n{ruta}");
        }

        public Dictionary<string, string> CargarTraducciones(string nombreFormulario)
        {

            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Traducciones\\{nombreFormulario}{SesionManager.Instancia.Idioma}.json");

            if (!File.Exists(ruta))
            {
                return new Dictionary<string, string>();
            }

            string contenido = File.ReadAllText(ruta);
            var traducciones = JsonConvert.DeserializeObject<Dictionary<string, string>>(contenido);
            return traducciones ?? new Dictionary<string, string>();
        }

        public void ActualizarTraducciones(string nombreFormulario, Form form)
        {
            traducciones = CargarTraducciones(nombreFormulario);

            if (traducciones != null)
            {
                void Recorrer(Control parent)
                {
                    foreach (Control ctrl in parent.Controls)
                    {
                        if (ctrl is Label lbl && traducciones.ContainsKey(lbl.Name))
                            lbl.Text = traducciones[lbl.Name];

                        else if (ctrl is TextBox txt && traducciones.ContainsKey(txt.Name))
                            txt.Text = traducciones[txt.Name];

                        else if (ctrl is Button btn && traducciones.ContainsKey(btn.Name))
                            btn.Text = traducciones[btn.Name];

                        else if (ctrl is RadioButton rb && traducciones.ContainsKey(rb.Name))
                            rb.Text = traducciones[rb.Name];

                        else if (ctrl is DataGridView dgv)
                        {
                            for (int i = 0; i < dgv.Columns.Count; i++)
                            {
                                string clave = $"{dgv.Name}_Col{i + 1}";
                                if (traducciones.ContainsKey(clave))
                                    dgv.Columns[i].HeaderText = traducciones[clave];
                            }
                        }

                        if (ctrl.HasChildren)
                            Recorrer(ctrl);
                    }
                }

                Recorrer(form);
            }
        }
    }

}

