#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// A02 REPORTE INTELIGENTE PREDICTIVO de ocupación (menú Reportes).
    /// Cuatro secciones: Panorama general (gráfico + lectura de resultados), Detalle por día,
    /// Por tipo de habitación e Insumos a reponer. ACTUALIZAR vuelve a calcular; VOLVER cierra.
    /// </summary>
    public class FrmReportePredictivo : Form
    {
        private static readonly CultureInfo AR = new CultureInfo("es-AR");
        private readonly ReportePredictivoBLL _bll = new ReportePredictivoBLL();
        private ReportePredictivoBE _rep;

        private Label lblPeriodo, lblMensaje, lblResumen, lblLectura;
        private Label[] kpiValor = new Label[4];
        private Panel pnlGrafico;
        private TabControl tabs;
        private DataGridView dgvDias, dgvTipos, dgvInsumos;

        public FrmReportePredictivo()
        {
            Construir();
            AyudaGUI.Instalar(this);
            Load += (s, e) => Generar();
        }

        public static Color ColorNivel(string nivel) => nivel switch
        {
            "Completo" => Tema.Peligro,
            "Alta" => Tema.Dorado,
            "Media" => Tema.Exito,
            _ => Color.FromArgb(90, 120, 170)
        };

        private void Construir()
        {
            Text = "Reporte predictivo de ocupación";
            ClientSize = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            var enc = UiMaestros.Encabezado(this, "REPORTE PREDICTIVO DE OCUPACIÓN", "Calculando…");
            lblPeriodo = enc.Controls.OfType<Label>().Last();

            string[] titulos = { "NOCHES VENDIDAS (28 DÍAS)", "OCUPACIÓN HISTÓRICA", "OCUPACIÓN PREVISTA (7 DÍAS)", "INGRESO ESTIMADO (7 DÍAS)" };
            for (int i = 0; i < 4; i++)
            {
                var card = new Panel { Location = new Point(20 + i * 290, 78), Size = new Size(270, 70), BackColor = Tema.Marino };
                card.Controls.Add(new Label { Text = titulos[i], ForeColor = Color.FromArgb(190, 205, 230), Font = Tema.Fuente(8.5f, FontStyle.Bold), AutoSize = false, Size = new Size(250, 20), Location = new Point(10, 8), TextAlign = ContentAlignment.MiddleCenter });
                kpiValor[i] = new Label { Text = "—", ForeColor = Tema.Dorado, Font = Tema.Fuente(16f, FontStyle.Bold), AutoSize = false, Size = new Size(250, 34), Location = new Point(10, 30), TextAlign = ContentAlignment.MiddleCenter };
                card.Controls.Add(kpiValor[i]);
                Controls.Add(card);
            }

            tabs = new TabControl { Location = new Point(20, 162), Size = new Size(1140, 470), Font = Tema.Fuente(9.5f, FontStyle.Bold) };
            Controls.Add(tabs);

            // 1) Panorama general
            var t1 = new TabPage("Panorama general") { BackColor = Color.White };
            lblResumen = new Label { Location = new Point(14, 10), Size = new Size(1100, 22), Font = Tema.Fuente(10f, FontStyle.Bold), ForeColor = Tema.Marino };
            pnlGrafico = new Panel { Location = new Point(14, 38), Size = new Size(640, 390), BackColor = Color.White };
            pnlGrafico.Paint += DibujarGrafico;
            var marcoLect = new Panel { Location = new Point(670, 38), Size = new Size(445, 390), BackColor = Tema.Fondo };
            marcoLect.Controls.Add(new Label { Text = "LECTURA DE LOS RESULTADOS", Font = Tema.Fuente(9.5f, FontStyle.Bold), ForeColor = Tema.Marino, AutoSize = true, Location = new Point(12, 10) });
            lblLectura = new Label { Location = new Point(12, 36), Size = new Size(420, 345), Font = Tema.Fuente(9.5f, FontStyle.Regular), ForeColor = Tema.Texto };
            marcoLect.Controls.Add(lblLectura);
            t1.Controls.AddRange(new Control[] { lblResumen, pnlGrafico, marcoLect });
            tabs.TabPages.Add(t1);

            // 2) Detalle por día
            var t2 = new TabPage("Detalle por día") { BackColor = Color.White };
            dgvDias = UiMaestros.Grilla(t2, 8, 8, 1115, 420, ("FECHA", 9), ("DÍA", 9), ("CONFIRM.", 8), ("PROYECCIÓN", 9), ("ESTIMADAS", 9), ("CAPACIDAD", 8),
                                        ("OCUPACIÓN", 8), ("NIVEL", 8), ("INGRESO ESTIMADO", 11), ("RECOMENDACIÓN", 30));
            tabs.TabPages.Add(t2);

            // 3) Por tipo de habitación
            var t3 = new TabPage("Por tipo de habitación") { BackColor = Color.White };
            dgvTipos = UiMaestros.Grilla(t3, 8, 8, 1115, 420, ("TIPO", 10), ("HABIT.", 6), ("PRECIO/NOCHE", 9), ("NOCHES 28 D", 8), ("PROM./DÍA", 7), ("OCUP. HIST.", 8),
                                         ("NOCHES EST. 7 D", 9), ("OCUP. PREV.", 8), ("TENDENCIA", 8), ("INGRESO EST. 7 D", 11), ("RECOMENDACIÓN", 26));
            tabs.TabPages.Add(t3);

            // 4) Insumos a reponer
            var t4 = new TabPage("Insumos a reponer") { BackColor = Color.White };
            t4.Controls.Add(new Label { Text = "Insumos con stock crítico (stock ≤ mínimo). La prioridad y la cantidad sugerida suben con la ocupación prevista: más huéspedes, más consumo de blancos, amenities y limpieza.",
                                        Location = new Point(10, 8), Size = new Size(1110, 36), ForeColor = Tema.TextoSuave, Font = Tema.Fuente(9f) });
            dgvInsumos = UiMaestros.Grilla(t4, 8, 48, 1115, 380, ("CÓDIGO", 8), ("INSUMO", 20), ("CATEGORÍA", 12), ("UNIDAD", 9), ("STOCK", 8), ("MÍNIMO", 8), ("REPONER (SUGERIDO)", 12), ("PRIORIDAD", 10));
            tabs.TabPages.Add(t4);

            foreach (var g in new[] { dgvDias, dgvTipos, dgvInsumos }) g.ScrollBars = ScrollBars.Vertical;

            lblMensaje = UiMaestros.CuadroMensaje(this, 20, 644, 760, 64);
            UiMaestros.Boton(this, "Actualizar", 800, 652, 170, 44, Tema.BotonDorado, Generar);
            UiMaestros.Boton(this, "Volver", 990, 652, 170, 44, Tema.BotonSecundario, Close);
        }

        private void Generar()
        {
            Cursor = Cursors.WaitCursor;
            var r = _bll.Generar();
            Cursor = Cursors.Default;
            if (!r.Exito) { _rep = null; UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); pnlGrafico.Invalidate(); return; }
            _rep = r.Dato;
            var p = _rep;
            lblPeriodo.Text = $"Estadías del {p.HistoricoDesde:dd/MM/yyyy} al {p.HistoricoHasta:dd/MM/yyyy} ({ReportePredictivoBE.DIAS_HISTORICOS} días completos) · " +
                              $"Estimación: {p.ProyeccionDesde:dd/MM} al {p.ProyeccionHasta:dd/MM} ({ReportePredictivoBE.DIAS_PROYECCION} días)";
            kpiValor[0].Text = $"{p.NochesHistoricas} noches";
            kpiValor[1].Text = $"{p.OcupacionHistorica:0.0} %";
            kpiValor[2].Text = $"{p.OcupacionPrevista:0.0} %";
            kpiValor[3].Text = p.IngresoEstimado.ToString("C0", AR);
            lblResumen.Text = $"{p.CapacidadTotal} habitaciones activas  |  {p.Tipos.Count} tipos de habitación  |  {p.Insumos.Count} insumo/s crítico/s  |  Ocupación estimada por día (próximos {ReportePredictivoBE.DIAS_PROYECCION} días)";
            lblLectura.Text = string.Join(Environment.NewLine + Environment.NewLine, p.Lectura.Select(l => "• " + l));

            dgvDias.Rows.Clear();
            foreach (var d in p.Dias)
            {
                int n = dgvDias.Rows.Add(d.Fecha.ToString("dd/MM/yyyy"), AR.TextInfo.ToTitleCase(d.Fecha.ToString("dddd", AR)), d.Confirmadas, d.Proyeccion.ToString("0.0"),
                                         d.Estimadas.ToString("0.0"), d.Capacidad, $"{d.Ocupacion:0.0} %", d.Nivel, d.IngresoEstimado.ToString("C2", AR), d.Recomendacion);
                dgvDias.Rows[n].Cells[7].Style.ForeColor = ColorNivel(d.Nivel);
                dgvDias.Rows[n].Cells[7].Style.Font = Tema.Fuente(9f, FontStyle.Bold);
            }
            dgvTipos.Rows.Clear();
            foreach (var t in p.Tipos)
            {
                int n = dgvTipos.Rows.Add(t.Tipo, t.Habitaciones, t.PrecioPorNoche.ToString("C0", AR), t.NochesHistoricas, t.PromedioDiario.ToString("0.00"),
                                          $"{t.OcupacionHistorica:0.0} %", t.NochesEstimadas.ToString("0.0"), $"{t.OcupacionPrevista:0.0} %", t.Tendencia,
                                          t.IngresoEstimado.ToString("C2", AR), t.Recomendacion);
                dgvTipos.Rows[n].Cells[8].Style.ForeColor = t.Tendencia == "Sube" ? Tema.Exito : t.Tendencia == "Baja" ? Tema.Peligro : Tema.Texto;
            }
            dgvInsumos.Rows.Clear();
            foreach (var i in p.Insumos)
            {
                int n = dgvInsumos.Rows.Add(i.Codigo, i.Nombre, i.Categoria, i.Unidad, i.StockActual.ToString("0.##"), i.StockMinimo.ToString("0.##"), i.ReponerSugerido.ToString("0.##"), i.Prioridad);
                dgvInsumos.Rows[n].Cells[7].Style.ForeColor = i.Prioridad == "Urgente" ? Tema.Peligro : i.Prioridad == "Alta" ? Tema.Dorado : Tema.Exito;
                dgvInsumos.Rows[n].Cells[7].Style.Font = Tema.Fuente(9f, FontStyle.Bold);
            }
            pnlGrafico.Invalidate();
            UiMaestros.Mostrar(lblMensaje, r.Mensaje + (p.SinHistorial ? " Sin estadías en los últimos 28 días: se usan solo las reservas confirmadas." : ""), true);
        }

        /// <summary>Barras horizontales: ocupación estimada de cada día (color según el nivel) y marca de las reservas confirmadas.</summary>
        private void DibujarGrafico(object sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Tema.Borde)) g.DrawRectangle(pen, 0, 0, pnlGrafico.Width - 1, pnlGrafico.Height - 1);
            if (_rep == null) return;
            int x0 = 120, ancho = pnlGrafico.Width - x0 - 130, y = 22, alto = 34, paso = 48;
            using var fTxt = Tema.Fuente(9f); using var fBold = Tema.Fuente(9f, FontStyle.Bold);
            using var gris = new SolidBrush(Tema.Fondo); using var texto = new SolidBrush(Tema.Texto); using var suave = new SolidBrush(Tema.TextoSuave);
            // guías 0-100 %
            using (var guia = new Pen(Color.FromArgb(230, 233, 240)) { DashStyle = DashStyle.Dash })
                foreach (int pct in new[] { 0, 25, 50, 75, 100 })
                {
                    float gx = x0 + ancho * pct / 100f;
                    g.DrawLine(guia, gx, 12, gx, y + paso * _rep.Dias.Count - 8);
                    g.DrawString(pct + " %", fTxt, suave, gx - 12, y + paso * _rep.Dias.Count - 6);
                }
            foreach (var d in _rep.Dias)
            {
                g.DrawString(AR.TextInfo.ToTitleCase(d.Fecha.ToString("ddd dd/MM", AR)), fBold, texto, 10, y + 9);
                g.FillRectangle(gris, x0, y, ancho, alto);
                float w = (float)(ancho * Math.Min(100m, d.Ocupacion) / 100m);
                using (var br = new SolidBrush(ColorNivel(d.Nivel))) g.FillRectangle(br, x0, y, w, alto);
                if (d.Capacidad > 0)   // marca de las reservas ya confirmadas
                {
                    float cx = x0 + ancho * d.Confirmadas / (float)d.Capacidad;
                    using var pc = new Pen(Tema.Marino, 2) { DashStyle = DashStyle.Dot };
                    g.DrawLine(pc, cx, y - 3, cx, y + alto + 3);
                }
                g.DrawString($"{d.Ocupacion:0} % · {d.Nivel}", fBold, texto, x0 + ancho + 8, y + 9);
                y += paso;
            }
            g.DrawString("Barra: ocupación estimada    ┊ línea punteada: reservas ya confirmadas", fTxt, suave, 10, pnlGrafico.Height - 22);
        }
    }
}
