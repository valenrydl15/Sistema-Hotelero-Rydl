#nullable disable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// MAESTRO DE INSUMOS (menú Maestros): alta, modificación y baja lógica de insumos (PRODUCTOS del enunciado).
    /// Cada cambio queda registrado automáticamente por los TRIGGERS en la bitácora de cambios (Insumos_C, CUN14).
    /// </summary>
    public class FrmMaestroInsumos : Form, SERVICIOS_HRS_480.Idioma.IObserver
    {
        private enum Modo { Consulta, Anadir, Modificar }

        private readonly InsumoBLL _insumos = new InsumoBLL();
        private Modo _modo = Modo.Consulta;
        private DataGridView dgvInsumos;
        private CheckBox chkBajas;
        private TextBox txtNombre, txtDescripcion, txtCategoria, txtUnidad;
        private NumericUpDown numStock, numMinimo;
        private Label lblMensaje, lblCodigo;
        private Button btnAnadir, btnModificar, btnEliminar, btnReactivar, btnAplicar, btnCancelar, btnSalir;

        public FrmMaestroInsumos()
        {
            Construir();
            AyudaGUI.Instalar(this);
            Load += (s, e) => Cargar();

            // Traducción (patrón Observer: se actualiza al cambiar el idioma).
            // Se aplica al cargar, cuando ya están creados todos los controles.
            SERVICIOS_HRS_480.Idioma.GestorTraducciones.Instancia.AgregarForm(this);
            FormClosed += (s, e) => SERVICIOS_HRS_480.Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            Load += (s, e) => ActualizarTraducciones();
        }

        public void ActualizarTraducciones()
        {
            new Traductor().ActualizarTraducciones("FrmMaestroInsumos", this);
        }

        private void Construir()
        {
            Text = "Maestro de Insumos";
            ClientSize = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            UiMaestros.Encabezado(this, "MAESTRO DE INSUMOS", "Alta, modificación y baja lógica · cada cambio queda en la bitácora de cambios (Insumos_C)");

            dgvInsumos = UiMaestros.Grilla(this, 20, 82, 870, 290,
                ("CÓDIGO", 10), ("NOMBRE", 22), ("CATEGORÍA", 13), ("UNIDAD", 10), ("STOCK", 9), ("MÍNIMO", 9), ("ESTADO", 10), ("ACTIVO", 8));
            dgvInsumos.SelectionChanged += (s, e) => { if (_modo == Modo.Consulta) CargarCampos(Seleccionado()); };
            chkBajas = new CheckBox { Text = "Mostrar también los dados de baja", Location = new Point(20, 378), AutoSize = true, ForeColor = Tema.TextoSuave };
            chkBajas.CheckedChanged += (s, e) => Cargar();
            Controls.Add(chkBajas);

            btnAnadir = UiMaestros.Boton(this, "Añadir", 910, 82, 170, 38, Tema.BotonPrimario, () => Entrar(Modo.Anadir));
            btnModificar = UiMaestros.Boton(this, "Modificar", 910, 130, 170, 38, Tema.BotonPrimario, () => Entrar(Modo.Modificar));
            btnEliminar = UiMaestros.Boton(this, "Eliminar", 910, 178, 170, 38, UiMaestros.Peligro, Eliminar);
            btnReactivar = UiMaestros.Boton(this, "Reactivar", 910, 226, 170, 38, Tema.BotonSecundario, Reactivar);

            int y = 412;
            lblCodigo = UiMaestros.Etiqueta(this, "Código: —", 20, y - 4, 300, negrita: true);
            txtNombre = Campo("Nombre:", 20, y += 26, 150);
            txtCategoria = Campo("Categoría:", 20, y += 34, 50);
            txtUnidad = Campo("Unidad:", 20, y += 34, 30);
            txtDescripcion = Campo("Descripción:", 20, y += 34, 300);
            UiMaestros.Etiqueta(this, "Stock actual:", 20, y += 36, 110, negrita: true);
            numStock = Numero(134, y - 2);
            UiMaestros.Etiqueta(this, "Stock mínimo:", 250, y, 110, negrita: true);
            numMinimo = Numero(364, y - 2);

            lblMensaje = UiMaestros.CuadroMensaje(this, 520, 412, 370, 190);
            btnAplicar = UiMaestros.Boton(this, "Aplicar", 910, 412, 170, 38, Tema.BotonDorado, Aplicar);
            btnCancelar = UiMaestros.Boton(this, "Cancelar", 910, 460, 170, 38, Tema.BotonSecundario, () => { CambiarModo(Modo.Consulta); CargarCampos(Seleccionado()); });
            btnSalir = UiMaestros.Boton(this, "Salir", 910, 508, 170, 38, Tema.BotonSecundario, Close);
            CambiarModo(Modo.Consulta);
        }

        private TextBox Campo(string etiqueta, int x, int y, int max)
        {
            UiMaestros.Etiqueta(this, etiqueta, x, y + 2, 110, negrita: true);
            return UiMaestros.Caja(this, x + 114, y, 360, max);
        }

        private NumericUpDown Numero(int x, int y)
        {
            var n = new NumericUpDown { Location = new Point(x, y), Size = new Size(100, 26), DecimalPlaces = 2, Maximum = 9999999, Minimum = 0, Font = Tema.Fuente(10), ThousandsSeparator = true };
            Controls.Add(n);
            return n;
        }

        private InsumoBE Seleccionado() => dgvInsumos.CurrentRow?.Tag as InsumoBE;

        private void Cargar()
        {
            int? sel = Seleccionado()?.IdInsumo;
            var r = _insumos.ObtenerInsumos(chkBajas.Checked);
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            dgvInsumos.Rows.Clear();
            foreach (var i in r.Dato)
            {
                int n = dgvInsumos.Rows.Add(i.Codigo, i.Nombre, i.Categoria, i.UnidadMedida, i.StockActual.ToString("0.##"), i.StockMinimo.ToString("0.##"),
                                            i.Faltante ? "Crítico" : "Normal", i.Activo ? "Sí" : "No (baja)");
                var row = dgvInsumos.Rows[n];
                row.Tag = i;
                if (!i.Activo) row.DefaultCellStyle.ForeColor = Tema.TextoSuave;
                else if (i.Faltante) row.Cells[6].Style.ForeColor = Tema.Peligro;
                if (i.IdInsumo == sel) { row.Selected = true; dgvInsumos.CurrentCell = row.Cells[0]; }
            }
            CargarCampos(Seleccionado());
        }

        private void CargarCampos(InsumoBE i)
        {
            lblCodigo.Text = "Código: " + (i?.Codigo ?? "—");
            txtNombre.Text = i?.Nombre ?? ""; txtCategoria.Text = i?.Categoria ?? ""; txtUnidad.Text = i?.UnidadMedida ?? "";
            txtDescripcion.Text = i?.Descripcion ?? "";
            numStock.Value = Math.Min(numStock.Maximum, i?.StockActual ?? 0);
            numMinimo.Value = Math.Min(numMinimo.Maximum, i?.StockMinimo ?? 0);
            btnReactivar.Enabled = _modo == Modo.Consulta && i != null && !i.Activo;
            btnModificar.Enabled = btnEliminar.Enabled = _modo == Modo.Consulta && (i?.Activo ?? false);
        }

        private void Entrar(Modo modo)
        {
            var sel = Seleccionado();
            if (modo == Modo.Modificar && (sel == null || !sel.Activo)) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un insumo activo.", false); return; }
            CambiarModo(modo);
            if (modo == Modo.Anadir)
            {
                CargarCampos(null);
                lblCodigo.Text = "Código: (nuevo)";
                txtCategoria.Text = "General";
                UiMaestros.Mostrar(lblMensaje, "Modo Añadir: completá los datos y presioná Aplicar.", true);
            }
            else
                UiMaestros.Mostrar(lblMensaje, $"Modo Modificar: editá los datos de {sel.Codigo} {sel.Nombre} y presioná Aplicar. El cambio quedará en la bitácora de cambios.", true);
            txtNombre.Focus();
        }

        private void CambiarModo(Modo modo)
        {
            _modo = modo;
            bool consulta = modo == Modo.Consulta;
            foreach (var t in new[] { txtNombre, txtCategoria, txtUnidad, txtDescripcion })
            { t.ReadOnly = consulta; t.BackColor = consulta ? Color.FromArgb(247, 248, 251) : Color.White; }
            numStock.Enabled = numMinimo.Enabled = !consulta;
            btnAplicar.Enabled = btnCancelar.Enabled = !consulta;
            btnAnadir.Enabled = btnSalir.Enabled = chkBajas.Enabled = dgvInsumos.Enabled = consulta;
            btnModificar.Enabled = btnEliminar.Enabled = btnReactivar.Enabled = consulta;
            if (consulta) { UiMaestros.Mostrar(lblMensaje, "", true); CargarCampos(Seleccionado()); }
        }

        private InsumoBE LeerCampos() => new InsumoBE
        {
            IdInsumo = _modo == Modo.Modificar ? Seleccionado()?.IdInsumo ?? 0 : 0,
            Nombre = txtNombre.Text, Categoria = txtCategoria.Text, UnidadMedida = txtUnidad.Text, Descripcion = txtDescripcion.Text,
            StockActual = numStock.Value, StockMinimo = numMinimo.Value
        };

        private void Aplicar()
        {
            var r = _modo == Modo.Anadir ? _insumos.Alta(LeerCampos()) : _insumos.Modificar(LeerCampos());
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            CambiarModo(Modo.Consulta);
            Cargar();
            Seleccionar(r.Dato.IdInsumo);
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
        }

        private void Eliminar()
        {
            var sel = Seleccionado();
            if (sel == null || !sel.Activo) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un insumo activo.", false); return; }
            if (MessageBox.Show($"¿Dar de baja el insumo {sel.Codigo} {sel.Nombre}?\n\nLa baja es lógica: queda en la bitácora de cambios y se puede reactivar.",
                    "Confirmar baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _insumos.Eliminar(sel.IdInsumo);
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, r.Exito);
            if (r.Exito) { Cargar(); UiMaestros.Mostrar(lblMensaje, r.Mensaje, true); }
        }

        private void Reactivar()
        {
            var sel = Seleccionado();
            if (sel == null || sel.Activo) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un insumo dado de baja.", false); return; }
            var r = _insumos.Reactivar(sel.IdInsumo);
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, r.Exito);
            if (r.Exito) { Cargar(); Seleccionar(sel.IdInsumo); UiMaestros.Mostrar(lblMensaje, r.Mensaje, true); }
        }

        private void Seleccionar(int idInsumo)
        {
            var fila = dgvInsumos.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => (x.Tag as InsumoBE)?.IdInsumo == idInsumo);
            if (fila != null) { dgvInsumos.ClearSelection(); fila.Selected = true; dgvInsumos.CurrentCell = fila.Cells[0]; }
        }
    }
}
