using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// Reporte de facturas de Recepción (PN1): lista las facturas emitidas en un período
    /// y permite exportar una factura o el listado completo a PDF.
    /// </summary>
    public class FrmReporteFacturas : Form
    {
        private readonly ReporteBLL _bll = new ReporteBLL();
        private DateTimePicker dtpDesde, dtpHasta;
        private CheckBox chkTodas;
        private Button btnBuscar, btnExportarFactura, btnExportarListado, btnCerrar;
        private DataGridView dgv;
        private Label lblMensaje;

        /// <param name="nroFacturaInicial">Si viene, queda seleccionada al abrir.</param>
        public FrmReporteFacturas(string nroFacturaInicial = null)
        {
            Construir();
            Load += (s, e) => { Buscar(); Seleccionar(nroFacturaInicial); };
        }

        private void Construir()
        {
            Text = "Hotel Rydl — Reporte de facturas";
            ClientSize = new Size(1000, 600);
            MinimumSize = new Size(900, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Marino };
            header.Controls.Add(new Label
            {
                Text = "Reporte de facturas", ForeColor = Color.White, AutoSize = true,
                Font = new Font("Georgia", 18f), Location = new Point(24, 14)
            });
            header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Tema.Dorado });

            lblMensaje = new Label
            {
                Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0), Font = Tema.Fuente(9.5f, FontStyle.Bold),
                ForeColor = Tema.TextoSuave, BackColor = Color.White
            };

            var filtros = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Fondo };
            filtros.Controls.Add(Etiqueta("Desde", 24, 10));
            dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(24, 30), Width = 120, Value = DateTime.Today.AddDays(-30) };
            filtros.Controls.Add(Etiqueta("Hasta", 160, 10));
            dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(160, 30), Width = 120, Value = DateTime.Today };
            chkTodas = new CheckBox { Text = "Todas las fechas", AutoSize = true, Location = new Point(300, 33), Checked = true };
            chkTodas.CheckedChanged += (s, e) => { dtpDesde.Enabled = dtpHasta.Enabled = !chkTodas.Checked; };
            btnBuscar = new Button { Text = "Buscar", Location = new Point(450, 24), Size = new Size(110, 34) };
            Tema.BotonPrimario(btnBuscar);
            btnBuscar.Click += (s, e) => Buscar();
            dtpDesde.Enabled = dtpHasta.Enabled = false;
            filtros.Controls.AddRange(new Control[] { dtpDesde, dtpHasta, chkTodas, btnBuscar });

            var pie = new Panel { Dock = DockStyle.Bottom, Height = 58, Width = 1000, BackColor = Tema.Fondo };
            btnExportarFactura = new Button { Text = "Exportar factura (PDF)", Location = new Point(24, 10), Size = new Size(220, 38), Enabled = false };
            Tema.BotonDorado(btnExportarFactura);
            btnExportarFactura.Click += (s, e) => ExportarFactura();
            btnExportarListado = new Button { Text = "Exportar listado (PDF)", Location = new Point(256, 10), Size = new Size(210, 38) };
            Tema.BotonSecundario(btnExportarListado);
            btnExportarListado.Click += (s, e) => ExportarListado();
            btnCerrar = new Button { Text = "Cerrar", Size = new Size(110, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(866, 10) };
            Tema.BotonSecundario(btnCerrar);
            btnCerrar.Click += (s, e) => Close();
            pie.Controls.AddRange(new Control[] { btnExportarFactura, btnExportarListado, btnCerrar });

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Tema.EstilizarGrid(dgv);
            foreach (var (t, w) in new[] { ("Factura", 11f), ("Emisión", 12f), ("Huésped", 24f), ("Hab.", 7f), ("Medio de pago", 14f), ("Noches", 8f), ("Total", 14f), ("Estado", 10f) })
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = t, FillWeight = w, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgv.SelectionChanged += (s, e) => btnExportarFactura.Enabled = dgv.CurrentRow?.Tag is FacturaReporteBE;

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 0), BackColor = Tema.Fondo };
            cuerpo.Controls.Add(dgv);

            Controls.Add(cuerpo);
            Controls.Add(pie);
            Controls.Add(lblMensaje);
            Controls.Add(filtros);
            Controls.Add(header);
        }

        private static Label Etiqueta(string texto, int x, int y)
            => new Label { Text = texto, AutoSize = true, ForeColor = Tema.TextoSuave, Font = Tema.Fuente(8.5f), Location = new Point(x, y) };

        private DateTime? Desde => chkTodas.Checked ? (DateTime?)null : dtpDesde.Value.Date;
        private DateTime? Hasta => chkTodas.Checked ? (DateTime?)null : dtpHasta.Value.Date;

        private void Msg(string texto, bool? ok)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok == null ? Tema.TextoSuave : (ok.Value ? Tema.Exito : Tema.Peligro);
        }

        private void Buscar()
        {
            dgv.Rows.Clear();
            var r = _bll.ObtenerFacturas(Desde, Hasta);
            if (!r.Exito) { Msg("✖ " + r.Mensaje, false); return; }
            foreach (var f in r.Dato)
            {
                int i = dgv.Rows.Add(f.NroFactura, f.FechaEmision.ToString("dd/MM/yyyy"), f.Huesped, f.NumeroHabitacion,
                    string.IsNullOrWhiteSpace(f.MetodoPago) ? "-" : f.MetodoPago, f.Noches, "$ " + f.MontoTotal.ToString("N2"), f.Estado);
                dgv.Rows[i].Tag = f;
            }
            dgv.ClearSelection();
            Msg(r.Dato.Count + (r.Dato.Count == 1 ? " factura." : " facturas."), null);
        }

        private void Seleccionar(string nro)
        {
            if (string.IsNullOrEmpty(nro)) return;
            foreach (DataGridViewRow row in dgv.Rows)
                if (row.Tag is FacturaReporteBE f && f.NroFactura == nro)
                {
                    row.Selected = true;
                    dgv.CurrentCell = row.Cells[0];
                    return;
                }
        }

        private void ExportarFactura()
        {
            var f = dgv.CurrentRow?.Tag as FacturaReporteBE;
            if (f == null) return;
            string ruta = ReportesUI.PedirRuta("Factura " + f.NroFactura + ".pdf");
            if (ruta == null) return;
            var r = _bll.ExportarFactura(f.NroFactura, ruta);
            Msg((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ExportarListado()
        {
            string ruta = ReportesUI.PedirRuta("Reporte de facturas " + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            if (ruta == null) return;
            var r = _bll.ExportarListadoFacturas(Desde, Hasta, ruta);
            Msg((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
