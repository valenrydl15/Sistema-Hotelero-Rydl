using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>CUN08 Realizar compra de insumos: orden de compra + envío al proveedor.</summary>
    public class OrdenCompraBLL
    {
        public const string DIRECCION_ENTREGA_DEFECTO = "Hotelería Rydl - Recepción de mercadería, Av. del Libertador 1000, CABA";
        private static readonly string[] TABLAS = { "OrdenesCompra", "SolicitudesCompra" };

        private readonly ProveedorBLL _proveedores = new ProveedorBLL();
        private readonly EmailServicio _email = new EmailServicio();

        public ResultadoReposicion<OrdenCompraBE> GenerarOrdenCompra(int idSolicitud, int idProveedor, decimal precioUnitario,
                                                                      int plazoDias, string? direccionEntrega = null)
        {
            const string OP = "Realizar compra de insumos";
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<OrdenCompraBE>(OP);

            direccionEntrega = string.IsNullOrWhiteSpace(direccionEntrega) ? DIRECCION_ENTREGA_DEFECTO : direccionEntrega.Trim();
            if (direccionEntrega.Length > 300)
                return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La dirección de entrega es demasiado larga.");

            // CUN09 (incluido): proveedor activo, precio y plazo válidos
            var sel = _proveedores.SeleccionarProveedor(idProveedor, precioUnitario, plazoDias);
            if (!sel.Exito) return ResultadoReposicion<OrdenCompraBE>.Fallo(sel.Mensaje);

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                int idOrden;
                string nroOrden;

                using (var dal = new ReposicionDAL())
                {
                    var s = dal.ObtenerSolicitudPorId(idSolicitud);
                    if (s == null)
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La solicitud no existe.");
                    if (s.Estado != "Aprobada")
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP,
                            $"Solo se compra una solicitud aprobada (SC-{idSolicitud:D4} está {s.Estado.ToLower()}).");

                    dal.Iniciar();
                    nroOrden = dal.GenerarNroOrden();
                    idOrden = dal.InsertarOrden(new OrdenCompraBE
                    {
                        NroOrden = nroOrden,
                        IdSolicitud = idSolicitud,
                        IdProveedor = idProveedor,
                        IdInsumo = s.IdInsumo,
                        Cantidad = s.CantidadSolicitada,
                        PrecioUnitario = precioUnitario,
                        PlazoDias = plazoDias,
                        DireccionEntrega = direccionEntrega
                    }, ReposicionComun.IdActual);

                    // La solicitud pasa a 'Ordenada' solo si seguía 'Aprobada' (evita dos órdenes por la misma solicitud)
                    if (!dal.ActualizarEstadoSolicitud(idSolicitud, "Ordenada", "Aprobada"))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<OrdenCompraBE>(OP, "La solicitud ya fue ordenada por otro usuario.");
                    }

                    dal.InsertarNotificacion(new NotificacionBE
                    {
                        IdProveedorDestino = idProveedor,
                        IdSolicitud = idSolicitud,
                        IdOrden = idOrden,
                        Tipo = "Orden enviada a proveedor",
                        Mensaje = $"Orden {nroOrden} para {sel.Dato.Proveedor.Nombre}: {s.CantidadSolicitada:0.##} {s.UnidadMedida} de {s.NombreInsumo}.",
                        Leida = true
                    });
                    dal.Confirmar();
                }

                // Envío fuera de la transacción: un correo no se puede "deshacer"
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
                        $"{nroOrden} emitida pero no enviada. {envio.Mensaje}", "FALLIDO");
                }
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                using var fin = new ReposicionDAL();
                orden = fin.ObtenerOrdenPorId(idOrden)!;
                string msg = envio.Exito
                    ? $"Orden {nroOrden} emitida. {envio.Mensaje}"
                    : $"Orden {nroOrden} emitida, pero NO se pudo enviar al proveedor. {envio.Mensaje} Podés reenviarla desde la lista de órdenes.";
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
