using DAL_HRS_480;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN10 Registrar recepción de insumos. Opción A del proceso: UNA recepción por orden;
    /// si lo recibido difiere de lo pedido, la orden queda "Con diferencia" y se resuelve por reembolso (CUN11).
    /// </summary>
    public class RecepcionBLL
    {
        private static readonly string[] TABLAS = { "RecepcionesInsumos", "OrdenesCompra", "Insumos", "MovimientosStock" };
        private readonly StockBLL _stock = new StockBLL();

        public ResultadoReposicion<RecepcionInsumoBE> RegistrarRecepcion(RecepcionInsumoBE datos)
        {
            const string OP = "Registrar recepción de insumos";
            if (!ControlAcceso.PuedeRegistrarRecepcionInsumos())
                return ReposicionComun.Denegado<RecepcionInsumoBE>(OP);

            if (datos == null || datos.IdOrden <= 0)
                return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Seleccioná la orden de compra recibida.");
            if (datos.CantidadRecibida < 0 || datos.CantidadRecibida > ReposicionComun.CANTIDAD_MAX)
                return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "La cantidad recibida no es válida (no puede ser negativa).");
            if ((datos.Observaciones ?? "").Length > 300)
                return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "Las observaciones no pueden superar los 300 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                RecepcionInsumoBE guardada;
                MovimientoStockBE mov;
                OrdenCompraBE orden;
                bool conDiferencia;

                using (var dal = new ReposicionDAL())
                {
                    orden = dal.ObtenerOrdenPorId(datos.IdOrden)!;
                    if (orden == null)
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "La orden de compra no existe.");
                    if (orden.Estado != "Emitida")
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP,
                            $"La orden {orden.NroOrden} ya tiene su recepción registrada (estado: {orden.Estado}).");

                    conDiferencia = datos.CantidadRecibida != orden.Cantidad;
                    string estadoRecepcion = conDiferencia ? "Con diferencia" : "Aceptado";

                    dal.Iniciar();
                    try
                    {
                        int idRecepcion = dal.InsertarRecepcion(new RecepcionInsumoBE
                        {
                            IdOrden = orden.IdOrden,
                            IdInsumo = orden.IdInsumo,           // el insumo sale de la orden, no de la pantalla
                            CantidadRecibida = datos.CantidadRecibida,
                            Estado = estadoRecepcion,
                            Observaciones = datos.Observaciones
                        }, ReposicionComun.IdActual);

                        mov = _stock.ActualizarStock(dal, orden.IdInsumo, datos.CantidadRecibida, idRecepcion);

                        string estadoOrden = conDiferencia ? "Con diferencia" : "Recibida";
                        if (!dal.ActualizarEstadoOrden(orden.IdOrden, estadoOrden, "Emitida"))
                        {
                            dal.Revertir();
                            return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, "La orden fue modificada por otro usuario. Volvé a intentar.");
                        }
                        dal.Confirmar();

                        guardada = new RecepcionInsumoBE
                        {
                            IdRecepcion = idRecepcion,
                            IdOrden = orden.IdOrden,
                            NroOrden = orden.NroOrden,
                            IdInsumo = orden.IdInsumo,
                            NombreInsumo = orden.NombreInsumo,
                            CantidadRecibida = datos.CantidadRecibida,
                            FechaRecepcion = DateTime.Now,
                            Estado = estadoRecepcion,
                            Observaciones = datos.Observaciones ?? ""
                        };
                    }
                    catch (SqlException ex) when (ReposicionComun.EsDuplicado(ex))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<RecepcionInsumoBE>(OP, $"La orden {orden.NroOrden} ya tiene su recepción registrada.");
                    }
                }

                BitacoraReposicion.Registrar(conDiferencia ? BitacoraReposicion.RECEPCION_DIF : BitacoraReposicion.RECEPCION,
                    conDiferencia ? 2 : 3,
                    $"{orden.NroOrden}: pedido {orden.Cantidad:0.##}, recibido {datos.CantidadRecibida:0.##} de {orden.NombreInsumo}.",
                    "EXITOSO");
                BitacoraReposicion.Registrar(BitacoraReposicion.STOCK_ACTUALIZADO, 4,
                    $"{mov.NombreInsumo}: {mov.CantidadAnterior:0.##} → {mov.CantidadNueva:0.##} (+{mov.CantidadMovimiento:0.##}).", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                string msg = $"Recepción registrada. Stock de {mov.NombreInsumo}: {mov.CantidadAnterior:0.##} + {mov.CantidadMovimiento:0.##} = {mov.CantidadNueva:0.##}.";
                if (conDiferencia)
                    msg += $" Atención: se pidieron {orden.Cantidad:0.##} y se recibieron {datos.CantidadRecibida:0.##}.";
                return ResultadoReposicion<RecepcionInsumoBE>.Ok(guardada, msg);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<RecepcionInsumoBE>(OP, ex); }
        }

        /// <summary>Órdenes emitidas esperando mercadería.</summary>
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
