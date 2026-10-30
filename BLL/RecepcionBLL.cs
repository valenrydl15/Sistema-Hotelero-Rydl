using DAL_HRS_480;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN12 Registrar recepción de insumos. El proveedor entrega la mercadería con su factura; el Encargado de Depósito
    /// registra la recepción (número de factura, cantidad recibida de cada insumo), la orden pasa a "Recibida" y el stock
    /// se actualiza solo con lo que llegó. Si algo no coincide con lo pedido, queda "Con diferencia" y se avisa a Compras.
    /// </summary>
    public class RecepcionBLL
    {
        private static readonly string[] TABLAS = { "RecepcionesInsumos", "RecepcionesInsumosDetalle", "OrdenesCompra", "Insumos", "MovimientosStock" };
        private readonly StockBLL _stock = new StockBLL();

        public ResultadoReposicion<RecepcionInsumoBE> RegistrarRecepcion(int idOrden, string nroFactura, decimal totalFactura,
                                                                          List<ItemRecepcionBE> recibidos, string? observaciones = null)
        {
            const string OP = "Registrar recepción de insumos";
            if (!ControlAcceso.PuedeRegistrarRecepcionInsumos()) return ReposicionComun.Denegado<RecepcionInsumoBE>(OP);

            nroFactura = (nroFactura ?? "").Trim();
            observaciones = (observaciones ?? "").Trim();
            recibidos ??= new List<ItemRecepcionBE>();
            if (nroFactura.Length == 0) return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Cargá el número de la factura del proveedor.");
            if (nroFactura.Length > 30) return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "El número de factura no puede superar los 30 caracteres.");
            if (totalFactura <= 0) return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Cargá el total de la factura.");
            if (recibidos.Any(r => r.CantidadRecibida < 0 || r.CantidadRecibida > ReposicionComun.CANTIDAD_MAX))
                return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Las cantidades recibidas no pueden ser negativas.");
            if (observaciones.Length > 300) return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Las observaciones no pueden superar los 300 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                OrdenCompraBE o;
                var rec = new RecepcionInsumoBE { IdOrden = idOrden, NroFactura = nroFactura, TotalFactura = totalFactura };
                var dif = new List<string>();
                using (var dal = new ReposicionDAL())
                {
                    o = dal.ObtenerOrdenPorId(idOrden)!;
                    if (o == null) return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "La orden de compra no existe.");
                    if (o.Estado != EstadoOrden.PAGADA)
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, $"Solo se reciben órdenes pagadas ({o.NroOrden} está {o.Estado.ToLower()}).");
                    if (dal.ExisteFactura(o.IdProveedor, nroFactura))
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, $"La factura {nroFactura} de {o.NombreProveedor} ya fue registrada.");
                    if (recibidos.Any(r => o.Items.All(i => i.IdInsumo != r.IdInsumo)))
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Hay un insumo que no está en la orden.");

                    // Lo que no se informa se toma como no recibido
                    foreach (var it in o.Items)
                    {
                        var r = recibidos.FirstOrDefault(x => x.IdInsumo == it.IdInsumo);
                        decimal cant = r?.CantidadRecibida ?? 0;
                        rec.Items.Add(new ItemRecepcionBE { IdInsumo = it.IdInsumo, NombreInsumo = it.NombreInsumo, CantidadPedida = it.Cantidad, CantidadRecibida = cant });
                        if (cant != it.Cantidad) dif.Add($"{it.NombreInsumo}: pedido {it.Cantidad:0.##}, recibido {cant:0.##}");
                    }
                    if (totalFactura != o.Total) dif.Add($"factura $ {totalFactura:N2} y orden $ {o.Total:N2}");
                    rec.Estado = dif.Count == 0 ? "Completa" : "Con diferencia";
                    rec.Observaciones = dif.Count == 0 ? observaciones : ("Diferencias: " + string.Join("; ", dif) + "." + (observaciones.Length > 0 ? " " + observaciones : ""));
                    if (rec.Observaciones.Length > 300) rec.Observaciones = rec.Observaciones.Substring(0, 300);

                    dal.Iniciar();
                    try
                    {
                        rec.IdRecepcion = dal.InsertarRecepcion(rec, ReposicionComun.IdActual);
                        foreach (var it in rec.Items)
                        {
                            dal.InsertarDetalleRecepcion(rec.IdRecepcion, it);
                            if (it.CantidadRecibida > 0)
                                rec.Movimientos.Add(_stock.ActualizarStock(dal, it.IdInsumo, it.CantidadRecibida, rec.IdRecepcion));
                        }
                        if (!dal.CambiarEstadoOrden(idOrden, EstadoOrden.RECIBIDA, EstadoOrden.PAGADA))
                        {
                            dal.Revertir();
                            return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "La orden fue modificada por otro usuario. Volvé a intentar.");
                        }
                        dal.Confirmar();
                    }
                    catch (SqlException ex) when (ReposicionComun.EsDuplicado(ex))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, $"La orden {o.NroOrden} ya tiene su recepción registrada.");
                    }
                }
                rec.NroOrden = o.NroOrden;
                rec.FechaRecepcion = DateTime.Now;

                if (dif.Count > 0)
                    new NotificacionBLL().NotificarRol(ControlAcceso.ROL_COMPRAS, "Recepción con diferencia",
                        $"La recepción de {o.NroOrden} ({o.NombreProveedor}) no coincide: {string.Join("; ", dif)}.", idOrden);
                BitacoraReposicion.Registrar(dif.Count == 0 ? BitacoraReposicion.RECEPCION : BitacoraReposicion.RECEPCION_DIF, dif.Count == 0 ? 3 : 2,
                    $"{o.NroOrden}, factura {nroFactura}: " + string.Join(", ", rec.Items.Select(i => $"{i.NombreInsumo} {i.CantidadRecibida:0.##}/{i.CantidadPedida:0.##}")) +
                    (dif.Count == 0 ? "." : ". Diferencias: " + string.Join("; ", dif) + "."), "EXITOSO");
                foreach (var m in rec.Movimientos)
                    BitacoraReposicion.Registrar(BitacoraReposicion.STOCK_ACTUALIZADO, 4, $"{m.NombreInsumo}: {m.CantidadAnterior:0.##} + {m.CantidadMovimiento:0.##} = {m.CantidadNueva:0.##}.", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                string msg = $"Recepción de {o.NroOrden} registrada (factura {nroFactura}). Stock actualizado: " +
                             string.Join("; ", rec.Movimientos.Select(m => $"{m.NombreInsumo} {m.CantidadAnterior:0.##} → {m.CantidadNueva:0.##}")) + ".";
                if (dif.Count > 0) msg += " No coincide con lo pedido: se avisó a Compras.";
                return ResultadoReposicion<RecepcionInsumoBE>.Ok(rec, msg);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<RecepcionInsumoBE>(OP, ex); }
        }

        /// <summary>Órdenes pagadas esperando la mercadería.</summary>
        public ResultadoReposicion<List<OrdenCompraBE>> ObtenerOrdenesARecibir()
        {
            if (!ControlAcceso.PuedeRegistrarRecepcionInsumos()) return ReposicionComun.Denegado<List<OrdenCompraBE>>("Ver órdenes a recibir");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(dal.ObtenerOrdenes(EstadoOrden.PAGADA));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>("Ver órdenes a recibir", ex); }
        }

        public ResultadoReposicion<List<RecepcionInsumoBE>> ObtenerRecepciones()
        {
            if (!ControlAcceso.PuedeAccederReposicion()) return ReposicionComun.Denegado<List<RecepcionInsumoBE>>("Ver recepciones");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<RecepcionInsumoBE>>.Ok(dal.ObtenerRecepciones());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<RecepcionInsumoBE>>("Ver recepciones", ex); }
        }
    }
}
