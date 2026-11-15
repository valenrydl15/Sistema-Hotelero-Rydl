#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// CUN14 BITÁCORA DE CAMBIOS – tabla Insumos_C (menú Maestros). Front-end de auditoría:
    ///  · Muestra el historial de cambios de los insumos (lo registran los triggers de la tabla Insumos).
    ///  · Filtros: Código, Nombre, Fecha inicial y Fecha final → APLICAR / LIMPIAR.
    ///  · ACTIVAR: la versión elegida pasa a ser la vigente (Act = 1) y el insumo vuelve a esos datos.
    ///  · A la derecha, la tabla Insumos actual (solo registros vigentes).
    /// </summary>
    public class FrmBitacoraCambios : Form
    {
        private readonly InsumoCBLL _cambios = new InsumoCBLL();
        private readonly InsumoBLL _insumos = new InsumoBLL();

        private DataGridView dgvCambios, dgvInsumos;
        private ComboBox cmbCodigo, cmbNombre;
        private DateTimePicker dtpInicio, dtpFin;
        private Label lblMensaje;
        private Button btnAplicar, btnLimpiar, btnSalir, btnActivar;

        private class OpcionInsumo
        {
            public int? Id; public string Texto;
            public override string ToString() => Texto;
        }

        public FrmBitacoraCambios()
        {
            Construir();
            AyudaGUI.Instalar(this);
            Load += (s, e) => { CargarCombos(); CargarInsumos(); Aplicar(); };
        }

        private void Construir()
        {
            Text = "Bitácora de cambios - Insumos_C";
            ClientSize = new Size(1290, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            UiMaestros.Encabezado(this, "BITÁCORA DE CAMBIOS · TABLA INSUMOS_C",
                "Historial de cada cambio en Insumos (registrado por triggers) · Act = 1 indica la versión vigente de cada insumo");

            UiMaestros.Etiqueta(this, "BITÁCORA DE CAMBIOS (Insumos_C)", 20, 76, 0, negrita: true);
            dgvCambios = UiMaestros.Grilla(this, 20, 100, 850, 380,
                ("CÓDIGO", 9), ("FECHA", 10), ("HORA", 7), ("NOMBRE", 18), ("EXIST.", 7), ("MÍN.", 6), ("DESC.", 18),
                ("ESTADO", 8), ("OPERACIÓN", 11), ("USUARIO", 10), ("ACT.", 5));
            dgvCambios.SelectionChanged += (s, e) => ActualizarBotonActivar();

            UiMaestros.Etiqueta(this, "TABLA INSUMOS (registros vigentes)", 890, 76, 0, negrita: true);
            dgvInsumos = UiMaestros.Grilla(this, 890, 100, 380, 380, ("CÓDIGO", 22), ("NOMBRE", 40), ("EXIST.", 18), ("ACTIVO", 20));

            // Filtros
            int y = 500;
            UiMaestros.Etiqueta(this, "Código:", 20, y + 4, 70, negrita: true);
            cmbCodigo = Combo(95, y, 230);
            UiMaestros.Etiqueta(this, "Nombre:", 350, y + 4, 70, negrita: true);
            cmbNombre = Combo(425, y, 240);
            UiMaestros.Etiqueta(this, "Fecha Ini.:", 20, y + 48, 75, negrita: true);
            dtpInicio = Fecha(95, y + 44);
            UiMaestros.Etiqueta(this, "Fecha Fin:", 350, y + 48, 75, negrita: true);
            dtpFin = Fecha(425, y + 44);
            UiMaestros.Etiqueta(this, "Tildá la casilla de la fecha para usarla como filtro.", 95, y + 76, 0);

            btnAplicar = UiMaestros.Boton(this, "APLICAR", 690, y - 2, 180, 36, Tema.BotonPrimario, Aplicar);
            btnLimpiar = UiMaestros.Boton(this, "LIMPIAR", 690, y + 42, 180, 36, Tema.BotonSecundario, Limpiar);
            btnActivar = UiMaestros.Boton(this, "ACTIVAR", 890, y - 2, 180, 36, Tema.BotonDorado, Activar);
            btnSalir = UiMaestros.Boton(this, "SALIR", 890, y + 42, 180, 36, Tema.BotonSecundario, Close);
            var tips = new ToolTip();
            tips.SetToolTip(btnActivar, "Activa la versión seleccionada: pasa a Act = 1 y el insumo vuelve a esos datos.");
            tips.SetToolTip(btnLimpiar, "Borra los filtros y vuelve a mostrar toda la bitácora.");

            lblMensaje = UiMaestros.CuadroMensaje(this, 1090, 496, 180, 166);
        }

        private ComboBox Combo(int x, int y)
        {
            var c = new ComboBox { Location = new Point(x, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = Tema.Fuente(10) };
            Controls.Add(c);
            return c;
        }

        private ComboBox Combo(int x, int y, int w) { var c = Combo(x, y); c.Width = w; return c; }

        private DateTimePicker Fecha(int x, int y)
        {
            var d = new DateTimePicker { Location = new Point(x, y), Size = new Size(230, 28), Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Tema.Fuente(10) };
            Controls.Add(d);
            return d;
        }

        private void CargarCombos()
        {
            cmbCodigo.Items.Clear();
            cmbCodigo.Items.Add(new OpcionInsumo { Id = null, Texto = "(Todos)" });
            var ins = _insumos.ObtenerInsumos(incluirBajas: true);
            if (ins.Exito)
                foreach (var i in ins.Dato.OrderBy(x => x.IdInsumo))
                    cmbCodigo.Items.Add(new OpcionInsumo { Id = i.IdInsumo, Texto = $"{i.Codigo}  {i.Nombre}" });
            cmbCodigo.SelectedIndex = 0;

            cmbNombre.Items.Clear();
            cmbNombre.Items.Add("(Todos)");
            var nombres = _cambios.ObtenerNombres();
            if (nombres.Exito) foreach (var n in nombres.Dato) cmbNombre.Items.Add(n);
            cmbNombre.SelectedIndex = 0;
        }

        private void CargarInsumos()
        {
            var r = _insumos.ObtenerInsumos(incluirBajas: true);
            dgvInsumos.Rows.Clear();
            if (!r.Exito) return;
            foreach (var i in r.Dato.OrderBy(x => x.IdInsumo))
            {
                int n = dgvInsumos.Rows.Add(i.Codigo, i.Nombre, i.StockActual.ToString("0.##"), i.Activo ? "1" : "0 (baja)");
                if (!i.Activo) dgvInsumos.Rows[n].DefaultCellStyle.ForeColor = Tema.TextoSuave;
            }
        }

        private FiltroCambiosBE LeerFiltro() => new FiltroCambiosBE
        {
            IdInsumo = (cmbCodigo.SelectedItem as OpcionInsumo)?.Id,
            Nombre = cmbNombre.SelectedIndex > 0 ? cmbNombre.SelectedItem.ToString() : null,
            FechaInicio = dtpInicio.Checked ? dtpInicio.Value.Date : (DateTime?)null,
            FechaFin = dtpFin.Checked ? dtpFin.Value.Date : (DateTime?)null
        };

        private void Aplicar()
        {
            var r = _cambios.Consultar(LeerFiltro());
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            dgvCambios.Rows.Clear();
            foreach (var c in r.Dato)
            {
                int n = dgvCambios.Rows.Add(c.Codigo, c.Fecha.ToString("dd/MM/yyyy"), c.Hora.ToString(@"hh\:mm"), c.Nombre,
                                            c.StockActual.ToString("0.##"), c.StockMinimo.ToString("0.##"), c.Descripcion,
                                            c.Activo ? "Activo" : "Baja", c.Operacion, c.Usuario, c.Act ? "1" : "0");
                var row = dgvCambios.Rows[n];
                row.Tag = c;
                if (c.Act)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(229, 244, 228);
                    row.DefaultCellStyle.Font = Tema.Fuente(9.5f, FontStyle.Bold);
                }
            }
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
            ActualizarBotonActivar();
        }

        private void Limpiar()
        {
            cmbCodigo.SelectedIndex = 0;
            cmbNombre.SelectedIndex = 0;
            dtpInicio.Checked = dtpFin.Checked = false;
            dtpInicio.Value = dtpFin.Value = DateTime.Today;
            Aplicar();
        }

        private InsumoCBE Seleccionado() => dgvCambios.CurrentRow?.Tag as InsumoCBE;

        private void ActualizarBotonActivar()
        {
            var c = Seleccionado();
            btnActivar.Enabled = c != null && !c.Act;
        }

        private void Activar()
        {
            var c = Seleccionado();
            if (c == null) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un registro de la bitácora.", false); return; }
            if (c.Act) { UiMaestros.Mostrar(lblMensaje, "Ese registro ya es la versión vigente (Act = 1).", false); return; }
            if (MessageBox.Show(
                    $"¿Activar la versión del {c.Fecha:dd/MM/yyyy} {c.Hora:hh\\:mm} de {c.Codigo}?\n\n" +
                    $"El insumo vuelve a: {c.Nombre} · stock {c.StockActual:0.##} · mínimo {c.StockMinimo:0.##} · {(c.Activo ? "activo" : "dado de baja")}.\n" +
                    "Esa versión pasa a Act = 1 y la actual a Act = 0.",
                    "Confirmar activación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _cambios.Activar(c.IdCambio);
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            CargarInsumos();
            Aplicar();
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
        }
    }
}
