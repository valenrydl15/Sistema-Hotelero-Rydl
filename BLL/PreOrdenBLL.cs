using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN07 Generar pre-orden de compra. El Encargado de Depósito elige el proveedor de la cotización
    /// (por defecto el más conveniente) y genera la pre-orden; Compras recibe un aviso para pagarla.
    /// </summary>
    public class PreOrdenBLL
    {
        private static readonly string[] TABLAS = { "PreOrdenesCompra", "Cotizaciones" };

        public ResultadoReposicion<PreOrdenCompraBE> GenerarPreOrden(int idCotizacion, int idDetalleElegido,
                                                                     string prioridad, string? observaciones = null)
        {
            const string OP = "Generar pre-orden de compra";
            if (!ControlAcceso.PuedeGenerarPreOrden())
                return ReposicionComun.Denegado<PreOrdenCompraBE>(OP);

            prioridad = string.IsNullOrWhiteSpace(prioridad) ? "Normal" : prioridad.Trim();
            observaciones = (observaciones ?? "").Trim();
            if (!ReposicionComun.PRIORIDADES.Contains(prioridad))
                return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "Prioridad inválida.");
            if (observaciones.Length > 300)
                return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "Las observaciones no pueden superar los 300 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                int idPreOrden;
                CotizacionBE? cot;
                CotizacionDetalleBE? elegido;

                using (var dal = new ReposicionDAL())
                {
                    cot = dal.ObtenerCotizacionPorId(idCotizacion);
                    if (cot == null)
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "La cotización no existe.");
                    if (cot.Estado != "Abierta")
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP,
                            $"La cotización {cot.NroCotizacion} ya no está abierta (estado: {cot.Estado}).");

                    elegido = cot.Detalles.FirstOrDefault(d => d.IdDetalleCotizacion == idDetalleElegido);
                    if (elegido == null)
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "Elegí uno de los proveedores cotizados.");

                    // Regla: si no se elige la oferta más conveniente, hay que justificarlo
                    if (!elegido.MasConveniente && observaciones.Length == 0)
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP,
                            "No elegiste la oferta más conveniente: indicá en observaciones por qué.");

                    var prov = dal.ObtenerProveedorPorId(elegido.IdProveedor);
                    if (prov == null || !prov.Activo)
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "El proveedor elegido ya no está activo.");

                    dal.Iniciar();
                    string nro = dal.GenerarNroPreOrden();
                    idPreOrden = dal.InsertarPreOrden(new PreOrdenCompraBE
                    {
                        NroPreOrden = nro,
                        IdCotizacion = cot.IdCotizacion,
                        IdDetalleCotizacion = elegido.IdDetalleCotizacion,
                        IdInsumo = cot.IdInsumo,
                        IdProveedor = elegido.IdProveedor,
                        Cantidad = cot.Cantidad,
                        PrecioUnitario = elegido.PrecioUnitario,
                        PlazoDias = elegido.PlazoDias,
                        Prioridad = prioridad,
                        Observaciones = observaciones
                    }, ReposicionComun.IdActual);

                    // Solo si la cotización seguía abierta (evita dos pre-órdenes de la misma cotización)
                    if (!dal.ActualizarEstadoCotizacion(cot.IdCotizacion, "Con pre-orden", "Abierta"))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<PreOrdenCompraBE>(OP, "La cotización ya fue usada por otro usuario.");
                    }
                    dal.Confirmar();
                }

                PreOrdenCompraBE po;
                using (var lectura = new ReposicionDAL()) po = lectura.ObtenerPreOrdenPorId(idPreOrden)!;

                // Aviso a Compras (no deshace la pre-orden si falla)
                new NotificacionBLL().NotificarPreOrden(idPreOrden);
                BitacoraReposicion.Registrar(BitacoraReposicion.PREORDEN, 4,
                    $"{po.NroPreOrden} ({cot.NroCotizacion}): {po.Cantidad:0.##} {po.UnidadMedida} de {po.NombreInsumo} a {po.NombreProveedor}, " +
                    $"total $ {po.PrecioTotal:N2}, prioridad {prioridad}{(elegido.MasConveniente ? "" : ". No es la más conveniente: " + observaciones)}.",
                    "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                return ResultadoReposicion<PreOrdenCompraBE>.Ok(po, $"Pre-orden {po.NroPreOrden} generada y Compras fue notificado.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<PreOrdenCompraBE>(OP, ex); }
        }

        /// <summary>Pre-órdenes esperando el pago (entrada de CUN09).</summary>
        public ResultadoReposicion<List<PreOrdenCompraBE>> ObtenerPreOrdenesPendientes()
        {
            if (!ControlAcceso.PuedeRealizarCompra())
                return ReposicionComun.Denegado<List<PreOrdenCompraBE>>("Ver pre-órdenes pendientes");
            return Listar("Ver pre-órdenes pendientes", "Pendiente");
        }

        /// <summary>Historial de pre-órdenes (consulta para todos los roles del proceso).</summary>
        public ResultadoReposicion<List<PreOrdenCompraBE>> ObtenerPreOrdenes(string? estado = null)
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<PreOrdenCompraBE>>("Ver pre-órdenes");
            return Listar("Ver pre-órdenes", estado);
        }

        private static ResultadoReposicion<List<PreOrdenCompraBE>> Listar(string op, string? estado)
        {
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<PreOrdenCompraBE>>.Ok(dal.ObtenerPreOrdenes(estado));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<PreOrdenCompraBE>>(op, ex); }
        }
    }
}
