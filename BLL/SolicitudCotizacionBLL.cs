using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN06 Generar solicitud de cotización. El Encargado de Compras pide cotización de los insumos faltantes
    /// a un proveedor y se la envía. Si el proveedor nunca le vendió al hotel, antes lo pre-registra (CUN07, extiende a CUN06).
    /// Para cotizar con más proveedores, se repite con cada uno.
    /// </summary>
    public class SolicitudCotizacionBLL
    {
        private static readonly string[] TABLAS = { "SolicitudesCotizacion", "SolicitudesCotizacionDetalle" };
        private readonly EmailServicio _email = new EmailServicio();

        public ResultadoReposicion<SolicitudCotizacionBE> GenerarSolicitudCotizacion(int idProveedor, List<ItemInsumoBE> items)
        {
            const string OP = "Generar solicitud de cotización";
            if (!ControlAcceso.PuedeGenerarSolicitudCotizacion())
                return ReposicionComun.Denegado<SolicitudCotizacionBE>(OP);

            items = (items ?? new List<ItemInsumoBE>()).Where(i => i != null).ToList();
            if (idProveedor <= 0)
                return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "Elegí el proveedor al que le vas a pedir la cotización.");
            if (items.Count == 0)
                return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "Seleccioná al menos un insumo faltante.");
            if (items.Select(i => i.IdInsumo).Distinct().Count() != items.Count)
                return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "Hay un insumo repetido en la solicitud.");
            if (items.Any(i => i.Cantidad <= 0 || i.Cantidad > ReposicionComun.CANTIDAD_MAX))
                return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "Todas las cantidades deben ser mayores a cero.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                int idSolicitud;
                ProveedorBE prov;
                using (var dal = new ReposicionDAL())
                {
                    prov = dal.ObtenerProveedorPorId(idProveedor)!;
                    if (prov == null || !prov.Activo)
                        return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "El proveedor no existe o está inactivo.");
                    foreach (var it in items)
                    {
                        var ins = dal.ObtenerInsumoPorId(it.IdInsumo);
                        if (ins == null || !ins.Activo)
                            return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP, "Uno de los insumos no existe o está inactivo.");
                        if (!ins.Faltante)
                            return ReposicionComun.Rechazo<SolicitudCotizacionBE>(OP,
                                $"{ins.Nombre} no es un faltante (stock {ins.StockActual:0.##}, mínimo {ins.StockMinimo:0.##}).");
                    }

                    dal.Iniciar();
                    var s = new SolicitudCotizacionBE { NroSolicitud = dal.GenerarNroSolicitud(), IdProveedor = idProveedor };
                    idSolicitud = dal.InsertarSolicitudCotizacion(s, ReposicionComun.IdActual);
                    foreach (var it in items) dal.InsertarDetalleSolicitud(idSolicitud, it);
                    dal.Confirmar();
                }

                SolicitudCotizacionBE sol;
                using (var lectura = new ReposicionDAL()) sol = lectura.ObtenerSolicitudPorId(idSolicitud)!;

                // Se le envía al proveedor (fuera de la transacción: un correo no se puede deshacer)
                var envio = _email.EnviarSolicitudCotizacion(sol);
                if (envio.Exito) { using var d2 = new ReposicionDAL(); d2.MarcarSolicitudEnviada(idSolicitud); sol.FechaEnvio = DateTime.Now; }

                BitacoraReposicion.Registrar(BitacoraReposicion.SOLICITUD_COTIZACION, 4,
                    $"{sol.NroSolicitud} a {prov.Nombre}: " + string.Join(", ", sol.Items.Select(i => $"{i.NombreInsumo} x{i.Cantidad:0.##}")) + ". " + envio.Mensaje,
                    "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                return ResultadoReposicion<SolicitudCotizacionBE>.Ok(sol,
                    $"Solicitud {sol.NroSolicitud} generada para {prov.Nombre}. " +
                    (envio.Exito ? envio.Mensaje : "No se pudo enviar por correo (" + envio.Mensaje + "): enviala por otro medio."));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<SolicitudCotizacionBE>(OP, ex); }
        }

        /// <summary>Solicitudes enviadas que esperan la cotización del proveedor (entrada de CUN08).</summary>
        public ResultadoReposicion<List<SolicitudCotizacionBE>> ObtenerSolicitudesSinCotizar()
        {
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<List<SolicitudCotizacionBE>>("Ver solicitudes de cotización");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<SolicitudCotizacionBE>>.Ok(dal.ObtenerSolicitudes("Enviada"));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<SolicitudCotizacionBE>>("Ver solicitudes de cotización", ex); }
        }
    }
}
