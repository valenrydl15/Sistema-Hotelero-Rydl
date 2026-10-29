using SEGURIDAD_HRS_480;
using DAL_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Fachada de Bitácora del proceso PN2 (Reposición de insumos).
    /// El módulo y los tipos de evento se buscan por NOMBRE (los crea Scripts\02_SQL_PN2_Reposicion.sql).
    /// Un fallo de bitácora nunca interrumpe la operación de negocio.
    /// </summary>
    public static class BitacoraReposicion
    {
        public const string MODULO = "Reposición de insumos";

        public const string CONSULTA_STOCK     = "Consulta de stock";
        public const string SOLICITUD_GENERADA = "Solicitud de compra generada";
        public const string SOLICITUD_APROBADA = "Solicitud de compra aprobada";
        public const string SOLICITUD_RECHAZADA = "Solicitud de compra rechazada";
        public const string ORDEN_EMITIDA      = "Orden de compra emitida y enviada";
        public const string ERROR_ENVIO        = "Error al enviar orden al proveedor";
        public const string RECEPCION          = "Recepción de insumos registrada";
        public const string RECEPCION_DIF      = "Recepción con diferencia";
        public const string STOCK_ACTUALIZADO  = "Stock actualizado";
        public const string REEMBOLSO          = "Solicitud de reembolso registrada";
        public const string OPERACION_RECH     = "Operación de reposición rechazada";
        public const string ACCESO_DENEGADO    = "Acceso denegado a reposición";
        public const string ERROR_TECNICO      = "Error técnico de reposición";
        public const string REPORTE_ORDEN      = "Reporte de orden de compra exportado";

        private static readonly object _lock = new object();
        private static readonly Bitacora _bitacora = new Bitacora();
        private static int? _idModulo;
        private static Dictionary<string, int> _ids = new Dictionary<string, int>();

        private static void Cargar()
        {
            if (_idModulo != null && _ids.Count > 0) return;
            foreach (var m in _bitacora.ObtenerModulos())
                if (m.Nombre == MODULO) { _idModulo = m.IdModulo; break; }
            if (_idModulo == null) return;
            var nuevo = new Dictionary<string, int>();
            foreach (var t in _bitacora.ObtenerTiposEvento(_idModulo))
                nuevo[t.Nombre] = t.IdTipoEvento;
            _ids = nuevo;
        }

        public static void Registrar(string evento, int criticidad, string detalle, string resultado)
        {
            try
            {
                int? idUsuario = SesionManager.Instancia.UsuarioActual?.IdUsuario;
                int? idEvento = null;
                int? idModulo;
                lock (_lock)
                {
                    Cargar();
                    idModulo = _idModulo;
                    if (_ids.TryGetValue(evento, out int id)) idEvento = id;
                }
                _bitacora.Registrar(idUsuario, idModulo, idEvento, criticidad, detalle, resultado);
            }
            catch
            {
                // La bitácora no debe bloquear la operación de negocio
            }
        }
    }
}
