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
    /// Pantalla del proceso PN2 "Gestión de reposición de insumos" (la "GUI" de los DSS CUN06 a CUN10).
    /// Una sección por caso de uso: cotización (CUN06), pre-orden (CUN07), compra con registro de proveedor
    /// nuevo y pago (CUN08/CUN09) y recepción con factura (CUN10); más órdenes y avisos. Solo habla con las BLL.
    /// </summary>
    public class FrmReposicion : Form
    {
        private readonly StockBLL _stock = new StockBLL();
        private readonly CotizacionBLL _cotizaciones = new CotizacionBLL();
        private readonly PreOrdenBLL _preordenes = new PreOrdenBLL();
        private readonly ProveedorBLL _proveedores = new ProveedorBLL();
        private readonly OrdenCompraBLL _ordenes = new OrdenCompraBLL();
        private readonly RecepcionBLL _recepciones = new RecepcionBLL();
        private readonly NotificacionBLL _avisos = new NotificacionBLL();
        private readonly ReporteBLL _reportes = new ReporteBLL();

        // Estructura
        private Panel pnlHeader, pnlSecciones;
        private Label lblTitulo, lblUsuario, lblMensaje;
        private readonly Dictionary<string, Panel> _paneles = new Dictionary<string, Panel>();
        private readonly Dictionary<string, Button> _botones = new Dictionary<string, Button>();

        // CUN06 Stock + cotización
        private DataGridView dgvStock, dgvOfertas;
        private CheckBox chkCriticos;
        private Label lblInsumoSel, lblStockInfo, lblUltimoPrecio, lblMejorOferta, lblPermisoCotizar;
        private NumericUpDown numCantCotizar;
        private TextBox txtObsCotizacion;
        private Button btnRegistrarCotizacion;

        // CUN07 Pre-orden
        private DataGridView dgvCotizaciones, dgvComparacion;
        private Label lblDetallePreOrden;
        private ComboBox cmbPrioridad;
        private TextBox txtObsPreOrden;
        private Button btnGenerarPreOrden, btnAnularCotizacion;

        // CUN08 / CUN09 Comprar
        private DataGridView dgvPreOrdenes;
        private Label lblDetalleCompra, lblEstadoProveedor, lblTotalCompra;
        private Panel pnlRegistroProveedor;
        private TextBox txtCuit, txtCbu, txtBanco, txtEmailProv, txtDireccion;
        private Button btnRegistrarProveedor, btnPagar;

        // CUN10 Recepción
        private DataGridView dgvARecibir;
        private Label lblDetalleRecepcion, lblControl;
        private TextBox txtNroFactura, txtObsRecepcion;
        private DateTimePicker dtpFechaFactura;
        private NumericUpDown numCantFacturada, numPrecioFacturado, numRecibida;
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
                lblUsuario.Text = $"{SesionManager.Instancia.UsuarioActual?.Nombre} {SesionManager.Instancia.UsuarioActual?.Apellido}  ·  {SesionManager.Instancia.UsuarioActual?.NombreRol}";
                AplicarPermisos();
                IrA("cotizar");
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
            lblTitulo = new Label
            {
                Text = "Reposición de insumos", ForeColor = Color.White, AutoSize = true,
                Font = new Font("Georgia", 20f, FontStyle.Regular), Location = new Point(24, 16)
            };
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
                Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0), Font = Tema.Fuente(9.5f, FontStyle.Bold),
                ForeColor = Tema.TextoSuave, BackColor = Color.White
            };

            var def = new[]
            {
                ("cotizar", "Stock y cotización"), ("preorden", "Pre-orden"), ("comprar", "Comprar"),
                ("recepcion", "Recepción"), ("ordenes", "Órdenes"), ("avisos", "Avisos")
            };
            int x = 24;
            foreach (var (clave, texto) in def)
            {
                var b = new Button { Text = texto, Size = new Size(160, 38), Location = new Point(x, 10) };
                string k = clave;
                b.Click += (s, e) => IrA(k);
                pnlSecciones.Controls.Add(b);
                _botones[clave] = b;
                x += 166;
            }

            ConstruirCotizacion();
            ConstruirPreOrden();
            ConstruirComprar();
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
            // Tamaño inicial = el área disponible del formulario, para que los Anchor de las tarjetas calculen bien
            var p = new Panel { Size = new Size(1120, 578), BackColor = Tema.Fondo, Visible = false };
            _paneles[clave] = p;
            return p;
        }

        private void IrA(string clave)
        {
            foreach (var kv in _paneles) kv.Value.Visible = kv.Key == clave;
            foreach (var kv in _botones)
            {
                if (kv.Key == clave) Tema.BotonPrimario(kv.Value);
                else Tema.BotonSecundario(kv.Value);
            }
            Mensaje("", null);
            switch (clave)
            {
                case "cotizar": CargarStock(); break;
                case "preorden": CargarCotizaciones(); break;
                case "comprar": CargarPreOrdenes(); break;
                case "recepcion": CargarARecibir(); break;
                case "ordenes": CargarOrdenes(); break;
                case "avisos": CargarAvisos(); break;
            }
            ActualizarContadorAvisos();
        }

        /// <summary>Cada rol ve solo las secciones de sus casos de uso (la BLL vuelve a validar).</summary>
        private void AplicarPermisos()
        {
            _botones["preorden"].Visible = ControlAcceso.PuedeGenerarPreOrden();
            _botones["comprar"].Visible = ControlAcceso.PuedeRealizarCompra();
            _botones["recepcion"].Visible = ControlAcceso.PuedeRegistrarRecepcionInsumos();

            int x = 24;
            foreach (var b in _botones.Values.Where(b => b.Visible))
            {
                b.Location = new Point(x, 10);
                x += 166;
            }

            // Compras puede consultar el stock pero no cotizar
            bool puedeCotizar = ControlAcceso.PuedeRealizarCotizacion();
            btnRegistrarCotizacion.Enabled = numCantCotizar.Enabled = txtObsCotizacion.Enabled = dgvOfertas.Enabled = puedeCotizar;
            lblPermisoCotizar.Visible = !puedeCotizar;
            if (!puedeCotizar) { btnRegistrarCotizacion.BackColor = Tema.Borde; btnRegistrarCotizacion.ForeColor = Tema.TextoSuave; }
        }

        private void Mensaje(string texto, bool? ok)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok == null ? Tema.TextoSuave : (ok.Value ? Tema.Exito : Tema.Peligro);
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
            var l = new Label
            {
                Location = new Point(x, y), Size = new Size(w, h), ForeColor = Tema.Texto,
                Font = Tema.Fuente(9.5f, negrita ? FontStyle.Bold : FontStyle.Regular)
            };
            padre.Controls.Add(l);
            return l;
        }

        private static TextBox Caja(Control padre, int x, int y, int w, int max, int h = 26)
        {
            var t = new TextBox
            {
                Location = new Point(x, y), Size = new Size(w, h), MaxLength = max, Font = Tema.Fuente(10),
                BorderStyle = BorderStyle.FixedSingle, Multiline = h > 30
            };
            padre.Controls.Add(t);
            return t;
        }

        private static NumericUpDown Numero(Control padre, int x, int y, int w, int decimales, decimal max)
        {
            var n = new NumericUpDown
            {
                Location = new Point(x, y), Size = new Size(w, 26), DecimalPlaces = decimales,
                Minimum = 0, Maximum = max, ThousandsSeparator = true, Font = Tema.Fuente(10)
            };
            padre.Controls.Add(n);
            return n;
        }

        private static DataGridView Grilla(Control padre, int x, int y, int w, int h, params (string titulo, float peso)[] cols)
        {
            var g = new DataGridView
            {
                Location = new Point(x, y), Size = new Size(w, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Tema.EstilizarGrid(g);
            foreach (var (titulo, peso) in cols)
                g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
            padre.Controls.Add(g);
            return g;
        }

        private static void Fila(DataGridView g, object tag, params object[] valores)
        {
            int i = g.Rows.Add(valores);
            g.Rows[i].Tag = tag;
        }

        private static T Seleccion<T>(DataGridView g) where T : class
            => g.CurrentRow?.Tag as T;

        private static string Plural(int n, string uno, string varios) => n == 1 ? uno : varios;
        private static string N(decimal d) => d.ToString("0.##");
        private static string Plata(decimal d) => "$ " + d.ToString("N2");

        private static decimal LeerDecimal(object valor)
        {
            string s = (valor?.ToString() ?? "").Replace("$", "").Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal d)) return d;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d)) return d;
            return 0;
        }

        // ═════════════════════════════════════════════════════════
        // CUN06 · Stock y realizar cotización
        // ═════════════════════════════════════════════════════════
        private void ConstruirCotizacion()
        {
            var sec = NuevaSeccion("cotizar");
            var izq = Tema.Card("Stock de insumos", 24, 6, 560, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chkCriticos = new CheckBox { Text = "Mostrar solo insumos críticos", AutoSize = true, Location = new Point(18, 52), ForeColor = Tema.Texto };
            chkCriticos.CheckedChanged += (s, e) => CargarStock();
            var btnAct = new Button { Text = "Actualizar", Size = new Size(110, 30), Location = new Point(430, 46), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnAct);
            btnAct.Click += (s, e) => CargarStock();
            dgvStock = Grilla(izq, 18, 88, 524, 454, ("Insumo", 38), ("Stock", 15), ("Mínimo", 14), ("Unidad", 16), ("Estado", 17));
            dgvStock.SelectionChanged += (s, e) => StockSeleccionado();
            izq.Controls.Add(chkCriticos);
            izq.Controls.Add(btnAct);

            var der = Tema.Card("Realizar cotización", 604, 6, 490, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblInsumoSel = Texto(der, 18, 50, 454, 22, true);
            lblStockInfo = Texto(der, 18, 72, 454, 20);
            lblStockInfo.ForeColor = Tema.TextoSuave;
            Etiqueta(der, "Cantidad a cotizar", 18, 98);
            numCantCotizar = Numero(der, 18, 116, 160, 2, 9999999);
            numCantCotizar.ValueChanged += (s, e) => RecalcularOfertas();
            lblUltimoPrecio = Texto(der, 196, 120, 276, 22);
            lblUltimoPrecio.ForeColor = Tema.TextoSuave;

            Etiqueta(der, "Precios informados por cada proveedor (dejá en 0 los que no cotizaron)", 18, 152);
            dgvOfertas = Grilla(der, 18, 172, 454, 176, ("Proveedor", 42), ("Precio unit. ($)", 22), ("Plazo (días)", 16), ("Subtotal", 22));
            dgvOfertas.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvOfertas.ReadOnly = false;
            dgvOfertas.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvOfertas.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvOfertas.Columns[0].ReadOnly = true;
            dgvOfertas.Columns[3].ReadOnly = true;
            dgvOfertas.Columns[1].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 235);
            dgvOfertas.Columns[2].DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 235);
            dgvOfertas.CellEndEdit += (s, e) => RecalcularOfertas();
            dgvOfertas.DataError += (s, e) => { e.ThrowException = false; };

            lblMejorOferta = Texto(der, 18, 354, 454, 40, true);
            lblMejorOferta.ForeColor = Tema.Exito;
            Etiqueta(der, "Observaciones (opcional)", 18, 398);
            txtObsCotizacion = Caja(der, 18, 416, 454, 300, 44);
            btnRegistrarCotizacion = new Button { Text = "Registrar cotización", Location = new Point(18, 470), Size = new Size(454, 42) };
            Tema.BotonDorado(btnRegistrarCotizacion);
            btnRegistrarCotizacion.Click += (s, e) => RegistrarCotizacion();
            der.Controls.Add(btnRegistrarCotizacion);
            lblPermisoCotizar = Texto(der, 18, 518, 454, 36);
            lblPermisoCotizar.Text = "Su rol puede consultar el stock, pero la cotización la realiza el Encargado de Depósito.";
            lblPermisoCotizar.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarStock()
        {
            var r = _stock.ObtenerStockInsumos(chkCriticos.Checked);
            dgvStock.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var i in r.Dato)
            {
                Fila(dgvStock, i, i.Nombre, N(i.StockActual), N(i.StockMinimo), i.UnidadMedida, i.Estado == "Critico" ? "Crítico" : "Normal");
                var celda = dgvStock.Rows[dgvStock.Rows.Count - 1].Cells[4];
                celda.Style.ForeColor = i.Estado == "Critico" ? Tema.Peligro : Tema.Exito;
                celda.Style.Font = Tema.Fuente(9.5f, FontStyle.Bold);
            }

            // Una fila por proveedor activo para cargar lo que cotizó cada uno
            dgvOfertas.Rows.Clear();
            var pr = _proveedores.ObtenerProveedoresActivos();
            if (pr.Exito)
                foreach (var p in pr.Dato)
                    Fila(dgvOfertas, p, p.Nombre, "0", "7", "");

            int crit = r.Dato.Count(i => i.Estado == "Critico");
            Mensaje($"{r.Dato.Count} insumos · {crit} en estado crítico.", null);
            StockSeleccionado();
        }

        private void StockSeleccionado()
        {
            var i = Seleccion<InsumoBE>(dgvStock);
            if (i == null) { lblInsumoSel.Text = "(seleccioná un insumo de la lista)"; lblStockInfo.Text = ""; lblUltimoPrecio.Text = ""; return; }
            lblInsumoSel.Text = i.Nombre;
            lblStockInfo.Text = $"Stock {N(i.StockActual)} {i.UnidadMedida} · mínimo {N(i.StockMinimo)} · {(i.Estado == "Critico" ? "CRÍTICO" : "normal")}";
            decimal sugerida = Math.Max(1, i.StockMinimo * 2 - i.StockActual);
            numCantCotizar.Value = Math.Min(numCantCotizar.Maximum, sugerida);
            decimal? ultimo = _cotizaciones.ObtenerUltimoPrecio(i.IdInsumo);
            lblUltimoPrecio.Text = ultimo.HasValue ? $"Último precio pagado: {Plata(ultimo.Value)}" : "Sin compras anteriores de este insumo";
            RecalcularOfertas();
        }

        private List<CotizacionDetalleBE> OfertasCargadas()
        {
            var lista = new List<CotizacionDetalleBE>();
            foreach (DataGridViewRow row in dgvOfertas.Rows)
            {
                if (!(row.Tag is ProveedorBE p)) continue;
                decimal precio = LeerDecimal(row.Cells[1].Value);
                if (precio <= 0) continue;
                lista.Add(new CotizacionDetalleBE
                {
                    IdProveedor = p.IdProveedor,
                    NombreProveedor = p.Nombre,
                    PrecioUnitario = precio,
                    PlazoDias = (int)Math.Round(LeerDecimal(row.Cells[2].Value)),
                    Subtotal = Math.Round(precio * numCantCotizar.Value, 2)
                });
            }
            return lista;
        }

        private void RecalcularOfertas()
        {
            var ofertas = OfertasCargadas();
            var mejor = ofertas.OrderBy(o => o.PrecioUnitario).ThenBy(o => o.PlazoDias).FirstOrDefault();
            foreach (DataGridViewRow row in dgvOfertas.Rows)
            {
                var p = row.Tag as ProveedorBE;
                decimal precio = LeerDecimal(row.Cells[1].Value);
                row.Cells[3].Value = precio > 0 ? Plata(precio * numCantCotizar.Value) : "";
                bool esMejor = mejor != null && p != null && p.IdProveedor == mejor.IdProveedor;
                row.DefaultCellStyle.Font = Tema.Fuente(9.5f, esMejor ? FontStyle.Bold : FontStyle.Regular);
                row.Cells[0].Value = (p?.Nombre ?? "") + (esMejor ? "  ★" : "");
            }
            lblMejorOferta.Text = mejor == null ? "" :
                $"★ Más conveniente: {mejor.NombreProveedor} · {Plata(mejor.Subtotal)} · {mejor.PlazoDias} días" +
                (ofertas.Count < CotizacionBLL.MIN_PROVEEDORES ? $"\n(cargá al menos {CotizacionBLL.MIN_PROVEEDORES} proveedores)" : "");
        }

        private void RegistrarCotizacion()
        {
            dgvOfertas.EndEdit();
            var i = Seleccion<InsumoBE>(dgvStock);
            if (i == null) { Mensaje("✖ Seleccioná un insumo.", false); return; }
            var ofertas = OfertasCargadas();

            var r = _cotizaciones.RegistrarCotizacion(i.IdInsumo, numCantCotizar.Value, ofertas, txtObsCotizacion.Text);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                txtObsCotizacion.Clear();
                var c = r.Dato;
                string detalle = string.Join("\n", c.Detalles.Select(d =>
                    $"{(d.MasConveniente ? "★ " : "   ")}{d.NombreProveedor}: {Plata(d.PrecioUnitario)} c/u · {Plata(d.Subtotal)} · {d.PlazoDias} días"));
                MessageBox.Show(
                    $"Cotización {c.NroCotizacion}\n\nInsumo: {c.NombreInsumo}\nCantidad: {N(c.Cantidad)} {c.UnidadMedida}\n\n{detalle}\n\n" +
                    "Siguiente paso: generar la pre-orden con el proveedor elegido (sección Pre-orden).",
                    "Cotización registrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarStock();
            }
        }

        // ═════════════════════════════════════════════════════════
        // CUN07 · Generar pre-orden de compra
        // ═════════════════════════════════════════════════════════
        private void ConstruirPreOrden()
        {
            var sec = NuevaSeccion("preorden");
            var izq = Tema.Card("Cotizaciones abiertas", 24, 6, 650, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvCotizaciones = Grilla(izq, 18, 56, 614, 210, ("Nro", 14), ("Fecha", 19), ("Insumo", 27), ("Cantidad", 15), ("Prov.", 9), ("Mejor oferta", 18));
            dgvCotizaciones.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvCotizaciones.SelectionChanged += (s, e) => CotizacionSeleccionada();
            var lblComp = new Label { Text = "Comparación de ofertas (elegí el proveedor)", AutoSize = true, Font = Tema.Fuente(10.5f, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(18, 280) };
            izq.Controls.Add(lblComp);
            dgvComparacion = Grilla(izq, 18, 306, 614, 236, ("Proveedor", 36), ("Precio unit.", 18), ("Plazo", 12), ("Total", 20), ("", 14));
            dgvComparacion.SelectionChanged += (s, e) => OfertaSeleccionada();

            var der = Tema.Card("Generar pre-orden", 694, 6, 400, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetallePreOrden = Texto(der, 18, 52, 364, 150);
            Etiqueta(der, "Prioridad", 18, 210);
            cmbPrioridad = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(18, 228), Size = new Size(170, 26), Font = Tema.Fuente(10) };
            cmbPrioridad.Items.AddRange(new object[] { "Baja", "Normal", "Alta", "Urgente" });
            cmbPrioridad.SelectedItem = "Normal";
            der.Controls.Add(cmbPrioridad);
            Etiqueta(der, "Observaciones (obligatorias si no elegís la más conveniente)", 18, 266);
            txtObsPreOrden = Caja(der, 18, 284, 364, 300, 70);
            btnGenerarPreOrden = new Button { Text = "Generar pre-orden de compra", Location = new Point(18, 370), Size = new Size(364, 42) };
            Tema.BotonDorado(btnGenerarPreOrden);
            btnGenerarPreOrden.Click += (s, e) => GenerarPreOrden();
            der.Controls.Add(btnGenerarPreOrden);
            btnAnularCotizacion = new Button { Text = "Anular cotización", Location = new Point(18, 420), Size = new Size(364, 34) };
            Tema.BotonSecundario(btnAnularCotizacion);
            btnAnularCotizacion.Click += (s, e) => AnularCotizacion();
            der.Controls.Add(btnAnularCotizacion);
            var nota = Texto(der, 18, 466, 364, 80);
            nota.Text = "Al generar la pre-orden, Compras recibe un aviso para pagarla. Si el proveedor nunca cobró, Compras primero lo registra.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarCotizaciones()
        {
            var r = _cotizaciones.ObtenerCotizacionesAbiertas();
            dgvCotizaciones.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var c in r.Dato)
            {
                var m = c.Detalles.FirstOrDefault(d => d.MasConveniente);
                Fila(dgvCotizaciones, c, c.NroCotizacion, c.FechaCotizacion.ToString("dd/MM/yyyy HH:mm"), c.NombreInsumo,
                     $"{N(c.Cantidad)} {c.UnidadMedida}", c.Detalles.Count, m == null ? "-" : Plata(m.Subtotal));
            }
            Mensaje(r.Dato.Count == 0 ? "No hay cotizaciones abiertas. Cotizá un insumo desde Stock y cotización."
                                      : $"{r.Dato.Count} {Plural(r.Dato.Count, "cotización abierta", "cotizaciones abiertas")}.", null);
            CotizacionSeleccionada();
        }

        private void CotizacionSeleccionada()
        {
            var c = Seleccion<CotizacionBE>(dgvCotizaciones);
            dgvComparacion.Rows.Clear();
            btnAnularCotizacion.Enabled = c != null;
            if (c == null) { lblDetallePreOrden.Text = "Seleccioná una cotización."; btnGenerarPreOrden.Enabled = false; return; }
            foreach (var d in c.Detalles)
            {
                Fila(dgvComparacion, d, d.NombreProveedor, Plata(d.PrecioUnitario), d.PlazoDias + " d", Plata(d.Subtotal), d.MasConveniente ? "★ mejor" : "");
                if (d.MasConveniente)
                {
                    var row = dgvComparacion.Rows[dgvComparacion.Rows.Count - 1];
                    row.DefaultCellStyle.Font = Tema.Fuente(9.5f, FontStyle.Bold);
                    row.Cells[4].Style.ForeColor = Tema.Exito;
                }
            }
            if (dgvComparacion.Rows.Count > 0) dgvComparacion.CurrentCell = dgvComparacion.Rows[0].Cells[0];
            OfertaSeleccionada();
        }

        private void OfertaSeleccionada()
        {
            var c = Seleccion<CotizacionBE>(dgvCotizaciones);
            var d = Seleccion<CotizacionDetalleBE>(dgvComparacion);
            btnGenerarPreOrden.Enabled = c != null && d != null;
            if (c == null || d == null) return;
            lblDetallePreOrden.Text =
                $"{c.NroCotizacion} · {c.NombreInsumo}\n{N(c.Cantidad)} {c.UnidadMedida}\n\n" +
                $"Proveedor elegido: {d.NombreProveedor}\nPrecio unitario: {Plata(d.PrecioUnitario)}\nPlazo: {d.PlazoDias} días\n" +
                $"Total: {Plata(d.Subtotal)}" + (d.MasConveniente ? "\n★ Es la oferta más conveniente" : "\n⚠ No es la más conveniente: justificalo");
        }

        private void GenerarPreOrden()
        {
            var c = Seleccion<CotizacionBE>(dgvCotizaciones);
            var d = Seleccion<CotizacionDetalleBE>(dgvComparacion);
            if (c == null || d == null) { Mensaje("✖ Seleccioná una cotización y el proveedor elegido.", false); return; }

            if (MessageBox.Show($"¿Generar la pre-orden de {N(c.Cantidad)} {c.UnidadMedida} de {c.NombreInsumo} con {d.NombreProveedor} por {Plata(d.Subtotal)}?",
                    "Confirmar pre-orden", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = _preordenes.GenerarPreOrden(c.IdCotizacion, d.IdDetalleCotizacion, cmbPrioridad.SelectedItem?.ToString() ?? "Normal", txtObsPreOrden.Text);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                txtObsPreOrden.Clear();
                var po = r.Dato;
                MessageBox.Show(
                    $"Pre-orden {po.NroPreOrden}\n\nInsumo: {po.NombreInsumo}\nCantidad: {N(po.Cantidad)} {po.UnidadMedida}\nProveedor: {po.NombreProveedor}\n" +
                    $"Total: {Plata(po.PrecioTotal)}\nPrioridad: {po.Prioridad}\n\nCompras fue notificado para realizar la compra.",
                    "Pre-orden generada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            CargarCotizaciones();
            ActualizarContadorAvisos();
        }

        private void AnularCotizacion()
        {
            var c = Seleccion<CotizacionBE>(dgvCotizaciones);
            if (c == null) return;
            string motivo = txtObsPreOrden.Text.Trim();
            if (motivo.Length == 0) { Mensaje("✖ Escribí en observaciones el motivo de la anulación.", false); txtObsPreOrden.Focus(); return; }
            if (MessageBox.Show($"¿Anular la cotización {c.NroCotizacion} ({c.NombreInsumo})?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            var r = _cotizaciones.AnularCotizacion(c.IdCotizacion, motivo);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito) txtObsPreOrden.Clear();
            CargarCotizaciones();
        }

        // ═════════════════════════════════════════════════════════
        // CUN09 · Realizar compra (CUN08 · Registrar proveedor nuevo la extiende)
        // ═════════════════════════════════════════════════════════
        private void ConstruirComprar()
        {
            var sec = NuevaSeccion("comprar");
            var izq = Tema.Card("Pre-órdenes pendientes de pago", 24, 6, 600, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvPreOrdenes = Grilla(izq, 18, 56, 564, 486, ("Nro", 13), ("Insumo", 21), ("Cantidad", 14), ("Proveedor", 25), ("Total", 18), ("Prioridad", 11));
            dgvPreOrdenes.SelectionChanged += (s, e) => PreOrdenSeleccionada();

            var der = Tema.Card("Compra de insumos", 644, 6, 450, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleCompra = Texto(der, 18, 50, 414, 58, true);
            lblEstadoProveedor = Texto(der, 18, 110, 414, 40, true);

            // CUN08: solo visible si el proveedor nunca cobró
            pnlRegistroProveedor = new Panel { Location = new Point(18, 152), Size = new Size(414, 176), BackColor = Color.FromArgb(253, 246, 232) };
            Etiqueta(pnlRegistroProveedor, "CUIT", 10, 6);
            txtCuit = Caja(pnlRegistroProveedor, 10, 24, 190, 13);
            Etiqueta(pnlRegistroProveedor, "CBU (22 dígitos)", 212, 6);
            txtCbu = Caja(pnlRegistroProveedor, 212, 24, 192, 22);
            txtCbu.TextChanged += (s, e) =>
            {
                string b = SERVICIOS_HRS_480.BancoServicio.BancoDelCBU(txtCbu.Text);
                if (b.Length > 0) txtBanco.Text = b;
            };
            Etiqueta(pnlRegistroProveedor, "Banco", 10, 58);
            txtBanco = Caja(pnlRegistroProveedor, 10, 76, 190, 100);
            Etiqueta(pnlRegistroProveedor, "Correo (si no lo tiene)", 212, 58);
            txtEmailProv = Caja(pnlRegistroProveedor, 212, 76, 192, 200);
            btnRegistrarProveedor = new Button { Text = "Registrar proveedor nuevo", Location = new Point(10, 120), Size = new Size(394, 40) };
            Tema.BotonPrimario(btnRegistrarProveedor);
            btnRegistrarProveedor.Click += (s, e) => RegistrarProveedor();
            pnlRegistroProveedor.Controls.Add(btnRegistrarProveedor);
            der.Controls.Add(pnlRegistroProveedor);

            Etiqueta(der, "Dirección de entrega", 18, 338);
            txtDireccion = Caja(der, 18, 356, 414, 300);
            txtDireccion.Text = OrdenCompraBLL.DIRECCION_ENTREGA_DEFECTO;
            lblTotalCompra = Texto(der, 18, 392, 414, 30, true);
            lblTotalCompra.Font = Tema.Fuente(13, FontStyle.Bold);
            lblTotalCompra.ForeColor = Tema.Marino;
            btnPagar = new Button { Text = "Pagar por transferencia y generar orden", Location = new Point(18, 428), Size = new Size(414, 44) };
            Tema.BotonDorado(btnPagar);
            btnPagar.Click += (s, e) => Pagar();
            der.Controls.Add(btnPagar);
            var nota = Texto(der, 18, 480, 414, 70);
            nota.Text = "El sistema se conecta con el banco y transfiere el total al CBU del proveedor. Con el pago aprobado " +
                        "se genera la orden de compra y se le envía al proveedor por correo junto con el comprobante.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarPreOrdenes()
        {
            var r = _preordenes.ObtenerPreOrdenesPendientes();
            dgvPreOrdenes.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var p in r.Dato)
            {
                Fila(dgvPreOrdenes, p, p.NroPreOrden, p.NombreInsumo, $"{N(p.Cantidad)} {p.UnidadMedida}",
                     p.NombreProveedor + (p.ProveedorNuevo ? " (nuevo)" : ""), Plata(p.PrecioTotal), p.Prioridad);
                if (p.ProveedorNuevo) dgvPreOrdenes.Rows[dgvPreOrdenes.Rows.Count - 1].Cells[3].Style.ForeColor = Tema.Peligro;
            }
            Mensaje(r.Dato.Count == 0 ? "No hay pre-órdenes pendientes de pago."
                                      : $"{r.Dato.Count} {Plural(r.Dato.Count, "pre-orden espera", "pre-órdenes esperan")} el pago (primero las más urgentes).", null);
            PreOrdenSeleccionada();
        }

        private void PreOrdenSeleccionada()
        {
            var p = Seleccion<PreOrdenCompraBE>(dgvPreOrdenes);
            if (p == null)
            {
                lblDetalleCompra.Text = "Seleccioná una pre-orden.";
                lblEstadoProveedor.Text = lblTotalCompra.Text = "";
                pnlRegistroProveedor.Visible = false;
                btnPagar.Enabled = false;
                return;
            }
            lblDetalleCompra.Text = $"{p.NroPreOrden} · {p.NombreInsumo}\n{N(p.Cantidad)} {p.UnidadMedida} × {Plata(p.PrecioUnitario)} · {p.PlazoDias} días";
            lblTotalCompra.Text = "Total a pagar: " + Plata(p.PrecioTotal);

            var prov = _proveedores.ObtenerProveedor(p.IdProveedor);
            bool nuevo = !prov.Exito || prov.Dato.EsNuevo;
            pnlRegistroProveedor.Visible = nuevo && ControlAcceso.PuedeRegistrarProveedor();
            btnPagar.Enabled = !nuevo;
            if (nuevo)
            {
                lblEstadoProveedor.ForeColor = Tema.Peligro;
                lblEstadoProveedor.Text = "PROVEEDOR NUEVO (nunca se le pagó):\nregistrá su CUIT y CBU para poder pagarle.";
                txtCuit.Text = prov.Exito ? prov.Dato.CUIT : "";
                txtCbu.Text = prov.Exito ? prov.Dato.CBU : "";
                txtEmailProv.Text = prov.Exito ? prov.Dato.Email : "";
            }
            else
            {
                lblEstadoProveedor.ForeColor = Tema.Exito;
                lblEstadoProveedor.Text = $"✔ {prov.Dato.Nombre} ya está registrado para pagos.\nCBU {prov.Dato.CBU}{(string.IsNullOrWhiteSpace(prov.Dato.Banco) ? "" : " · " + prov.Dato.Banco)}";
            }
        }

        private void RegistrarProveedor()
        {
            var p = Seleccion<PreOrdenCompraBE>(dgvPreOrdenes);
            if (p == null) return;
            var r = _proveedores.RegistrarProveedorNuevo(new ProveedorBE
            {
                IdProveedor = p.IdProveedor,
                CUIT = txtCuit.Text,
                CBU = txtCbu.Text,
                Banco = txtBanco.Text,
                Email = txtEmailProv.Text
            });
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito) CargarPreOrdenes();
        }

        private void Pagar()
        {
            var p = Seleccion<PreOrdenCompraBE>(dgvPreOrdenes);
            if (p == null) { Mensaje("✖ Seleccioná una pre-orden.", false); return; }

            if (MessageBox.Show(
                    $"Se transferirán {Plata(p.PrecioTotal)} a {p.NombreProveedor} y se generará la orden de compra:\n\n" +
                    $"{N(p.Cantidad)} {p.UnidadMedida} de {p.NombreInsumo}\nPrecio unitario {Plata(p.PrecioUnitario)} · Plazo {p.PlazoDias} días\n\n¿Confirmás el pago?",
                    "Confirmar compra", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = _ordenes.RealizarCompra(p.IdPreOrden, txtDireccion.Text);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
                MessageBox.Show(
                    $"Orden {r.Dato.NroOrden}\n\nProveedor: {r.Dato.NombreProveedor}\nInsumo: {r.Dato.NombreInsumo}\nCantidad: {N(r.Dato.Cantidad)}\n" +
                    $"Total pagado: {Plata(r.Dato.PrecioTotal)}\nComprobante: {r.Dato.NroOperacionPago}\n\n{r.Mensaje}",
                    "Compra realizada", MessageBoxButtons.OK, r.Dato.FechaEnvio.HasValue ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            else
                MessageBox.Show(r.Mensaje, "No se realizó la compra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            CargarPreOrdenes();
        }

        // ═════════════════════════════════════════════════════════
        // CUN10 · Registrar insumos recibidos (con la factura del proveedor)
        // ═════════════════════════════════════════════════════════
        private void ConstruirRecepcion()
        {
            var sec = NuevaSeccion("recepcion");
            var izq = Tema.Card("Órdenes pagadas esperando los insumos", 24, 6, 640, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvARecibir = Grilla(izq, 18, 56, 604, 486, ("Orden", 13), ("Pagada", 14), ("Proveedor", 24), ("Insumo", 22), ("Pedido", 11), ("Entrega hasta", 16));
            dgvARecibir.SelectionChanged += (s, e) => ARecibirSeleccionada();

            var der = Tema.Card("Registrar insumos recibidos", 684, 6, 410, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleRecepcion = Texto(der, 18, 48, 374, 84);
            var lblFac = new Label { Text = "Factura del proveedor", AutoSize = true, Font = Tema.Fuente(10, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(18, 134) };
            der.Controls.Add(lblFac);
            Etiqueta(der, "Número de factura", 18, 158);
            txtNroFactura = Caja(der, 18, 176, 180, 30);
            Etiqueta(der, "Fecha de la factura", 212, 158);
            dtpFechaFactura = new DateTimePicker { Location = new Point(212, 176), Size = new Size(180, 26), Format = DateTimePickerFormat.Short, Font = Tema.Fuente(10) };
            der.Controls.Add(dtpFechaFactura);
            Etiqueta(der, "Cantidad facturada", 18, 210);
            numCantFacturada = Numero(der, 18, 228, 180, 2, 9999999);
            Etiqueta(der, "Precio unitario facturado ($)", 212, 210);
            numPrecioFacturado = Numero(der, 212, 228, 180, 2, 99999999);
            Etiqueta(der, "Cantidad que realmente llegó", 18, 262);
            numRecibida = Numero(der, 18, 280, 180, 2, 9999999);
            numCantFacturada.ValueChanged += (s, e) => ControlarRecepcion();
            numPrecioFacturado.ValueChanged += (s, e) => ControlarRecepcion();
            numRecibida.ValueChanged += (s, e) => ControlarRecepcion();
            lblControl = Texto(der, 18, 312, 374, 62, true);
            Etiqueta(der, "Observaciones (opcional)", 18, 378);
            txtObsRecepcion = Caja(der, 18, 396, 374, 300, 48);
            btnRegistrarRecepcion = new Button { Text = "Registrar insumos recibidos", Location = new Point(18, 452), Size = new Size(374, 42) };
            Tema.BotonDorado(btnRegistrarRecepcion);
            btnRegistrarRecepcion.Click += (s, e) => RegistrarRecepcion();
            der.Controls.Add(btnRegistrarRecepcion);
            var nota = Texto(der, 18, 500, 374, 52);
            nota.Text = "El stock se actualiza con lo que realmente llegó. Si no coincide con la factura o la orden, se avisa a Compras.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarARecibir()
        {
            var r = _recepciones.ObtenerOrdenesPendientesDeRecepcion();
            dgvARecibir.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var o in r.Dato)
                Fila(dgvARecibir, o, o.NroOrden, (o.FechaPago ?? o.FechaEmision).ToString("dd/MM/yyyy"), o.NombreProveedor, o.NombreInsumo, N(o.Cantidad),
                     o.PlazoDias.HasValue ? o.FechaEmision.AddDays(o.PlazoDias.Value).ToString("dd/MM/yyyy") : "-");
            Mensaje(r.Dato.Count == 0 ? "No hay órdenes esperando insumos." : $"{r.Dato.Count} {Plural(r.Dato.Count, "orden espera", "órdenes esperan")} los insumos y la factura.", null);
            ARecibirSeleccionada();
        }

        private void ARecibirSeleccionada()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            btnRegistrarRecepcion.Enabled = o != null;
            if (o == null) { lblDetalleRecepcion.Text = "Seleccioná una orden."; lblControl.Text = ""; return; }
            lblDetalleRecepcion.Text =
                $"{o.NroOrden} · {o.NombreProveedor}\nInsumo: {o.NombreInsumo}\nPedido: {N(o.Cantidad)} × {Plata(o.PrecioUnitario)} = {Plata(o.PrecioTotal)}\n" +
                (string.IsNullOrWhiteSpace(o.NroOperacionPago) ? "" : $"Pago: {o.NroOperacionPago}");
            // Por defecto, lo que dice la orden (el depósito corrige lo que no coincida)
            numCantFacturada.Value = Math.Min(numCantFacturada.Maximum, o.Cantidad);
            numPrecioFacturado.Value = Math.Min(numPrecioFacturado.Maximum, o.PrecioUnitario);
            numRecibida.Value = Math.Min(numRecibida.Maximum, o.Cantidad);
            dtpFechaFactura.Value = DateTime.Today;
            txtNroFactura.Clear();
            ControlarRecepcion();
        }

        /// <summary>Vista previa del control que hace la BLL: orden vs factura vs mercadería.</summary>
        private void ControlarRecepcion()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            if (o == null) return;
            var dif = new List<string>();
            if (numCantFacturada.Value != o.Cantidad) dif.Add($"factura {N(numCantFacturada.Value)} vs orden {N(o.Cantidad)}");
            if (numPrecioFacturado.Value != o.PrecioUnitario) dif.Add($"precio {Plata(numPrecioFacturado.Value)} vs pagado {Plata(o.PrecioUnitario)}");
            if (numRecibida.Value != numCantFacturada.Value) dif.Add($"llegaron {N(numRecibida.Value)} vs facturadas {N(numCantFacturada.Value)}");
            lblControl.ForeColor = dif.Count == 0 ? Tema.Exito : Tema.Peligro;
            lblControl.Text = dif.Count == 0 ? "✔ Los productos coinciden con la factura y la orden." : "✖ No coincide: " + string.Join("; ", dif) + ".";
        }

        private void RegistrarRecepcion()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            if (o == null) { Mensaje("✖ Seleccioná una orden.", false); return; }

            bool coincide = lblControl.ForeColor == Tema.Exito;
            if (MessageBox.Show($"¿Registrar la factura {txtNroFactura.Text.Trim()} y {N(numRecibida.Value)} de {o.NombreInsumo} recibidos (orden {o.NroOrden})?" +
                                (coincide ? "" : "\n\nATENCIÓN: " + lblControl.Text),
                    "Confirmar recepción", MessageBoxButtons.YesNo, coincide ? MessageBoxIcon.Question : MessageBoxIcon.Warning) != DialogResult.Yes) return;

            var r = _recepciones.RegistrarInsumosRecibidos(new RecepcionConFacturaBE
            {
                IdOrden = o.IdOrden,
                NroFactura = txtNroFactura.Text,
                FechaFactura = dtpFechaFactura.Value.Date,
                CantidadFacturada = numCantFacturada.Value,
                PrecioUnitarioFacturado = numPrecioFacturado.Value,
                CantidadRecibida = numRecibida.Value,
                Observaciones = txtObsRecepcion.Text
            });
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                txtObsRecepcion.Clear();
                MessageBox.Show(r.Mensaje, "Insumos registrados", MessageBoxButtons.OK,
                    r.Dato.Coincide ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            CargarARecibir();
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
            cmbFiltroOrden.Items.AddRange(new object[] { "Todas", "Emitida", "Recibida", "Con diferencia" });
            cmbFiltroOrden.SelectedIndex = 0;
            cmbFiltroOrden.SelectedIndexChanged += (s, e) => CargarOrdenes();
            card.Controls.Add(cmbFiltroOrden);
            btnReenviar = new Button { Text = "Reenviar al proveedor", Size = new Size(190, 30), Location = new Point(862, 14), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnReenviar);
            btnReenviar.Click += (s, e) => Reenviar();
            card.Controls.Add(btnReenviar);
            btnExportarListadoOrdenes = new Button { Text = "Listado (PDF)", Size = new Size(150, 30), Location = new Point(448, 14), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnExportarListadoOrdenes);
            btnExportarListadoOrdenes.Click += (s, e) => ExportarListadoOrdenes();
            card.Controls.Add(btnExportarListadoOrdenes);
            btnExportarOrden = new Button { Text = "Exportar orden (PDF)", Size = new Size(200, 30), Location = new Point(646, 14), Anchor = AnchorStyles.Top | AnchorStyles.Right, Enabled = false };
            Tema.BotonDorado(btnExportarOrden);
            btnExportarOrden.Click += (s, e) => ExportarOrden();
            card.Controls.Add(btnExportarOrden);
            dgvOrdenes = Grilla(card, 18, 56, 1034, 486, ("Orden", 8), ("Emisión", 8), ("Pre-orden", 8), ("Proveedor", 15), ("Insumo", 13), ("Cant.", 5), ("Total", 9), ("Pago", 18), ("Estado", 10), ("Enviada", 8));
            dgvOrdenes.SelectionChanged += (s, e) =>
            {
                var o = Seleccion<OrdenCompraBE>(dgvOrdenes);
                btnReenviar.Enabled = o != null && o.Estado == "Emitida" && ControlAcceso.PuedeGestionarCompras();
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
                Fila(dgvOrdenes, o, o.NroOrden, o.FechaEmision.ToString("dd/MM/yyyy"), string.IsNullOrWhiteSpace(o.NroPreOrden) ? "-" : o.NroPreOrden,
                     o.NombreProveedor, o.NombreInsumo, N(o.Cantidad), Plata(o.PrecioTotal),
                     string.IsNullOrWhiteSpace(o.NroOperacionPago) ? "-" : o.NroOperacionPago, o.Estado,
                     o.FechaEnvio.HasValue ? o.FechaEnvio.Value.ToString("dd/MM HH:mm") : "NO enviada");
            btnExportarOrden.Enabled = Seleccion<OrdenCompraBE>(dgvOrdenes) != null;
            Mensaje($"{r.Dato.Count} {Plural(r.Dato.Count, "orden", "órdenes")}.", null);
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
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            CargarOrdenes();
        }

        private void ConstruirAvisos()
        {
            var sec = NuevaSeccion("avisos");
            var card = Tema.Card("Mis avisos", 24, 6, 1070, 560);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnMarcarLeidas = new Button { Text = "Marcar todos como leídos", Size = new Size(210, 30), Location = new Point(842, 14), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnMarcarLeidas);
            btnMarcarLeidas.Click += (s, e) => { _avisos.MarcarTodasLeidas(); CargarAvisos(); ActualizarContadorAvisos(); };
            card.Controls.Add(btnMarcarLeidas);
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
