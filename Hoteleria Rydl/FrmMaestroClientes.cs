#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BLL_HRS_480;
using ENTIDADES_HRS_480;

namespace GUI_HRS_480
{
    /// <summary>
    /// CUN13 MAESTRO DE CLIENTES – ABM con Serializar / Des-serializar XML (menú Maestros).
    /// Los clientes del hotel son los huéspedes (tabla Huespedes).
    ///  · ABM: Actualizar, Añadir, Modificar, Eliminar + Aplicar / Cancelar.
    ///  · SERIALIZAR: guarda en un XML (ubicación y nombre a elección) los clientes que se ven en la grilla.
    ///  · DES-SERIALIZAR: carga en la grilla los clientes de un XML elegido del disco.
    ///  · LIMPIAR: vacía la grilla (no toca la base).
    /// Solo habla con las BLL (ClienteBLL, SerializarBLL, DesSerializarBLL), que validan el rol de nuevo.
    /// </summary>
    public class FrmMaestroClientes : Form, SERVICIOS_HRS_480.Idioma.IObserver
    {
        private enum Modo { Consulta, Anadir, Modificar }

        private readonly ClienteBLL _clientes = new ClienteBLL();
        private readonly SerializarBLL _serializar = new SerializarBLL();
        private readonly DesSerializarBLL _desSerializar = new DesSerializarBLL();

        private Modo _modo = Modo.Consulta;
        private DataGridView dgvClientes;
        private TextBox txtDNI, txtApellidos, txtNombres, txtEmail, txtCelular, txtDireccion, txtRutaSerializar, txtRutaDesSerializar;
        private Label lblMensaje, lblOrigen;
        private Button btnActualizar, btnAnadir, btnModificar, btnEliminar, btnAplicar, btnCancelar, btnSalir,
                       btnSerializar, btnDesSerializar, btnLimpiar;

        public FrmMaestroClientes()
        {
            Construir();
            AyudaGUI.Instalar(this);
            Load += (s, e) => Actualizar();

            // Traducción (patrón Observer: se actualiza al cambiar el idioma).
            // Se aplica al cargar, cuando ya están creados todos los controles.
            SERVICIOS_HRS_480.Idioma.GestorTraducciones.Instancia.AgregarForm(this);
            FormClosed += (s, e) => SERVICIOS_HRS_480.Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            Load += (s, e) => ActualizarTraducciones();
        }

        public void ActualizarTraducciones()
        {
            new Traductor().ActualizarTraducciones("FrmMaestroClientes", this);
        }

