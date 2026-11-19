using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SEGURIDAD_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// Traduce los formularios con los archivos Traducciones\{Formulario}{idioma}.json.
    /// Cada clave del JSON puede ser:
    ///   • el NOMBRE del control (btnSalir, lblTitulo, dgv_Col1 para las columnas de una grilla con nombre), o
    ///   • el TEXTO ORIGINAL en español del control (para las pantallas que crean sus controles por código
    ///     y no les ponen nombre: "Ver disponibilidad", "CÓDIGO", el título de la ventana, etc.).
    /// El texto original de cada control se guarda la primera vez, así se puede pasar de un idioma a otro
    /// y volver al español. Solo se cambian los controles que figuran en el JSON: los que muestran datos no se tocan.
    /// </summary>
    public class Traductor
    {
        public Dictionary<string, string> traducciones;

        // texto original (español) de cada control / columna / formulario
        private static readonly ConditionalWeakTable<object, string> _originales = new ConditionalWeakTable<object, string>();

        public Traductor()
        {
            traducciones = new Dictionary<string, string>();
        }

        public void ExportarTextosAJson(Form formulario, string nombreArchivo)
        {
            string carpetaDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Traducciones");
            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);
            string ruta = Path.Combine(carpetaDestino, $"{nombreArchivo}espanol.json");

            // Se agregan las claves que falten sin pisar las que ya están traducidas a mano
            var traducciones = File.Exists(ruta)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(ruta)) ?? new Dictionary<string, string>()
                : new Dictionary<string, string>();

            void Agregar(string clave, string texto)
            {
                if (!string.IsNullOrWhiteSpace(clave) && !string.IsNullOrWhiteSpace(texto) && !traducciones.ContainsKey(clave))
                    traducciones[clave] = texto;
            }

            void Recorrer(Control parent)
            {
                foreach (Control ctrl in parent.Controls)
                {
                    if (ctrl is Label || ctrl is Button || ctrl is RadioButton || ctrl is CheckBox || ctrl is GroupBox || ctrl is TabPage)
                        Agregar(string.IsNullOrEmpty(ctrl.Name) ? ctrl.Text : ctrl.Name, ctrl.Text);

                    else if (ctrl is DataGridView dgv)
                    {
                        for (int i = 0; i < dgv.Columns.Count; i++)
                            Agregar(string.IsNullOrEmpty(dgv.Name) ? dgv.Columns[i].HeaderText : $"{dgv.Name}_Col{i + 1}", dgv.Columns[i].HeaderText);
                    }

                    if (ctrl.HasChildren)
                        Recorrer(ctrl);
                }
            }

            Agregar(formulario.Text, formulario.Text);
            Recorrer(formulario);

            File.WriteAllText(ruta, JsonConvert.SerializeObject(traducciones, Newtonsoft.Json.Formatting.Indented));
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

        private static string Original(object o, string textoActual)
        {
            if (_originales.TryGetValue(o, out var original)) return original;
            original = textoActual ?? "";
            _originales.Add(o, original);
            return original;
        }

        /// <summary>Traducción para un control: primero por nombre, después por su texto original.</summary>
        private string Buscar(string nombre, object objeto, string textoActual)
        {
            string original = Original(objeto, textoActual);
            if (!string.IsNullOrEmpty(nombre) && traducciones.TryGetValue(nombre, out var t)) return t;
            if (!string.IsNullOrWhiteSpace(original) && traducciones.TryGetValue(original, out t)) return t;
            return null;
        }

        public void ActualizarTraducciones(string nombreFormulario, Form form)
        {
            traducciones = CargarTraducciones(nombreFormulario);
            if (traducciones == null || traducciones.Count == 0) return;

            string t;
            if ((t = Buscar(null, form, form.Text)) != null) form.Text = t;

            void Recorrer(Control parent)
            {
                foreach (Control ctrl in parent.Controls)
                {
                    if (ctrl is TextBox txt)
                    {
                        // los TextBox solo por nombre (su texto es lo que escribe el usuario)
                        if (!string.IsNullOrEmpty(txt.Name) && traducciones.TryGetValue(txt.Name, out var tt))
                            txt.Text = tt;
                    }
                    else if (ctrl is Label || ctrl is Button || ctrl is RadioButton || ctrl is CheckBox || ctrl is GroupBox || ctrl is TabPage)
                    {
                        if ((t = Buscar(ctrl.Name, ctrl, ctrl.Text)) != null)
                            ctrl.Text = t;
                    }
                    else if (ctrl is DataGridView dgv)
                    {
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            var col = dgv.Columns[i];
                            string clave = string.IsNullOrEmpty(dgv.Name) ? null : $"{dgv.Name}_Col{i + 1}";
                            if ((t = Buscar(clave, col, col.HeaderText)) != null)
                                col.HeaderText = t;
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
