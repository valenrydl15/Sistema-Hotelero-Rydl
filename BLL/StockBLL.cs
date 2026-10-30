using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>Stock de insumos: consulta de faltantes (paso 1 del PN) y alta de ingresos (CUN12).</summary>
    public class StockBLL
    {
        /// <param name="soloFaltantes">true = solo los insumos con stock en o por debajo del mínimo.</param>
        public ResultadoReposicion<List<InsumoBE>> ObtenerStockInsumos(bool soloFaltantes = false)
        {
            if (!ControlAcceso.PuedeConsultarStock())
                return ReposicionComun.Denegado<List<InsumoBE>>("Consultar stock");
            try
            {
                using var dal = new ReposicionDAL();
                var lista = dal.ObtenerInsumos();
                if (soloFaltantes) lista = lista.Where(i => i.Faltante).ToList();
                BitacoraReposicion.Registrar(BitacoraReposicion.CONSULTA_STOCK, 5,
                    $"{lista.Count} insumos consultados{(soloFaltantes ? " (solo faltantes)" : "")}.", "EXITOSO");
                return ResultadoReposicion<List<InsumoBE>>.Ok(lista);
            }
            catch (Exception ex) { return ReposicionComun.ErrorTecnico<List<InsumoBE>>("Consultar stock", ex); }
        }

        /// <summary>
        /// Suma lo recibido al stock y deja el movimiento (cantidad anterior, recibida y disponible).
        /// Corre DENTRO de la transacción de la recepción: si algo falla no queda nada a medias.
        /// </summary>
        public MovimientoStockBE ActualizarStock(ReposicionDAL dal, int idInsumo, decimal cantidadRecibida, int idRecepcion)
        {
            var insumo = dal.ObtenerInsumoPorId(idInsumo, bloquear: true)
                         ?? throw new InvalidOperationException("Un insumo de la orden ya no existe.");
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
                CantidadNueva = total,
                FechaMovimiento = DateTime.Now
            };
            mov.IdMovimiento = dal.InsertarMovimientoStock(mov, ReposicionComun.IdActual);
            return mov;
        }
    }
}