        private void Construir()
        {
            Text = "Maestro de Clientes - ABM";
            ClientSize = new Size(1100, 690);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            UiMaestros.Encabezado(this, "MAESTRO DE CLIENTES - ABM", "Clientes del hotel (huéspedes) · alta, modificación, baja · serialización XML");

            var tips = new ToolTip { AutoPopDelay = 8000, InitialDelay = 300 };

            // Grilla (matriz de datos)
            dgvClientes = UiMaestros.Grilla(this, 20, 82, 870, 250,
                ("DNI", 11), ("APELLIDOS", 15), ("NOMBRES", 15), ("EMAIL", 20), ("CELULAR", 12), ("DIRECCIÓN", 20), ("TARJETA", 18));
            dgvClientes.SelectionChanged += (s, e) => { if (_modo == Modo.Consulta) CargarCampos(Seleccionado()); };
            lblOrigen = UiMaestros.Etiqueta(this, "", 20, 336, 870);

            // Botones ABM
            btnActualizar = UiMaestros.Boton(this, "Actualizar", 910, 82, 170, 38, Tema.BotonPrimario, Actualizar);
            btnAnadir = UiMaestros.Boton(this, "Añadir", 910, 130, 170, 38, Tema.BotonPrimario, () => Entrar(Modo.Anadir));
            btnModificar = UiMaestros.Boton(this, "Modificar", 910, 178, 170, 38, Tema.BotonPrimario, () => Entrar(Modo.Modificar));
            btnEliminar = UiMaestros.Boton(this, "Eliminar", 910, 226, 170, 38, UiMaestros.Peligro, Eliminar);
            AyudaGUI.Tip(tips, btnActualizar, "Actualizar: vuelve a cargar en la grilla los clientes guardados en la base de datos\n(reemplaza lo que haya en la grilla, por ejemplo clientes des-serializados).");

            // Campos
            int y = 362;
            txtDNI = Campo("DNI:", y, 8);
            txtApellidos = Campo("Apellidos:", y += 34, 100);
            txtNombres = Campo("Nombres:", y += 34, 100);
            txtEmail = Campo("Email:", y += 34, 200);
            txtCelular = Campo("Celular:", y += 34, 25);
            txtDireccion = Campo("Dirección:", y += 34, 200);

            lblMensaje = UiMaestros.CuadroMensaje(this, 480, 362, 410, 194);

            btnAplicar = UiMaestros.Boton(this, "Aplicar", 910, 362, 170, 38, Tema.BotonDorado, Aplicar);
            btnCancelar = UiMaestros.Boton(this, "Cancelar", 910, 410, 170, 38, Tema.BotonSecundario, () => { CambiarModo(Modo.Consulta); CargarCampos(Seleccionado()); });
            btnSalir = UiMaestros.Boton(this, "Salir", 910, 458, 170, 38, Tema.BotonSecundario, Close);

            UiMaestros.Linea(this, 20, 578, 1060);

            // Serializar / Des-serializar
            btnSerializar = UiMaestros.Boton(this, "SERIALIZAR", 20, 594, 200, 34, Tema.BotonDorado, Serializar);
            txtRutaSerializar = UiMaestros.Caja(this, 20, 640, 360, 400);
            txtRutaSerializar.ReadOnly = true;
            var btnUbicacion = UiMaestros.BotonCarpeta(this, 384, 640, ElegirUbicacion);
            AyudaGUI.Tip(tips, btnUbicacion, "Elegir la carpeta y el nombre del archivo XML donde se van a guardar los clientes de la grilla.");
            AyudaGUI.Tip(tips, btnSerializar, "Guarda en el archivo XML elegido los clientes que se ven en la grilla.");

            btnDesSerializar = UiMaestros.Boton(this, "DES-SERIALIZAR", 480, 594, 200, 34, Tema.BotonDorado, DesSerializar);
            txtRutaDesSerializar = UiMaestros.Caja(this, 480, 640, 360, 400);
            txtRutaDesSerializar.ReadOnly = true;
            var btnArchivo = UiMaestros.BotonCarpeta(this, 844, 640, ElegirArchivo);
            AyudaGUI.Tip(tips, btnArchivo, "Elegir del disco el archivo XML de clientes a des-serializar.");
            AyudaGUI.Tip(tips, btnDesSerializar, "Carga en la grilla los clientes del archivo XML elegido (no los guarda en la base).");

            btnLimpiar = UiMaestros.Boton(this, "LIMPIAR", 910, 594, 170, 34, Tema.BotonSecundario, Limpiar);
            AyudaGUI.Tip(tips, btnLimpiar, "Limpiar: vacía la grilla y los campos de la pantalla.\nNo borra nada de la base de datos ni de los archivos XML.");

            CambiarModo(Modo.Consulta);
        }

        private TextBox Campo(string etiqueta, int y, int max)
        {
            var l = UiMaestros.Etiqueta(this, etiqueta, 20, y + 2, 100, negrita: true);
            l.BackColor = Color.FromArgb(226, 240, 229);
            l.Padding = new Padding(6, 0, 0, 0);
            return UiMaestros.Caja(this, 124, y, 330, max);
        }

        // ── Grilla ────────────────────────────────────────────────────
        private void CargarGrilla(IEnumerable<ClienteBE> clientes)
        {
            dgvClientes.Rows.Clear();
            foreach (var c in clientes) AgregarFila(c);
            ActualizarOrigen();
            CargarCampos(Seleccionado());
        }

        private void AgregarFila(ClienteBE c)
        {
            int i = dgvClientes.Rows.Add(c.DNI, c.Apellido, c.Nombre, c.Email, c.Celular, c.Direccion, c.Tarjeta);
            PintarFila(dgvClientes.Rows[i], c);
        }

