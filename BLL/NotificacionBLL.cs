using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Avisos internos del proceso. Son "mejor esfuerzo": si fallan no se deshace la operación de negocio
    /// (la operación ya quedó registrada); el fallo queda en bitácora.
    /// </summary>
    public class NotificacionBLL
    {
        /// <summary>Avisa a todos los usuarios activos de un rol (si no hay ninguno, a los Administradores).</summary>
        public int NotificarRol(string rol, string tipo, string mensaje, int? idOrden = null)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var destinatarios = dal.ObtenerUsuariosPorRol(rol);
                if (destinatarios.Count == 0 && rol == ControlAcceso.ROL_DEPOSITO)
                    destinatarios = dal.ObtenerUsuariosPorRol("Encargado de Almacén");   // nombre viejo del rol
                if (destinatarios.Count == 0) destinatarios = dal.ObtenerUsuariosPorRol(ControlAcceso.ROL_ADMIN);
                foreach (var u in destinatarios)
                    dal.InsertarNotificacion(new NotificacionBE { IdUsuarioDestino = u.IdUsuario, IdOrden = idOrden, Tipo = tipo, Mensaje = mensaje });
                return destinatarios.Count;
            }
            catch (Exception ex) { return Falla("Notificar " + rol, ex); }
        }

        // ── Bandeja del usuario actual ───────────────────────────────────────
        public List<NotificacionBE> ObtenerMisNotificaciones(bool soloNoLeidas)
        {
            try
            {
                int id = ReposicionComun.IdActual;
                if (id == 0) return new List<NotificacionBE>();
                using var dal = new ReposicionDAL();
                return dal.ObtenerNotificaciones(id, soloNoLeidas);
            }
            catch { return new List<NotificacionBE>(); }
        }

        public int ContarNoLeidas()
        {
            try
            {
                int id = ReposicionComun.IdActual;
                if (id == 0) return 0;
                using var dal = new ReposicionDAL();
                return dal.ContarNotificacionesNoLeidas(id);
            }
            catch { return 0; }
        }

        public void MarcarTodasLeidas()
        {
            try
            {
                int id = ReposicionComun.IdActual;
                if (id == 0) return;
                using var dal = new ReposicionDAL();
                dal.MarcarNotificacionesLeidas(id);
            }
            catch { }
        }

        // ── util ─────────────────────────────────────────────────────────────
        private static int Falla(string operacion, Exception ex)
        {
            BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1,
                $"{operacion}: no se pudo notificar. {ex.Message}", "FALLIDO");
            return 0;
        }
    }
}
