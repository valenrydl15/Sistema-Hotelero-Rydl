using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN09 Realizar compra de insumos. El Encargado de Compras toma una pre-orden pendiente, el sistema se conecta
    /// con el banco y transfiere el total al proveedor; con el pago aprobado se genera la orden de compra y se le envía
    /// al proveedor (con el comprobante). Si el proveedor nunca cobró, antes hay que registrarlo (CUN08, extiende a CUN09).
    /// </summary>
    public class OrdenCompraBLL
    {
        public const string DIRECCION_ENTREGA_DEFECTO = "Hotelería Rydl - Recepción de mercadería, Av. del Libertador 1000, CABA";
        private static readonly string[] TABLAS = { "PagosProveedor", "OrdenesCompra", "PreOrdenesCompra" };

        private readonly BancoServicio _banco = new BancoServicio();
        private readonly EmailServicio _email = new EmailServicio();

        public ResultadoReposicion<OrdenCompraBE> RealizarCompra(int idPreOrden, string? direccionEntrega = null)
        {
            const string OP = "Realizar compra de insumos";
            if (!ControlAcceso.PuedeRealizarCompra())
                return ReposicionComun.Denegado<OrdenCompraBE>(OP);

            direccionEntrega = string.IsNullOrWhiteSpace(direccionEntrega) ? DIRECCION_ENTREGA_DEFECTO : direccionEntrega.Trim();
            if (direccionEntrega.Length > 300)
                return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La dirección de entrega es demasiado larga.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                PreOrdenCompraBE? po;
                ProveedorBE? prov;

                using (var dal = new ReposicionDAL())
                {
                    po = dal.ObtenerPreOrdenPorId(idPreOrden);
                    if (po == null)
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La pre-orden no existe.");
                    if (po.Estado != "Pendiente")
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, $"La pre-orden {po.NroPreOrden} ya fue comprada.");
                    prov = dal.ObtenerProveedorPorId(po.IdProveedor);
                }
                if (prov == null || !prov.Activo)
                    return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "El proveedor de la pre-orden no existe o está inactivo.");
                if (prov.EsNuevo)
                    return ReposicionComun.Rechazo<OrdenCompraBE>(OP,
                        $"{prov.Nombre} es un proveedor nuevo (nunca se le pagó): primero registrá sus datos bancarios.");
                if (string.IsNullOrWhiteSpace(prov.Email) || !prov.Email.Contains('@'))
                    return ReposicionComun.Rechazo<OrdenCompraBE>(OP, $"{prov.Nombre} no tiene un correo válido: no se le puede enviar la orden.");

                // ── 1) Conexión con el banco: transferencia por el total de la pre-orden ──
                var trf = _banco.RealizarTransferencia(prov.CUIT, prov.CBU, po.PrecioTotal, "Pre-orden " + po.NroPreOrden);
                if (!trf.Aprobada)
                {
                    using (var dal = new ReposicionDAL())
                        dal.InsertarPagoProveedor(new PagoProveedorBE
                        {
                            IdPreOrden = po.IdPreOrden,
                            IdProveedor = prov.IdProveedor,
                            Monto = po.PrecioTotal,
                            CBUDestino = prov.CBU,
                            Estado = "Rechazado",
                            DetalleBanco = trf.Mensaje
                        }, ReposicionComun.IdActual);
                    BitacoraReposicion.Registrar(BitacoraReposicion.PAGO_RECHAZADO, 2,
                        $"{po.NroPreOrden} a {prov.Nombre} por $ {po.PrecioTotal:N2}: {trf.Mensaje}", "FALLIDO");
                    DigitoVerificador.RecalcularSi(dvOk, "PagosProveedor");
                    return ResultadoReposicion<OrdenCompraBE>.Fallo(trf.Mensaje + " La pre-orden sigue pendiente.");
                }

                // ── 2) Pago aprobado: se registra el pago y se genera la orden (todo o nada) ──
                int idOrden;
                string nroOrden;
                using (var dal = new ReposicionDAL())
                {
                    dal.Iniciar();
                    int idPago = dal.InsertarPagoProveedor(new PagoProveedorBE
                    {
                        IdPreOrden = po.IdPreOrden,
                        IdProveedor = prov.IdProveedor,
                        Monto = po.PrecioTotal,
                        CBUDestino = prov.CBU,
                        NroOperacion = trf.NroOperacion,
                        Estado = "Aprobado",
                        DetalleBanco = trf.Mensaje
                    }, ReposicionComun.IdActual);

                    nroOrden = dal.GenerarNroOrden();
                    idOrden = dal.InsertarOrden(new OrdenCompraBE
                    {
                        NroOrden = nroOrden,
                        IdPreOrden = po.IdPreOrden,
                        IdPagoProveedor = idPago,
                        IdProveedor = po.IdProveedor,
                        IdInsumo = po.IdInsumo,
                        Cantidad = po.Cantidad,
                        PrecioUnitario = po.PrecioUnitario,
                        PlazoDias = po.PlazoDias,
                        DireccionEntrega = direccionEntrega
                    }, ReposicionComun.IdActual);

                    // La pre-orden pasa a 'Comprada' solo si seguía 'Pendiente' (evita pagar dos veces)
                    if (!dal.ActualizarEstadoPreOrden(po.IdPreOrden, "Comprada", "Pendiente"))
                    {
                        dal.Revertir();
                        BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1,
                            $"{po.NroPreOrden}: el banco aprobó {trf.NroOperacion} pero la pre-orden ya había sido comprada. Revisar el pago duplicado.",
                            "FALLIDO");
                        return ResultadoReposicion<OrdenCompraBE>.Fallo(
                            $"La pre-orden ya había sido comprada por otro usuario. El comprobante {trf.NroOperacion} quedó en bitácora para revisarlo.");
                    }

                    dal.InsertarNotificacion(new NotificacionBE
                    {
                        IdProveedorDestino = prov.IdProveedor,
                        IdOrden = idOrden,
                        Tipo = "Orden enviada a proveedor",
                        Mensaje = $"Orden {nroOrden} para {prov.Nombre}: {po.Cantidad:0.##} {po.UnidadMedida} de {po.NombreInsumo}. Pago {trf.NroOperacion}.",
                        Leida = true
                    });
                    dal.Confirmar();
                }

                BitacoraReposicion.Registrar(BitacoraReposicion.PAGO_REALIZADO, 3,
                    $"{po.NroPreOrden}: $ {po.PrecioTotal:N2} a {prov.Nombre} (CBU {prov.CBU}). Comprobante {trf.NroOperacion}.", "EXITOSO");

                // ── 3) Envío de la orden al proveedor (fuera de la transacción: un correo no se puede deshacer) ──
                OrdenCompraBE orden;
                using (var lectura = new ReposicionDAL()) orden = lectura.ObtenerOrdenPorId(idOrden)!;
                var envio = _email.EnviarOrdenAProveedor(orden, direccionEntrega);
                if (envio.Exito)
                {
                    using var dal2 = new ReposicionDAL();
                    dal2.MarcarOrdenEnviada(idOrden);
                    BitacoraReposicion.Registrar(BitacoraReposicion.ORDEN_EMITIDA, 3,
                        $"{nroOrden} a {orden.NombreProveedor}: {orden.Cantidad:0.##} de {orden.NombreInsumo}, total $ {orden.PrecioTotal:N2}. {envio.Mensaje}", "EXITOSO");
                }
                else
                {
                    BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_ENVIO, 2,
                        $"{nroOrden} pagada y emitida pero no enviada. {envio.Mensaje}", "FALLIDO");
                }
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                using var fin = new ReposicionDAL();
                orden = fin.ObtenerOrdenPorId(idOrden)!;
                string msg = $"Pago aprobado ({trf.NroOperacion}) y orden {nroOrden} generada. " +
                             (envio.Exito ? envio.Mensaje : $"No se pudo enviar al proveedor: {envio.Mensaje} Podés reenviarla desde Órdenes.");
                return ResultadoReposicion<OrdenCompraBE>.Ok(orden, msg);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<OrdenCompraBE>(OP, ex); }
        }

        /// <summary>Vuelve a enviar una orden que quedó emitida sin enviarse.</summary>
        public ResultadoReposicion<OrdenCompraBE> ReenviarOrden(int idOrden)
        {
            const string OP = "Reenviar orden de compra";
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<OrdenCompraBE>(OP);
            try
            {
                bool dvOk = DigitoVerificador.Consistente("OrdenesCompra");
                OrdenCompraBE? orden;
                using (var dal = new ReposicionDAL()) orden = dal.ObtenerOrdenPorId(idOrden);
                if (orden == null) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La orden no existe.");
                if (orden.Estado != "Emitida")
                    return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "Solo se reenvían órdenes emitidas que aún no se recibieron.");

                var envio = _email.EnviarOrdenAProveedor(orden, string.IsNullOrWhiteSpace(orden.DireccionEntrega) ? DIRECCION_ENTREGA_DEFECTO : orden.DireccionEntrega);
                if (!envio.Exito)
                {
                    BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_ENVIO, 2, $"{orden.NroOrden}: reenvío fallido. {envio.Mensaje}", "FALLIDO");
                    return ResultadoReposicion<OrdenCompraBE>.Fallo(envio.Mensaje);
                }

                using (var dal = new ReposicionDAL()) { dal.MarcarOrdenEnviada(idOrden); orden = dal.ObtenerOrdenPorId(idOrden)!; }
                BitacoraReposicion.Registrar(BitacoraReposicion.ORDEN_EMITIDA, 3, $"{orden.NroOrden} reenviada a {orden.NombreProveedor}. {envio.Mensaje}", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, "OrdenesCompra");
                return ResultadoReposicion<OrdenCompraBE>.Ok(orden, envio.Mensaje);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<OrdenCompraBE>(OP, ex); }
        }

        /// <param name="estado">null = todas.</param>
        public ResultadoReposicion<List<OrdenCompraBE>> ObtenerOrdenes(string? estado = null)
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<OrdenCompraBE>>("Ver órdenes de compra");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(dal.ObtenerOrdenes(estado));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>("Ver órdenes de compra", ex); }
        }
    }
}
