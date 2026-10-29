using DAL_HRS_480;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace BLL_HRS_480
{
    /// <summary>CUN11 Registrar solicitud de reembolso por faltante en una recepción.</summary>
    public class ReembolsoBLL
    {
        private static readonly string[] TABLAS = { "SolicitudesReembolso", "OrdenesCompra" };

        public ResultadoReposicion<SolicitudReembolsoBE> RegistrarSolicitudReembolso(int idOrden, string motivo)
        {
            const string OP = "Registrar solicitud de reembolso";
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<SolicitudReembolsoBE>(OP);

            motivo = (motivo ?? "").Trim();
            if (motivo.Length == 0)
                return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, "Indicá el motivo del reembolso.");
            if (motivo.Length > 300)
                return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, "El motivo no puede superar los 300 caracteres.");

            try
            {
                bool dvOk = DigitoVerificador.Consistente(TABLAS);
                SolicitudReembolsoBE reembolso;

                using (var dal = new ReposicionDAL())
                {
                    var orden = dal.ObtenerOrdenPorId(idOrden);
                    if (orden == null)
                        return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, "La orden no existe.");
                    if (dal.ExisteReembolso(idOrden) || orden.Estado == "Con reembolso")
                        return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, $"La orden {orden.NroOrden} ya tiene un reembolso registrado.");
                    if (orden.Estado != "Con diferencia")
                        return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP,
                            $"La orden {orden.NroOrden} no tiene una recepción con diferencia (estado: {orden.Estado}).");

                    decimal recibida = dal.ObtenerCantidadRecibidaPorOrden(idOrden);
                    decimal diferencia = orden.Cantidad - recibida;
                    if (diferencia <= 0)
                        return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP,
                            "No hay faltante: se recibió igual o más de lo pedido, no corresponde reembolso.");

                    reembolso = new SolicitudReembolsoBE
                    {
                        IdOrden = idOrden,
                        NroOrden = orden.NroOrden,
                        NombreInsumo = orden.NombreInsumo,
                        NombreProveedor = orden.NombreProveedor,
                        CantidadComprada = orden.Cantidad,
                        CantidadRecibida = recibida,
                        Diferencia = diferencia,
                        MontoReembolso = Math.Round(diferencia * orden.PrecioUnitario, 2),
                        Motivo = motivo
                    };

                    dal.Iniciar();
                    try
                    {
                        reembolso.IdReembolso = dal.InsertarReembolso(reembolso, ReposicionComun.IdActual);
                        if (!dal.ActualizarEstadoOrden(idOrden, "Con reembolso", "Con diferencia"))
                        {
                            dal.Revertir();
                            return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, "La orden fue modificada por otro usuario. Volvé a intentar.");
                        }
                        dal.Confirmar();
                    }
                    catch (SqlException ex) when (ReposicionComun.EsDuplicado(ex))
                    {
                        dal.Revertir();
                        return ReposicionComun.Rechazo<SolicitudReembolsoBE>(OP, $"La orden {orden.NroOrden} ya tiene un reembolso registrado.");
                    }
                }

                reembolso.FechaReembolso = DateTime.Now;
                BitacoraReposicion.Registrar(BitacoraReposicion.REEMBOLSO, 3,
                    $"{reembolso.NroOrden}: faltan {reembolso.Diferencia:0.##} de {reembolso.NombreInsumo}; reembolso $ {reembolso.MontoReembolso:N2}. {motivo}", "EXITOSO");
                DigitoVerificador.RecalcularSi(dvOk, TABLAS);

                return ResultadoReposicion<SolicitudReembolsoBE>.Ok(reembolso,
                    $"Reembolso registrado por $ {reembolso.MontoReembolso:N2} ({reembolso.Diferencia:0.##} unidades faltantes).");
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<SolicitudReembolsoBE>(OP, ex); }
        }

        /// <summary>Órdenes recibidas con diferencia que aún no tienen reembolso.</summary>
        public ResultadoReposicion<List<OrdenCompraBE>> ObtenerOrdenesConDiferencia()
        {
            if (!ControlAcceso.PuedeGestionarCompras())
                return ReposicionComun.Denegado<List<OrdenCompraBE>>("Ver órdenes con diferencia");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<OrdenCompraBE>>.Ok(dal.ObtenerOrdenes("Con diferencia"));
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<OrdenCompraBE>>("Ver órdenes con diferencia", ex); }
        }

        /// <summary>Cantidad efectivamente recibida de una orden (para mostrar el faltante antes de pedir el reembolso).</summary>
        public decimal ObtenerCantidadRecibida(int idOrden)
        {
            if (!ControlAcceso.PuedeGestionarCompras()) return 0;
            try
            {
                using var dal = new ReposicionDAL();
                return dal.ObtenerCantidadRecibidaPorOrden(idOrden);
            }
            catch { return 0; }
        }

        public ResultadoReposicion<List<SolicitudReembolsoBE>> ObtenerReembolsos()
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<SolicitudReembolsoBE>>("Ver reembolsos");
            try
            {
                using var dal = new ReposicionDAL();
                return ResultadoReposicion<List<SolicitudReembolsoBE>>.Ok(dal.ObtenerReembolsos());
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<SolicitudReembolsoBE>>("Ver reembolsos", ex); }
        }
    }
}
