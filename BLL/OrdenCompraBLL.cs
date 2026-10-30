using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN09 Generar orden de compra (Gerente): aprueba la pre-orden (estado "Generada") o la cancela ("Cancelada").
    /// Incluye CUN11 Pagar orden de compra: se conecta con el banco, y si aprueba la orden pasa a "Pagada" y se
    /// le envía al proveedor; si rechaza, queda "Generada". Si el proveedor está pre-registrado, antes se registra (CUN10).
    /// </summary>
    public class OrdenCompraBLL
    {
        public const string DIRECCION_ENTREGA_DEFECTO = "Hotelería Rydl - Recepción de mercadería, Av. del Libertador 1000, CABA";
        private readonly BancoServicio _banco = new BancoServicio();
        private readonly EmailServicio _email = new EmailServicio();

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

        /// <summary>Pre-órdenes para aprobar y órdenes generadas pendientes de pago (bandeja del Gerente).</summary>
        public ResultadoReposicion<List<OrdenCompraBE>> ObtenerBandejaGerente()
        {
            if (!ControlAcceso.PuedeGenerarOrden())
                return ReposicionComun.Denegado<List<OrdenCompraBE>>("Ver pre-órdenes");
            try
            {
                using var dal = new ReposicionDAL();
                var l = dal.ObtenerOrdenes(EstadoOrden.PRE_ORDEN);
                l.AddRange(dal.ObtenerOrdenes(EstadoOrden.GENERADA));
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(l);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>("Ver pre-órdenes", ex); }
        }

        /// <summary>Datos bancarios que informó el proveedor en su cotización (para CUN10).</summary>
        public ResultadoReposicion<CotizacionBE> ObtenerCotizacionDeOrden(int idOrden)
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<CotizacionBE>("Ver cotización");
            try
            {
                using var dal = new ReposicionDAL();
                var o = dal.ObtenerOrdenPorId(idOrden);
                var c = o == null ? null : dal.ObtenerCotizacionPorId(o.IdCotizacion);
                return c == null ? ResultadoReposicion<CotizacionBE>.Fallo("No se encontró la cotización de la orden.") : ResultadoReposicion<CotizacionBE>.Ok(c);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<CotizacionBE>("Ver cotización", ex); }
        }

        // ══════════════════════════════════════════════════════
        // CUN09 Generar orden de compra / cancelar pre-orden
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<OrdenCompraBE> GenerarOrdenCompra(int idOrden)
            => CambiarPreOrden(idOrden, true, null);

        public ResultadoReposicion<OrdenCompraBE> CancelarPreOrden(int idOrden, string motivo)
            => CambiarPreOrden(idOrden, false, motivo);

        private ResultadoReposicion<OrdenCompraBE> CambiarPreOrden(int idOrden, bool aprobar, string? motivo)
        {
            string OP = aprobar ? "Generar orden de compra" : "Cancelar pre-orden";
            if (!ControlAcceso.PuedeGenerarOrden()) return ReposicionComun.Denegado<OrdenCompraBE>(OP);
            motivo = (motivo ?? "").Trim();
            if (!aprobar && motivo.Length == 0) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "Indicá el motivo de la cancelación.");
            if (motivo.Length > 250) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "El motivo no puede superar los 250 caracteres.");
            try
            {
                bool dvOk = DigitoVerificador.Consistente("OrdenesCompra");
                OrdenCompraBE? o;
                using (var dal = new ReposicionDAL())
                {
                    o = dal.ObtenerOrdenPorId(idOrden);
                    if (o == null) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La pre-orden no existe.");
                    if (!dal.CambiarEstadoOrden(idOrden, aprobar ? EstadoOrden.GENERADA : EstadoOrden.CANCELADA, EstadoOrden.PRE_ORDEN,
                                                ReposicionComun.IdActual, aprobar ? null : motivo))
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, $"{o.NroOrden} ya no es una pre-orden (estado: {o.Estado}).");
                    o = dal.ObtenerOrdenPorId(idOrden)!;
                }
                BitacoraReposicion.Registrar(aprobar ? BitacoraReposicion.ORDEN_GENERADA : BitacoraReposicion.ORDEN_CANCELADA, 3,
                    $"{o.NroOrden} ({o.NombreProveedor}, $ {o.Total:N2}) {(aprobar ? "generada" : "cancelada: " + motivo)}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, "OrdenesCompra");
                return ResultadoReposicion<OrdenCompraBE>.Ok(o, aprobar
                    ? $"Orden {o.NroOrden} generada." + (o.EstadoProveedor == ProveedorBE.REGISTRADO ? " Ahora se paga." : " El proveedor está pre-registrado: registralo para poder pagar.")
                    : $"Pre-orden {o.NroOrden} cancelada.");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<OrdenCompraBE>(OP, ex); }
        }

        // ══════════════════════════════════════════════════════
        // CUN11 Pagar orden de compra (y enviarla al proveedor)
        // ══════════════════════════════════════════════════════
        public ResultadoReposicion<OrdenCompraBE> PagarOrden(int idOrden, string? direccionEntrega = null)
        {
            const string OP = "Pagar orden de compra";
            if (!ControlAcceso.PuedePagarOrden()) return ReposicionComun.Denegado<OrdenCompraBE>(OP);
            direccionEntrega = string.IsNullOrWhiteSpace(direccionEntrega) ? DIRECCION_ENTREGA_DEFECTO : direccionEntrega.Trim();
            if (direccionEntrega.Length > 300) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La dirección de entrega es demasiado larga.");

            string[] TABLAS = { "PagosOrdenCompra", "OrdenesCompra" };
            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                OrdenCompraBE? o; ProveedorBE? prov;
                using (var dal = new ReposicionDAL())
                {
                    o = dal.ObtenerOrdenPorId(idOrden);
                    if (o == null) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La orden no existe.");
                    if (o.Estado != EstadoOrden.GENERADA)
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, $"Solo se pagan órdenes generadas ({o.NroOrden} está {o.Estado.ToLower()}).");
                    prov = dal.ObtenerProveedorPorId(o.IdProveedor);
                }
                if (prov == null || !prov.Activo) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "El proveedor no existe o está inactivo.");
                if (prov.PreRegistrado)
                    return ReposicionComun.Rechazo<OrdenCompraBE>(OP, $"{prov.Nombre} está pre-registrado: primero registralo con su CUIT, banco, CBU y titular.");

                // Conexión con el banco: número de orden, banco, CBU, titular y monto
                var trf = _banco.Transferir(o.NroOrden, prov.Banco, prov.CBU, prov.Titular, o.Total);
                var pago = new PagoOrdenCompraBE { IdOrden = idOrden, Monto = o.Total, Banco = prov.Banco, CBU = prov.CBU, Titular = prov.Titular, DetalleBanco = trf.Mensaje };
                if (!trf.Aprobada)
                {
                    pago.Estado = "Rechazado";
                    using (var dal = new ReposicionDAL()) dal.InsertarPago(pago, ReposicionComun.IdActual);
                    BitacoraReposicion.Registrar(BitacoraReposicion.PAGO_RECHAZADO, 2, $"{o.NroOrden} por $ {o.Total:N2}: {trf.Mensaje}", "FALLIDO");
                    DigitoVerificador.RecalcularSi(dvOk, "PagosOrdenCompra");
                    return ResultadoReposicion<OrdenCompraBE>.Fallo(trf.Mensaje + " La orden queda en estado Generada.");
                }

                pago.Estado = "Aprobado"; pago.NroOperacion = trf.NroOperacion;
                using (var dal = new ReposicionDAL())
                {
                    dal.Iniciar();
                    dal.InsertarPago(pago, ReposicionComun.IdActual);
                    if (!dal.CambiarEstadoOrden(idOrden, EstadoOrden.PAGADA, EstadoOrden.GENERADA, null, null, direccionEntrega))
                    {
                        dal.Revertir();
                        BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1,
                            $"{o.NroOrden}: el banco aprobó {trf.NroOperacion} pero la orden ya no estaba Generada. Revisar pago duplicado.", "FALLIDO");
                        return ResultadoReposicion<OrdenCompraBE>.Fallo($"La orden ya había sido pagada. El comprobante {trf.NroOperacion} quedó en bitácora para revisarlo.");
                    }
                    dal.InsertarNotificacion(new NotificacionBE
                    { IdProveedorDestino = prov.IdProveedor, IdOrden = idOrden, Tipo = "Orden enviada a proveedor", Mensaje = $"{o.NroOrden} pagada ({trf.NroOperacion}).", Leida = true });
                    dal.Confirmar();
                }
                BitacoraReposicion.Registrar(BitacoraReposicion.PAGO_APROBADO, 3,
                    $"{o.NroOrden}: $ {o.Total:N2} a {prov.Nombre} ({prov.Banco}, CBU {prov.CBU}). Operación {trf.NroOperacion}.", "EXITOSO");

                // Se le envía al proveedor la orden pagada (número de orden, productos, cantidades, número de operación)
                using (var lectura = new ReposicionDAL()) o = lectura.ObtenerOrdenPorId(idOrden)!;
                var envio = _email.EnviarOrdenAProveedor(o, direccionEntrega);
                if (envio.Exito)
                {
                    using (var d2 = new ReposicionDAL()) d2.MarcarOrdenEnviada(idOrden);
                    BitacoraReposicion.Registrar(BitacoraReposicion.ORDEN_ENVIADA, 4, $"{o.NroOrden} a {o.NombreProveedor}. {envio.Mensaje}", "EXITOSO");
                }
                else BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_ENVIO, 2, $"{o.NroOrden} pagada pero no enviada. {envio.Mensaje}", "FALLIDO");

                new NotificacionBLL().NotificarRol(ControlAcceso.ROL_DEPOSITO, "Mercadería a recibir",
                    $"La orden {o.NroOrden} de {o.NombreProveedor} está pagada: esperar la mercadería con su factura ({o.ResumenItems}).", idOrden);
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                using (var fin = new ReposicionDAL()) o = fin.ObtenerOrdenPorId(idOrden)!;
                return ResultadoReposicion<OrdenCompraBE>.Ok(o, $"Pago aprobado (operación {trf.NroOperacion}). Orden {o.NroOrden} pagada. " +
                    (envio.Exito ? envio.Mensaje : "No se pudo enviar al proveedor: " + envio.Mensaje + " Reenviala desde Órdenes."));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<OrdenCompraBE>(OP, ex); }
        }

        /// <summary>Vuelve a enviar al proveedor una orden pagada.</summary>
        public ResultadoReposicion<OrdenCompraBE> ReenviarOrden(int idOrden)
        {
            const string OP = "Reenviar orden de compra";
            if (!ControlAcceso.PuedePagarOrden()) return ReposicionComun.Denegado<OrdenCompraBE>(OP);
            try
            {
                OrdenCompraBE? o;
                using (var dal = new ReposicionDAL()) o = dal.ObtenerOrdenPorId(idOrden);
                if (o == null) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La orden no existe.");
                if (o.Estado != EstadoOrden.PAGADA) return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "Solo se reenvían órdenes pagadas que aún no se recibieron.");
                bool dvOk = DigitoVerificador.Consistente("OrdenesCompra");
                var envio = _email.EnviarOrdenAProveedor(o, string.IsNullOrWhiteSpace(o.DireccionEntrega) ? DIRECCION_ENTREGA_DEFECTO : o.DireccionEntrega);
                if (!envio.Exito)
                {
                    BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_ENVIO, 2, $"{o.NroOrden}: reenvío fallido. {envio.Mensaje}", "FALLIDO");
                    return ResultadoReposicion<OrdenCompraBE>.Fallo(envio.Mensaje);
                }
                using (var dal = new ReposicionDAL()) { dal.MarcarOrdenEnviada(idOrden); o = dal.ObtenerOrdenPorId(idOrden)!; }
                BitacoraReposicion.Registrar(BitacoraReposicion.ORDEN_ENVIADA, 4, $"{o.NroOrden} reenviada. {envio.Mensaje}", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, "OrdenesCompra");
                return ResultadoReposicion<OrdenCompraBE>.Ok(o, envio.Mensaje);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<OrdenCompraBE>(OP, ex); }
        }
    }
}
