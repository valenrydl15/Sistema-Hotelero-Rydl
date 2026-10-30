using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN08 Generar pre-orden de compra. El Encargado de Compras registra las cotizaciones que mandan los proveedores
    /// (productos que pueden proveer, cantidad disponible, precio unitario y sus datos bancarios), las compara,
    /// elige para cada insumo la oferta que prefiere y genera las pre-órdenes (una por proveedor) en estado "Pre-orden".
    /// </summary>
    public class PreOrdenBLL
    {
        private static readonly string[] TABLAS_COT = { "Cotizaciones", "CotizacionesDetalle", "SolicitudesCotizacion" };
        private static readonly string[] TABLAS_PRE = { "OrdenesCompra", "OrdenesCompraDetalle", "Cotizaciones" };

        /// <summary>Registra la cotización que respondió el proveedor a una solicitud. Cantidad del ítem = cantidad disponible.</summary>
        public ResultadoReposicion<CotizacionBE> RegistrarCotizacion(int idSolicitud, List<ItemInsumoBE> items,
                                                                      string cuit, string banco, string cbu, string titular)
        {
            const string OP = "Registrar cotización";
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<CotizacionBE>(OP);

            items = (items ?? new List<ItemInsumoBE>()).Where(i => i != null && i.Cantidad > 0).ToList();
            banco = (banco ?? "").Trim(); titular = (titular ?? "").Trim();
            if (items.Count == 0)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "El proveedor tiene que cotizar al menos un insumo (cantidad disponible mayor a cero).");
            if (items.Any(i => i.PrecioUnitario <= 0 || i.PrecioUnitario > 99_999_999m || i.Cantidad > ReposicionComun.CANTIDAD_MAX))
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Cada insumo cotizado necesita un precio unitario mayor a cero.");
            if (!BancoServicio.ValidarCUIT(cuit))
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "El CUIT del proveedor debe tener 11 dígitos.");
            if (!BancoServicio.ValidarCBU(cbu))
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "El CBU del proveedor debe tener 22 dígitos.");
            if (banco.Length == 0) banco = BancoServicio.BancoDelCBU(cbu);
            if (banco.Length == 0 || titular.Length == 0)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Cargá el banco y el titular de la cuenta del proveedor.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS_COT);
                int idCot;
                SolicitudCotizacionBE? sol;
                using (var dal = new ReposicionDAL())
                {
                    sol = dal.ObtenerSolicitudPorId(idSolicitud);
                    if (sol == null) return ReposicionComun.Rechazo<CotizacionBE>(OP, "La solicitud de cotización no existe.");
                    if (sol.Estado != "Enviada")
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, $"La solicitud {sol.NroSolicitud} ya tiene su cotización registrada.");
                    if (items.Any(i => sol.Items.All(s => s.IdInsumo != i.IdInsumo)))
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, "Solo se cotizan los insumos que estaban en la solicitud.");

                    dal.Iniciar();
                    idCot = dal.InsertarCotizacion(new CotizacionBE
                    {
                        IdSolicitudCotizacion = idSolicitud, IdProveedor = sol.IdProveedor,
                        CUIT = BancoServicio.SoloDigitos(cuit), Banco = banco, CBU = BancoServicio.SoloDigitos(cbu), Titular = titular
                    }, ReposicionComun.IdActual);
                    foreach (var it in items) dal.InsertarDetalleCotizacion(idCot, it);
                    if (!dal.ActualizarEstadoSolicitud(idSolicitud, "Cotizada", "Enviada"))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, "Otro usuario registró la cotización de esta solicitud.");
                    }
                    dal.Confirmar();
                }
                CotizacionBE cot;
                using (var lectura = new ReposicionDAL()) cot = lectura.ObtenerCotizacionPorId(idCot)!;
                BitacoraReposicion.Registrar(BitacoraReposicion.COTIZACION, 4,
                    $"{sol.NroSolicitud} de {cot.NombreProveedor}: " + string.Join(", ", cot.Items.Select(i => $"{i.NombreInsumo} {i.Cantidad:0.##} x $ {i.PrecioUnitario:N2}")) + ".",
                    "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS_COT);
                return ResultadoReposicion<CotizacionBE>.Ok(cot, $"Cotización de {cot.NombreProveedor} registrada ({cot.Items.Count} insumos).");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<CotizacionBE>(OP, ex); }
        }

        /// <summary>
        /// Comparación: todas las ofertas de las cotizaciones recibidas. Por insumo se marca la más conveniente
        /// (menor precio unitario; en empate, la de más cantidad disponible) y queda elegida por defecto.
        /// </summary>
        public ResultadoReposicion<List<OfertaBE>> ObtenerOfertasParaComparar()
        {
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<List<OfertaBE>>("Comparar cotizaciones");
            try
            {
                using var dal = new ReposicionDAL();
                var ofertas = dal.ObtenerOfertasPendientes();
                foreach (var g in ofertas.GroupBy(o => o.IdInsumo))
                {
                    var mejor = g.OrderBy(o => o.PrecioUnitario).ThenByDescending(o => o.CantidadDisponible).First();
                    mejor.MasConveniente = mejor.Elegida = true;
                }
                return ResultadoReposicion<List<OfertaBE>>.Ok(ofertas.OrderBy(o => o.NombreInsumo).ThenBy(o => o.PrecioUnitario).ToList());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OfertaBE>>("Comparar cotizaciones", ex); }
        }

        /// <summary>Genera una pre-orden por proveedor con las ofertas elegidas (a lo sumo una por insumo).</summary>
        public ResultadoReposicion<List<OrdenCompraBE>> GenerarPreOrdenes(List<OfertaBE> elegidas)
        {
            const string OP = "Generar pre-orden de compra";
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<List<OrdenCompraBE>>(OP);
            elegidas = (elegidas ?? new List<OfertaBE>()).Where(o => o != null).ToList();
            if (elegidas.Count == 0)
                return ReposicionComun.Rechazo<List<OrdenCompraBE>>(OP, "Elegí al menos una oferta.");
            if (elegidas.GroupBy(o => o.IdInsumo).Any(g => g.Count() > 1))
                return ReposicionComun.Rechazo<List<OrdenCompraBE>>(OP, "Elegí una sola oferta por insumo.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS_PRE);
                var ids = new List<int>();
                using (var dal = new ReposicionDAL())
                {
                    var pendientes = dal.ObtenerOfertasPendientes();
                    var reales = new List<OfertaBE>();
                    foreach (var e in elegidas)
                    {
                        var r = pendientes.FirstOrDefault(p => p.IdCotizacion == e.IdCotizacion && p.IdInsumo == e.IdInsumo);
                        if (r == null)
                            return ReposicionComun.Rechazo<List<OrdenCompraBE>>(OP, "Una de las ofertas elegidas ya no está disponible (cotización procesada).");
                        reales.Add(r);
                    }
                    var decididos = new HashSet<int>(reales.Select(r => r.IdInsumo));

                    dal.Iniciar();
                    foreach (var g in reales.GroupBy(r => r.IdCotizacion))
                    {
                        var items = g.Select(r => new ItemInsumoBE { IdInsumo = r.IdInsumo, Cantidad = r.CantidadAPedir, PrecioUnitario = r.PrecioUnitario }).ToList();
                        var o = new OrdenCompraBE
                        {
                            NroOrden = dal.GenerarNroOrden(),
                            IdProveedor = g.First().IdProveedor,
                            IdCotizacion = g.Key,
                            Total = items.Sum(i => i.Subtotal)
                        };
                        int idOrden = dal.InsertarOrden(o, ReposicionComun.IdActual);
                        foreach (var it in items) dal.InsertarDetalleOrden(idOrden, it);
                        ids.Add(idOrden);
                    }
                    // Una cotización queda procesada cuando ya se decidieron todos sus insumos
                    foreach (var cot in pendientes.GroupBy(p => p.IdCotizacion))
                        if (cot.All(p => decididos.Contains(p.IdInsumo)))
                            dal.ActualizarEstadoCotizacion(cot.Key, "Procesada", "Recibida");
                    dal.Confirmar();
                }

                var ordenes = new List<OrdenCompraBE>();
                using (var lectura = new ReposicionDAL()) foreach (var id in ids) ordenes.Add(lectura.ObtenerOrdenPorId(id)!);
                foreach (var o in ordenes)
                {
                    new NotificacionBLL().NotificarRol(ControlAcceso.ROL_GERENTE, "Pre-orden para aprobar",
                        $"Nueva pre-orden {o.NroOrden} de {o.NombreProveedor} por $ {o.Total:N2}: {o.ResumenItems}." +
                        (o.EstadoProveedor == ProveedorBE.REGISTRADO ? "" : " El proveedor está pre-registrado."), o.IdOrden);
                    BitacoraReposicion.Registrar(BitacoraReposicion.PREORDEN, 4, $"{o.NroOrden} a {o.NombreProveedor}: {o.ResumenItems}; total $ {o.Total:N2}.", "EXITOSO");
                }
                DigitoVerificador.RecalcularSi(dvOk, TABLAS_PRE);
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(ordenes,
                    $"{ordenes.Count} pre-orden(es) generada(s): {string.Join(", ", ordenes.Select(o => o.NroOrden))}. El Gerente de Compras fue notificado.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>(OP, ex); }
        }
    }
}
