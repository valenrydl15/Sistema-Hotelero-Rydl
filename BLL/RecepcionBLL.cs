using DAL_HRS_480;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN10 Registrar insumos recibidos. El proveedor, con el pago acreditado, envía los productos y su factura.
    /// El Encargado de Depósito registra la factura y lo que realmente llegó; el sistema compara orden, factura y
    /// mercadería. El stock se actualiza con lo recibido. Si algo no coincide, la recepción queda "Con diferencia"
    /// y se avisa a Compras. Una sola recepción por orden.
    /// </summary>
    public class RecepcionBLL
    {
        private static readonly string[] TABLAS = { "FacturasProveedor", "RecepcionesInsumos", "OrdenesCompra", "Insumos", "MovimientosStock" };
        private readonly StockBLL _stock = new StockBLL();

        public ResultadoReposicion<ControlRecepcionBE> RegistrarInsumosRecibidos(RecepcionConFacturaBE datos)
        {
            const string OP = "Registrar insumos recibidos";
            if (!ControlAcceso.PuedeRegistrarRecepcionInsumos())
                return ReposicionComun.Denegado<ControlRecepcionBE>(OP);

            if (datos == null || datos.IdOrden <= 0)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "Seleccioná la orden de compra recibida.");
            string nroFactura = (datos.NroFactura ?? "").Trim();
            if (nroFactura.Length == 0)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "Cargá el número de la factura del proveedor.");
            if (nroFactura.Length > 30)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "El número de factura no puede superar los 30 caracteres.");
            if (datos.FechaFactura.Date > DateTime.Today)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "La fecha de la factura no puede ser futura.");
            if (datos.CantidadFacturada <= 0 || datos.CantidadFacturada > ReposicionComun.CANTIDAD_MAX)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "La cantidad facturada debe ser mayor a cero.");
            if (datos.PrecioUnitarioFacturado <= 0 || datos.PrecioUnitarioFacturado > CotizacionBLL.PRECIO_MAX)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "El precio unitario de la factura debe ser mayor a cero.");
            if (datos.CantidadRecibida < 0 || datos.CantidadRecibida > ReposicionComun.CANTIDAD_MAX)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "La cantidad recibida no es válida (no puede ser negativa).");
            string obsUsuario = (datos.Observaciones ?? "").Trim();
            if (obsUsuario.Length > 300)
                return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "Las observaciones no pueden superar los 300 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                var control = new ControlRecepcionBE();
                OrdenCompraBE orden;

                using (var dal = new ReposicionDAL())
                {
                    orden = dal.ObtenerOrdenPorId(datos.IdOrden)!;
                    if (orden == null)
                        return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "La orden de compra no existe.");
                    if (orden.Estado != "Emitida")
                        return ReposicionComun.Rechazo<ControlRecepcionBE>(OP,
                            $"La orden {orden.NroOrden} ya tiene sus insumos registrados (estado: {orden.Estado}).");
                    DateTime desde = (orden.FechaPago ?? orden.FechaEmision).Date;
                    if (datos.FechaFactura.Date < desde)
                        return ReposicionComun.Rechazo<ControlRecepcionBE>(OP,
                            $"La fecha de la factura no puede ser anterior al pago/emisión de la orden ({desde:dd/MM/yyyy}).");
                    if (dal.ExisteFacturaProveedor(orden.IdProveedor, nroFactura))
                        return ReposicionComun.Rechazo<ControlRecepcionBE>(OP,
                            $"La factura {nroFactura} de {orden.NombreProveedor} ya fue registrada.");

                    // ── Control: ¿coinciden orden, factura y mercadería? ──
                    if (datos.CantidadFacturada != orden.Cantidad)
                        control.Diferencias.Add($"la factura dice {datos.CantidadFacturada:0.##} y la orden {orden.Cantidad:0.##}");
                    if (datos.PrecioUnitarioFacturado != orden.PrecioUnitario)
                        control.Diferencias.Add($"precio facturado $ {datos.PrecioUnitarioFacturado:N2} y pagado $ {orden.PrecioUnitario:N2}");
                    if (datos.CantidadRecibida != datos.CantidadFacturada)
                        control.Diferencias.Add($"llegaron {datos.CantidadRecibida:0.##} y se facturaron {datos.CantidadFacturada:0.##}");

                    string estado = control.Coincide ? "Aceptado" : "Con diferencia";
                    string obs = control.Coincide ? obsUsuario
                        : ("No coincide: " + string.Join("; ", control.Diferencias) + "." + (obsUsuario.Length > 0 ? " " + obsUsuario : ""));
                    if (obs.Length > 300) obs = obs.Substring(0, 300);

                    dal.Iniciar();
                    try
                    {
                        int idFactura = dal.InsertarFacturaProveedor(new FacturaProveedorBE
                        {
                            IdOrden = orden.IdOrden,
                            IdProveedor = orden.IdProveedor,
                            NroFactura = nroFactura,
                            FechaFactura = datos.FechaFactura.Date,
                            IdInsumo = orden.IdInsumo,            // el insumo sale de la orden, no de la pantalla
                            CantidadFacturada = datos.CantidadFacturada,
                            PrecioUnitario = datos.PrecioUnitarioFacturado
                        }, ReposicionComun.IdActual);

                        int idRecepcion = dal.InsertarRecepcion(new RecepcionInsumoBE
                        {
                            IdOrden = orden.IdOrden,
                            IdInsumo = orden.IdInsumo,
                            CantidadRecibida = datos.CantidadRecibida,
                            Estado = estado,
                            Observaciones = obs,
                            IdFacturaProveedor = idFactura
                        }, ReposicionComun.IdActual);

                        // El stock refleja lo que realmente hay en el depósito
                        control.Movimiento = _stock.ActualizarStock(dal, orden.IdInsumo, datos.CantidadRecibida, idRecepcion);

                        if (!dal.ActualizarEstadoOrden(orden.IdOrden, control.Coincide ? "Recibida" : "Con diferencia", "Emitida"))
                        {
                            dal.Revertir();
                            return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, "La orden fue modificada por otro usuario. Volvé a intentar.");
                        }
                        dal.Confirmar();

                        control.Recepcion = new RecepcionInsumoBE
                        {
                            IdRecepcion = idRecepcion,
                            IdOrden = orden.IdOrden,
                            NroOrden = orden.NroOrden,
                            IdInsumo = orden.IdInsumo,
                            NombreInsumo = orden.NombreInsumo,
                            CantidadRecibida = datos.CantidadRecibida,
                            FechaRecepcion = DateTime.Now,
                            Estado = estado,
                            Observaciones = obs,
                            IdFacturaProveedor = idFactura,
                            NroFactura = nroFactura
                        };
                    }
                    catch (SqlException ex) when (ReposicionComun.EsDuplicado(ex))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<ControlRecepcionBE>(OP, $"La orden {orden.NroOrden} o la factura {nroFactura} ya estaban registradas.");
                    }
                }

                var mov = control.Movimiento;
                if (!control.Coincide)
                    new NotificacionBLL().NotificarDiferenciaRecepcion(orden.IdOrden, string.Join("; ", control.Diferencias));

                BitacoraReposicion.Registrar(BitacoraReposicion.FACTURA_PROVEEDOR, 4,
                    $"{orden.NroOrden}: factura {nroFactura} de {orden.NombreProveedor} por {datos.CantidadFacturada:0.##} × $ {datos.PrecioUnitarioFacturado:N2}.", "EXITOSO");
                BitacoraReposicion.Registrar(control.Coincide ? BitacoraReposicion.RECEPCION : BitacoraReposicion.RECEPCION_DIF,
                    control.Coincide ? 3 : 2,
                    $"{orden.NroOrden}: pedido {orden.Cantidad:0.##}, recibido {datos.CantidadRecibida:0.##} de {orden.NombreInsumo}." +
                    (control.Coincide ? " Coincide con la factura." : " No coincide: " + string.Join("; ", control.Diferencias) + "."),
                    "EXITOSO");
                BitacoraReposicion.Registrar(BitacoraReposicion.STOCK_ACTUALIZADO, 4,
                    $"{mov.NombreInsumo}: {mov.CantidadAnterior:0.##} → {mov.CantidadNueva:0.##} (+{mov.CantidadMovimiento:0.##}).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                string msg = $"Insumos registrados. Stock de {mov.NombreInsumo}: {mov.CantidadAnterior:0.##} + {mov.CantidadMovimiento:0.##} = {mov.CantidadNueva:0.##}.";
                if (!control.Coincide)
                    msg += " No coincide: " + string.Join("; ", control.Diferencias) + ". Se avisó a Compras.";
                return ResultadoReposicion<ControlRecepcionBE>.Ok(control, msg);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<ControlRecepcionBE>(OP, ex); }
        }

        /// <summary>Órdenes pagadas y enviadas, esperando los productos y la factura.</summary>
        public ResultadoReposicion<List<OrdenCompraBE>> ObtenerOrdenesPendientesDeRecepcion()
        {
            if (!ControlAcceso.PuedeRegistrarRecepcionInsumos())
                return ReposicionComun.Denegado<List<OrdenCompraBE>>("Ver órdenes a recibir");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(dal.ObtenerOrdenes("Emitida"));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>("Ver órdenes a recibir", ex); }
        }

        public ResultadoReposicion<List<RecepcionInsumoBE>> ObtenerRecepciones()
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<RecepcionInsumoBE>>("Ver recepciones");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<RecepcionInsumoBE>>.Ok(dal.ObtenerRecepciones());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<RecepcionInsumoBE>>("Ver recepciones", ex); }
        }
    }
}
