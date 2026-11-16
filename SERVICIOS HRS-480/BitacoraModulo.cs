using DAL_HRS_480;
using SEGURIDAD_HRS_480;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Registra en la Bitácora de eventos buscando el módulo y el tipo de evento por NOMBRE
    /// (los crea Scripts\05_SQL_Entrega3_Maestros.sql). Una falla de bitácora nunca corta la operación.
    /// </summary>
    public static class BitacoraModulo
    {
        // Módulo "Dígito Verificador"
        public const string MOD_DV = "Dígito Verificador";
        public const string DV_INCONSISTENCIA = "Inconsistencia de DV detectada";
        public const string DV_RECALCULADO = "DV recalculado";
        public const string DV_BACKUP = "Backup generado";
        public const string DV_RESTORE = "Restore de backup ejecutado";
        public const string DV_CANCELADA = "Reparación de DV cancelada";

        // Módulo "Maestros"
        public const string MOD_MAESTROS = "Maestros";
        public const string CLIENTE_ALTA = "Alta de cliente";
        public const string CLIENTE_MODIF = "Modificación de cliente";
        public const string CLIENTE_BAJA = "Baja de cliente";
        public const string SERIALIZAR = "Clientes serializados a XML";
        public const string DESSERIALIZAR = "Clientes des-serializados desde XML";
        public const string INSUMO_ALTA = "Alta de insumo";
        public const string INSUMO_MODIF = "Modificación de insumo";
        public const string INSUMO_BAJA = "Baja de insumo";
        public const string CAMBIOS_CONSULTA = "Consulta de bitácora de cambios";
        public const string VERSION_ACTIVADA = "Versión de insumo activada";
        public const string MAESTROS_RECHAZO = "Operación rechazada en maestros";
        public const string MAESTROS_ERROR = "Error técnico en maestros";

        // Módulo "Reportes" (A02 Reporte Inteligente Predictivo)
        public const string MOD_REPORTES = "Reportes";
        public const string REPORTE_PREDICTIVO = "Reporte predictivo generado";
        public const string REPORTES_RECHAZO = "Acceso denegado a reportes";
        public const string REPORTES_ERROR = "Error técnico en reportes";

        private static readonly object _lock = new object();
        private static readonly Bitacora _bitacora = new Bitacora();
        private static readonly Dictionary<string, (int? mod, int? ev)> _cache = new Dictionary<string, (int?, int?)>();

        /// <summary>Olvida los Ids guardados (después de un restore pueden cambiar).</summary>
        public static void Reiniciar() { lock (_lock) _cache.Clear(); }

        public static void Registrar(string modulo, string evento, int criticidad, string detalle, string resultado, int? idUsuario = null)
        {
            try
            {
                (int? mod, int? ev) ids;
                lock (_lock)
                {
                    string clave = modulo + "|" + evento;
                    if (!_cache.TryGetValue(clave, out ids) || ids.ev == null)
                    {
                        int? idMod = _bitacora.ObtenerModulos().FirstOrDefault(m => m.Nombre == modulo)?.IdModulo;
                        int? idEv = idMod == null ? null
                                  : _bitacora.ObtenerTiposEvento(idMod).FirstOrDefault(t => t.Nombre == evento)?.IdTipoEvento;
                        ids = (idMod, idEv);
                        _cache[clave] = ids;
                    }
                }
                _bitacora.Registrar(idUsuario ?? SesionManager.Instancia.UsuarioActual?.IdUsuario,
                                    ids.mod, ids.ev, criticidad, detalle, resultado);
            }
            catch
            {
                // La bitácora no debe bloquear la operación
            }
        }
    }
}
