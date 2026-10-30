#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// Pantalla del PN2 "Gestión de reposición de insumos" (la "GUI" de los DSS CUN06 a CUN12).
    ///   Encargado de Compras : Faltantes y solicitud (CUN06 + CUN07) · Cotizaciones y Pre-órdenes (CUN08)
    ///   Gerente de Compras   : Gerencia (CUN09 + CUN10 + CUN11)
    ///   Encargado de Depósito: Recepción (CUN12)
    /// Todos: Órdenes (consulta y PDF) y Avisos. Solo habla con las BLL, que vuelven a validar el rol.
    /// </summary>
    public class FrmReposicion : Form
    {
        private readonly StockBLL _stock = new StockBLL();
        private readonly SolicitudCotizacionBLL _solicitudes = new SolicitudCotizacionBLL();
        private readonly PreOrdenBLL _preordenes = new PreOrdenBLL();
        private readonly ProveedorBLL _proveedores = new ProveedorBLL();
        private readonly OrdenCompraBLL _ordenes = new OrdenCompraBLL();
        private readonly RecepcionBLL _recepciones = new RecepcionBLL();
        private readonly NotificacionBLL _avisos = new NotificacionBLL();
        private readonly ReporteBLL _reportes = new ReporteBLL();

        private Panel pnlHeader, pnlSecciones;
        private Label lblTitulo, lblUsuario, lblMensaje;
        private readonly Dictionary<string, Panel> _paneles = new Dictionary<string, Panel>();
        private readonly Dictionary<string, Button> _botones = new Dictionary<string, Button>();

        // CUN06 / CUN07
        private DataGridView dgvFaltantes;
        private CheckBox chkTodoElStock;
        private ComboBox cmbProveedor;
        private Label lblEstadoProv, lblResumenSolicitud;
        private TextBox txtPreEmpresa, txtPreContacto, txtPreTelefono, txtPreEmail, txtPreDireccion;
        private Button btnPreRegistrar, btnGenerarSolicitud;
        private Panel pnlSolicitud;

        // CUN08 (cotizaciones recibidas)
        private DataGridView dgvSolicitudes, dgvItemsCotizacion;
        private Label lblSolicitudSel;
        private TextBox txtCotCuit, txtCotBanco, txtCotCbu, txtCotTitular;
        private Button btnRegistrarCotizacion;

        // CUN08 (comparación y pre-órdenes)
        private DataGridView dgvOfertas;
        private Label lblResumenPreOrden;
        private Button btnGenerarPreOrdenes;
        private bool _ajustandoOfertas;

        // CUN09 / CUN10 / CUN11
        private DataGridView dgvBandeja;
        private Label lblDetalleOrden, lblEstadoProvOrden;
        private TextBox txtMotivoCancelacion, txtRegCuit, txtRegBanco, txtRegCbu, txtRegTitular, txtDireccion;
        private Button btnGenerarOrden, btnCancelarPreOrden, btnRegistrarProveedor, btnPagar;
        private Panel pnlRegistroProveedor;

        // CUN12
        private DataGridView dgvARecibir, dgvItemsRecepcion;
        private Label lblDetalleRecepcion;
        private TextBox txtNroFactura, txtObsRecepcion;
        private NumericUpDown numTotalFactura;
        private Button btnRegistrarRecepcion;

        // Órdenes y avisos
        private DataGridView dgvOrdenes, dgvAvisos;
        private ComboBox cmbFiltroOrden;
        private Button btnReenviar, btnMarcarLeidas, btnExportarOrden, btnExportarListadoOrdenes;

        public FrmReposicion()
        {
            Construir();
            Load += (s, e) =>
            {
                var u = SesionManager.Instancia.UsuarioActual;
                lblUsuario.Text = $"{u?.Nombre} {u?.Apellido}  ·  {u?.NombreRol}";
                AplicarPermisos();
                IrA(_botones.First(b => b.Value.Visible).Key);
            };
        }

        // ═════════════════════════════════════════════════════════
        // Estructura
        // ═════════════════════════════════════════════════════════
        private void Construir()
        {
            SuspendLayout();
            Text = "Hotel Rydl — Reposición de insumos";
            ClientSize = new Size(1120, 740);
            MinimumSize = new Size(1136, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);

            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.Marino };
            lblTitulo = new Label { Text = "Reposición de insumos", ForeColor = Color.White, AutoSize = true, Font = new Font("Georgia", 20f), Location = new Point(24, 16) };
            lblUsuario = new Label
            {
                ForeColor = Color.FromArgb(205, 214, 230), AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
                Font = Tema.Fuente(9.5f), Dock = DockStyle.Right, Width = 440, Padding = new Padding(0, 0, 24, 0)
            };
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblUsuario);
            pnlHeader.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Tema.Dorado });

            pnlSecciones = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Tema.Fondo };
            lblMensaje = new Label
            {
                Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0),
                Font = Tema.Fuente(9.5f, FontStyle.Bold), ForeColor = Tema.TextoSuave, BackColor = Color.White
            };

            var def = new[]
            {
                ("faltantes", "Faltantes y solicitud"), ("cotizaciones", "Cotizaciones"), ("preordenes", "Pre-órdenes"),
                ("gerencia", "Gerencia"), ("recepcion", "Recepción"), ("ordenes", "Órdenes"), ("avisos", "Avisos")
            };
            foreach (var (clave, texto) in def)
            {
                var b = new Button { Text = texto, Size = new Size(142, 38) };
                string k = clave;
                b.Click += (s, e) => IrA(k);
                pnlSecciones.Controls.Add(b);
                _botones[clave] = b;
            }

            ConstruirFaltantes();
            ConstruirCotizaciones();
            ConstruirPreOrdenes();
            ConstruirGerencia();
            ConstruirRecepcion();
            ConstruirOrdenes();
            ConstruirAvisos();

            foreach (var p in _paneles.Values) { p.Dock = DockStyle.Fill; Controls.Add(p); }
            Controls.Add(lblMensaje);
            Controls.Add(pnlSecciones);
            Controls.Add(pnlHeader);
            ResumeLayout(false);
        }

        private Panel NuevaSeccion(string clave)
        {
            var p = new Panel { Size = new Size(1120, 578), BackColor = Tema.Fondo, Visible = false };
            _paneles[clave] = p;
            return p;
        }

        private void IrA(string clave)
        {
            foreach (var kv in _paneles) kv.Value.Visible = kv.Key == clave;
            foreach (var kv in _botones) { if (kv.Key == clave) Tema.BotonPrimario(kv.Value); else Tema.BotonSecundario(kv.Value); }
            Mensaje("", null);
            switch (clave)
            {
                case "faltantes": CargarFaltantes(); break;
                case "cotizaciones": CargarSolicitudes(); break;
                case "preordenes": CargarOfertas(); break;
                case "gerencia": CargarBandeja(); break;
                case "recepcion": CargarARecibir(); break;
                case "ordenes": CargarOrdenes(); break;
                case "avisos": CargarAvisos(); break;
            }
            ActualizarContadorAvisos();
        }

        /// <summary>Cada rol ve solo las secciones de sus casos de uso.</summary>
        private void AplicarPermisos()
        {
            _botones["faltantes"].Visible = ControlAcceso.PuedeConsultarStock();
            _botones["cotizaciones"].Visible = ControlAcceso.PuedeGenerarPreOrden();
            _botones["preordenes"].Visible = ControlAcceso.PuedeGenerarPreOrden();
            _botones["gerencia"].Visible = ControlAcceso.PuedeGenerarOrden();
            _botones["recepcion"].Visible = ControlAcceso.PuedeRegistrarRecepcionInsumos();
            int x = 24;
            foreach (var b in _botones.Values.Where(b => b.Visible)) { b.Location = new Point(x, 10); x += 148; }

            // Solo el Encargado de Compras pide cotizaciones; el resto ve el stock
            pnlSolicitud.Visible = ControlAcceso.PuedeGenerarSolicitudCotizacion();
            dgvFaltantes.Columns[0].Visible = dgvFaltantes.Columns[6].Visible = ControlAcceso.PuedeGenerarSolicitudCotizacion();
        }

        private void Mensaje(string texto, bool? ok)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok == null ? Tema.TextoSuave : (ok.Value ? Tema.Exito : Tema.Peligro);
        }

        private void Resultado<T>(ResultadoReposicion<T> r, string tituloOk = null)
        {
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito && tituloOk != null) MessageBox.Show(this, r.Mensaje, tituloOk, MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (!r.Exito) MessageBox.Show(this, r.Mensaje, "No se pudo completar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ActualizarContadorAvisos()
        {
            int n = _avisos.ContarNoLeidas();
            _botones["avisos"].Text = n > 0 ? $"Avisos ({n})" : "Avisos";
        }

        // ── helpers de construcción ──────────────────────────────────────
        private static Label Etiqueta(Control padre, string texto, int x, int y)
        {
            var l = new Label { Text = texto, AutoSize = true, ForeColor = Tema.TextoSuave, Font = Tema.Fuente(8.5f), Location = new Point(x, y) };
            padre.Controls.Add(l);
            return l;
        }

        private static Label Texto(Control padre, int x, int y, int w, int h, bool negrita = false)
        {
            var l = new Label { Location = new Point(x, y), Size = new Size(w, h), ForeColor = Tema.Texto, Font = Tema.Fuente(9.5f, negrita ? FontStyle.Bold : FontStyle.Regular) };
            padre.Controls.Add(l);
            return l;
        }

        private static TextBox Caja(Control padre, int x, int y, int w, int max, int h = 26)
        {
            var t = new TextBox { Location = new Point(x, y), Size = new Size(w, h), MaxLength = max, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle, Multiline = h > 30 };
            padre.Controls.Add(t);
            return t;
        }

        private static TextBox Campo(Control padre, string etiqueta, int x, int y, int w, int max)
        {
            Etiqueta(padre, etiqueta, x, y);
            return Caja(padre, x, y + 18, w, max);
        }

        private static Button Boton(Control padre, string texto, int x, int y, int w, int h, Action<Button> estilo, Action click)
        {
            var b = new Button { Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
            estilo(b);
            b.Click += (s, e) => click();
            padre.Controls.Add(b);
            return b;
        }

        private static DataGridView Grilla(Control padre, int x, int y, int w, int h, params (string titulo, float peso)[] cols)
        {
            var g = new DataGridView { Location = new Point(x, y), Size = new Size(w, h), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Tema.EstilizarGrid(g);
            foreach (var (titulo, peso) in cols)
                g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
            padre.Controls.Add(g);
            return g;
        }

        /// <summary>Grilla con celdas editables: <paramref name="editables"/> son los índices de columna que se pueden escribir.</summary>
        private static void HacerEditable(DataGridView g, params int[] editables)
        {
            g.ReadOnly = false;
            g.SelectionMode = DataGridViewSelectionMode.CellSelect;
            g.EditMode = DataGridViewEditMode.EditOnEnter;
            g.DataError += (s, e) => { e.ThrowException = false; };
            for (int i = 0; i < g.Columns.Count; i++)
            {
                bool ed = editables.Contains(i);
                g.Columns[i].ReadOnly = !ed;
                if (ed) g.Columns[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 235);
            }
        }

        private static DataGridViewCheckBoxColumn ColumnaCheck(string titulo, float peso)
            => new DataGridViewCheckBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable };

        private static void Fila(DataGridView g, object tag, params object[] valores) { int i = g.Rows.Add(valores); g.Rows[i].Tag = tag; }
        private static T Seleccion<T>(DataGridView g) where T : class => g.CurrentRow?.Tag as T;
        private static string N(decimal d) => d.ToString("0.##");
        private static string Plata(decimal d) => "$ " + d.ToString("N2");
        private static bool Marcado(DataGridViewRow r, int col) => r.Cells[col].Value is bool b && b;

        private static decimal LeerDecimal(object valor)
        {
            string s = (valor?.ToString() ?? "").Replace("$", "").Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal d)) return d;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d)) return d;
            return 0;
        }

        // ═════════════════════════════════════════════════════════
        // CUN06 Generar solicitud de cotización · CUN07 Pre-registrar proveedor
        // ═════════════════════════════════════════════════════════
        private void ConstruirFaltantes()
        {
            var sec = NuevaSeccion("faltantes");
            var izq = Tema.Card("Insumos faltantes (stock en o por debajo del mínimo)", 24, 6, 600, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chkTodoElStock = new CheckBox { Text = "Ver todo el stock", AutoSize = true, Location = new Point(18, 52), ForeColor = Tema.Texto };
            chkTodoElStock.CheckedChanged += (s, e) => CargarFaltantes();
            izq.Controls.Add(chkTodoElStock);
            dgvFaltantes = new DataGridView { Location = new Point(18, 82), Size = new Size(564, 460), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Tema.EstilizarGrid(dgvFaltantes);
            dgvFaltantes.Columns.Add(ColumnaCheck("Pedir", 8));
            foreach (var (t, p) in new[] { ("Código", 12f), ("Insumo", 30f), ("Categoría", 15f), ("Stock", 11f), ("Mínimo", 11f), ("Cantidad", 13f) })
                dgvFaltantes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = t, FillWeight = p, SortMode = DataGridViewColumnSortMode.NotSortable });
            HacerEditable(dgvFaltantes, 0, 6);
            dgvFaltantes.CellValueChanged += (s, e) => ResumenSolicitud();
            dgvFaltantes.CurrentCellDirtyStateChanged += (s, e) => { if (dgvFaltantes.IsCurrentCellDirty) dgvFaltantes.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            izq.Controls.Add(dgvFaltantes);

            pnlSolicitud = Tema.Card("Solicitud de cotización", 644, 6, 450, 560);
            pnlSolicitud.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            var der = pnlSolicitud;
            Etiqueta(der, "Proveedor al que se le pide la cotización", 18, 50);
            cmbProveedor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(18, 68), Size = new Size(414, 26), Font = Tema.Fuente(10) };
            cmbProveedor.SelectedIndexChanged += (s, e) =>
            {
                var p = cmbProveedor.SelectedItem as ProveedorBE;
                lblEstadoProv.Text = p == null ? "" : $"{p.Estado} · {p.Contacto} · {p.Telefono}" + (string.IsNullOrWhiteSpace(p.Email) ? " · sin correo" : " · " + p.Email);
            };
            der.Controls.Add(cmbProveedor);
            lblEstadoProv = Texto(der, 18, 98, 414, 20);
            lblEstadoProv.ForeColor = Tema.TextoSuave;

            var pre = new Panel { Location = new Point(18, 124), Size = new Size(414, 236), BackColor = Color.FromArgb(253, 246, 232) };
            var lp = new Label { Text = "¿Nunca le vendió al hotel? Pre-registralo (CUN07)", AutoSize = true, Font = Tema.Fuente(9.5f, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(10, 8) };
            pre.Controls.Add(lp);
            txtPreEmpresa = Campo(pre, "Empresa", 10, 34, 192, 150);
            txtPreContacto = Campo(pre, "Contacto", 212, 34, 192, 100);
            txtPreTelefono = Campo(pre, "Teléfono", 10, 86, 192, 30);
            txtPreEmail = Campo(pre, "Correo (opcional)", 212, 86, 192, 200);
            txtPreDireccion = Campo(pre, "Dirección", 10, 138, 394, 300);
            btnPreRegistrar = Boton(pre, "Pre-registrar proveedor", 10, 190, 394, 36, Tema.BotonPrimario, PreRegistrar);
            der.Controls.Add(pre);

            lblResumenSolicitud = Texto(der, 18, 372, 414, 40, true);
            btnGenerarSolicitud = Boton(der, "Generar y enviar solicitud de cotización", 18, 418, 414, 44, Tema.BotonDorado, GenerarSolicitud);
            var nota = Texto(der, 18, 470, 414, 70);
            nota.Text = "Tildá los insumos y ajustá las cantidades. Para cotizar con más proveedores, generá una solicitud para cada uno.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(pnlSolicitud);
        }

        private void CargarFaltantes()
        {
            var r = _stock.ObtenerStockInsumos(!chkTodoElStock.Checked);
            dgvFaltantes.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var i in r.Dato)
            {
                Fila(dgvFaltantes, i, i.Faltante, i.Codigo, i.Nombre, i.Categoria, N(i.StockActual), N(i.StockMinimo), i.Faltante ? N(i.CantidadSugerida) : "");
                var row = dgvFaltantes.Rows[dgvFaltantes.Rows.Count - 1];
                row.Cells[4].Style.ForeColor = i.Faltante ? Tema.Peligro : Tema.Exito;
                row.Cells[4].Style.Font = Tema.Fuente(9.5f, FontStyle.Bold);
            }
            CargarProveedores(cmbProveedor);
            int falt = r.Dato.Count(i => i.Faltante);
            Mensaje(falt == 0 && !chkTodoElStock.Checked ? "No hay insumos faltantes: no hace falta pedir cotización." : $"{falt} insumos faltantes.", null);
            ResumenSolicitud();
        }

        private void CargarProveedores(ComboBox cmb, int? seleccionar = null)
        {
            var sel = seleccionar ?? (cmb.SelectedItem as ProveedorBE)?.IdProveedor;
            var pr = _proveedores.ObtenerProveedoresActivos();
            cmb.Items.Clear();
            if (pr.Exito) foreach (var p in pr.Dato) cmb.Items.Add(p);
            int idx = cmb.Items.Cast<ProveedorBE>().ToList().FindIndex(p => p.IdProveedor == sel);
            if (cmb.Items.Count > 0) cmb.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private List<ItemInsumoBE> ItemsTildados()
            => dgvFaltantes.Rows.Cast<DataGridViewRow>().Where(r => Marcado(r, 0) && r.Tag is InsumoBE)
               .Select(r => new ItemInsumoBE { IdInsumo = ((InsumoBE)r.Tag).IdInsumo, NombreInsumo = ((InsumoBE)r.Tag).Nombre, Cantidad = LeerDecimal(r.Cells[6].Value) }).ToList();

        private void ResumenSolicitud()
        {
            var items = ItemsTildados();
            lblResumenSolicitud.Text = items.Count == 0 ? "Tildá los insumos a cotizar." : $"{items.Count} insumo(s): " + string.Join(", ", items.Select(i => $"{i.NombreInsumo} x{N(i.Cantidad)}"));
        }

        private void PreRegistrar()
        {
            var r = _proveedores.PreRegistrarProveedor(new ProveedorBE
            { Nombre = txtPreEmpresa.Text, Contacto = txtPreContacto.Text, Telefono = txtPreTelefono.Text, Email = txtPreEmail.Text, Direccion = txtPreDireccion.Text });
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (!r.Exito) return;
            foreach (var t in new[] { txtPreEmpresa, txtPreContacto, txtPreTelefono, txtPreEmail, txtPreDireccion }) t.Clear();
            CargarProveedores(cmbProveedor, r.Dato.IdProveedor);
        }

        private void GenerarSolicitud()
        {
            dgvFaltantes.EndEdit();
            var p = cmbProveedor.SelectedItem as ProveedorBE;
            if (p == null) { Mensaje("✖ Elegí un proveedor.", false); return; }
            var r = _solicitudes.GenerarSolicitudCotizacion(p.IdProveedor, ItemsTildados());
            Resultado(r, "Solicitud de cotización generada");
        }

        // ═════════════════════════════════════════════════════════
        // CUN08 (1/2) Registrar la cotización que respondió el proveedor
        // ═════════════════════════════════════════════════════════
        private void ConstruirCotizaciones()
        {
            var sec = NuevaSeccion("cotizaciones");
            var izq = Tema.Card("Solicitudes enviadas, esperando cotización", 24, 6, 470, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dgvSolicitudes = Grilla(izq, 18, 56, 434, 486, ("Nro", 20), ("Fecha", 22), ("Proveedor", 34), ("Insumos", 12));
            dgvSolicitudes.SelectionChanged += (s, e) => SolicitudSeleccionada();

            var der = Tema.Card("Registrar cotización recibida", 514, 6, 580, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSolicitudSel = Texto(der, 18, 48, 544, 22, true);
            Etiqueta(der, "Lo que puede proveer (dejá en 0 lo que no cotizó)", 18, 74);
            dgvItemsCotizacion = Grilla(der, 18, 94, 544, 220, ("Insumo", 38), ("Solicitado", 16), ("Disponible", 16), ("Precio unit. ($)", 18), ("Subtotal", 18));
            dgvItemsCotizacion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            HacerEditable(dgvItemsCotizacion, 2, 3);
            dgvItemsCotizacion.CellEndEdit += (s, e) => RecalcularCotizacion();
            var lb = new Label { Text = "Datos del proveedor para el pago", AutoSize = true, Font = Tema.Fuente(10, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(18, 326) };
            der.Controls.Add(lb);
            txtCotCuit = Campo(der, "CUIT (11 dígitos)", 18, 350, 260, 15);
            txtCotBanco = Campo(der, "Banco", 302, 350, 260, 100);
            txtCotCbu = Campo(der, "CBU (22 dígitos)", 18, 404, 260, 30);
            txtCotTitular = Campo(der, "Titular de la cuenta", 302, 404, 260, 150);
            txtCotCbu.TextChanged += (s, e) => { string b = SERVICIOS_HRS_480.BancoServicio.BancoDelCBU(txtCotCbu.Text); if (b.Length > 0) txtCotBanco.Text = b; };
            btnRegistrarCotizacion = Boton(der, "Registrar cotización", 18, 470, 544, 44, Tema.BotonDorado, RegistrarCotizacion);

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarSolicitudes()
        {
            var r = _solicitudes.ObtenerSolicitudesSinCotizar();
            dgvSolicitudes.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var s in r.Dato) Fila(dgvSolicitudes, s, s.NroSolicitud, s.FechaSolicitud.ToString("dd/MM/yyyy"), s.NombreProveedor, s.Items.Count);
            Mensaje(r.Dato.Count == 0 ? "No hay solicitudes esperando cotización." : $"{r.Dato.Count} solicitud(es) esperando la cotización del proveedor.", null);
            SolicitudSeleccionada();
        }

        private void SolicitudSeleccionada()
        {
            var s = Seleccion<SolicitudCotizacionBE>(dgvSolicitudes);
            dgvItemsCotizacion.Rows.Clear();
            btnRegistrarCotizacion.Enabled = s != null;
            if (s == null) { lblSolicitudSel.Text = "Seleccioná una solicitud."; return; }
            lblSolicitudSel.Text = $"{s.NroSolicitud} · {s.NombreProveedor}";
            foreach (var i in s.Items) Fila(dgvItemsCotizacion, i, i.NombreInsumo, N(i.Cantidad), N(i.Cantidad), "0", "");
            foreach (var t in new[] { txtCotCuit, txtCotBanco, txtCotCbu, txtCotTitular }) t.Clear();
            txtCotTitular.Text = s.NombreProveedor;
        }

        private List<ItemInsumoBE> ItemsCotizados()
            => dgvItemsCotizacion.Rows.Cast<DataGridViewRow>().Where(r => r.Tag is ItemInsumoBE)
               .Select(r => new ItemInsumoBE { IdInsumo = ((ItemInsumoBE)r.Tag).IdInsumo, NombreInsumo = ((ItemInsumoBE)r.Tag).NombreInsumo, Cantidad = LeerDecimal(r.Cells[2].Value), PrecioUnitario = LeerDecimal(r.Cells[3].Value) }).ToList();

        private void RecalcularCotizacion()
        {
            foreach (DataGridViewRow r in dgvItemsCotizacion.Rows)
            {
                decimal c = LeerDecimal(r.Cells[2].Value), p = LeerDecimal(r.Cells[3].Value);
                r.Cells[4].Value = c > 0 && p > 0 ? Plata(c * p) : "";
            }
        }

        private void RegistrarCotizacion()
        {
            dgvItemsCotizacion.EndEdit();
            var s = Seleccion<SolicitudCotizacionBE>(dgvSolicitudes);
            if (s == null) return;
            var r = _preordenes.RegistrarCotizacion(s.IdSolicitudCotizacion, ItemsCotizados(), txtCotCuit.Text, txtCotBanco.Text, txtCotCbu.Text, txtCotTitular.Text);
            Resultado(r);
            if (r.Exito) CargarSolicitudes();
        }

        // ═════════════════════════════════════════════════════════
        // CUN08 (2/2) Comparar cotizaciones y generar las pre-órdenes
        // ═════════════════════════════════════════════════════════
        private void ConstruirPreOrdenes()
        {
            var sec = NuevaSeccion("preordenes");
            var card = Tema.Card("Comparación de cotizaciones: elegí una oferta por insumo", 24, 6, 1070, 560);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvOfertas = new DataGridView { Location = new Point(18, 56), Size = new Size(1034, 410), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Tema.EstilizarGrid(dgvOfertas);
            dgvOfertas.Columns.Add(ColumnaCheck("Elegir", 6));
            foreach (var (t, p) in new[] { ("Insumo", 20f), ("Proveedor", 22f), ("Solicitado", 9f), ("Disponible", 9f), ("A pedir", 9f), ("Precio unit.", 10f), ("Subtotal", 11f), ("", 8f) })
                dgvOfertas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = t, FillWeight = p, SortMode = DataGridViewColumnSortMode.NotSortable });
            HacerEditable(dgvOfertas, 0);
            dgvOfertas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOfertas.CurrentCellDirtyStateChanged += (s, e) => { if (dgvOfertas.IsCurrentCellDirty) dgvOfertas.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            dgvOfertas.CellValueChanged += (s, e) => OfertaTildada(e.RowIndex, e.ColumnIndex);
            card.Controls.Add(dgvOfertas);
            lblResumenPreOrden = Texto(card, 18, 478, 700, 60, true);
            lblResumenPreOrden.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnGenerarPreOrdenes = Boton(card, "Generar pre-órdenes de compra", 742, 486, 310, 46, Tema.BotonDorado, GenerarPreOrdenes);
            btnGenerarPreOrdenes.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sec.Controls.Add(card);
        }

        private void CargarOfertas()
        {
            var r = _preordenes.ObtenerOfertasParaComparar();
            _ajustandoOfertas = true;
            dgvOfertas.Rows.Clear();
            if (r.Exito)
                foreach (var o in r.Dato)
                {
                    Fila(dgvOfertas, o, o.Elegida, o.NombreInsumo, o.NombreProveedor, N(o.CantidadSolicitada), N(o.CantidadDisponible), N(o.CantidadAPedir),
                         Plata(o.PrecioUnitario), Plata(o.Subtotal), o.MasConveniente ? "★ mejor" : "");
                    if (o.MasConveniente) dgvOfertas.Rows[dgvOfertas.Rows.Count - 1].Cells[8].Style.ForeColor = Tema.Exito;
                }
            _ajustandoOfertas = false;
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            Mensaje(r.Dato.Count == 0 ? "No hay cotizaciones para comparar. Registralas en la sección Cotizaciones." : $"{r.Dato.Select(o => o.IdInsumo).Distinct().Count()} insumos cotizados.", null);
            ResumenPreOrden();
        }

        /// <summary>Al tildar una oferta se destildan las otras del mismo insumo (una por insumo).</summary>
        private void OfertaTildada(int fila, int col)
        {
            if (_ajustandoOfertas || col != 0 || fila < 0) return;
            var o = dgvOfertas.Rows[fila].Tag as OfertaBE;
            if (o != null && Marcado(dgvOfertas.Rows[fila], 0))
            {
                _ajustandoOfertas = true;
                foreach (DataGridViewRow r in dgvOfertas.Rows)
                    if (r.Index != fila && r.Tag is OfertaBE x && x.IdInsumo == o.IdInsumo) r.Cells[0].Value = false;
                _ajustandoOfertas = false;
            }
            ResumenPreOrden();
        }

        private List<OfertaBE> OfertasElegidas()
            => dgvOfertas.Rows.Cast<DataGridViewRow>().Where(r => Marcado(r, 0) && r.Tag is OfertaBE).Select(r => (OfertaBE)r.Tag).ToList();

        private void ResumenPreOrden()
        {
            var el = OfertasElegidas();
            lblResumenPreOrden.Text = el.Count == 0 ? "No hay ofertas elegidas." :
                string.Join("\n", el.GroupBy(o => o.NombreProveedor).Select(g => $"Pre-orden a {g.Key}: {g.Count()} insumo(s), {Plata(g.Sum(o => o.Subtotal))}"));
            btnGenerarPreOrdenes.Enabled = el.Count > 0;
        }

        private void GenerarPreOrdenes()
        {
            var el = OfertasElegidas();
            if (MessageBox.Show(this, lblResumenPreOrden.Text + "\n\n¿Generar las pre-órdenes?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _preordenes.GenerarPreOrdenes(el);
            Resultado(r, "Pre-órdenes generadas");
            CargarOfertas();
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // CUN09 Generar orden de compra · CUN10 Registrar proveedor · CUN11 Pagar orden de compra
        // ═════════════════════════════════════════════════════════
        private void ConstruirGerencia()
        {
            var sec = NuevaSeccion("gerencia");
            var izq = Tema.Card("Pre-órdenes a aprobar y órdenes a pagar", 24, 6, 590, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvBandeja = Grilla(izq, 18, 56, 554, 486, ("Nro", 15), ("Fecha", 17), ("Proveedor", 30), ("Total", 19), ("Estado", 15));
            dgvBandeja.SelectionChanged += (s, e) => OrdenSeleccionada();

            var der = Tema.Card("Orden seleccionada", 634, 6, 460, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleOrden = Texto(der, 18, 46, 424, 104);
            Etiqueta(der, "Motivo (solo si se cancela)", 18, 152);
            txtMotivoCancelacion = Caja(der, 18, 170, 424, 250);
            btnGenerarOrden = Boton(der, "Aprobar y generar orden", 18, 202, 260, 38, Tema.BotonPrimario, GenerarOrden);
            btnCancelarPreOrden = Boton(der, "Cancelar pre-orden", 286, 202, 156, 38, Tema.BotonSecundario, CancelarPreOrden);
            lblEstadoProvOrden = Texto(der, 18, 248, 424, 20, true);

            pnlRegistroProveedor = new Panel { Location = new Point(18, 270), Size = new Size(424, 150), BackColor = Color.FromArgb(253, 246, 232) };
            txtRegCuit = Campo(pnlRegistroProveedor, "CUIT", 10, 4, 196, 15);
            txtRegBanco = Campo(pnlRegistroProveedor, "Banco", 218, 4, 196, 100);
            txtRegCbu = Campo(pnlRegistroProveedor, "CBU", 10, 54, 196, 30);
            txtRegTitular = Campo(pnlRegistroProveedor, "Titular", 218, 54, 196, 150);
            btnRegistrarProveedor = Boton(pnlRegistroProveedor, "Registrar proveedor (datos de su cotización)", 10, 106, 404, 36, Tema.BotonPrimario, RegistrarProveedor);
            der.Controls.Add(pnlRegistroProveedor);

            Etiqueta(der, "Dirección de entrega", 18, 428);
            txtDireccion = Caja(der, 18, 446, 424, 300);
            txtDireccion.Text = OrdenCompraBLL.DIRECCION_ENTREGA_DEFECTO;
            btnPagar = Boton(der, "Pagar orden y enviarla al proveedor", 18, 482, 424, 44, Tema.BotonDorado, () => Pagar(false));

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarBandeja(int? seleccionar = null)
        {
            var r = _ordenes.ObtenerBandejaGerente();
            dgvBandeja.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var o in r.Dato)
            {
                Fila(dgvBandeja, o, o.NroOrden, (o.FechaGeneracion ?? o.FechaPreOrden).ToString("dd/MM/yyyy"), o.NombreProveedor, Plata(o.Total), o.Estado);
                if (o.IdOrden == seleccionar) dgvBandeja.CurrentCell = dgvBandeja.Rows[dgvBandeja.Rows.Count - 1].Cells[0];
            }
            Mensaje(r.Dato.Count == 0 ? "No hay pre-órdenes ni órdenes pendientes de pago." :
                $"{r.Dato.Count(o => o.Estado == EstadoOrden.PRE_ORDEN)} pre-orden(es) a aprobar · {r.Dato.Count(o => o.Estado == EstadoOrden.GENERADA)} orden(es) a pagar.", null);
            OrdenSeleccionada();
        }

        private void OrdenSeleccionada()
        {
            var o = Seleccion<OrdenCompraBE>(dgvBandeja);
            bool pre = o?.Estado == EstadoOrden.PRE_ORDEN, gen = o?.Estado == EstadoOrden.GENERADA;
            btnGenerarOrden.Enabled = btnCancelarPreOrden.Enabled = txtMotivoCancelacion.Enabled = pre;
            if (o == null) { lblDetalleOrden.Text = "Seleccioná una pre-orden."; lblEstadoProvOrden.Text = ""; pnlRegistroProveedor.Visible = false; btnPagar.Enabled = false; return; }
            lblDetalleOrden.Text = $"{o.NroOrden} · {o.NombreProveedor} · {o.Estado}\n" +
                string.Join("\n", o.Items.Take(4).Select(i => $"  {i.NombreInsumo}: {N(i.Cantidad)} x {Plata(i.PrecioUnitario)} = {Plata(i.Subtotal)}")) +
                (o.Items.Count > 4 ? $"\n  … y {o.Items.Count - 4} más" : "") + $"\nTotal: {Plata(o.Total)}";
            bool provPre = o.EstadoProveedor != ProveedorBE.REGISTRADO;
            lblEstadoProvOrden.ForeColor = provPre ? Tema.Peligro : Tema.Exito;
            lblEstadoProvOrden.Text = provPre ? "Proveedor PRE-REGISTRADO: registralo antes de pagar (CUN10)." : "✔ Proveedor registrado.";
            pnlRegistroProveedor.Visible = provPre;
            if (provPre)
            {
                var c = _ordenes.ObtenerCotizacionDeOrden(o.IdOrden);
                if (c.Exito) { txtRegCuit.Text = c.Dato.CUIT; txtRegBanco.Text = c.Dato.Banco; txtRegCbu.Text = c.Dato.CBU; txtRegTitular.Text = c.Dato.Titular; }
            }
            btnPagar.Enabled = gen && !provPre;
        }

        private void GenerarOrden()
        {
            var o = Seleccion<OrdenCompraBE>(dgvBandeja);
            if (o == null) return;
            if (MessageBox.Show(this, $"¿Aprobar la pre-orden {o.NroOrden} de {o.NombreProveedor} por {Plata(o.Total)}?", "Generar orden de compra",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _ordenes.GenerarOrdenCompra(o.IdOrden);
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (!r.Exito) { MessageBox.Show(this, r.Mensaje, "No se pudo generar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            CargarBandeja(o.IdOrden);
            // CUN09 incluye CUN11: si el proveedor ya está registrado se paga a continuación
            if (r.Dato.EstadoProveedor == ProveedorBE.REGISTRADO) Pagar(true);
            else MessageBox.Show(this, r.Mensaje, "Orden generada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CancelarPreOrden()
        {
            var o = Seleccion<OrdenCompraBE>(dgvBandeja);
            if (o == null) return;
            var r = _ordenes.CancelarPreOrden(o.IdOrden, txtMotivoCancelacion.Text);
            Resultado(r);
            if (r.Exito) { txtMotivoCancelacion.Clear(); CargarBandeja(); }
        }

        private void RegistrarProveedor()
        {
            var o = Seleccion<OrdenCompraBE>(dgvBandeja);
            if (o == null) return;
            var r = _proveedores.RegistrarProveedor(o.IdProveedor, txtRegCuit.Text, txtRegBanco.Text, txtRegCbu.Text, txtRegTitular.Text);
            Resultado(r);
            if (!r.Exito) return;
            CargarBandeja(o.IdOrden);
            if (o.Estado == EstadoOrden.GENERADA) Pagar(true);
        }

        private void Pagar(bool automatico)
        {
            var o = Seleccion<OrdenCompraBE>(dgvBandeja);
            if (o == null) return;
            if (MessageBox.Show(this, $"Se va a pagar por transferencia la orden {o.NroOrden}:\n\n{o.NombreProveedor}\nTotal {Plata(o.Total)}\n\n¿Confirmás el pago?",
                    automatico ? "Pagar orden de compra" : "Confirmar pago", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _ordenes.PagarOrden(o.IdOrden, txtDireccion.Text);
            Resultado(r, "Orden pagada");
            CargarBandeja();
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // CUN12 Registrar recepción de insumos
        // ═════════════════════════════════════════════════════════
        private void ConstruirRecepcion()
        {
            var sec = NuevaSeccion("recepcion");
            var izq = Tema.Card("Órdenes pagadas: mercadería a recibir", 24, 6, 560, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvARecibir = Grilla(izq, 18, 56, 524, 486, ("Orden", 16), ("Pagada", 17), ("Proveedor", 34), ("Total", 19), ("Ítems", 10));
            dgvARecibir.SelectionChanged += (s, e) => ARecibirSeleccionada();

            var der = Tema.Card("Registrar recepción con la factura", 604, 6, 490, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleRecepcion = Texto(der, 18, 46, 454, 22, true);
            txtNroFactura = Campo(der, "Número de factura", 18, 72, 220, 30);
            Etiqueta(der, "Total de la factura ($)", 252, 72);
            numTotalFactura = new NumericUpDown { Location = new Point(252, 90), Size = new Size(220, 26), DecimalPlaces = 2, Maximum = 999999999, ThousandsSeparator = true, Font = Tema.Fuente(10) };
            der.Controls.Add(numTotalFactura);
            Etiqueta(der, "Cantidad realmente recibida de cada insumo", 18, 126);
            dgvItemsRecepcion = Grilla(der, 18, 146, 454, 220, ("Insumo", 46), ("Pedido", 18), ("Recibido", 18), ("Dif.", 14));
            dgvItemsRecepcion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            HacerEditable(dgvItemsRecepcion, 2);
            dgvItemsRecepcion.CellEndEdit += (s, e) => RecalcularRecepcion();
            Etiqueta(der, "Observaciones (opcional)", 18, 374);
            txtObsRecepcion = Caja(der, 18, 392, 454, 300, 44);
            btnRegistrarRecepcion = Boton(der, "Registrar recepción y actualizar stock", 18, 448, 454, 44, Tema.BotonDorado, RegistrarRecepcion);
            var nota = Texto(der, 18, 500, 454, 50);
            nota.Text = "Después se le devuelve al proveedor la factura firmada. El stock se actualiza solo con lo recibido.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarARecibir()
        {
            var r = _recepciones.ObtenerOrdenesARecibir();
            dgvARecibir.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var o in r.Dato) Fila(dgvARecibir, o, o.NroOrden, (o.FechaPago ?? o.FechaPreOrden).ToString("dd/MM/yyyy"), o.NombreProveedor, Plata(o.Total), o.Items.Count);
            Mensaje(r.Dato.Count == 0 ? "No hay órdenes pagadas esperando mercadería." : $"{r.Dato.Count} orden(es) esperando la mercadería.", null);
            ARecibirSeleccionada();
        }

        private void ARecibirSeleccionada()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            dgvItemsRecepcion.Rows.Clear();
            btnRegistrarRecepcion.Enabled = o != null;
            if (o == null) { lblDetalleRecepcion.Text = "Seleccioná una orden."; return; }
            lblDetalleRecepcion.Text = $"{o.NroOrden} · {o.NombreProveedor} · {Plata(o.Total)}";
            foreach (var i in o.Items) Fila(dgvItemsRecepcion, i, i.NombreInsumo, N(i.Cantidad), N(i.Cantidad), "");
            numTotalFactura.Value = Math.Min(numTotalFactura.Maximum, o.Total);
            txtNroFactura.Clear();
        }

        private void RecalcularRecepcion()
        {
            foreach (DataGridViewRow r in dgvItemsRecepcion.Rows)
            {
                decimal d = LeerDecimal(r.Cells[2].Value) - LeerDecimal(r.Cells[1].Value);
                r.Cells[3].Value = d == 0 ? "" : (d > 0 ? "+" : "") + N(d);
                r.Cells[3].Style.ForeColor = Tema.Peligro;
            }
        }

        private void RegistrarRecepcion()
        {
            dgvItemsRecepcion.EndEdit();
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            if (o == null) return;
            var items = dgvItemsRecepcion.Rows.Cast<DataGridViewRow>().Where(r => r.Tag is ItemInsumoBE)
                .Select(r => new ItemRecepcionBE { IdInsumo = ((ItemInsumoBE)r.Tag).IdInsumo, CantidadRecibida = LeerDecimal(r.Cells[2].Value) }).ToList();
            var r = _recepciones.RegistrarRecepcion(o.IdOrden, txtNroFactura.Text, numTotalFactura.Value, items, txtObsRecepcion.Text);
            Resultado(r, "Recepción registrada");
            if (r.Exito) { txtObsRecepcion.Clear(); CargarARecibir(); }
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // Órdenes (consulta, reenvío, reportes PDF) y Avisos
        // ═════════════════════════════════════════════════════════
        private void ConstruirOrdenes()
        {
            var sec = NuevaSeccion("ordenes");
            var card = Tema.Card("Órdenes de compra", 24, 6, 1070, 560);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Etiqueta(card, "Estado", 200, 20);
            cmbFiltroOrden = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(252, 15), Size = new Size(170, 26), Font = Tema.Fuente(10) };
            cmbFiltroOrden.Items.AddRange(new object[] { "Todas", EstadoOrden.PRE_ORDEN, EstadoOrden.GENERADA, EstadoOrden.PAGADA, EstadoOrden.RECIBIDA, EstadoOrden.CANCELADA });
            cmbFiltroOrden.SelectedIndex = 0;
            cmbFiltroOrden.SelectedIndexChanged += (s, e) => CargarOrdenes();
            card.Controls.Add(cmbFiltroOrden);
            btnExportarListadoOrdenes = Boton(card, "Listado (PDF)", 448, 14, 150, 30, Tema.BotonSecundario, ExportarListadoOrdenes);
            btnExportarListadoOrdenes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportarOrden = Boton(card, "Exportar orden (PDF)", 646, 14, 200, 30, Tema.BotonDorado, ExportarOrden);
            btnExportarOrden.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReenviar = Boton(card, "Reenviar al proveedor", 862, 14, 190, 30, Tema.BotonSecundario, Reenviar);
            btnReenviar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dgvOrdenes = Grilla(card, 18, 56, 1034, 486, ("Orden", 8), ("Fecha", 8), ("Proveedor", 15), ("Insumos", 27), ("Total", 9), ("Estado", 9), ("Pago", 15), ("Enviada", 9));
            dgvOrdenes.SelectionChanged += (s, e) =>
            {
                var o = Seleccion<OrdenCompraBE>(dgvOrdenes);
                btnReenviar.Enabled = o != null && o.Estado == EstadoOrden.PAGADA && ControlAcceso.PuedePagarOrden();
                btnExportarOrden.Enabled = o != null;
            };
            sec.Controls.Add(card);
        }

        private void CargarOrdenes()
        {
            string filtro = cmbFiltroOrden.SelectedItem?.ToString();
            var r = _ordenes.ObtenerOrdenes(filtro == null || filtro == "Todas" ? null : filtro);
            dgvOrdenes.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var o in r.Dato)
                Fila(dgvOrdenes, o, o.NroOrden, (o.FechaGeneracion ?? o.FechaPreOrden).ToString("dd/MM/yyyy"), o.NombreProveedor, o.ResumenItems, Plata(o.Total), o.Estado,
                     string.IsNullOrWhiteSpace(o.NroOperacionPago) ? "-" : o.NroOperacionPago, o.FechaEnvio.HasValue ? o.FechaEnvio.Value.ToString("dd/MM HH:mm") : "-");
            btnExportarOrden.Enabled = Seleccion<OrdenCompraBE>(dgvOrdenes) != null;
            Mensaje($"{r.Dato.Count} órdenes.", null);
        }

        private void ExportarOrden()
        {
            var o = Seleccion<OrdenCompraBE>(dgvOrdenes);
            if (o == null) return;
            string ruta = ReportesUI.PedirRuta("Orden de compra " + o.NroOrden + ".pdf");
            if (ruta == null) return;
            var r = _reportes.ExportarOrdenCompra(o.IdOrden, ruta);
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ExportarListadoOrdenes()
        {
            string filtro = cmbFiltroOrden.SelectedItem?.ToString();
            string estado = filtro == null || filtro == "Todas" ? null : filtro;
            string ruta = ReportesUI.PedirRuta("Reporte de órdenes de compra " + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            if (ruta == null) return;
            var r = _reportes.ExportarListadoOrdenes(estado, ruta);
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Reenviar()
        {
            var o = Seleccion<OrdenCompraBE>(dgvOrdenes);
            if (o == null) return;
            var r = _ordenes.ReenviarOrden(o.IdOrden);
            Mensaje((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            CargarOrdenes();
        }

        private void ConstruirAvisos()
        {
            var sec = NuevaSeccion("avisos");
            var card = Tema.Card("Mis avisos", 24, 6, 1070, 560);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnMarcarLeidas = Boton(card, "Marcar todos como leídos", 842, 14, 210, 30, Tema.BotonSecundario, () => { _avisos.MarcarTodasLeidas(); CargarAvisos(); ActualizarContadorAvisos(); });
            btnMarcarLeidas.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dgvAvisos = Grilla(card, 18, 56, 1034, 486, ("Fecha", 13), ("Tipo", 18), ("Aviso", 60), ("Estado", 9));
            sec.Controls.Add(card);
        }

        private void CargarAvisos()
        {
            dgvAvisos.Rows.Clear();
            var lista = _avisos.ObtenerMisNotificaciones(false);
            foreach (var n in lista)
            {
                Fila(dgvAvisos, n, n.Fecha.ToString("dd/MM/yyyy HH:mm"), n.Tipo, n.Mensaje, n.Leida ? "Leído" : "NUEVO");
                if (!n.Leida) dgvAvisos.Rows[dgvAvisos.Rows.Count - 1].DefaultCellStyle.Font = Tema.Fuente(9.5f, FontStyle.Bold);
            }
            Mensaje(lista.Count == 0 ? "No tenés avisos." : $"{lista.Count} avisos ({lista.Count(n => !n.Leida)} sin leer).", null);
        }
    }
}
