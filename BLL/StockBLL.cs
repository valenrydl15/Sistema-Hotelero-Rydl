using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>Consulta de stock y alta de ingresos al stock (usado por CUN06 y CUN10).</summary>
    public class StockBLL
    {
        /// <summary>Stock de todos los insumos activos, con su estado calculado (Crítico/Normal).</summary>
        public ResultadoReposicion<List<InsumoBE>> ObtenerStockInsumos(bool soloCriticos = false)
        {
            if (!ControlAcceso.PuedeAccederReposicion())
                return ReposicionComun.Denegado<List<InsumoBE>>("Consultar stock");
            try
            {
                using var dal = new ReposicionDAL();
                var lista = dal.ObtenerInsumos();
                if (soloCriticos) lista = lista.Where(i => i.Estado == "Critico").ToList();

                BitacoraReposicion.Registrar(BitacoraReposicion.CONSULTA_STOCK, 5,
                    $"{lista.Count} insumos consultados{(soloCriticos ? " (solo críticos)" : "")}.", "EXITOSO");
                return ResultadoReposicion<List<InsumoBE>>.Ok(lista);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<InsumoBE>>("Consultar stock", ex); }
        }

        /// <summary>
        /// Suma lo recibido al stock y deja el movimiento registrado.
        /// Se ejecuta DENTRO de la transacción de la recepción (el dal ya tiene la transacción abierta):
        /// si algo falla, no queda ni el stock modificado ni el movimiento.
        /// </summary>
        public MovimientoStockBE ActualizarStock(ReposicionDAL dal, int idInsumo, decimal cantidadRecibida, int idRecepcion)
        {
            var insumo = dal.ObtenerInsumoPorId(idInsumo, bloquear: true)
                         ?? throw new InvalidOperationException("El insumo de la orden ya no existe.");

            decimal total = insumo.StockActual + cantidadRecibida;
            dal.ActualizarStockInsumo(idInsumo, total);

            var mov = new MovimientoStockBE
            {
                IdInsumo = idInsumo,
                NombreInsumo = insumo.Nombre,
                IdRecepcion = idRecepcion,
                Tipo = "Ingreso",
                CantidadAnterior = insumo.StockActual,
                CantidadMovimiento = cantidadRecibida,
                CantidadNueva = total
            };
            mov.IdMovimiento = dal.InsertarMovimientoStock(mov, ReposicionComun.IdActual);
            return mov;
        }
    }
}
