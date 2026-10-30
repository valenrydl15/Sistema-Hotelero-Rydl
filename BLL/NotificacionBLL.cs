using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Avisos internos del proceso. Son "mejor esfuerzo": si fallan no se deshace la operación de negocio
    /// (la pre-orden o la recepción ya quedaron registradas); el fallo queda en bitácora.
    /// </summary>
    public class NotificacionBLL
    {
        /// <summary>Destinatarios = usuarios activos del rol; si no hay ninguno, los Administradores.</summary>
        private static List<Usuario> Destinatarios(ReposicionDAL dal, string rol)
        {
            var lista = dal.ObtenerUsuariosPorRol(rol);
            return lista.Count > 0 ? lista : dal.ObtenerUsuariosPorRol(ControlAcceso.ROL_ADMIN);
        }

        /// <summary>CUN07: avisa a Compras que hay una pre-orden lista para pagar.</summary>
        public int NotificarPreOrden(int idPreOrden)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var po = dal.ObtenerPreOrdenPorId(idPreOrden);
                if (po == null) return 0;

                string msg = $"Nueva pre-orden {po.NroPreOrden}: {po.Cantidad:0.##} {po.UnidadMedida} de {po.NombreInsumo} " +
                             $"a {po.NombreProveedor} por $ {po.PrecioTotal:N2} (prioridad {po.Prioridad})." +
                             (po.ProveedorNuevo ? " Es un proveedor nuevo: hay que registrarlo antes de pagar." : "");
                int n = 0;
                foreach (var u in Destinatarios(dal, ControlAcceso.ROL_COMPRAS))
                {
                    dal.InsertarNotificacion(new NotificacionBE
                    { IdUsuarioDestino = u.IdUsuario, Tipo = "Pre-orden para pagar", Mensaje = msg });
                    n++;
                }
                return n;
            }
            catch (Exception ex) { return Falla("NotificarPreOrden", ex); }
        }

        /// <summary>CUN10 (no coincide): avisa a Compras para que lo reclame al proveedor.</summary>
        public int NotificarDiferenciaRecepcion(int idOrden, string diferencias)
        {
            try
            {
                using var dal = new ReposicionDAL();
                var o = dal.ObtenerOrdenPorId(idOrden);
                if (o == null) return 0;

                string msg = $"Los insumos recibidos de la orden {o.NroOrden} ({o.NombreInsumo}, proveedor {o.NombreProveedor}) " +
                             $"no coinciden: {diferencias}. Reclamalo al proveedor.";
                int n = 0;
                foreach (var u in Destinatarios(dal, ControlAcceso.ROL_COMPRAS))
                {
                    dal.InsertarNotificacion(new NotificacionBE
                    { IdUsuarioDestino = u.IdUsuario, IdOrden = idOrden, Tipo = "Recepción con diferencia", Mensaje = msg });
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
