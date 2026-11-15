#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GUI_HRS_480
{
    /// <summary>
    /// Ayuda en pantalla: cada GUI tiene un botón "?" en la esquina superior derecha.
    ///  · Al pasar el mouse por encima muestra de qué se trata la pantalla, cómo se usa y para qué sirve.
    ///  · Al hacer clic (o con la tecla F1) abre la misma ayuda en una ventana para leerla con calma.
    /// Los textos están todos acá, uno por pantalla (clave = nombre de la clase del formulario).
    /// </summary>
    public static class AyudaGUI
    {
        private sealed class Ayuda
        {
            public string Titulo, QueEs, ParaQue;
            public string[] ComoSeUsa;
        }

        private static readonly Dictionary<string, Ayuda> _ayudas = new Dictionary<string, Ayuda>
        {
            ["FrmLogin"] = new Ayuda
            {
                Titulo = "Inicio de sesión",
                QueEs = "Pantalla de ingreso al sistema de gestión hotelera Hotel Rydl.",
                ComoSeUsa = new[]
                {
                    "Escribí tu usuario (DNI + Nombre) y tu contraseña (la inicial es DNI + Apellido).",
                    "Presioná INGRESAR. Si es tu primer ingreso, el sistema te pide cambiar la contraseña.",
                    "Después de 3 contraseñas incorrectas la cuenta se bloquea: la desbloquea el administrador.",
                    "Si el sistema detecta datos alterados (Dígito Verificador), el administrador ve la pantalla de reparación y el resto de los usuarios recibe el aviso de contactarlo.",
                },
                ParaQue = "Identificar a cada usuario: según su rol, el menú muestra solo las opciones que puede usar y todo lo que hace queda en la bitácora."
            },
            ["FrmMenuPrincipal"] = new Ayuda
            {
                Titulo = "Menú principal",
                QueEs = "Pantalla central del sistema, con la barra lateral de opciones agrupadas por sección.",
                ComoSeUsa = new[]
                {
                    "OPERACIÓN: Check-In / Check-Out (Recepción) y Gestión de insumos (Reposición, PN2).",
                    "MAESTROS: Maestro de Clientes (con XML), Maestro de Insumos y Bitácora de Cambios.",
                    "ADMINISTRACIÓN: usuarios, bitácora de eventos, familias, roles, backup y restore.",
                    "CUENTA: cambiar contraseña, cambiar idioma y cerrar sesión.",
                    "Solo aparecen las opciones que permite tu rol.",
                },
                ParaQue = "Acceder desde un solo lugar a todos los módulos del hotel."
            },
            ["FrmGestionUsuarios"] = new Ayuda
            {
                Titulo = "Gestión de usuarios",
                QueEs = "ABM de los usuarios que pueden ingresar al sistema.",
                ComoSeUsa = new[]
                {
                    "Filtrá por DNI, apellido, nombre, login o rol y presioná APLICAR FILTRO.",
                    "CREAR: completá los datos y el rol, y presioná APLICAR (la clave inicial es DNI + Apellido).",
                    "MODIFICAR: cambiá el email o el rol del usuario seleccionado.",
                    "DESBLOQUEAR: reactiva una cuenta bloqueada y le resetea la contraseña.",
                    "ACTIVAR / DESACTIVAR: habilita o inhabilita el acceso del usuario.",
                    "El usuario maestro (Admin) no puede ser modificado por otros usuarios.",
                },
                ParaQue = "Controlar quién entra al sistema y con qué rol."
            },
            ["FrmBitacora"] = new Ayuda
            {
                Titulo = "Bitácora de eventos",
                QueEs = "Registro de todo lo que pasa en el sistema: ingresos, operaciones, errores, Dígito Verificador, backups.",
                ComoSeUsa = new[]
                {
                    "Filtrá por usuario, fecha inicial y final, módulo, evento y criticidad, y presioná Aplicar.",
                    "Limpiar borra los filtros. Imprimir genera el listado de lo que se ve.",
                    "La criticidad va de 1 (más crítico) a 5 (informativo).",
                },
                ParaQue = "Auditar quién hizo qué y cuándo, y detectar problemas o accesos indebidos."
            },
            ["FrmGeneradorFamilias"] = new Ayuda
            {
                Titulo = "Gestión de familias",
                QueEs = "ABM de familias de permisos (grupos de permisos que se asignan juntos).",
                ComoSeUsa = new[]
                {
                    "Generar: escribí el nombre y agregá permisos o familias con doble clic en la lista Disponibles.",
                    "Modificar: elegí una familia existente y cambiá sus componentes.",
                    "Eliminar: borra la familia seleccionada. Aplicar confirma y Cancelar descarta.",
                },
                ParaQue = "Armar perfiles de permisos reutilizables para asignarlos a los roles."
            },
            ["FrmGeneradorRoles"] = new Ayuda
            {
                Titulo = "Gestión de roles",
                QueEs = "ABM de los roles del sistema (Administrador, Recepcionista, Encargado de Compras, etc.).",
                ComoSeUsa = new[]
                {
                    "Crear: escribí el nombre y agregá permisos y familias con doble clic en Disponibles.",
                    "Modificar: elegí un rol y cambiá sus componentes. Eliminar: los usuarios del rol pasan a Básico.",
                    "El rol Administrador no se puede modificar ni eliminar.",
                },
                ParaQue = "Definir qué puede hacer cada tipo de usuario."
            },
            ["FrmCambioClave"] = new Ayuda
            {
                Titulo = "Cambiar contraseña",
                QueEs = "Cambio de la contraseña del usuario en sesión (o el cambio obligatorio del primer ingreso).",
                ComoSeUsa = new[]
                {
                    "Escribí la contraseña actual, la nueva y repetila en Confirmar.",
                    "Presioná ACEPTAR. Si es el cambio obligatorio, después volvés a iniciar sesión.",
                },
                ParaQue = "Mantener la cuenta segura y reemplazar la contraseña inicial."
            },
            ["FrmIdiomas"] = new Ayuda
            {
                Titulo = "Cambiar idioma",
                QueEs = "Selección del idioma de las pantallas.",
                ComoSeUsa = new[] { "Elegí Español, Inglés o Alemán y presioná Aceptar: las pantallas abiertas se traducen al instante." },
                ParaQue = "Usar el sistema en el idioma de cada usuario."
            },
            ["FrmReparacionDV"] = new Ayuda
            {
                Titulo = "Reparación del Dígito Verificador",
                QueEs = "Aparece al iniciar sesión (solo al administrador) cuando el DV detectó datos modificados por fuera del sistema.",
                ComoSeUsa = new[]
                {
                    "Revisá en la lista qué tablas tienen diferencias.",
                    "Recalcular: acepta el estado actual de la base como válido y vuelve a calcular el DV.",
                    "Restore: vuelve la base al backup elegido (se pierden los cambios posteriores).",
                    "Salir: deja la inconsistencia pendiente. Todas las opciones quedan en la bitácora.",
                },
                ParaQue = "Recuperar la integridad de los datos antes de seguir usando el sistema."
            },
            ["FrmSeleccionarBackup"] = new Ayuda
            {
                Titulo = "Restore de la base de datos",
                QueEs = "Restauración de la base desde un backup guardado.",
                ComoSeUsa = new[]
                {
                    "Elegí en la lista el backup al que querés volver y presioná Restaurar.",
                    "Confirmá: la base se reemplaza entera y se pierden los cambios posteriores al backup.",
                },
                ParaQue = "Volver a un estado anterior ante un error grave o datos dañados."
            },
            ["FrmRecepcion"] = new Ayuda
            {
                Titulo = "Recepción: Check-IN / Check-OUT (PN1)",
                QueEs = "Pantalla de la recepción del hotel para registrar la llegada y la salida de los huéspedes.",
                ComoSeUsa = new[]
                {
                    "Check-IN: elegí las fechas y Ver disponibilidad, seleccioná la habitación, ingresá el DNI del huésped (si es nuevo, completá sus datos), el medio de pago y Confirmar Check-IN. Se cobra y se emite la factura.",
                    "Check-OUT: elegí la estadía de la lista y presioná Realizar Check-OUT.",
                    "Buscar / exportar factura y Reporte de todas las facturas generan los PDF.",
                },
                ParaQue = "Ocupar y liberar habitaciones, cobrar las estadías y emitir las facturas."
            },
            ["FrmReporteFacturas"] = new Ayuda
            {
                Titulo = "Reporte de facturas",
                QueEs = "Consulta de las facturas emitidas en la recepción.",
                ComoSeUsa = new[]
                {
                    "Elegí el período (o Todas las fechas) y presioná Buscar.",
                    "Exportar factura (PDF): guarda la factura seleccionada. Exportar listado (PDF): guarda todas las de la grilla.",
                },
                ParaQue = "Reimprimir facturas y obtener el listado de lo facturado."
            },
            ["FrmReposicion"] = new Ayuda
            {
                Titulo = "Reposición de insumos (PN2)",
                QueEs = "Proceso de compra de insumos: de los faltantes a la recepción en el depósito. Cada rol ve sus secciones.",
                ComoSeUsa = new[]
                {
                    "Faltantes y solicitud (Encargado de Compras): tildá insumos faltantes y generá la solicitud de cotización al proveedor (si es nuevo, pre-registralo).",
                    "Cotizaciones y Pre-órdenes (Compras): cargá lo que cotizó cada proveedor, compará y generá las pre-órdenes.",
                    "Gerencia (Gerente de Compras): aprobá o cancelá cada pre-orden, registrá al proveedor y pagá la orden.",
                    "Recepción (Encargado de Depósito): registrá la factura y lo recibido; el stock se actualiza solo.",
                    "Órdenes: consulta y PDF. Avisos: notificaciones entre las áreas.",
                },
                ParaQue = "Que el hotel no se quede sin insumos, comprando al mejor precio y con control de cada paso."
            },
            ["FrmMaestroClientes"] = new Ayuda
            {
                Titulo = "Maestro de Clientes (CUN13)",
                QueEs = "ABM de los clientes del hotel (huéspedes), con serialización y des-serialización XML.",
                ComoSeUsa = new[]
                {
                    "Actualizar: carga en la grilla los clientes de la base. Añadir / Modificar / Eliminar + Aplicar o Cancelar.",
                    "SERIALIZAR: con el botón de la carpeta elegí dónde guardar el XML y su nombre, y presioná SERIALIZAR: se guardan los clientes de la grilla.",
                    "DES-SERIALIZAR: con el botón de la carpeta elegí un XML y presioná DES-SERIALIZAR: sus clientes se cargan en la grilla (en amarillo, sin guardarse en la base).",
                    "Para guardar en la base un cliente del XML, seleccionalo y usá Añadir. LIMPIAR vacía la grilla.",
                },
                ParaQue = "Mantener los datos de los clientes y poder exportarlos o importarlos en archivos XML."
            },
            ["FrmMaestroInsumos"] = new Ayuda
            {
                Titulo = "Maestro de Insumos",
                QueEs = "ABM de los insumos del hotel (blancos, amenities, limpieza, etc.).",
                ComoSeUsa = new[]
                {
                    "Añadir o Modificar: completá nombre, categoría, unidad, stock y stock mínimo, y presioná Aplicar.",
                    "Eliminar: baja lógica (el insumo queda inactivo). Reactivar: lo vuelve a habilitar.",
                    "Tildá \"Mostrar también los dados de baja\" para verlos.",
                    "Cada cambio queda guardado automáticamente en la Bitácora de Cambios.",
                },
                ParaQue = "Mantener el catálogo de insumos que usa el proceso de reposición."
            },
            ["FrmBitacoraCambios"] = new Ayuda
            {
                Titulo = "Bitácora de cambios de insumos (CUN14)",
                QueEs = "Historial de todos los cambios de los insumos (tabla Insumos_C), registrado por triggers de la base.",
                ComoSeUsa = new[]
                {
                    "Filtrá por Código, Nombre, Fecha inicial y Fecha final (tildá la casilla de la fecha) y presioná APLICAR. LIMPIAR borra los filtros.",
                    "La fila resaltada (Act = 1) es la versión vigente de cada insumo.",
                    "ACTIVAR: seleccioná una versión anterior y el insumo vuelve a esos datos (pasa a Act = 1).",
                    "A la derecha se ve la tabla Insumos actual.",
                },
                ParaQue = "Auditar quién cambió cada insumo, qué cambió y cuándo, y deshacer un cambio si hace falta."
            },
        };

        /// <summary>Agrega el botón de ayuda "?" y la tecla F1 al formulario (si tiene texto de ayuda).</summary>
        public static void Instalar(Form f)
        {
            if (f == null || !_ayudas.TryGetValue(f.GetType().Name, out var a)) return;
            if (f.Controls.OfType<BotonAyuda>().Any()) return;   // ya instalada

            var b = new BotonAyuda { Name = "btnAyudaGUI", Size = new Size(30, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, TabStop = false };
            b.Location = LugarLibre(f, b.Size);
            // Fondo = el color de lo que queda debajo (por ejemplo, el encabezado azul)
            var debajo = f.Controls.Cast<Control>().FirstOrDefault(c => c.Visible && c.Dock != DockStyle.None && c.Bounds.Contains(b.Location));
            b.BackColor = debajo?.BackColor ?? f.BackColor;
            var tip = new ToolTip { AutoPopDelay = 32000, InitialDelay = 150, ReshowDelay = 100, ToolTipTitle = "Ayuda · " + a.Titulo, ToolTipIcon = ToolTipIcon.Info, ShowAlways = true };
            tip.SetToolTip(b, TextoTooltip(a));
            b.Click += (s, e) => Mostrar(f, a);
            f.Controls.Add(b);
            b.BringToFront();
            f.Resize += (s, e) => b.BringToFront();

            f.KeyPreview = true;
            f.KeyDown += (s, e) => { if (e.KeyCode == Keys.F1) { e.Handled = true; Mostrar(f, a); } };
        }

        /// <summary>Esquina superior derecha; si hay un control ahí, se corre a la izquierda hasta encontrar lugar.</summary>
        private static Point LugarLibre(Form f, Size tam)
        {
            var ocupados = f.Controls.Cast<Control>()
                .Where(c => c.Visible && c.Dock == DockStyle.None)
                .Select(c => Rectangle.Inflate(c.Bounds, 3, 3)).ToList();
            foreach (int y in new[] { 6, 2 })
                for (int x = f.ClientSize.Width - tam.Width - 8; x > f.ClientSize.Width / 2; x -= 6)
                {
                    var r = new Rectangle(new Point(x, y), tam);
                    if (!ocupados.Any(o => o.IntersectsWith(r))) return r.Location;
                }
            return new Point(f.ClientSize.Width - tam.Width - 8, 2);
        }

        private static string TextoTooltip(Ayuda a)
        {
            var sb = new StringBuilder();
            sb.AppendLine("QUÉ ES"); Envolver(sb, a.QueEs, "  ");
            sb.AppendLine(); sb.AppendLine("CÓMO SE USA");
            foreach (var p in a.ComoSeUsa) Envolver(sb, p, "  • ", "     ");
            sb.AppendLine(); sb.AppendLine("PARA QUÉ SIRVE"); Envolver(sb, a.ParaQue, "  ");
            sb.AppendLine(); sb.Append("(Clic en ? o F1 para ver la ayuda en una ventana)");
            return sb.ToString();
        }

        /// <summary>El ToolTip no corta las líneas solo: se cortan a ~80 caracteres.</summary>
        private static void Envolver(StringBuilder sb, string texto, string primera, string siguientes = null)
        {
            siguientes ??= primera;
            var linea = new StringBuilder(primera);
            foreach (var w in texto.Split(' '))
            {
                if (linea.Length + w.Length > 82 && linea.ToString().Trim().Length > 0)
                {
                    sb.AppendLine(linea.ToString().TrimEnd());
                    linea.Clear().Append(siguientes);
                }
                linea.Append(w).Append(' ');
            }
            sb.AppendLine(linea.ToString().TrimEnd());
        }

        private static void Mostrar(Form duenio, Ayuda a)
        {
            using var v = new Form
            {
                Text = "Ayuda · " + a.Titulo, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, ClientSize = new Size(620, 460), BackColor = Tema.Fondo
            };
            var head = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Tema.Marino };
            head.Controls.Add(new Label { Text = a.Titulo, ForeColor = Color.White, Font = Tema.Fuente(13f, FontStyle.Bold), AutoSize = true, Location = new Point(18, 14) });
            var cuerpo = new RichTextBox
            {
                ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = Tema.Fuente(10f),
                Location = new Point(18, 70), Size = new Size(584, 330), ScrollBars = RichTextBoxScrollBars.Vertical, TabStop = false
            };
            void Titulo(string t) { cuerpo.SelectionFont = Tema.Fuente(10.5f, FontStyle.Bold); cuerpo.SelectionColor = Tema.Marino; cuerpo.AppendText(t + "\n"); }
            void Texto(string t) { cuerpo.SelectionFont = Tema.Fuente(10f); cuerpo.SelectionColor = Tema.Texto; cuerpo.AppendText(t + "\n"); }
            Titulo("Qué es"); Texto(a.QueEs); Texto("");
            Titulo("Cómo se usa"); foreach (var p in a.ComoSeUsa) Texto("•  " + p); Texto("");
            Titulo("Para qué sirve"); Texto(a.ParaQue);
            var ok = new Button { Text = "Entendido", Size = new Size(140, 36), Location = new Point(462, 412), DialogResult = DialogResult.OK };
            Tema.BotonPrimario(ok);
            v.Controls.Add(cuerpo); v.Controls.Add(head); v.Controls.Add(ok);
            v.AcceptButton = ok;
            v.ShowDialog(duenio);
        }

        /// <summary>Círculo dorado con el signo "?".</summary>
        private sealed class BotonAyuda : Control
        {
            private bool _hover;
            public BotonAyuda()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                Cursor = Cursors.Help;
                AccessibleName = "Ayuda de la pantalla";
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(1, 1, Width - 3, Height - 3);
                using (var br = new SolidBrush(_hover ? Color.FromArgb(180, 142, 28) : Tema.Dorado)) e.Graphics.FillEllipse(br, r);
                using (var pen = new Pen(Color.White, 1.5f)) e.Graphics.DrawEllipse(pen, r);
                TextRenderer.DrawText(e.Graphics, "?", Tema.Fuente(12f, FontStyle.Bold), r, Color.White,
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
