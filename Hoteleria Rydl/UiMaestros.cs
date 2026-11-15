#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GUI_HRS_480
{
    /// <summary>Helpers de construcción de las pantallas del menú Maestros (mismo estilo que el resto del sistema).</summary>
    internal static class UiMaestros
    {
        public static Panel Encabezado(Form f, string titulo, string subtitulo)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Marino };
            p.Controls.Add(new Label { Text = titulo, ForeColor = Color.White, Font = Tema.Fuente(15f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 8) });
            p.Controls.Add(new Label { Text = subtitulo, ForeColor = Color.FromArgb(190, 205, 230), Font = Tema.Fuente(9f), AutoSize = true, Location = new Point(22, 38) });
            p.Paint += (s, e) => { using (var br = new SolidBrush(Tema.Dorado)) e.Graphics.FillRectangle(br, 0, p.Height - 3, p.Width, 3); };
            f.Controls.Add(p);
            return p;
        }

        public static Label Etiqueta(Control padre, string texto, int x, int y, int w = 0, bool negrita = false)
        {
            var l = new Label
            {
                Text = texto, Location = new Point(x, y), ForeColor = negrita ? Tema.Marino : Tema.TextoSuave,
                Font = Tema.Fuente(negrita ? 9.5f : 9f, negrita ? FontStyle.Bold : FontStyle.Regular)
            };
            if (w > 0) { l.AutoSize = false; l.Size = new Size(w, 22); l.TextAlign = ContentAlignment.MiddleLeft; } else l.AutoSize = true;
            padre.Controls.Add(l);
            return l;
        }

        public static TextBox Caja(Control padre, int x, int y, int w, int max)
        {
            var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 26), MaxLength = max, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            padre.Controls.Add(t);
            return t;
        }

        public static Button Boton(Control padre, string texto, int x, int y, int w, int h, Action<Button> estilo, Action click)
        {
            var b = new Button { Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
            estilo(b);
            b.Click += (s, e) => click();
            padre.Controls.Add(b);
            return b;
        }

        public static void Peligro(Button b)
        {
            Tema.BotonPrimario(b);
            b.BackColor = Tema.Peligro;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 45, 35);
        }

        public static DataGridView Grilla(Control padre, int x, int y, int w, int h, params (string titulo, float peso)[] cols)
        {
            var g = new DataGridView { Location = new Point(x, y), Size = new Size(w, h) };
            Tema.EstilizarGrid(g);
            g.ScrollBars = ScrollBars.Both;
            foreach (var (titulo, peso) in cols)
                g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
            g.Paint += (s, e) => { using (var pen = new Pen(Tema.Borde)) e.Graphics.DrawRectangle(pen, 0, 0, g.Width - 1, g.Height - 1); };
            padre.Controls.Add(g);
            return g;
        }

        /// <summary>Cuadro de mensajes de la pantalla (el "Mensaje:" del enunciado).</summary>
        public static Label CuadroMensaje(Control padre, int x, int y, int w, int h)
        {
            var marco = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.White };
            marco.Paint += (s, e) => { using (var pen = new Pen(Tema.Borde)) e.Graphics.DrawRectangle(pen, 0, 0, marco.Width - 1, marco.Height - 1); };
            marco.Controls.Add(new Label { Text = "Mensaje:", Font = Tema.Fuente(9f, FontStyle.Bold), ForeColor = Tema.Marino, AutoSize = true, Location = new Point(10, 8) });
            var l = new Label { Location = new Point(10, 30), Size = new Size(w - 20, h - 38), Font = Tema.Fuente(9.5f), ForeColor = Tema.Texto };
            marco.Controls.Add(l);
            padre.Controls.Add(marco);
            return l;
        }

        public static void Mostrar(Label cuadro, string texto, bool ok)
        {
            cuadro.Text = texto ?? "";
            cuadro.ForeColor = ok ? Tema.Exito : Tema.Peligro;
        }

        public static void Linea(Control padre, int x, int y, int w)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, 1), BackColor = Tema.Borde };
            padre.Controls.Add(p);
        }

        /// <summary>Botón chico con el ícono de carpeta (seleccionar ubicación / archivo).</summary>
        public static Button BotonCarpeta(Control padre, int x, int y, Action click)
        {
            var b = new Button { Text = "📁", Location = new Point(x, y), Size = new Size(36, 26), Font = new Font("Segoe UI Emoji", 9f) };
            Tema.BotonSecundario(b);
            b.Font = new Font("Segoe UI Emoji", 9f);
            b.Click += (s, e) => click();
            padre.Controls.Add(b);
            return b;
        }
    }
}
