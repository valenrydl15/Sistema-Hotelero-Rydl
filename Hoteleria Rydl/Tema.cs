using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GUI_HRS_480
{
    /// <summary>Paleta y estilos compartidos por las pantallas (menú, recepción).</summary>
    public static class Tema
    {
        public static readonly Color Marino      = Color.FromArgb(20, 45, 80);
        public static readonly Color MarinoOscuro= Color.FromArgb(13, 31, 58);
        public static readonly Color MarinoHover = Color.FromArgb(34, 66, 110);
        public static readonly Color Dorado      = Color.FromArgb(201, 162, 39);
        public static readonly Color Fondo       = Color.FromArgb(243, 245, 249);
        public static readonly Color Tarjeta     = Color.White;
        public static readonly Color Borde       = Color.FromArgb(219, 224, 233);
        public static readonly Color Texto       = Color.FromArgb(38, 50, 72);
        public static readonly Color TextoSuave  = Color.FromArgb(110, 122, 142);
        public static readonly Color Exito       = Color.FromArgb(30, 130, 70);
        public static readonly Color Peligro     = Color.FromArgb(192, 57, 43);

        public static Font Fuente(float size, FontStyle style = FontStyle.Regular)
            => new Font("Segoe UI", size, style, GraphicsUnit.Point);

        public static void BotonPrimario(Button b)
        {
            Base(b);
            b.BackColor = Marino;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = MarinoHover;
            b.FlatAppearance.MouseDownBackColor = MarinoOscuro;
            b.Font = Fuente(10, FontStyle.Bold);
        }

        public static void BotonDorado(Button b)
        {
            Base(b);
            b.BackColor = Dorado;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 142, 28);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(150, 118, 22);
            b.Font = Fuente(10, FontStyle.Bold);
        }

        public static void BotonSecundario(Button b)
        {
            Base(b);
            b.BackColor = Color.White;
            b.ForeColor = Marino;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Borde;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 240, 247);
            b.Font = Fuente(9.5f, FontStyle.Bold);
        }

        /// <summary>Botón de la barra lateral: plano, texto a la izquierda.</summary>
        public static void BotonLateral(Button b)
        {
            Base(b);
            b.BackColor = Marino;
            b.ForeColor = Color.FromArgb(225, 231, 242);
            b.FlatAppearance.MouseOverBackColor = MarinoHover;
            b.FlatAppearance.MouseDownBackColor = MarinoOscuro;
            b.Font = Fuente(10.5f);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(22, 0, 0, 0);
            b.Dock = DockStyle.Top;
            b.Height = 40;
        }

        private static void Base(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
        }

        public static void EstilizarGrid(DataGridView g)
        {
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Color.FromArgb(232, 236, 243);
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Marino;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Fuente(9, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Marino;
            g.ColumnHeadersHeight = 34;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.DefaultCellStyle.Font = Fuente(9.5f);
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 232, 247);
            g.DefaultCellStyle.SelectionForeColor = Marino;
            g.RowTemplate.Height = 30;
            g.RowHeadersVisible = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.ScrollBars = ScrollBars.Vertical;
        }

        // ══════════════════════════════════════════════════════
        // Estilo automático para pantallas existentes (no cambia layout ni lógica)
        // ══════════════════════════════════════════════════════
        public static void Aplicar(Form f)
        {
            f.BackColor = Fondo;
            Recorrer(f);
        }

        private static readonly string[] _palabrasSecundarias =
            { "cancel", "cerrar", "salir", "volver", "limpiar", "atrás", "atras", "close", "back", "exit" };
        private static readonly string[] _palabrasPeligro =
            { "eliminar", "borrar", "baja", "delete", "löschen" };

        private static void Recorrer(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                if (c is Button b)
                {
                    string t = (b.Text ?? "").ToLowerInvariant();
                    if (_palabrasPeligro.Any(w => t.Contains(w))) { BotonPrimario(b); b.BackColor = Peligro; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 45, 35); }
                    else if (_palabrasSecundarias.Any(w => t.Contains(w))) BotonSecundario(b);
                    else BotonPrimario(b);
                }
                else if (c is TextBox tb)
                {
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is DataGridView g)
                {
                    EstilizarGridVisual(g);
                }
                else if (c is GroupBox gb)
                {
                    gb.ForeColor = Marino;
                }
                else if (c is Panel p && p.BackColor == SystemColors.Control)
                {
                    p.BackColor = Fondo;
                }

                if (c.HasChildren) Recorrer(c);
            }
        }

        /// <summary>Solo colores y fuentes: respeta columnas, edición y selección ya configuradas.</summary>
        public static void EstilizarGridVisual(DataGridView g)
        {
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.GridColor = Color.FromArgb(232, 236, 243);
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = Marino;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Fuente(9, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Marino;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 232, 247);
            g.DefaultCellStyle.SelectionForeColor = Marino;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
        }

        /// <summary>Panel blanco con borde fino y título, para agrupar campos.</summary>
        public static Panel Card(string titulo, int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Tarjeta
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Borde))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                using (var br = new SolidBrush(Dorado))
                    e.Graphics.FillRectangle(br, 0, 0, 4, 44);
            };
            p.Controls.Add(new Label
            {
                Text = titulo,
                Font = Fuente(11.5f, FontStyle.Bold),
                ForeColor = Marino,
                AutoSize = true,
                Location = new Point(18, 12)
            });
            return p;
        }
    }
}
