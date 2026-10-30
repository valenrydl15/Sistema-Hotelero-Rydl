using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;


namespace SERVICIOS_HRS_480
{
    public class DigitoVerificadorServicio
    {
        // Inyección de dependencias: BLL para calcular, DAL para persistir
        private readonly DigitoVerificadorCalculo _bll = new DigitoVerificadorCalculo();
        private readonly DigitoVerificadorDAL _dal = new DigitoVerificadorDAL();
        private readonly Bitacora _bDAL = new Bitacora();
        private static string _carpetaBackups;
        private const string CARPETA_BACKUP_FALLBACK = @"C:\HotelRydl\Backups";

        private static string CarpetaBackups => GetCarpetaBackups();

        public static void SetCarpetaBackups(string nuevaRuta)
        {
            if (!string.IsNullOrWhiteSpace(nuevaRuta))
                _carpetaBackups = nuevaRuta;
        }

        /// <summary>
        /// Carpeta de backups: la que tiene configurada la instancia de SQL Server;
        /// si no se puede leer, C:\HotelRydl\Backups.
        /// </summary>
        public static string GetCarpetaBackups()
        {
            if (_carpetaBackups == null)
            {
                string ruta = null;
                try { ruta = new DigitoVerificadorDAL().ObtenerCarpetaBackupPorDefecto(); }
                catch { }

                _carpetaBackups = string.IsNullOrWhiteSpace(ruta) ? CARPETA_BACKUP_FALLBACK : ruta;
            }
            return _carpetaBackups;
        }

        // Tablas del sistema controladas por el DV
        public static readonly string[] TablasControladas =
{
    "Usuarios", "Roles", "Familias", "Permisos",
    "RolFamilias", "RolPermisos", "FamiliaPermisos", "FamiliaFamilias",
    "Huespedes", "Habitaciones", "Reservas",
    "CheckIns", "CheckOuts", "Pagos", "Facturas",
    "Insumos", "Proveedores", "SolicitudesCotizacion", "SolicitudesCotizacionDetalle", "Cotizaciones", "CotizacionesDetalle",
    "OrdenesCompra", "OrdenesCompraDetalle", "PagosOrdenCompra", "RecepcionesInsumos", "RecepcionesInsumosDetalle", "MovimientosStock"
};

        // ══════════════════════════════════════════════════════
        // DIMENSIÓN 1: CÁLCULO + ALMACENAMIENTO
        // Se llama después de CADA operación ABM que modifique
        // alguna de las tablas controladas.
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Recalcula y guarda el DV de una tabla específica.
        /// Llamar después de cualquier INSERT / UPDATE / DELETE
        /// sobre esa tabla.
        /// </summary>
        public void RecalcularYGuardar(string nombreTabla)
        {
            // DAL trae los datos crudos
            var dt = _dal.ObtenerDatosTabla(nombreTabla);

            // BLL calcula DVH (por columnas) y DVV (por filas)
            string dvh = _bll.CalcularDVH(dt);
            string dvv = _bll.CalcularDVV(dt);

            // DAL persiste el resultado
            _dal.GuardarDV(nombreTabla, dvh, dvv);
        }

        /// <summary>
        /// Recalcula y guarda el DV de TODAS las tablas controladas.
        /// Se llama al login (primera verificación) o después de
        /// operaciones que afectan múltiples tablas.
        /// </summary>
        public void RecalcularTodo()
        {
            foreach (var tabla in TablasControladas)
                RecalcularYGuardar(tabla);
        }

        public void RecalcularTablas(params string[] tablas)
        {
            foreach (var t in tablas.Distinct())
                RecalcularYGuardar(t);
        }

        /// <summary>
        /// Guarda un snapshot de los DVs actuales como estado correcto.
        /// Se llama automáticamente después de un login exitoso.
        /// </summary>
        public void GuardarBackupEstadoCorrecto(string descripcion = null)
        {
            string desc = descripcion ?? $"Backup automático {DateTime.Now:dd/MM/yyyy HH:mm}";
            foreach (var tabla in TablasControladas)
            {
                var dv = _dal.ObtenerDVAlmacenado(tabla);
                if (dv != null)
                    _dal.GuardarBackupDV(tabla, dv.DVH, dv.DVV, desc);
            }
        }

        // ══════════════════════════════════════════════════════
        // Recalcular SOLO si la tabla estaba íntegra antes de la operación.
        // Si ya estaba alterada, NO se "blanquea" la alteración: el DV viejo
        // queda y la inconsistencia se detecta en el próximo login.
        //
        // Uso en los servicios de ABM:
        //   bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
        //   ... operación sobre la base ...
        //   DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
        // ══════════════════════════════════════════════════════
        public static bool EstaConsistente(params string[] tablas)
        {
            try
            {
                var dv = new DigitoVerificadorServicio();
                foreach (var t in tablas)
                    if (!dv.TablaConsistente(t)) return false;
                return true;
            }
            catch { return false; }
        }

