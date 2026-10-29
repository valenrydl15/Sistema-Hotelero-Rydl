using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Avisos internos del proceso. Son "mejor esfuerzo": si fallan no se deshace la operación de negocio
    /// (la solicitud o la recepción ya quedaron registradas); el fallo queda en bitácora.
    /// </summary>
    public class NotificacionBLL
    {
        /// <summary>Destinatarios = usuarios activos del rol; si no hay ninguno, los Administradores.</summary>
        private static List<Usuario> Destinatarios(ReposicionDAL dal, string rol)
        {
            var lista = dal.ObtenerUsuariosPorRol(rol);
            return lista.Count > 0 ? lista : dal.ObtenerUsuariosPorRol(ControlAcceso.ROL_ADMIN);
        }

        /// <summary>CUN06: avisa a Compras que hay una nueva solicitud para validar.</summary>
        public int NotificarSolicitud(int idSolicitud)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var s = dal.ObtenerSolicitudPorId(idSolicitud);
                if (s == null) return 0;

                string msg = $"Nueva solicitud de compra SC-{s.IdSolicitud:D4}: {s.CantidadSolicitada:0.##} {s.UnidadMedida} de " +
                             $"{s.NombreInsumo} (prioridad {s.Prioridad}) solicitada el {s.FechaSolicitud:dd/MM/yyyy HH:mm}.";
                int n = 0;
                foreach (var u in Destinatarios(dal, ControlAcceso.ROL_COMPRAS))
                {
                    dal.InsertarNotificacion(new NotificacionBE
                    { IdUsuarioDestino = u.IdUsuario, IdSolicitud = idSolicitud, Tipo = "Solicitud pendiente", Mensaje = msg });
                    n++;
                }
                return n;
            }
            catch (Exception ex) { return Falla("NotificarSolicitud", ex); }
        }

        /// <summary>CUN07 (rechazo): avisa a quien pidió la compra que fue rechazada y por qué.</summary>
        public int NotificarRechazo(int idSolicitud, string motivo)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var s = dal.ObtenerSolicitudPorId(idSolicitud);
                if (s == null) return 0;

                if (s.IdUsuarioSolicita == null) return 0;

                dal.InsertarNotificacion(new NotificacionBE
                {
                    IdUsuarioDestino = s.IdUsuarioSolicita,
                    IdSolicitud = idSolicitud,
                    Tipo = "Solicitud rechazada",
                    Mensaje = $"La solicitud SC-{s.IdSolicitud:D4} ({s.NombreInsumo}) fue rechazada. Motivo: {motivo}"
                });
                return 1;
            }
            catch (Exception ex) { return Falla("NotificarRechazo", ex); }
        }

        /// <summary>CUN10 (diferencia): avisa a Compras que se recibió una cantidad distinta de la pedida.</summary>
        public int NotificarDiferenciaRecepcion(int idOrden, decimal cantidadRecibida)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var o = dal.ObtenerOrdenPorId(idOrden);
                if (o == null) return 0;

                string msg = $"Recepción con diferencia en la orden {o.NroOrden} ({o.NombreInsumo}, proveedor {o.NombreProveedor}): " +
                             $"se pidieron {o.Cantidad:0.##} y se recibieron {cantidadRecibida:0.##}. " +
                             "Evalúe registrar una solicitud de reembolso.";
                int n = 0;
                foreach (var u in Destinatarios(dal, ControlAcceso.ROL_COMPRAS))
                {
                    dal.InsertarNotificacion(new NotificacionBE
                    { IdUsuarioDestino = u.IdUsuario, IdOrden = idOrden, Tipo = "Diferencia en recepción", Mensaje = msg });
                    n++;
                }
                return n;
            }
            catch (Exception ex) { return Falla("NotificarDiferenciaRecepcion", ex); }
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
