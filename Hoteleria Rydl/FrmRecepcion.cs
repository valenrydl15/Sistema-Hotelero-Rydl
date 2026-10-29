using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// Pantalla única de Recepción (la "GUI" del DSS de CUN01).
    /// Check-IN (con selección de habitación, datos del cliente y cobro)
    /// y Check-OUT en el mismo formulario. Solo habla con CheckInBLL;
    /// la BLL es quien lanza los casos de uso incluidos/extendidos.
    /// </summary>
    public class FrmRecepcion : Form
    {
        private readonly CheckInBLL _bll = new CheckInBLL();

        private List<HabitacionBE> _disponibles = new List<HabitacionBE>();
        private HabitacionBE _habitacionSel;
        private List<ReservaBE> _activas = new List<ReservaBE>();
        private ReservaBE _reservaSel;

        // Encabezado / modo
        private Panel pnlHeader, pnlModo, pnlCheckIn, pnlCheckOut;
        private Label lblTitulo, lblUsuario, lblMensaje;
        private Button btnModoCheckIn, btnModoCheckOut, btnReporteFacturas, btnListadoFacturas;

        // Check-IN
        private Panel cardHab, cardCliente, cardCobro;
        private DateTimePicker dtpIngreso, dtpSalida;
        private Button btnBuscar, btnConfirmarCheckIn, btnLimpiar;
        private DataGridView dgvHabitaciones;
        private Label lblHabSel;
        private TextBox txtNombre, txtApellido, txtDNI, txtEmail, txtTelefono, txtNacionalidad, txtObservaciones;
        private Label lblClienteInfo;
        private bool _clienteExistente;
        private ComboBox cmbMetodoPago;
        private TextBox txtNumeroTarjeta, txtCodigoSeguridad;
        private Label lblMontoHab, lblImpuestos, lblTotal;

        // Check-OUT
        private DataGridView dgvActivas;
        private Label lblCoHuesped, lblCoHab, lblCoEstadia, lblCoMonto, lblCoImpuestos, lblCoTotal;
        private Button btnRealizarCheckOut, btnActualizar;

        public FrmRecepcion()
        {
            Construir();
            Load += FrmRecepcion_Load;
        }

        // ═════════════════════════════════════════════════════════
        // Construcción de la interfaz
        // ═════════════════════════════════════════════════════════
        private void Construir()
        {
            SuspendLayout();
            Text = "Hotel Rydl — Recepción";
            ClientSize = new Size(1120, 740);
            MinimumSize = new Size(1136, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);

            // ── Encabezado ──
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.Marino };
            lblTitulo = new Label
            {
                Text = "Recepción", ForeColor = Color.White, AutoSize = true,
                Font = new Font("Georgia", 20f, FontStyle.Regular), Location = new Point(24, 16)
            };
            lblUsuario = new Label
            {
                ForeColor = Color.FromArgb(205, 214, 230), AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight, Font = Tema.Fuente(9.5f),
                Dock = DockStyle.Right, Width = 440, Padding = new Padding(0, 0, 24, 0)
            };
            pnlHeader.Controls.Add(lblTitulo);
            pnlHeader.Controls.Add(lblUsuario);
            var linea = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Tema.Dorado };
            pnlHeader.Controls.Add(linea);

            // ── Selector de modo ──
            pnlModo = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Tema.Fondo };
            btnModoCheckIn = new Button { Text = "Check-IN", Size = new Size(170, 38), Location = new Point(24, 10) };
            btnModoCheckOut = new Button { Text = "Check-OUT", Size = new Size(170, 38), Location = new Point(200, 10) };
            btnModoCheckIn.Click += (s, e) => SetModo(true);
            btnModoCheckOut.Click += (s, e) => SetModo(false);
            pnlModo.Controls.Add(btnModoCheckIn);
            pnlModo.Controls.Add(btnModoCheckOut);
            btnListadoFacturas = new Button { Text = "Reporte de todas las facturas (PDF)", Size = new Size(260, 38), Location = new Point(576, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonDorado(btnListadoFacturas);
            btnListadoFacturas.Click += (s, e) => ExportarTodasLasFacturas();
            pnlModo.Controls.Add(btnListadoFacturas);
            btnReporteFacturas = new Button { Text = "Buscar / exportar factura", Size = new Size(230, 38), Location = new Point(846, 10), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.BotonSecundario(btnReporteFacturas);
            btnReporteFacturas.Click += (s, e) => { using (var f = new FrmReporteFacturas()) f.ShowDialog(this); };
            pnlModo.Controls.Add(btnReporteFacturas);

            // ── Mensaje inferior ──
            lblMensaje = new Label
            {
                Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0), Font = Tema.Fuente(9.5f, FontStyle.Bold),
                ForeColor = Tema.TextoSuave, BackColor = Color.White
            };

            ConstruirCheckIn();
            ConstruirCheckOut();

            // El orden de Controls.Add define el apilado de los Dock
            Controls.Add(pnlCheckIn);
            Controls.Add(pnlCheckOut);
            Controls.Add(lblMensaje);
            Controls.Add(pnlModo);
            Controls.Add(pnlHeader);
            ResumeLayout(false);
        }

        private Label Etiqueta(Control padre, string texto, int x, int y)
        {
            var l = new Label { Text = texto, AutoSize = true, ForeColor = Tema.TextoSuave, Font = Tema.Fuente(8.5f), Location = new Point(x, y) };
            padre.Controls.Add(l);
            return l;
        }

        private TextBox Campo(Control padre, string etiqueta, int x, int y, int ancho)
        {
            Etiqueta(padre, etiqueta, x, y);
            var t = new TextBox { Location = new Point(x, y + 20), Size = new Size(ancho, 26), Font = Tema.Fuente(10), BorderStyle = BorderStyle.FixedSingle };
            padre.Controls.Add(t);
            return t;
        }

        private void ConstruirCheckIn()
        {
            pnlCheckIn = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Tema.Fondo };

            // ── 1. Fechas y habitación ──
            cardHab = Tema.Card("1  ·  Fechas y habitación", 24, 6, 600, 566);
            Etiqueta(cardHab, "Ingreso", 18, 56);
            dtpIngreso = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(18, 76), Size = new Size(150, 26) };
            Etiqueta(cardHab, "Salida", 188, 56);
            dtpSalida = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(188, 76), Size = new Size(150, 26) };
            btnBuscar = new Button { Text = "Ver disponibilidad", Location = new Point(360, 70), Size = new Size(222, 36) };
            Tema.BotonPrimario(btnBuscar);
            btnBuscar.Click += btnBuscar_Click;
            dtpIngreso.ValueChanged += (s, e) => FechasCambiaron();
            dtpSalida.ValueChanged += (s, e) => FechasCambiaron();

            dgvHabitaciones = new DataGridView { Location = new Point(18, 124), Size = new Size(564, 380) };
            dgvHabitaciones.Columns.Add("Numero", "Hab.");
            dgvHabitaciones.Columns.Add("Tipo", "Tipo");
            dgvHabitaciones.Columns.Add("Piso", "Piso");
            dgvHabitaciones.Columns.Add("Precio", "Precio/noche");
            Tema.EstilizarGrid(dgvHabitaciones);
            dgvHabitaciones.SelectionChanged += dgvHabitaciones_SelectionChanged;

            lblHabSel = new Label
            {
                Text = "Buscá disponibilidad y elegí una habitación.", AutoSize = false,
                Location = new Point(18, 516), Size = new Size(564, 36),
                Font = Tema.Fuente(10, FontStyle.Bold), ForeColor = Tema.TextoSuave,
                TextAlign = ContentAlignment.MiddleLeft
            };
            cardHab.Controls.AddRange(new Control[] { dtpIngreso, dtpSalida, btnBuscar, dgvHabitaciones, lblHabSel });

            // ── 2. Datos del cliente ──
            cardCliente = Tema.Card("2  ·  Datos del cliente", 644, 6, 440, 318);
            txtNombre = Campo(cardCliente, "Nombre", 18, 56, 190);
            txtApellido = Campo(cardCliente, "Apellido", 226, 56, 190);
            txtDNI = Campo(cardCliente, "DNI", 18, 112, 190);
            txtTelefono = Campo(cardCliente, "Teléfono", 226, 112, 190);
            txtEmail = Campo(cardCliente, "Email", 18, 168, 190);
            txtNacionalidad = Campo(cardCliente, "Nacionalidad", 226, 168, 190);
            txtObservaciones = Campo(cardCliente, "Observaciones", 18, 224, 398);
            lblClienteInfo = new Label
            {
                AutoSize = false, Location = new Point(18, 278), Size = new Size(398, 30),
                Font = Tema.Fuente(9, FontStyle.Bold), ForeColor = Tema.TextoSuave,
                Text = "Ingresá el DNI: si el huésped ya está registrado se cargan sus datos."
            };
            cardCliente.Controls.Add(lblClienteInfo);

            // Autocompletar por DNI (al salir del campo o con Enter)
            txtDNI.Leave += (s, e) => BuscarCliente();
            txtDNI.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarCliente(); }
            };
            txtDNI.TextChanged += (s, e) => { if (_clienteExistente) LiberarDatosCliente(true); };

            // ── 3. Cobro ──
            cardCobro = Tema.Card("3  ·  Cobro y confirmación", 644, 336, 440, 236);
            Etiqueta(cardCobro, "Método de pago", 18, 54);
            cmbMetodoPago = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(18, 74), Size = new Size(190, 26), Font = Tema.Fuente(10) };
            cmbMetodoPago.Items.AddRange(new object[] { "Tarjeta de crédito", "Tarjeta de débito", "Transferencia" });
            cmbMetodoPago.SelectedIndex = 0;
            cardCobro.Controls.Add(cmbMetodoPago);
            txtNumeroTarjeta = Campo(cardCobro, "N° de tarjeta", 226, 54, 190);
            txtNumeroTarjeta.MaxLength = 19;
            txtCodigoSeguridad = Campo(cardCobro, "Código de seguridad", 18, 106, 190);
            txtCodigoSeguridad.MaxLength = 4;
            txtCodigoSeguridad.UseSystemPasswordChar = true;

            lblMontoHab = new Label { AutoSize = true, Font = Tema.Fuente(9.5f), ForeColor = Tema.Texto, Location = new Point(226, 110) };
            lblImpuestos = new Label { AutoSize = true, Font = Tema.Fuente(9.5f), ForeColor = Tema.Texto, Location = new Point(226, 132) };
            lblTotal = new Label { AutoSize = true, Font = Tema.Fuente(13, FontStyle.Bold), ForeColor = Tema.Marino, Location = new Point(226, 156) };
            cardCobro.Controls.AddRange(new Control[] { lblMontoHab, lblImpuestos, lblTotal });

            btnConfirmarCheckIn = new Button { Text = "Confirmar Check-IN", Location = new Point(18, 172), Size = new Size(190, 44) };
            Tema.BotonDorado(btnConfirmarCheckIn);
            btnConfirmarCheckIn.Click += btnConfirmarCheckIn_Click;
            btnLimpiar = new Button { Text = "Limpiar", Location = new Point(226, 186), Size = new Size(190, 30) };
            Tema.BotonSecundario(btnLimpiar);
            btnLimpiar.Click += (s, e) => ReiniciarCheckIn(true);
            cardCobro.Controls.Add(btnConfirmarCheckIn);
            cardCobro.Controls.Add(btnLimpiar);

            pnlCheckIn.Controls.AddRange(new Control[] { cardHab, cardCliente, cardCobro });
        }

        private void ConstruirCheckOut()
        {
            pnlCheckOut = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Tema.Fondo, Visible = false };

            var cardLista = Tema.Card("Huéspedes alojados", 24, 6, 640, 566);
            dgvActivas = new DataGridView { Location = new Point(18, 56), Size = new Size(604, 440) };
            dgvActivas.Columns.Add("Reserva", "Reserva");
            dgvActivas.Columns.Add("Huesped", "Huésped");
            dgvActivas.Columns.Add("DNI", "DNI");
            dgvActivas.Columns.Add("Hab", "Hab.");
            dgvActivas.Columns.Add("Salida", "Salida");
            Tema.EstilizarGrid(dgvActivas);
            dgvActivas.SelectionChanged += dgvActivas_SelectionChanged;
            btnActualizar = new Button { Text = "Actualizar lista", Location = new Point(18, 510), Size = new Size(160, 36) };
            Tema.BotonSecundario(btnActualizar);
            btnActualizar.Click += (s, e) => CargarActivas();
            cardLista.Controls.AddRange(new Control[] { dgvActivas, btnActualizar });

            var cardResumen = Tema.Card("Resumen del Check-OUT", 684, 6, 400, 566);
            lblCoHuesped = ResumenLabel(cardResumen, 60, 11, true);
            lblCoHab = ResumenLabel(cardResumen, 100, 10, false);
            lblCoEstadia = ResumenLabel(cardResumen, 128, 10, false);
            lblCoMonto = ResumenLabel(cardResumen, 190, 10.5f, false);
            lblCoImpuestos = ResumenLabel(cardResumen, 218, 10.5f, false);
            lblCoTotal = ResumenLabel(cardResumen, 262, 16, true);
            lblCoTotal.ForeColor = Tema.Marino;

            btnRealizarCheckOut = new Button { Text = "Realizar Check-OUT", Location = new Point(18, 480), Size = new Size(364, 48), Enabled = false };
            Tema.BotonDorado(btnRealizarCheckOut);
            btnRealizarCheckOut.Click += btnRealizarCheckOut_Click;
            cardResumen.Controls.Add(btnRealizarCheckOut);

            pnlCheckOut.Controls.AddRange(new Control[] { cardLista, cardResumen });
        }

        private Label ResumenLabel(Control padre, int y, float size, bool negrita)
        {
            var l = new Label
            {
                AutoSize = false, Location = new Point(18, y), Size = new Size(364, size > 12 ? 40 : 26),
                Font = Tema.Fuente(size, negrita ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Tema.Texto
            };
            padre.Controls.Add(l);
            return l;
        }

        // ═════════════════════════════════════════════════════════
        // Carga / modo
        // ═════════════════════════════════════════════════════════
        private void FrmRecepcion_Load(object sender, EventArgs e)
        {
            var u = SesionManager.Instancia.UsuarioActual;

            // Solo Recepcionista y Administrador (la BLL lo vuelve a validar)
            if (!ControlAcceso.PuedeOperarRecepcion())
            {
                MessageBox.Show("No tenés permiso para acceder a Recepción.\nSolo Recepcionista o Administrador.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            lblUsuario.Text = u == null ? "" : u.Nombre + " " + u.Apellido + "   ·   " + u.NombreRol;

            dtpIngreso.MinDate = DateTime.Today;
            dtpIngreso.Value = DateTime.Today;
            dtpSalida.MinDate = DateTime.Today.AddDays(1);
            dtpSalida.Value = DateTime.Today.AddDays(1);

            ReiniciarCheckIn(false);
            SetModo(true);
        }

        private void SetModo(bool checkIn)
        {
            pnlCheckIn.Visible = checkIn;
            pnlCheckOut.Visible = !checkIn;

            if (checkIn) { Tema.BotonPrimario(btnModoCheckIn); Tema.BotonSecundario(btnModoCheckOut); }
            else { Tema.BotonSecundario(btnModoCheckIn); Tema.BotonPrimario(btnModoCheckOut); }

            if (!checkIn) CargarActivas();
            Msg(checkIn ? "Elegí las fechas y buscá disponibilidad." : "Seleccioná al huésped que se retira.", null);
        }

        // ═════════════════════════════════════════════════════════
        // CHECK-IN
        // ═════════════════════════════════════════════════════════
        private void FechasCambiaron()
        {
            if (dtpSalida == null || dtpIngreso == null) return;
            if (dtpSalida.Value.Date <= dtpIngreso.Value.Date)
            {
                dtpSalida.MinDate = dtpIngreso.Value.Date.AddDays(1);
                if (dtpSalida.Value.Date <= dtpIngreso.Value.Date)
                    dtpSalida.Value = dtpIngreso.Value.Date.AddDays(1);
            }
            else
            {
                dtpSalida.MinDate = dtpIngreso.Value.Date.AddDays(1);
            }
            // Si cambian las fechas, la lista de disponibles ya no es válida
            if (_disponibles.Count > 0 || _habitacionSel != null)
                ReiniciarCheckIn(false);
        }

        private void ReiniciarCheckIn(bool limpiarCampos)
        {
            _disponibles = new List<HabitacionBE>();
            _habitacionSel = null;
            dgvHabitaciones.Rows.Clear();
            lblHabSel.Text = "Buscá disponibilidad y elegí una habitación.";
            lblHabSel.ForeColor = Tema.TextoSuave;
            cardCliente.Enabled = false;
            cardCobro.Enabled = false;
            lblMontoHab.Text = lblImpuestos.Text = lblTotal.Text = "";

            if (limpiarCampos)
            {
                LiberarDatosCliente(false);
                foreach (var t in new[] { txtNombre, txtApellido, txtDNI, txtEmail, txtTelefono,
                                          txtNacionalidad, txtObservaciones, txtNumeroTarjeta, txtCodigoSeguridad })
                    t.Clear();
                cmbMetodoPago.SelectedIndex = 0;
                Msg("Formulario limpio.", null);
            }
        }

        // ── Cliente ya registrado: precarga de datos por DNI ──
        private TextBox[] CamposCliente => new[] { txtNombre, txtApellido, txtEmail, txtTelefono, txtNacionalidad };

        private void BuscarCliente()
        {
            string dni = txtDNI.Text.Trim();
            if (_clienteExistente || dni.Length == 0) return;

            var h = _bll.BuscarClientePorDNI(dni);
            if (h == null)
            {
                lblClienteInfo.Text = "DNI nuevo: completá los datos y se registrará al confirmar.";
                lblClienteInfo.ForeColor = Tema.TextoSuave;
                return;
            }

            txtNombre.Text = h.Nombre;
            txtApellido.Text = h.Apellido;
            txtEmail.Text = h.Email;
            txtTelefono.Text = h.Telefono;
            txtNacionalidad.Text = h.Nacionalidad;

            // Datos del huésped ya registrado: solo lectura (el check-in no los modifica)
            foreach (var t in CamposCliente) { t.ReadOnly = true; t.BackColor = Color.FromArgb(236, 240, 247); }
            _clienteExistente = true;

            lblClienteInfo.Text = "✔ Huésped registrado: " + h.Apellido + ", " + h.Nombre + ". Datos cargados.";
            lblClienteInfo.ForeColor = Tema.Exito;
            Msg("✔ Huésped ya registrado, datos cargados. Podés continuar con el cobro.", true);
        }

        /// <summary>Vuelve a habilitar los campos (si cambia el DNI o se limpia el formulario).</summary>
        private void LiberarDatosCliente(bool vaciar)
        {
            _clienteExistente = false;
            foreach (var t in CamposCliente)
            {
                t.ReadOnly = false;
                t.BackColor = Color.White;
                if (vaciar) t.Clear();
            }
            lblClienteInfo.Text = "Ingresá el DNI: si el huésped ya está registrado se cargan sus datos.";
            lblClienteInfo.ForeColor = Tema.TextoSuave;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            ReiniciarCheckIn(false);
            _disponibles = _bll.VerificarDisponibilidad(dtpIngreso.Value.Date, dtpSalida.Value.Date, out string msg);

            if (_disponibles.Count == 0)
            {
                Msg(string.IsNullOrEmpty(msg) ? "Sin habitaciones disponibles." : msg, false);
                return;
            }

            foreach (var h in _disponibles)
            {
                int idx = dgvHabitaciones.Rows.Add(h.Numero, h.NombreTipo, h.Piso, "$" + h.PrecioPorNoche.ToString("N0"));
                dgvHabitaciones.Rows[idx].Tag = h;
            }
            dgvHabitaciones.ClearSelection();
            Msg("✔ " + _disponibles.Count + " habitaciones disponibles. Elegí una.", true);
        }

        private void dgvHabitaciones_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvHabitaciones.CurrentRow == null || !(dgvHabitaciones.CurrentRow.Tag is HabitacionBE)) return;
            if (dgvHabitaciones.SelectedRows.Count == 0) return;

            _habitacionSel = (HabitacionBE)dgvHabitaciones.CurrentRow.Tag;
            lblHabSel.Text = "Seleccionada: Hab. " + _habitacionSel.Numero + " — " + _habitacionSel.NombreTipo +
                             " — $" + _habitacionSel.PrecioPorNoche.ToString("N0") + "/noche";
            lblHabSel.ForeColor = Tema.Exito;

            try
            {
                var r = _bll.CalcularResumenCheckIn(dtpIngreso.Value.Date, dtpSalida.Value.Date, _habitacionSel.PrecioPorNoche);
                lblMontoHab.Text = r.CantNoches + " noche/s: $" + r.MontoHabitacion.ToString("N2");
                lblImpuestos.Text = "Impuestos (21%): $" + r.Impuestos.ToString("N2");
                lblTotal.Text = "TOTAL $" + r.MontoTotal.ToString("N2");
            }
            catch (Exception ex) { Msg("❌ " + ex.Message, false); }

            cardCliente.Enabled = true;
            cardCobro.Enabled = true;
        }

        private void btnConfirmarCheckIn_Click(object sender, EventArgs e)
        {
            if (_habitacionSel == null) { Msg("❌ Seleccioná una habitación.", false); return; }

            var solicitud = new SolicitudCheckInBE
            {
                Cliente = new HuespedBE
                {
                    Nombre = txtNombre.Text.Trim(),
                    Apellido = txtApellido.Text.Trim(),
                    DNI = txtDNI.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Nacionalidad = txtNacionalidad.Text.Trim()
                },
                IdHabitacion = _habitacionSel.IdHabitacion,
                FechaIngreso = dtpIngreso.Value.Date,
                FechaSalida = dtpSalida.Value.Date,
                Observaciones = txtObservaciones.Text.Trim(),
                MetodoPago = cmbMetodoPago.SelectedItem == null ? "" : cmbMetodoPago.SelectedItem.ToString(),
                NumeroTarjeta = txtNumeroTarjeta.Text.Trim(),
                CodigoSeguridad = txtCodigoSeguridad.Text.Trim()
            };

            btnConfirmarCheckIn.Enabled = false;
            Cursor = Cursors.WaitCursor;
            ResultadoOperacionBE res;
            try { res = _bll.RegistrarCheckIn(solicitud); }
            finally { Cursor = Cursors.Default; btnConfirmarCheckIn.Enabled = true; }

            MessageBox.Show(res.Mensaje, res.Exito ? "Check-in realizado" : "No se pudo completar",
                MessageBoxButtons.OK, res.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (res.Exito)
            {
                ReiniciarCheckIn(true);
                Msg("✔ Check-in registrado. Factura " + res.NroFactura + ".", true);
                OfrecerExportarFactura(res.NroFactura);
            }
            else
            {
                Msg(res.Mensaje.Replace("\n", " "), false);
                // Si el pago fue rechazado o la habitación se ocupó, se refresca la búsqueda
                btnBuscar_Click(this, EventArgs.Empty);
            }
        }

        /// <summary>Reporte PDF con TODAS las facturas generadas (sin filtro de fechas).</summary>
        private void ExportarTodasLasFacturas()
        {
            string ruta = ReportesUI.PedirRuta("Reporte de facturas " + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            if (ruta == null) return;
            var r = new ReporteBLL().ExportarListadoFacturas(null, null, ruta);
            Msg((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Después del cobro del check-in ofrece guardar la factura recién emitida en PDF.</summary>
        private void OfrecerExportarFactura(string nroFactura)
        {
            if (string.IsNullOrEmpty(nroFactura)) return;
            var q = MessageBox.Show(this, "¿Querés exportar la factura " + nroFactura + " a PDF?", "Factura emitida",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (q != DialogResult.Yes) return;
            string ruta = ReportesUI.PedirRuta("Factura " + nroFactura + ".pdf");
            if (ruta == null) return;
            var r = new ReporteBLL().ExportarFactura(nroFactura, ruta);
            Msg((r.Exito ? "✔ " : "✖ ") + r.Mensaje, r.Exito);
            if (r.Exito) ReportesUI.OfrecerAbrir(this, r.Dato, r.Mensaje);
            else MessageBox.Show(this, r.Mensaje, "No se pudo exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ═════════════════════════════════════════════════════════
        // CHECK-OUT  (CUN05, lanzado por la BLL)
        // ═════════════════════════════════════════════════════════
        private void CargarActivas()
        {
            _reservaSel = null;
            btnRealizarCheckOut.Enabled = false;
            LimpiarResumenCheckOut();
            dgvActivas.Rows.Clear();

            _activas = _bll.ObtenerReservasActivas();
            foreach (var r in _activas)
            {
                int idx = dgvActivas.Rows.Add(r.IdReserva, r.ApellidoHuesped + ", " + r.NombreHuesped,
                    r.DNIHuesped, r.NumeroHabitacion, r.FechaSalida.ToString("dd/MM/yyyy"));
                dgvActivas.Rows[idx].Tag = r;
            }
            dgvActivas.ClearSelection();
            if (_activas.Count == 0) Msg("No hay huéspedes con check-in activo.", null);
        }

        private void LimpiarResumenCheckOut()
        {
            lblCoHuesped.Text = "Seleccioná una reserva";
            lblCoHuesped.ForeColor = Tema.TextoSuave;
            lblCoHab.Text = lblCoEstadia.Text = lblCoMonto.Text = lblCoImpuestos.Text = lblCoTotal.Text = "";
        }

        private void dgvActivas_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvActivas.SelectedRows.Count == 0 || !(dgvActivas.CurrentRow.Tag is ReservaBE)) return;

            var sel = (ReservaBE)dgvActivas.CurrentRow.Tag;
            var resumen = _bll.CalcularResumenCheckOut(sel.IdReserva);
            if (resumen.Reserva == null) return;

            _reservaSel = resumen.Reserva;
            var r = _reservaSel;
            lblCoHuesped.Text = r.ApellidoHuesped + ", " + r.NombreHuesped;
            lblCoHuesped.ForeColor = Tema.Texto;
            lblCoHab.Text = "Hab. " + r.NumeroHabitacion + " — " + r.TipoHabitacion;
            lblCoEstadia.Text = r.FechaIngreso.ToString("dd/MM/yyyy") + " → " + r.FechaSalida.ToString("dd/MM/yyyy") + "  (" + r.CantNoches + " noches)";
            lblCoMonto.Text = "Habitación: $" + resumen.MontoHabitacion.ToString("N2");
            lblCoImpuestos.Text = "Impuestos (21%): $" + resumen.Impuestos.ToString("N2");
            lblCoTotal.Text = "TOTAL $" + resumen.MontoTotal.ToString("N2");
            btnRealizarCheckOut.Enabled = true;
        }

        private void btnRealizarCheckOut_Click(object sender, EventArgs e)
        {
            if (_reservaSel == null) return;

            var conf = MessageBox.Show(
                "¿Confirmás el Check-OUT de " + _reservaSel.ApellidoHuesped + ", " + _reservaSel.NombreHuesped +
                " (Hab. " + _reservaSel.NumeroHabitacion + ")?",
                "Confirmar Check-OUT", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (conf != DialogResult.Yes) return;

            var res = _bll.RealizarCheckOut(_reservaSel.IdReserva);
            MessageBox.Show(res.Mensaje, res.Exito ? "Check-out realizado" : "No se pudo completar",
                MessageBoxButtons.OK, res.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            Msg(res.Mensaje.Replace("\n", " "), res.Exito);
            CargarActivas();
        }

        // ═════════════════════════════════════════════════════════
        private void Msg(string texto, bool? ok)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = ok == null ? Tema.TextoSuave : (ok.Value ? Tema.Exito : Tema.Peligro);
        }
    }
}