        public static void RecalcularSiConsistente(bool recalcular, params string[] tablas)
        {
            if (!recalcular) return;
            try { new DigitoVerificadorServicio().RecalcularTablas(tablas); }
            catch { /* el DV no debe bloquear la operación; se detecta en el próximo login */ }
        }

        private bool TablaConsistente(string tabla)
        {
            var almacenado = _dal.ObtenerDVAlmacenado(tabla);
            if (almacenado == null) return false;                              // fila de DV borrada
            if (almacenado.DVH == "0" && almacenado.DVV == "0") return true;   // estado inicial

            var dt = _dal.ObtenerDatosTabla(tabla);
            return _bll.CalcularDVH(dt) == almacenado.DVH
                && _bll.CalcularDVV(dt) == almacenado.DVV;
        }

        // Marca que indica que el DV de esta base ya usa el formato independiente de la cultura
        private const string MARCA_FORMATO = "__FORMATO_DV_V2";

        /// <summary>
        /// Se ejecuta una vez al iniciar: si la base viene de una versión anterior (cuyo DV se calculó
        /// con el formato regional de la PC), recalcula todo con el formato nuevo.
        /// </summary>
        public void MigrarFormatoSiCorresponde()
        {
            if (_dal.ObtenerDVAlmacenado(MARCA_FORMATO) != null) return;
            RecalcularTodo();
            _dal.GuardarDV(MARCA_FORMATO, "v2", "v2");
        }