        private static void PintarFila(DataGridViewRow r, ClienteBE c)
        {
            r.Tag = c;
            r.SetValues(c.DNI, c.Apellido, c.Nombre, c.Email, c.Celular, c.Direccion, c.Tarjeta);
            r.DefaultCellStyle.BackColor = c.DesdeXml ? Color.FromArgb(255, 248, 225) : Color.Empty;
        }

        private void ActualizarOrigen()
        {
            var filas = FilasDeLaGrilla();
            int xml = filas.Count(c => c.DesdeXml);
            lblOrigen.Text = filas.Count == 0 ? "La grilla está vacía."
                : xml == 0 ? $"{filas.Count} cliente/s de la base de datos."
                : $"{filas.Count} cliente/s · {xml} des-serializado/s desde XML (fondo amarillo: no están guardados en la base; para guardarlos usá Añadir).";
        }

        private List<ClienteBE> FilasDeLaGrilla()
            => dgvClientes.Rows.Cast<DataGridViewRow>().Select(r => r.Tag as ClienteBE).Where(c => c != null).ToList();

        private ClienteBE Seleccionado() => dgvClientes.CurrentRow?.Tag as ClienteBE;

        private DataGridViewRow FilaPorDNI(string dni)
            => dgvClientes.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (r.Tag as ClienteBE)?.DNI == dni);

        private void CargarCampos(ClienteBE c)
        {
            txtDNI.Text = c?.DNI ?? ""; txtApellidos.Text = c?.Apellido ?? ""; txtNombres.Text = c?.Nombre ?? "";
            txtEmail.Text = c?.Email ?? ""; txtCelular.Text = c?.Celular ?? ""; txtDireccion.Text = c?.Direccion ?? "";
        }

        private ClienteBE LeerCampos() => new ClienteBE
        {
            DNI = txtDNI.Text, Apellido = txtApellidos.Text, Nombre = txtNombres.Text,
            Email = txtEmail.Text, Celular = txtCelular.Text, Direccion = txtDireccion.Text
        };

        // ── Modos ─────────────────────────────────────────────────────
        private void Entrar(Modo modo)
        {
            var sel = Seleccionado();
            if (modo == Modo.Modificar && sel == null) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un cliente de la grilla.", false); return; }
            CambiarModo(modo);
            if (modo == Modo.Anadir)
            {
                // Si hay una fila des-serializada seleccionada, se ofrece para pasarla a la base
                if (sel != null && sel.DesdeXml) CargarCampos(sel); else CargarCampos(null);
                UiMaestros.Mostrar(lblMensaje, "Modo Añadir: completá los datos y presioná Aplicar.", true);
                txtDNI.Focus();
            }
            else
            {
                CargarCampos(sel);
                UiMaestros.Mostrar(lblMensaje, $"Modo Modificar: editá los datos de {sel.Apellido}, {sel.Nombre} y presioná Aplicar. El DNI no se modifica.", true);
                txtApellidos.Focus();
            }
        }

        private void CambiarModo(Modo modo)
        {
            _modo = modo;
            bool consulta = modo == Modo.Consulta;
            foreach (var t in new[] { txtDNI, txtApellidos, txtNombres, txtEmail, txtCelular, txtDireccion })
            {
                t.ReadOnly = consulta;
                t.BackColor = consulta ? Color.FromArgb(247, 248, 251) : Color.White;
            }
            txtDNI.ReadOnly = modo != Modo.Anadir;
            txtDNI.BackColor = modo == Modo.Anadir ? Color.White : Color.FromArgb(247, 248, 251);
            btnAplicar.Enabled = btnCancelar.Enabled = !consulta;
            foreach (var b in new[] { btnActualizar, btnAnadir, btnModificar, btnEliminar, btnSalir, btnSerializar, btnDesSerializar, btnLimpiar })
                b.Enabled = consulta;
            dgvClientes.Enabled = consulta;
            if (consulta) UiMaestros.Mostrar(lblMensaje, "", true);
        }

