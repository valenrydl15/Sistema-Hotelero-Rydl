#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// Pantalla del proceso PN2 "Gestión de reposición de insumos" (la "GUI" de los DSS CUN06 a CUN11).
    /// Una sección por caso de uso: stock/solicitud, validar, comprar (incluye seleccionar proveedor),
    /// recepción y reembolso; más órdenes y avisos. Solo habla con las BLL.
    /// </summary>
    public class FrmReposicion : Form
    {
        private readonly StockBLL _stock = new StockBLL();
        private readonly SolicitudCompraBLL _solicitudes = new SolicitudCompraBLL();
        private readonly ProveedorBLL _proveedores = new ProveedorBLL();
        private readonly OrdenCompraBLL _ordenes = new OrdenCompraBLL();
        private readonly RecepcionBLL _recepciones = new RecepcionBLL();
        private readonly ReembolsoBLL _reembolsos = new ReembolsoBLL();
        private readonly NotificacionBLL _avisos = new NotificacionBLL();

        // Estructura
        private Panel pnlHeader, pnlSecciones;
        private Label lblTitulo, lblUsuario, lblMensaje;
        private readonly Dictionary<string, Panel> _paneles = new Dictionary<string, Panel>();
        private readonly Dictionary<string, Button> _botones = new Dictionary<string, Button>();
        private string _actual = "stock";

        // CUN06 Stock + solicitud
        private DataGridView dgvStock;
        private CheckBox chkCriticos;
        private Label lblInsumoSel, lblStockInfo, lblPermisoSolicitud;
        private NumericUpDown numCantSolicitud;
        private ComboBox cmbPrioridad;
        private TextBox txtObsSolicitud;
        private Button btnGenerarSolicitud;

        // CUN07 Validar
        private DataGridView dgvPendientes;
        private Label lblDetallePendiente;
        private TextBox txtMotivoValidacion;
        private Button btnAprobar, btnRechazar;

        // CUN08 / CUN09 Comprar
        private DataGridView dgvAprobadas;
        private Label lblDetalleCompra, lblCondiciones, lblTotalCompra;
        private ComboBox cmbProveedor;
        private NumericUpDown numPrecio, numPlazo;
        private TextBox txtDireccion;
        private Button btnEmitirOrden;

        // CUN10 Recepción
        private DataGridView dgvARecibir;
        private Label lblDetalleRecepcion;
        private NumericUpDown numRecibida;
        private TextBox txtObsRecepcion;
        private Button btnRegistrarRecepcion;

        // CUN11 Reembolso
        private DataGridView dgvConDiferencia, dgvReembolsos;
        private Label lblDetalleReembolso;
        private TextBox txtMotivoReembolso;
        private Button btnRegistrarReembolso;

        // Órdenes y avisos
        private DataGridView dgvOrdenes, dgvAvisos;
        private ComboBox cmbFiltroOrden;
        private Button btnReenviar, btnMarcarLeidas, btnExportarOrden, btnExportarListadoOrdenes;
        private readonly ReporteBLL _reportes = new ReporteBLL();

        public FrmReposicion()
        {
            Construir();
            Load += (s, e) =>
            {
                lblUsuario.Text = $"{SesionManager.Instancia.UsuarioActual?.Nombre} {SesionManager.Instancia.UsuarioActual?.Apellido}  ·  {SesionManager.Instancia.UsuarioActual?.NombreRol}";
                AplicarPermisos();
                IrA("stock");
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
                ("stock", "Stock y solicitud"), ("validar", "Validar"), ("comprar", "Comprar"),
                ("recepcion", "Recepción"), ("reembolso", "Reembolsos"), ("ordenes", "Órdenes"), ("avisos", "Avisos")
            };
            int x = 24;
            foreach (var (clave, texto) in def)
            {
                var b = new Button { Text = texto, Size = new Size(142, 38), Location = new Point(x, 10) };
                string k = clave;
                b.Click += (s, e) => IrA(k);
                pnlSecciones.Controls.Add(b);
                _botones[clave] = b;
                x += 148;
            }

            ConstruirStock();
            ConstruirValidar();
            ConstruirComprar();
            ConstruirRecepcion();
            ConstruirReembolso();
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
            _actual = clave;
            foreach (var kv in _paneles) kv.Value.Visible = kv.Key == clave;
            foreach (var kv in _botones)
            {
                if (kv.Key == clave) Tema.BotonPrimario(kv.Value);
                else Tema.BotonSecundario(kv.Value);
            }
            Mensaje("", null);
            switch (clave)
            {
                case "stock": CargarStock(); break;
                case "validar": CargarPendientes(); break;
                case "comprar": CargarAprobadas(); break;
                case "recepcion": CargarARecibir(); break;
                case "reembolso": CargarReembolsos(); break;
                case "ordenes": CargarOrdenes(); break;
                case "avisos": CargarAvisos(); break;
            }
            ActualizarContadorAvisos();
        }

        private void AplicarPermisos()
        {
            bool compras = ControlAcceso.PuedeGestionarCompras();
            _botones["validar"].Visible = compras;
            _botones["comprar"].Visible = compras;
            _botones["reembolso"].Visible = compras;
            _botones["recepcion"].Visible = ControlAcceso.PuedeRegistrarRecepcionInsumos();

            // Reacomoda los botones visibles
            int x = 24;
            foreach (var b in _botones.Values.Where(b => b.Visible))
            {
                b.Location = new Point(x, 10);
                x += 148;
            }

            bool puedeSolicitar = ControlAcceso.PuedeGenerarSolicitud();
            btnGenerarSolicitud.Enabled = puedeSolicitar;
            numCantSolicitud.Enabled = cmbPrioridad.Enabled = txtObsSolicitud.Enabled = puedeSolicitar;
            lblPermisoSolicitud.Visible = !puedeSolicitar;
            if (!puedeSolicitar) { btnGenerarSolicitud.BackColor = Tema.Borde; btnGenerarSolicitud.ForeColor = Tema.TextoSuave; }
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

        // ═════════════════════════════════════════════════════════
        // CUN06 · Stock y generar solicitud
        // ═════════════════════════════════════════════════════════
        private void ConstruirStock()
        {
            var sec = NuevaSeccion("stock");
            var izq = Tema.Card("Stock de insumos", 24, 6, 650, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chkCriticos = new CheckBox { Text = "Mostrar solo insumos críticos", AutoSize = true, Location = new Point(18, 52), ForeColor = Tema.Texto };
            chkCriticos.CheckedChanged += (s, e) => CargarStock();
            var btnAct = new Button { Text = "Actualizar", Size = new Size(110, 30), Location = new Point(520, 46), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnAct);
            btnAct.Click += (s, e) => CargarStock();
            dgvStock = Grilla(izq, 18, 88, 614, 454, ("Insumo", 38), ("Stock actual", 17), ("Mínimo", 14), ("Unidad", 15), ("Estado", 16));
            dgvStock.SelectionChanged += (s, e) => StockSeleccionado();
            izq.Controls.Add(chkCriticos);
            izq.Controls.Add(btnAct);

            var der = Tema.Card("Nueva solicitud de compra", 694, 6, 400, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            Etiqueta(der, "Insumo seleccionado", 18, 56);
            lblInsumoSel = Texto(der, 18, 74, 364, 24, true);
            lblStockInfo = Texto(der, 18, 98, 364, 40);
            lblStockInfo.ForeColor = Tema.TextoSuave;
            Etiqueta(der, "Cantidad a solicitar", 18, 148);
            numCantSolicitud = Numero(der, 18, 168, 170, 2, 9999999);
            Etiqueta(der, "Prioridad", 208, 148);
            cmbPrioridad = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(208, 168), Size = new Size(174, 26), Font = Tema.Fuente(10) };
            cmbPrioridad.Items.AddRange(new object[] { "Baja", "Normal", "Alta", "Urgente" });
            cmbPrioridad.SelectedItem = "Normal";
            der.Controls.Add(cmbPrioridad);
            Etiqueta(der, "Observaciones (opcional)", 18, 210);
            txtObsSolicitud = new TextBox { Location = new Point(18, 230), Size = new Size(364, 90), Multiline = true, MaxLength = 300, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            der.Controls.Add(txtObsSolicitud);
            btnGenerarSolicitud = new Button { Text = "Generar solicitud de compra", Location = new Point(18, 340), Size = new Size(364, 42) };
            Tema.BotonDorado(btnGenerarSolicitud);
            btnGenerarSolicitud.Click += (s, e) => GenerarSolicitud();
            der.Controls.Add(btnGenerarSolicitud);
            lblPermisoSolicitud = Texto(der, 18, 392, 364, 40);
            lblPermisoSolicitud.Text = "Su rol puede consultar el stock, pero no generar solicitudes.";
            lblPermisoSolicitud.ForeColor = Tema.TextoSuave;
            Texto(der, 18, 450, 364, 90).Text =
                "Al generar la solicitud, el área de Compras recibe un aviso para validarla. " +
                "No se puede pedir un insumo que ya tiene una solicitud pendiente o aprobada.";
            der.Controls[der.Controls.Count - 1].ForeColor = Tema.TextoSuave;

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
            int crit = r.Dato.Count(i => i.Estado == "Critico");
            Mensaje($"{r.Dato.Count} insumos · {crit} en estado crítico.", null);
            StockSeleccionado();
        }

        private void StockSeleccionado()
        {
            var i = Seleccion<InsumoBE>(dgvStock);
            if (i == null) { lblInsumoSel.Text = "(seleccioná un insumo de la lista)"; lblStockInfo.Text = ""; return; }
            lblInsumoSel.Text = i.Nombre;
            lblStockInfo.Text = $"Stock {N(i.StockActual)} {i.UnidadMedida} · mínimo {N(i.StockMinimo)} · {(i.Estado == "Critico" ? "CRÍTICO" : "normal")}";
            decimal sugerida = Math.Max(1, i.StockMinimo * 2 - i.StockActual);
            numCantSolicitud.Value = Math.Min(numCantSolicitud.Maximum, sugerida);
            cmbPrioridad.SelectedItem = i.Estado == "Critico" ? "Alta" : "Normal";
        }

        private void GenerarSolicitud()
        {
            var i = Seleccion<InsumoBE>(dgvStock);
            if (i == null) { Mensaje("Seleccioná un insumo.", false); return; }

            var r = _solicitudes.GenerarSolicitud(new SolicitudCompraBE
            {
                IdInsumo = i.IdInsumo,
                CantidadSolicitada = numCantSolicitud.Value,
                Prioridad = cmbPrioridad.SelectedItem?.ToString() ?? "Normal",
                Observaciones = txtObsSolicitud.Text
            });
            Mensaje(r.Exito ? $"✔ SC-{r.Dato.IdSolicitud:D4}: {r.Mensaje}" : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                txtObsSolicitud.Clear();
                MessageBox.Show(
                    $"Solicitud SC-{r.Dato.IdSolicitud:D4}\n\nInsumo: {r.Dato.NombreInsumo}\nCantidad: {N(r.Dato.CantidadSolicitada)} {r.Dato.UnidadMedida}\n" +
                    $"Fecha: {r.Dato.FechaSolicitud:dd/MM/yyyy HH:mm}\nPrioridad: {r.Dato.Prioridad}\n\nCompras fue notificado.",
                    "Solicitud generada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // CUN07 · Validar solicitud
        // ═════════════════════════════════════════════════════════
        private void ConstruirValidar()
        {
            var sec = NuevaSeccion("validar");
            var izq = Tema.Card("Solicitudes pendientes de validación", 24, 6, 700, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvPendientes = Grilla(izq, 18, 56, 664, 486, ("Nro", 11), ("Fecha", 19), ("Insumo", 22), ("Cantidad", 17), ("Prioridad", 12), ("Presupuesto", 15), ("Solicitó", 14));
            dgvPendientes.SelectionChanged += (s, e) => PendienteSeleccionada();

            var der = Tema.Card("Decisión", 744, 6, 350, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetallePendiente = Texto(der, 18, 56, 314, 150);
            Etiqueta(der, "Motivo (obligatorio si se rechaza)", 18, 220);
            txtMotivoValidacion = new TextBox { Location = new Point(18, 240), Size = new Size(314, 90), Multiline = true, MaxLength = 250, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            der.Controls.Add(txtMotivoValidacion);
            btnAprobar = new Button { Text = "Aprobar solicitud", Location = new Point(18, 350), Size = new Size(314, 42) };
            Tema.BotonPrimario(btnAprobar); btnAprobar.BackColor = Tema.Exito;
            btnAprobar.Click += (s, e) => Validar(true);
            btnRechazar = new Button { Text = "Rechazar solicitud", Location = new Point(18, 402), Size = new Size(314, 42) };
            Tema.BotonPrimario(btnRechazar); btnRechazar.BackColor = Tema.Peligro;
            btnRechazar.Click += (s, e) => Validar(false);
            der.Controls.Add(btnAprobar);
            der.Controls.Add(btnRechazar);

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarPendientes()
        {
            var r = _solicitudes.ObtenerSolicitudesPendientes();
            dgvPendientes.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var s in r.Dato)
                Fila(dgvPendientes, s, $"SC-{s.IdSolicitud:D4}", s.FechaSolicitud.ToString("dd/MM/yyyy HH:mm"), s.NombreInsumo,
                     $"{N(s.CantidadSolicitada)} {s.UnidadMedida}", s.Prioridad,
                     s.PresupuestoEstimado.HasValue ? Plata(s.PresupuestoEstimado.Value) : "sin dato", s.NombreUsuarioSolic);
            Mensaje(r.Dato.Count == 0 ? "No hay solicitudes pendientes." : $"{r.Dato.Count} {Plural(r.Dato.Count, "solicitud pendiente", "solicitudes pendientes")} (primero las más urgentes).", null);
            PendienteSeleccionada();
        }

        private void PendienteSeleccionada()
        {
            var s = Seleccion<SolicitudCompraBE>(dgvPendientes);
            btnAprobar.Enabled = btnRechazar.Enabled = s != null;
            lblDetallePendiente.Text = s == null ? "Seleccioná una solicitud." :
                $"SC-{s.IdSolicitud:D4}\n\nInsumo: {s.NombreInsumo}\nCantidad: {N(s.CantidadSolicitada)} {s.UnidadMedida}\n" +
                $"Prioridad: {s.Prioridad}\nPresupuesto estimado: {(s.PresupuestoEstimado.HasValue ? Plata(s.PresupuestoEstimado.Value) : "sin historial de precios")}\n" +
                $"Solicitó: {s.NombreUsuarioSolic}" + (string.IsNullOrWhiteSpace(s.Observaciones) ? "" : $"\nObs.: {s.Observaciones}");
        }

        private void Validar(bool aprobar)
        {
            var s = Seleccion<SolicitudCompraBE>(dgvPendientes);
            if (s == null) return;
            string motivo = txtMotivoValidacion.Text.Trim();
            if (!aprobar && motivo.Length == 0) { Mensaje("✖ Para rechazar indicá el motivo.", false); txtMotivoValidacion.Focus(); return; }

            if (MessageBox.Show($"¿{(aprobar ? "Aprobar" : "Rechazar")} la solicitud SC-{s.IdSolicitud:D4} ({s.NombreInsumo})?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = _solicitudes.ValidarSolicitud(s.IdSolicitud, aprobar, motivo);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                if (!aprobar) _avisos.NotificarRechazo(s.IdSolicitud, motivo);   // CUN07: rechazo → aviso al solicitante
                txtMotivoValidacion.Clear();
            }
            CargarPendientes();
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // CUN08 · Realizar compra (incluye CUN09 · Seleccionar proveedor)
        // ═════════════════════════════════════════════════════════
        private void ConstruirComprar()
        {
            var sec = NuevaSeccion("comprar");
            var izq = Tema.Card("Solicitudes aprobadas (sin orden de compra)", 24, 6, 600, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvAprobadas = Grilla(izq, 18, 56, 564, 486, ("Nro", 13), ("Insumo", 30), ("Cantidad", 22), ("Prioridad", 14), ("Presupuesto", 21));
            dgvAprobadas.SelectionChanged += (s, e) => AprobadaSeleccionada();

            var der = Tema.Card("Orden de compra", 644, 6, 450, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleCompra = Texto(der, 18, 52, 414, 56, true);
            Etiqueta(der, "Proveedor", 18, 116);
            cmbProveedor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(18, 136), Size = new Size(414, 26), Font = Tema.Fuente(10) };
            cmbProveedor.SelectedIndexChanged += (s, e) =>
            {
                var p = cmbProveedor.SelectedItem as ProveedorBE;
                lblCondiciones.Text = p == null ? "" : $"Pago: {p.CondicionesPago}  ·  {p.Email}";
            };
            der.Controls.Add(cmbProveedor);
            lblCondiciones = Texto(der, 18, 166, 414, 22);
            lblCondiciones.ForeColor = Tema.TextoSuave;
            Etiqueta(der, "Precio unitario ($)", 18, 200);
            numPrecio = Numero(der, 18, 220, 200, 2, 99999999);
            numPrecio.ValueChanged += (s, e) => RecalcularTotalCompra();
            Etiqueta(der, "Plazo de entrega (días)", 238, 200);
            numPlazo = Numero(der, 238, 220, 194, 0, 365);
            numPlazo.Value = 7;
            Etiqueta(der, "Dirección de entrega", 18, 262);
            txtDireccion = new TextBox { Location = new Point(18, 282), Size = new Size(414, 26), MaxLength = 300, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle, Text = OrdenCompraBLL.DIRECCION_ENTREGA_DEFECTO };
            der.Controls.Add(txtDireccion);
            lblTotalCompra = Texto(der, 18, 326, 414, 30, true);
            lblTotalCompra.Font = Tema.Fuente(13, FontStyle.Bold);
            lblTotalCompra.ForeColor = Tema.Marino;
            btnEmitirOrden = new Button { Text = "Emitir orden y enviar al proveedor", Location = new Point(18, 372), Size = new Size(414, 44) };
            Tema.BotonDorado(btnEmitirOrden);
            btnEmitirOrden.Click += (s, e) => EmitirOrden();
            der.Controls.Add(btnEmitirOrden);
            var nota = Texto(der, 18, 432, 414, 100);
            nota.Text = "La orden se envía por correo al proveedor con la dirección de entrega. " +
                        "Si el correo no está configurado (correo.txt) el sistema lo simula y guarda una copia en la carpeta Correos_Enviados.";
            nota.ForeColor = Tema.TextoSuave;

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarAprobadas()
        {
            var r = _solicitudes.ObtenerSolicitudesAprobadas();
            dgvAprobadas.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var s in r.Dato)
                Fila(dgvAprobadas, s, $"SC-{s.IdSolicitud:D4}", s.NombreInsumo, $"{N(s.CantidadSolicitada)} {s.UnidadMedida}", s.Prioridad,
                     s.PresupuestoEstimado.HasValue ? Plata(s.PresupuestoEstimado.Value) : "sin dato");

            var pr = _proveedores.ObtenerProveedoresActivos();
            cmbProveedor.Items.Clear();
            if (pr.Exito) foreach (var p in pr.Dato) cmbProveedor.Items.Add(p);
            if (cmbProveedor.Items.Count > 0) cmbProveedor.SelectedIndex = 0;

            Mensaje(r.Dato.Count == 0 ? "No hay solicitudes aprobadas para comprar." : $"{r.Dato.Count} {Plural(r.Dato.Count, "solicitud aprobada espera", "solicitudes aprobadas esperan")} su orden de compra.", null);
            AprobadaSeleccionada();
        }

        private void AprobadaSeleccionada()
        {
            var s = Seleccion<SolicitudCompraBE>(dgvAprobadas);
            btnEmitirOrden.Enabled = s != null;
            lblDetalleCompra.Text = s == null ? "Seleccioná una solicitud aprobada."
                : $"SC-{s.IdSolicitud:D4} · {s.NombreInsumo}\n{N(s.CantidadSolicitada)} {s.UnidadMedida}";
            if (s?.PresupuestoEstimado != null && s.CantidadSolicitada > 0)
            {
                decimal unit = Math.Round(s.PresupuestoEstimado.Value / s.CantidadSolicitada, 2);
                if (unit > 0 && unit <= numPrecio.Maximum) numPrecio.Value = unit;   // precio de la última compra como punto de partida
            }
            RecalcularTotalCompra();
        }

        private void RecalcularTotalCompra()
        {
            var s = Seleccion<SolicitudCompraBE>(dgvAprobadas);
            lblTotalCompra.Text = s == null ? "" : "Total: " + Plata(s.CantidadSolicitada * numPrecio.Value);
        }

        private void EmitirOrden()
        {
            var s = Seleccion<SolicitudCompraBE>(dgvAprobadas);
            var p = cmbProveedor.SelectedItem as ProveedorBE;
            if (s == null) { Mensaje("✖ Seleccioná una solicitud aprobada.", false); return; }
            if (p == null) { Mensaje("✖ Seleccioná un proveedor.", false); return; }

            if (MessageBox.Show(
                    $"Se emitirá una orden de compra a {p.Nombre}:\n\n{N(s.CantidadSolicitada)} {s.UnidadMedida} de {s.NombreInsumo}\n" +
                    $"Precio unitario {Plata(numPrecio.Value)} · Total {Plata(s.CantidadSolicitada * numPrecio.Value)}\nPlazo: {numPlazo.Value:0} días\n\n¿Confirmás?",
                    "Confirmar compra", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = _ordenes.GenerarOrdenCompra(s.IdSolicitud, p.IdProveedor, numPrecio.Value, (int)numPlazo.Value, txtDireccion.Text);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
                MessageBox.Show(
                    $"Orden {r.Dato.NroOrden}\n\nProveedor: {r.Dato.NombreProveedor}\nInsumo: {r.Dato.NombreInsumo}\nCantidad: {N(r.Dato.Cantidad)}\n" +
                    $"Precio total: {Plata(r.Dato.PrecioTotal)}\nEmisión: {r.Dato.FechaEmision:dd/MM/yyyy HH:mm}\n\n{r.Mensaje}",
                    "Compra realizada", MessageBoxButtons.OK, r.Dato.FechaEnvio.HasValue ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            CargarAprobadas();
        }

        // ═════════════════════════════════════════════════════════
        // CUN10 · Registrar recepción de insumos
        // ═════════════════════════════════════════════════════════
        private void ConstruirRecepcion()
        {
            var sec = NuevaSeccion("recepcion");
            var izq = Tema.Card("Órdenes de compra esperando mercadería", 24, 6, 680, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvARecibir = Grilla(izq, 18, 56, 644, 486, ("Orden", 13), ("Emisión", 16), ("Proveedor", 25), ("Insumo", 22), ("Pedido", 12), ("Entrega hasta", 16));
            dgvARecibir.SelectionChanged += (s, e) => ARecibirSeleccionada();

            var der = Tema.Card("Registrar recepción", 724, 6, 370, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleRecepcion = Texto(der, 18, 52, 334, 120);
            Etiqueta(der, "Cantidad realmente recibida", 18, 184);
            numRecibida = Numero(der, 18, 204, 200, 2, 9999999);
            Etiqueta(der, "Observaciones (opcional)", 18, 246);
            txtObsRecepcion = new TextBox { Location = new Point(18, 266), Size = new Size(334, 80), Multiline = true, MaxLength = 300, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            der.Controls.Add(txtObsRecepcion);
            btnRegistrarRecepcion = new Button { Text = "Registrar recepción", Location = new Point(18, 366), Size = new Size(334, 42) };
            Tema.BotonDorado(btnRegistrarRecepcion);
            btnRegistrarRecepcion.Click += (s, e) => RegistrarRecepcion();
            der.Controls.Add(btnRegistrarRecepcion);
            var nota = Texto(der, 18, 424, 334, 110);
            nota.Text = "Cada orden se recibe una sola vez. El stock se actualiza con lo realmente recibido; " +
                        "si difiere de lo pedido, Compras recibe un aviso y puede pedir el reembolso.";
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
                Fila(dgvARecibir, o, o.NroOrden, o.FechaEmision.ToString("dd/MM/yyyy"), o.NombreProveedor, o.NombreInsumo, N(o.Cantidad),
                     o.PlazoDias.HasValue ? o.FechaEmision.AddDays(o.PlazoDias.Value).ToString("dd/MM/yyyy") : "-");
            Mensaje(r.Dato.Count == 0 ? "No hay órdenes esperando mercadería." : $"{r.Dato.Count} {Plural(r.Dato.Count, "orden espera", "órdenes esperan")} mercadería.", null);
            ARecibirSeleccionada();
        }

        private void ARecibirSeleccionada()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            btnRegistrarRecepcion.Enabled = o != null;
            lblDetalleRecepcion.Text = o == null ? "Seleccioná una orden." :
                $"{o.NroOrden}\n\nProveedor: {o.NombreProveedor}\nInsumo: {o.NombreInsumo}\nCantidad pedida: {N(o.Cantidad)}\nTotal: {Plata(o.PrecioTotal)}";
            if (o != null) numRecibida.Value = Math.Min(numRecibida.Maximum, o.Cantidad);   // por defecto, lo pedido
        }

        private void RegistrarRecepcion()
        {
            var o = Seleccion<OrdenCompraBE>(dgvARecibir);
            if (o == null) { Mensaje("✖ Seleccioná una orden.", false); return; }

            string aviso = numRecibida.Value == o.Cantidad ? "" : $"\n\nATENCIÓN: se pidieron {N(o.Cantidad)} y está registrando {N(numRecibida.Value)}.";
            if (MessageBox.Show($"¿Registrar la recepción de {N(numRecibida.Value)} de {o.NombreInsumo} (orden {o.NroOrden})?{aviso}",
                    "Confirmar recepción", MessageBoxButtons.YesNo, aviso.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = _recepciones.RegistrarRecepcion(new RecepcionInsumoBE
            {
                IdOrden = o.IdOrden,
                CantidadRecibida = numRecibida.Value,
                Observaciones = txtObsRecepcion.Text
            });
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito)
            {
                // CUN10: si lo recibido difiere de lo pedido, se avisa a Compras
                if (r.Dato.Estado == "Con diferencia")
                    _avisos.NotificarDiferenciaRecepcion(o.IdOrden, r.Dato.CantidadRecibida);
                txtObsRecepcion.Clear();
                MessageBox.Show(r.Mensaje + (r.Dato.Estado == "Con diferencia" ? "\n\nSe avisó a Compras de la diferencia." : ""),
                    "Recepción registrada", MessageBoxButtons.OK,
                    r.Dato.Estado == "Con diferencia" ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            CargarARecibir();
            ActualizarContadorAvisos();
        }

        // ═════════════════════════════════════════════════════════
        // CUN11 · Registrar solicitud de reembolso
        // ═════════════════════════════════════════════════════════
        private void ConstruirReembolso()
        {
            var sec = NuevaSeccion("reembolso");
            var izq = Tema.Card("Órdenes recibidas con diferencia", 24, 6, 700, 560);
            izq.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvConDiferencia = Grilla(izq, 18, 56, 664, 210, ("Orden", 14), ("Proveedor", 30), ("Insumo", 28), ("Pedido", 12), ("Precio unit.", 16));
            dgvConDiferencia.SelectionChanged += (s, e) => ConDiferenciaSeleccionada();
            dgvConDiferencia.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var lblHist = new Label { Text = "Reembolsos registrados", AutoSize = true, Font = Tema.Fuente(10.5f, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(18, 282) };
            izq.Controls.Add(lblHist);
            dgvReembolsos = Grilla(izq, 18, 308, 664, 234, ("Fecha", 15), ("Orden", 12), ("Insumo", 22), ("Faltante", 11), ("Monto", 15), ("Estado", 12), ("Motivo", 30));

            var der = Tema.Card("Solicitar reembolso", 744, 6, 350, 560);
            der.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblDetalleReembolso = Texto(der, 18, 52, 314, 170);
            Etiqueta(der, "Motivo del reembolso", 18, 236);
            txtMotivoReembolso = new TextBox { Location = new Point(18, 256), Size = new Size(314, 90), Multiline = true, MaxLength = 300, Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            der.Controls.Add(txtMotivoReembolso);
            btnRegistrarReembolso = new Button { Text = "Registrar solicitud de reembolso", Location = new Point(18, 366), Size = new Size(314, 42) };
            Tema.BotonDorado(btnRegistrarReembolso);
            btnRegistrarReembolso.Click += (s, e) => RegistrarReembolso();
            der.Controls.Add(btnRegistrarReembolso);

            sec.Controls.Add(izq);
            sec.Controls.Add(der);
        }

        private void CargarReembolsos()
        {
            var r = _reembolsos.ObtenerOrdenesConDiferencia();
            dgvConDiferencia.Rows.Clear();
            if (!r.Exito) { Mensaje(r.Mensaje, false); return; }
            foreach (var o in r.Dato)
                Fila(dgvConDiferencia, o, o.NroOrden, o.NombreProveedor, o.NombreInsumo, N(o.Cantidad), Plata(o.PrecioUnitario));

            dgvReembolsos.Rows.Clear();
            var h = _reembolsos.ObtenerReembolsos();
            if (h.Exito)
                foreach (var x in h.Dato)
                    Fila(dgvReembolsos, x, x.FechaReembolso.ToString("dd/MM/yyyy"), x.NroOrden, x.NombreInsumo, N(x.Diferencia), Plata(x.MontoReembolso), x.Estado, x.Motivo);

            Mensaje(r.Dato.Count == 0 ? "No hay órdenes con diferencia pendientes de reembolso." : $"{r.Dato.Count} {Plural(r.Dato.Count, "orden con diferencia sin reembolso", "órdenes con diferencia sin reembolso")}.", null);
            ConDiferenciaSeleccionada();
        }

        private void ConDiferenciaSeleccionada()
        {
            var o = Seleccion<OrdenCompraBE>(dgvConDiferencia);
            btnRegistrarReembolso.Enabled = o != null;
            if (o == null) { lblDetalleReembolso.Text = "Seleccioná una orden con diferencia."; return; }
            decimal recibida = _reembolsos.ObtenerCantidadRecibida(o.IdOrden);
            decimal falta = o.Cantidad - recibida;
            lblDetalleReembolso.Text =
                $"{o.NroOrden} · {o.NombreProveedor}\n\nInsumo: {o.NombreInsumo}\nComprado: {N(o.Cantidad)}\nRecibido: {N(recibida)}\n" +
                (falta > 0 ? $"Faltante: {N(falta)}\nReembolso estimado: {Plata(falta * o.PrecioUnitario)}"
                           : "Se recibió de más: no corresponde reembolso.");
        }

        private void RegistrarReembolso()
        {
            var o = Seleccion<OrdenCompraBE>(dgvConDiferencia);
            if (o == null) return;
            var r = _reembolsos.RegistrarSolicitudReembolso(o.IdOrden, txtMotivoReembolso.Text);
            Mensaje(r.Exito ? "✔ " + r.Mensaje : "✖ " + r.Mensaje, r.Exito);
            if (r.Exito) txtMotivoReembolso.Clear();
            CargarReembolsos();
        }

        // ═════════════════════════════════════════════════════════
        // Órdenes (consulta, reenvío) y Avisos
        // ═════════════════════════════════════════════════════════
        private void ConstruirOrdenes()
        {
            var sec = NuevaSeccion("ordenes");
            var card = Tema.Card("Órdenes de compra", 24, 6, 1070, 560);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Etiqueta(card, "Estado", 200, 20);
            cmbFiltroOrden = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(252, 15), Size = new Size(170, 26), Font = Tema.Fuente(10) };
            cmbFiltroOrden.Items.AddRange(new object[] { "Todas", "Emitida", "Recibida", "Con diferencia", "Con reembolso" });
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
            dgvOrdenes = Grilla(card, 18, 56, 1034, 486, ("Orden", 9), ("Emisión", 11), ("Proveedor", 17), ("Insumo", 15), ("Cantidad", 8), ("P. unit.", 9), ("Total", 10), ("Plazo", 6), ("Estado", 11), ("Enviada", 11));
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
                Fila(dgvOrdenes, o, o.NroOrden, o.FechaEmision.ToString("dd/MM/yyyy"), o.NombreProveedor, o.NombreInsumo, N(o.Cantidad),
                     Plata(o.PrecioUnitario), Plata(o.PrecioTotal), o.PlazoDias.HasValue ? o.PlazoDias + " d" : "-", o.Estado,
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