        // ══════════════════════════════════════════════════════
        // DIMENSIÓN 2: VERIFICACIÓN
        // Compara el DV recalculado con el almacenado en la DB.
        // Si difieren → inconsistencia detectada.
        // ══════════════════════════════════════════════════════
        public ResultadoVerificacionDV Verificar()
        {
            var resultado = new ResultadoVerificacionDV { Consistente = true };

            foreach (var tabla in TablasControladas)
            {
                var almacenado = _dal.ObtenerDVAlmacenado(tabla);

                // Fila del DV borrada: no se puede saltear el control, es una inconsistencia
                if (almacenado == null)
                {
                    var dtFalta = _dal.ObtenerDatosTabla(tabla);
                    string dvhNuevo = _bll.CalcularDVH(dtFalta);
                    string dvvNuevo = _bll.CalcularDVV(dtFalta);

                    resultado.Consistente = false;
                    resultado.Inconsistencias.Add(new InconsistenciaDV
                    {
                        FechaDeteccion = DateTime.Now,
                        NombreTabla = tabla,
                        DVH_Esperado = "(sin registro de DV)",
                        DVH_Calculado = dvhNuevo,
                        DVV_Esperado = "(sin registro de DV)",
                        DVV_Calculado = dvvNuevo
                    });
                    _dal.RegistrarInconsistencia(tabla, "(sin registro de DV)", dvhNuevo,
                                                 "(sin registro de DV)", dvvNuevo);
                    continue;
                }

                // Si el DV está en '0' (estado inicial), se inicializa sin verificar,
                // pero queda registrado en bitácora para que sea trazable.
                if (almacenado.DVH == "0" && almacenado.DVV == "0")
                {
                    RecalcularYGuardar(tabla);
                    try
                    {
                        _bDAL.Registrar(null, 1, null, 2,
                            $"DV de la tabla '{tabla}' inicializado (estaba en estado inicial '0').",
                            "EXITOSO");
                    }
                    catch { }
                    continue;
                }

                // Recalcular en tiempo real con la BLL
                var dt = _dal.ObtenerDatosTabla(tabla);
                string dvhActual = _bll.CalcularDVH(dt);
                string dvvActual = _bll.CalcularDVV(dt);

                bool dvhOk = dvhActual == almacenado.DVH;
                bool dvvOk = dvvActual == almacenado.DVV;

                if (!dvhOk || !dvvOk)
                {
                    resultado.Consistente = false;

                    var inc = new InconsistenciaDV
                    {
                        FechaDeteccion = DateTime.Now,
                        NombreTabla = tabla,
                        DVH_Esperado = almacenado.DVH,
                        DVH_Calculado = dvhActual,
                        DVV_Esperado = almacenado.DVV,
                        DVV_Calculado = dvvActual
                    };
                    resultado.Inconsistencias.Add(inc);

                    // Persistir la inconsistencia en la DB
                    _dal.RegistrarInconsistencia(tabla,
                        almacenado.DVH, dvhActual,
                        almacenado.DVV, dvvActual);
                }
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════
        // DIMENSIÓN 3: REPARACIÓN
        // ══════════════════════════════════════════════════════

        // ──────────────────────────────────────────────────────
        // OPCIÓN A: RECALCULAR
        // "Bancarme la falla": el estado actual de la DB
        // pasa a ser el nuevo estado válido.
        // ──────────────────────────────────────────────────────
        public void Recalcular(List<InconsistenciaDV> inconsistencias)
        {
            foreach (var inc in inconsistencias)
            {
                // BLL recalcula, DAL guarda
                RecalcularYGuardar(inc.NombreTabla);
                _dal.MarcarInconsistenciaResuelta(inc.IdInconsistencia, "RECALCULO");
            }
        }

        // ──────────────────────────────────────────────────────
        // OPCIÓN B: RESTORE
        // Genera un .bak de la DB actual (por seguridad) y luego
        // restaura desde el backup indicado.
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// Genera un backup físico .bak de la DB actual en SQL Server.
        /// Retorna la ruta del archivo generado.
        /// </summary>
        public string EjecutarBackup()
        {
            try { Directory.CreateDirectory(CarpetaBackups); } catch { }

            // Calcular el número de secuencia para hoy
            string prefijoHoy = $"HotelRydl{DateTime.Now:ddMMyy}_";
            int secuencia = 1;

            if (Directory.Exists(CarpetaBackups))
            {
                // Contar cuántos backups de HOY ya existen
                var backupsHoy = Directory.GetFiles(
                    CarpetaBackups, $"{prefijoHoy}*.bak");
                secuencia = backupsHoy.Length + 1;
            }

            string nombreArchivo = $"{prefijoHoy}{secuencia:D2}.bak";
            string rutaArchivo = Path.Combine(CarpetaBackups, nombreArchivo);

            _dal.EjecutarBackupSQLServer(rutaArchivo);
            return rutaArchivo;
        }

        /// <summary>
        /// Restaura la DB desde el archivo .bak indicado.
        /// Después del restore recalcula los DVs con el estado restaurado
        /// y marca las inconsistencias como resueltas.
        /// </summary>
        public void EjecutarRestore(string rutaArchivoBak,
                                     List<InconsistenciaDV> inconsistencias)
        {
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
            System.Threading.Thread.Sleep(2000);

            _dal.EjecutarRestoreSQLServer(rutaArchivoBak);

            // Un backup anterior a PN2 no trae las tablas de reposición: se vuelven a crear antes de recalcular
            ReposicionSetup.Asegurar();

            RecalcularTodo();

            foreach (var inc in inconsistencias)
                _dal.MarcarInconsistenciaResuelta(inc.IdInconsistencia, "RESTORE");

            // Registrar en bitácora quién hizo el restore y desde qué archivo
            try
            {
                string nombreArchivo = Path.GetFileName(rutaArchivoBak);
                string usuario = SesionManager.Instancia.UsuarioActual?.NombreUsuario
                                       ?? "Administrador";
                int? idUsuario = SesionManager.Instancia.UsuarioActual?.IdUsuario;

                _bDAL.Registrar(
                    idUsuario,
                    idModulo: 1,
                    idTipoEvento: 10,   // ajustá al id que corresponda en tu tabla TiposEvento
                    criticidad: 1,
                    detalle: $"RESTORE ejecutado desde archivo: {nombreArchivo}. Usuario: {usuario}.",
                    resultado: "EXITOSO");
            }
            catch { /* la bitácora no debe bloquear el restore */ }
        }

        /// <summary>
        /// Lista los archivos .bak disponibles ordenados del más reciente al más antiguo.
        /// </summary>
        public List<string> ObtenerBackupsDisponibles()
        {
            if (!Directory.Exists(CarpetaBackups))
                return new List<string>();

            // Filtra solo los backups de HotelRydl, no otros archivos .bak
            // que SQL Server pueda tener en esa carpeta
            var archivos = new List<string>(
                Directory.GetFiles(CarpetaBackups, "HotelRydl*.bak"));

            archivos.Sort();
            archivos.Reverse();  // más reciente primero
            return archivos;
        }

        public List<string> ObtenerBackupsDesde(string carpeta)
        {
            if (!Directory.Exists(carpeta))
                return new List<string>();

            var archivos = new List<string>(
                Directory.GetFiles(carpeta, "HotelRydl*.bak"));

            archivos.Sort();
            archivos.Reverse();
            return archivos;
        }

        public List<InconsistenciaDV> ObtenerInconsistenciasPendientes()
            => _dal.ObtenerInconsistencias(soloNoResueltas: true);
    
    }
}