        // ── ABM ───────────────────────────────────────────────────────
        private void Actualizar()
        {
            var r = _clientes.ObtenerClientes();
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            CargarGrilla(r.Dato);
            UiMaestros.Mostrar(lblMensaje, $"Se cargaron {r.Dato.Count} cliente/s de la base de datos.", true);
        }

        private void Aplicar()
        {
            var c = LeerCampos();
            var r = _modo == Modo.Anadir ? _clientes.Alta(c) : _clientes.Modificar(c);
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }

            var fila = FilaPorDNI(r.Dato.DNI);
            if (fila?.Tag is ClienteBE anterior && string.IsNullOrEmpty(r.Dato.Tarjeta))
            {
                // la tarjeta no se edita en el maestro: se conserva lo que ya mostraba la grilla
                r.Dato.Tarjeta = anterior.Tarjeta;
                r.Dato.TarjetaCifrada = anterior.TarjetaCifrada;
            }
            if (fila != null) PintarFila(fila, r.Dato); else AgregarFila(r.Dato);
            CambiarModo(Modo.Consulta);
            fila = FilaPorDNI(r.Dato.DNI);
            if (fila != null) { dgvClientes.ClearSelection(); fila.Selected = true; dgvClientes.CurrentCell = fila.Cells[0]; }
            ActualizarOrigen();
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
        }

        private void Eliminar()
        {
            var sel = Seleccionado();
            if (sel == null) { UiMaestros.Mostrar(lblMensaje, "Seleccioná un cliente de la grilla.", false); return; }
            if (sel.DesdeXml)
            {
                UiMaestros.Mostrar(lblMensaje, "Ese cliente vino del XML y no está en la base: para sacarlo de la grilla usá Limpiar o Actualizar.", false);
                return;
            }
            if (MessageBox.Show($"¿Eliminar al cliente {sel.Apellido}, {sel.Nombre} (DNI {sel.DNI})?\n\nLa baja es lógica: conserva sus reservas e historial.",
                    "Confirmar baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = _clientes.Eliminar(sel.DNI);
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            var fila = FilaPorDNI(sel.DNI);
            if (fila != null) dgvClientes.Rows.Remove(fila);
            CargarCampos(Seleccionado());
            ActualizarOrigen();
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
        }

        private void Limpiar()
        {
            dgvClientes.Rows.Clear();
            CargarCampos(null);
            ActualizarOrigen();
            UiMaestros.Mostrar(lblMensaje, "Grilla vacía. La base de datos no se modificó.", true);
        }

        // ── Serializar / Des-serializar ───────────────────────────────
        private void ElegirUbicacion()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Elegí dónde guardar el archivo XML de clientes",
                Filter = "Archivo XML (*.xml)|*.xml",
                DefaultExt = "xml",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = $"Clientes_HotelRydl_{DateTime.Now:yyyyMMdd_HHmm}.xml"
            };
            if (!string.IsNullOrEmpty(txtRutaSerializar.Text)) dlg.InitialDirectory = Path.GetDirectoryName(txtRutaSerializar.Text);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtRutaSerializar.Text = dlg.FileName;
                UiMaestros.Mostrar(lblMensaje, "Ubicación elegida. Presioná SERIALIZAR para guardar los clientes de la grilla.", true);
            }
        }

        private void Serializar()
        {
            var r = _serializar.Serializar(FilasDeLaGrilla(), txtRutaSerializar.Text);
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, r.Exito);
            if (r.Exito) txtRutaSerializar.Text = r.Dato;
        }

        private void ElegirArchivo()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Elegí el archivo XML de clientes a des-serializar",
                Filter = "Archivo XML (*.xml)|*.xml",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtRutaDesSerializar.Text = dlg.FileName;
                UiMaestros.Mostrar(lblMensaje, "Archivo elegido. Presioná DES-SERIALIZAR para cargarlo en la grilla.", true);
            }
        }

        private void DesSerializar()
        {
            var r = _desSerializar.DesSerializar(txtRutaDesSerializar.Text);
            if (!r.Exito) { UiMaestros.Mostrar(lblMensaje, r.Mensaje, false); return; }
            CargarGrilla(r.Dato);
            UiMaestros.Mostrar(lblMensaje, r.Mensaje, true);
        }
    }
}
