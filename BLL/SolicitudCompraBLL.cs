using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>CUN06 Generar solicitud de compra y CUN07 Validar solicitud de compra.</summary>
    public class SolicitudCompraBLL
    {
        private static readonly string[] TABLAS = { "SolicitudesCompra" };

        // ══════════════════════════════════════════════════════
        // CUN06
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<SolicitudCompraBE> GenerarSolicitud(SolicitudCompraBE pedido)
        {
            const string OP = "Generar solicitud de compra";
            if (!ControlAcceso.PuedeGenerarSolicitud())
                return ReposicionComun.Denegado<SolicitudCompraBE>(OP);

            if (pedido == null || pedido.IdInsumo <= 0)
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "Seleccioná un insumo.");
            if (pedido.CantidadSolicitada <= 0)
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "La cantidad debe ser mayor a cero.");
            if (pedido.CantidadSolicitada > ReposicionComun.CANTIDAD_MAX)
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "La cantidad supera el máximo permitido.");

            string prioridad = string.IsNullOrWhiteSpace(pedido.Prioridad) ? "Normal" : pedido.Prioridad.Trim();
            if (!ReposicionComun.PRIORIDADES.Contains(prioridad))
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "Prioridad inválida.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                int idSolicitud;
                InsumoBE insumo;

                using (var dal = new ReposicionDAL())
                {
                    insumo = dal.ObtenerInsumoPorId(pedido.IdInsumo)!;
                    if (insumo == null || !insumo.Activo)
                        return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "El insumo no existe o está inactivo.");

                    // Regla: no se duplican pedidos abiertos del mismo insumo
                    var abierta = dal.ObtenerSolicitudes("Pendiente").Concat(dal.ObtenerSolicitudes("Aprobada"))
                                     .FirstOrDefault(x => x.IdInsumo == pedido.IdInsumo);
                    if (abierta != null)
                        return ReposicionComun.Rechazo<SolicitudCompraBE>(OP,
                            $"Ya hay una solicitud {abierta.Estado.ToLower()} para {insumo.Nombre} (SC-{abierta.IdSolicitud:D4}).");

                    // Presupuesto estimado = cantidad × último precio pagado (si hay historia)
                    decimal? ultimo = dal.ObtenerUltimoPrecioInsumo(pedido.IdInsumo);
                    var nueva = new SolicitudCompraBE
                    {
                        IdInsumo = pedido.IdInsumo,
                        CantidadSolicitada = pedido.CantidadSolicitada,
                        Prioridad = prioridad,
                        PresupuestoEstimado = ultimo.HasValue ? Math.Round(ultimo.Value * pedido.CantidadSolicitada, 2) : (decimal?)null,
                        Observaciones = pedido.Observaciones
                    };
                    idSolicitud = dal.InsertarSolicitud(nueva, ReposicionComun.IdActual);
                }

                // Avisos y trazabilidad (no deshacen la solicitud ya registrada)
                new NotificacionBLL().NotificarSolicitud(idSolicitud);
                BitacoraReposicion.Registrar(BitacoraReposicion.SOLICITUD_GENERADA, 4,
                    $"SC-{idSolicitud:D4}: {pedido.CantidadSolicitada:0.##} {insumo.UnidadMedida} de {insumo.Nombre}, prioridad {prioridad}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                using var lectura = new ReposicionDAL();
                return ResultadoReposicion<SolicitudCompraBE>.Ok(lectura.ObtenerSolicitudPorId(idSolicitud)!,
                    "Solicitud generada y Compras fue notificado.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<SolicitudCompraBE>(OP, ex); }
        }

        // ══════════════════════════════════════════════════════
        // CUN07
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<List<SolicitudCompraBE>> ObtenerSolicitudesPendientes()
            => Listar("Ver solicitudes pendientes", d => d.ObtenerSolicitudesPendientes(), ControlAcceso.PuedeGestionarCompras());

        /// <summary>Aprobadas sin orden de compra todavía (entrada de CUN08).</summary>
        public ResultadoReposicion<List<SolicitudCompraBE>> ObtenerSolicitudesAprobadas()
            => Listar("Ver solicitudes aprobadas", d => d.ObtenerSolicitudesAprobadas(), ControlAcceso.PuedeGestionarCompras());

        /// <summary>Historial completo (todos los estados) para consulta.</summary>
        public ResultadoReposicion<List<SolicitudCompraBE>> ObtenerHistorial()
            => Listar("Ver historial de solicitudes", d => d.ObtenerSolicitudes(), ControlAcceso.PuedeAccederReposicion());

        private static ResultadoReposicion<List<SolicitudCompraBE>> Listar(string op,
            Func<ReposicionDAL, List<SolicitudCompraBE>> consulta, bool permitido)
        {
            if (!permitido) return ReposicionComun.Denegado<List<SolicitudCompraBE>>(op);
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<SolicitudCompraBE>>.Ok(consulta(dal));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<SolicitudCompraBE>>(op, ex); }
        }

        public ResultadoReposicion<SolicitudCompraBE> ValidarSolicitud(int idSolicitud, bool aprobada, string motivo)
        {
            const string OP = "Validar solicitud de compra";
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<SolicitudCompraBE>(OP);

            motivo = (motivo ?? "").Trim();
            if (!aprobada && motivo.Length == 0)
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "Para rechazar una solicitud indicá el motivo.");
            if (motivo.Length > 250)
                return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "El motivo no puede superar los 250 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                string nuevoEstado = aprobada ? "Aprobada" : "Rechazada";
                SolicitudCompraBE? s;

                using (var dal = new ReposicionDAL())
                {
                    s = dal.ObtenerSolicitudPorId(idSolicitud);
                    if (s == null)
                        return ReposicionComun.Rechazo<SolicitudCompraBE>(OP, "La solicitud no existe.");

                    // Solo cambia si sigue 'Pendiente': dos usuarios no pueden validarla a la vez
                    string? obs = motivo.Length == 0 ? null : (aprobada ? "Aprobada: " : "Rechazada: ") + motivo;
                    bool ok = dal.ActualizarEstadoSolicitud(idSolicitud, nuevoEstado, "Pendiente",
                                                            ReposicionComun.IdActual, obs);
                    if (!ok)
                        return ReposicionComun.Rechazo<SolicitudCompraBE>(OP,
                            $"La solicitud SC-{idSolicitud:D4} ya fue procesada (estado: {s.Estado}).");
                }

                BitacoraReposicion.Registrar(aprobada ? BitacoraReposicion.SOLICITUD_APROBADA : BitacoraReposicion.SOLICITUD_RECHAZADA, 3,
                    $"SC-{idSolicitud:D4} ({s.NombreInsumo}) {nuevoEstado.ToLower()}{(motivo.Length > 0 ? ". " + motivo : "")}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                using var lectura = new ReposicionDAL();
                return ResultadoReposicion<SolicitudCompraBE>.Ok(lectura.ObtenerSolicitudPorId(idSolicitud)!,
                    aprobada ? "Solicitud aprobada: puede continuar con la compra." : "Solicitud rechazada.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<SolicitudCompraBE>(OP, ex); }
        }
    }
}
