using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN06 Realizar cotización. El Encargado de Depósito revisa el stock y cotiza el insumo con varios
    /// proveedores para ver cuál es el más conveniente (menor total y, si empatan, menor plazo).
    /// </summary>
    public class CotizacionBLL
    {
        public const int MIN_PROVEEDORES = 2;
        public const decimal PRECIO_MAX = 99_999_999m;
        public const int PLAZO_MAX_DIAS = 365;
        private static readonly string[] TABLAS = { "Cotizaciones", "CotizacionesDetalle" };

        public ResultadoReposicion<CotizacionBE> RegistrarCotizacion(int idInsumo, decimal cantidad,
                                                                      List<CotizacionDetalleBE> ofertas, string? observaciones = null)
        {
            const string OP = "Realizar cotización";
            if (!ControlAcceso.PuedeRealizarCotizacion())
                return ReposicionComun.Denegado<CotizacionBE>(OP);

            observaciones = (observaciones ?? "").Trim();
            if (idInsumo <= 0)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Seleccioná el insumo a cotizar.");
            if (cantidad <= 0 || cantidad > ReposicionComun.CANTIDAD_MAX)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "La cantidad a cotizar debe ser mayor a cero.");
            if (observaciones.Length > 300)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Las observaciones no pueden superar los 300 caracteres.");

            ofertas = (ofertas ?? new List<CotizacionDetalleBE>()).Where(o => o != null).ToList();
            if (ofertas.Select(o => o.IdProveedor).Distinct().Count() != ofertas.Count)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Hay un proveedor cargado dos veces en la cotización.");
            if (ofertas.Count < MIN_PROVEEDORES)
                return ReposicionComun.Rechazo<CotizacionBE>(OP,
                    $"Cargá el precio de al menos {MIN_PROVEEDORES} proveedores para poder comparar.");
            foreach (var o in ofertas)
            {
                if (o.PrecioUnitario <= 0 || o.PrecioUnitario > PRECIO_MAX)
                    return ReposicionComun.Rechazo<CotizacionBE>(OP, "Todos los precios cotizados deben ser mayores a cero.");
                if (o.PlazoDias < 0 || o.PlazoDias > PLAZO_MAX_DIAS)
                    return ReposicionComun.Rechazo<CotizacionBE>(OP, $"El plazo de entrega debe estar entre 0 y {PLAZO_MAX_DIAS} días.");
            }

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                int idCotizacion;
                InsumoBE insumo;

                using (var dal = new ReposicionDAL())
                {
                    insumo = dal.ObtenerInsumoPorId(idInsumo)!;
                    if (insumo == null || !insumo.Activo)
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, "El insumo no existe o está inactivo.");

                    // Regla: no se cotiza dos veces el mismo insumo mientras haya un pedido abierto
                    string? abierto = dal.ObtenerPedidoAbiertoDeInsumo(idInsumo);
                    if (abierto != null)
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, $"{insumo.Nombre} ya tiene {abierto} sin terminar.");

                    foreach (var o in ofertas)
                    {
                        var p = dal.ObtenerProveedorPorId(o.IdProveedor);
                        if (p == null || !p.Activo)
                            return ReposicionComun.Rechazo<CotizacionBE>(OP, "Uno de los proveedores no existe o está inactivo.");
                    }

                    dal.Iniciar();
                    idCotizacion = dal.InsertarCotizacion(new CotizacionBE
                    {
                        IdInsumo = idInsumo,
                        Cantidad = cantidad,
                        Observaciones = observaciones
                    }, ReposicionComun.IdActual);
                    foreach (var o in ofertas)
                        dal.InsertarDetalleCotizacion(new CotizacionDetalleBE
                        {
                            IdCotizacion = idCotizacion,
                            IdProveedor = o.IdProveedor,
                            PrecioUnitario = o.PrecioUnitario,
                            PlazoDias = o.PlazoDias
                        });
                    dal.Confirmar();
                }

                CotizacionBE cot;
                using (var lectura = new ReposicionDAL()) cot = lectura.ObtenerCotizacionPorId(idCotizacion)!;
                var mejor = cot.Detalles.First(d => d.MasConveniente);

                BitacoraReposicion.Registrar(BitacoraReposicion.COTIZACION, 4,
                    $"{cot.NroCotizacion}: {cantidad:0.##} {insumo.UnidadMedida} de {insumo.Nombre} con {cot.Detalles.Count} proveedores. " +
                    $"Más conveniente: {mejor.NombreProveedor} ($ {mejor.Subtotal:N2}, {mejor.PlazoDias} días).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                return ResultadoReposicion<CotizacionBE>.Ok(cot,
                    $"Cotización {cot.NroCotizacion} registrada. La más conveniente es {mejor.NombreProveedor} por $ {mejor.Subtotal:N2}.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<CotizacionBE>(OP, ex); }
        }

        /// <summary>Cotizaciones abiertas (entrada de CUN07).</summary>
        public ResultadoReposicion<List<CotizacionBE>> ObtenerCotizacionesAbiertas()
        {
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<List<CotizacionBE>>("Ver cotizaciones abiertas");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<CotizacionBE>>.Ok(dal.ObtenerCotizaciones("Abierta"));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<CotizacionBE>>("Ver cotizaciones abiertas", ex); }
        }

        /// <summary>Si la cotización se cargó mal, se anula y el insumo se puede volver a cotizar.</summary>
        public ResultadoReposicion<CotizacionBE> AnularCotizacion(int idCotizacion, string motivo)
        {
            const string OP = "Anular cotización";
            if (!ControlAcceso.PuedeRealizarCotizacion())
                return ReposicionComun.Denegado<CotizacionBE>(OP);
            motivo = (motivo ?? "").Trim();
            if (motivo.Length == 0)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "Indicá el motivo de la anulación.");
            if (motivo.Length > 250)
                return ReposicionComun.Rechazo<CotizacionBE>(OP, "El motivo no puede superar los 250 caracteres.");
            try
            {
                bool dvOk = DigitoVerificador.Consistente("Cotizaciones");
                CotizacionBE? c;
                using (var dal = new ReposicionDAL())
                {
                    c = dal.ObtenerCotizacionPorId(idCotizacion);
                    if (c == null) return ReposicionComun.Rechazo<CotizacionBE>(OP, "La cotización no existe.");
                    if (!dal.ActualizarEstadoCotizacion(idCotizacion, "Anulada", "Abierta"))
                        return ReposicionComun.Rechazo<CotizacionBE>(OP, $"La cotización {c.NroCotizacion} ya no está abierta (estado: {c.Estado}).");
                }
                BitacoraReposicion.Registrar(BitacoraReposicion.COTIZACION_ANULADA, 3, $"{c.NroCotizacion} ({c.NombreInsumo}) anulada. {motivo}", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, "Cotizaciones");
                c.Estado = "Anulada";
                return ResultadoReposicion<CotizacionBE>.Ok(c, $"Cotización {c.NroCotizacion} anulada.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<CotizacionBE>(OP, ex); }
        }

        /// <summary>Último precio pagado por el insumo (referencia al cotizar).</summary>
        public decimal? ObtenerUltimoPrecio(int idInsumo)
        {
            if (!ControlAcceso.PuedeAccederReposicion()) return null;
            try
            {
                using var dal = new ReposicionDAL();
                return dal.ObtenerUltimoPrecioInsumo(idInsumo);
            }
            catch { return null; }
        }
    }
}
