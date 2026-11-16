using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// A02 Reporte Inteligente Predictivo de ocupación.
    /// Analiza las estadías de los últimos 28 días completos y proyecta la ocupación de los próximos 7 días:
    ///  · Proyección de cada día = promedio de habitaciones ocupadas los mismos días de la semana de las últimas 4 semanas.
    ///  · Estimadas = el mayor valor entre las reservas ya confirmadas y la proyección, sin superar la capacidad.
    ///  · Con eso calcula ocupación (%), nivel de demanda, ingreso estimado (con 21 % de impuestos) y recomendaciones,
    ///    y prioriza los insumos críticos según la ocupación prevista (vínculo con el PN2).
    /// Es de solo lectura: no modifica tablas ni dígitos verificadores. Cada generación queda en la bitácora.
    /// </summary>
    public class ReportePredictivoBLL
    {
        private readonly ReportePredictivoDAL _dal = new ReportePredictivoDAL();
        private readonly InsumoDAL _insumos = new InsumoDAL();
        private static readonly CultureInfo AR = new CultureInfo("es-AR");

        // Estados de reserva que cuentan
        private static readonly string[] REALIZADAS = { "CheckIn", "CheckOut" };          // estadías que ocurrieron
        private static readonly string[] CONFIRMADAS = { "Pendiente", "Confirmada", "CheckIn" };   // ocupan noches futuras

        public ResultadoOperacion<ReportePredictivoBE> Generar() => Generar(DateTime.Today);

        public ResultadoOperacion<ReportePredictivoBE> Generar(DateTime hoy)
        {
            if (!ControlAcceso.PuedeVerReportePredictivo())
            {
                BitacoraModulo.Registrar(BitacoraModulo.MOD_REPORTES, BitacoraModulo.REPORTES_RECHAZO, 3,
                    "Acceso denegado: reporte predictivo de ocupación.", "RECHAZADO");
                return ResultadoOperacion<ReportePredictivoBE>.Fallo("No tenés permiso para ver el reporte predictivo.");
            }
            try
            {
                hoy = hoy.Date;
                var tipos = _dal.ObtenerCapacidadPorTipo();
                if (tipos.Count == 0)
                    return ResultadoOperacion<ReportePredictivoBE>.Fallo("No hay tipos de habitación activos con habitaciones: no se puede estimar la ocupación.");
                var reservas = _dal.ObtenerReservas(hoy.AddDays(-ReportePredictivoBE.DIAS_HISTORICOS), hoy.AddDays(ReportePredictivoBE.DIAS_PROYECCION));
                var insumos = _insumos.ObtenerInsumos(false);
                var rep = Calcular(tipos, reservas, insumos, hoy);

                BitacoraModulo.Registrar(BitacoraModulo.MOD_REPORTES, BitacoraModulo.REPORTE_PREDICTIVO, 5,
                    $"Reporte predictivo {rep.ProyeccionDesde:dd/MM/yyyy}-{rep.ProyeccionHasta:dd/MM/yyyy}: ocupación prevista {rep.OcupacionPrevista:0.0} % " +
                    $"(histórica {rep.OcupacionHistorica:0.0} %), ingreso estimado {rep.IngresoEstimado.ToString("C0", AR)}, {rep.Insumos.Count} insumo/s crítico/s.", "EXITOSO");
                return ResultadoOperacion<ReportePredictivoBE>.Ok(rep, $"Reporte generado: próximos {ReportePredictivoBE.DIAS_PROYECCION} días.");
            }
            catch (Exception ex)
            {
                BitacoraModulo.Registrar(BitacoraModulo.MOD_REPORTES, BitacoraModulo.REPORTES_ERROR, 1, "Reporte predictivo: " + ex.Message, "FALLIDO");
                return ResultadoOperacion<ReportePredictivoBE>.Fallo("No se pudo generar el reporte. Volvé a intentar o avisá al administrador.");
            }
        }

        /// <summary>Cálculo puro del reporte (sin base de datos): se puede probar con datos de ejemplo.</summary>
        public static ReportePredictivoBE Calcular(List<CapacidadTipoBE> tipos, List<ReservaPeriodoBE> reservas, List<InsumoBE> insumos, DateTime hoy)
        {
            hoy = hoy.Date;
            int H = ReportePredictivoBE.DIAS_HISTORICOS, P = ReportePredictivoBE.DIAS_PROYECCION;
            decimal IVA = 1 + ReportePredictivoBE.IMPUESTOS;
            var rep = new ReportePredictivoBE
            {
                HistoricoDesde = hoy.AddDays(-H), HistoricoHasta = hoy.AddDays(-1),
                ProyeccionDesde = hoy, ProyeccionHasta = hoy.AddDays(P - 1),
                CapacidadTotal = tipos.Sum(t => t.Habitaciones)
            };
            var noches = Enumerable.Range(1, H).Select(i => hoy.AddDays(-i)).ToList();
            var futuras = Enumerable.Range(0, P).Select(i => hoy.AddDays(i)).ToList();

            // proyección por tipo y día de la semana
            var estim = new Dictionary<(int tipo, DateTime dia), (int conf, decimal proy, decimal est)>();
            foreach (var t in tipos)
            {
                var realizadas = reservas.Where(r => r.IdTipoHabitacion == t.IdTipoHabitacion && REALIZADAS.Contains(r.Estado)).ToList();
                var confirmadas = reservas.Where(r => r.IdTipoHabitacion == t.IdTipoHabitacion && CONFIRMADAS.Contains(r.Estado)).ToList();
                var ocupHist = noches.ToDictionary(n => n, n => Math.Min(t.Habitaciones, realizadas.Count(r => r.OcupaNoche(n))));
                int totalHist = ocupHist.Values.Sum();

                var pt = new PrediccionTipoBE
                {
                    Tipo = t.Tipo, Habitaciones = t.Habitaciones, PrecioPorNoche = t.PrecioPorNoche, NochesHistoricas = totalHist,
                    PromedioDiario = Math.Round((decimal)totalHist / H, 2),
                    OcupacionHistorica = t.Habitaciones == 0 ? 0 : Math.Round((decimal)totalHist / (t.Habitaciones * H) * 100m, 1)
                };
                foreach (var d in futuras)
                {
                    var mismos = ocupHist.Where(x => x.Key.DayOfWeek == d.DayOfWeek).Select(x => x.Value).ToList();
                    decimal proy = mismos.Count == 0 ? 0 : Math.Round((decimal)mismos.Average(), 1);
                    int conf = Math.Min(t.Habitaciones, confirmadas.Count(r => r.OcupaNoche(d)));
                    decimal est = Math.Min(t.Habitaciones, Math.Max(conf, proy));
                    estim[(t.IdTipoHabitacion, d)] = (conf, proy, est);
                    pt.NochesEstimadas += est;
                }
                pt.OcupacionPrevista = t.Habitaciones == 0 ? 0 : Math.Round(pt.NochesEstimadas / (t.Habitaciones * P) * 100m, 1);
                decimal dif = pt.OcupacionPrevista - pt.OcupacionHistorica;
                pt.Tendencia = dif >= 10 ? "Sube" : dif <= -10 ? "Baja" : "Estable";
                pt.IngresoEstimado = Math.Round(pt.NochesEstimadas * t.PrecioPorNoche * IVA, 2);
                pt.Recomendacion = pt.OcupacionPrevista >= 85 ? "Demanda alta: evitar sobreventa y revisar la tarifa"
                                 : pt.OcupacionPrevista <= 30 ? "Demanda baja: ofrecer promociones"
                                 : "Demanda normal: mantener la tarifa";
                rep.Tipos.Add(pt);
                rep.NochesHistoricas += totalHist;
            }

            foreach (var d in futuras)
            {
                var pd = new PrediccionDiaBE { Fecha = d, Capacidad = rep.CapacidadTotal };
                foreach (var t in tipos)
                {
                    var (conf, proy, est) = estim[(t.IdTipoHabitacion, d)];
                    pd.Confirmadas += conf; pd.Proyeccion += proy; pd.Estimadas += est;
                    pd.IngresoEstimado += est * t.PrecioPorNoche * IVA;
                }
                pd.IngresoEstimado = Math.Round(pd.IngresoEstimado, 2);
                pd.Nivel = Nivel(pd.Ocupacion);
                pd.Recomendacion = pd.Nivel switch
                {
                    "Completo" => "No aceptar reservas sin disponibilidad confirmada; reforzar recepción y limpieza",
                    "Alta" => "Asegurar insumos y personal; controlar la disponibilidad antes de confirmar",
                    "Media" => "Operación normal",
                    _ => "Ofrecer promociones para ese día"
                };
                rep.Dias.Add(pd);
            }

            int cap = rep.CapacidadTotal;
            rep.OcupacionHistorica = cap == 0 ? 0 : Math.Round((decimal)rep.NochesHistoricas / (cap * H) * 100m, 1);
            rep.OcupacionPrevista = cap == 0 ? 0 : Math.Round(rep.Dias.Sum(d => d.Estimadas) / (cap * P) * 100m, 1);
            rep.IngresoEstimado = rep.Dias.Sum(d => d.IngresoEstimado);
            rep.SinHistorial = rep.NochesHistoricas == 0;

            // insumos críticos priorizados por la ocupación prevista
            foreach (var i in (insumos ?? new List<InsumoBE>()).Where(i => i.Activo && i.Faltante))
            {
                decimal sugerido = Math.Max(1, Math.Ceiling(i.StockMinimo * (1 + rep.OcupacionPrevista / 100m) - i.StockActual));
                rep.Insumos.Add(new InsumoPrioridadBE
                {
                    Codigo = i.Codigo, Nombre = i.Nombre, Categoria = i.Categoria, Unidad = i.UnidadMedida,
                    StockActual = i.StockActual, StockMinimo = i.StockMinimo, ReponerSugerido = sugerido,
                    Prioridad = i.StockActual <= 0 || rep.OcupacionPrevista >= 70 ? "Urgente" : rep.OcupacionPrevista >= 40 ? "Alta" : "Normal"
                });
            }
            rep.Insumos = rep.Insumos.OrderBy(x => x.Prioridad == "Urgente" ? 0 : x.Prioridad == "Alta" ? 1 : 2)
                                     .ThenByDescending(x => x.StockMinimo - x.StockActual).ToList();

            // lectura de los resultados
            rep.Lectura.Add($"Ocupación prevista para los próximos {P} días: {rep.OcupacionPrevista:0.0} % (histórica de los últimos {H} días: {rep.OcupacionHistorica:0.0} %).");
            var pico = rep.Dias.OrderByDescending(d => d.Ocupacion).ThenBy(d => d.Fecha).First();
            rep.Lectura.Add($"Día de mayor ocupación prevista: {pico.Fecha.ToString("dddd dd/MM", AR)} con {pico.Ocupacion:0.0} % (nivel {pico.Nivel}).");
            var top = rep.Tipos.OrderByDescending(t => t.OcupacionPrevista).First();
            rep.Lectura.Add($"Tipo de habitación con mayor demanda prevista: {top.Tipo} ({top.OcupacionPrevista:0.0} %, tendencia {top.Tendencia.ToLower()}).");
            rep.Lectura.Add($"Ingreso estimado de los próximos {P} días (con impuestos): {rep.IngresoEstimado.ToString("C2", AR)}.");
            int urg = rep.Insumos.Count(x => x.Prioridad == "Urgente");
            rep.Lectura.Add(rep.Insumos.Count == 0 ? "No hay insumos críticos para reponer."
                : $"Insumos críticos a reponer: {rep.Insumos.Count} ({urg} urgente/s por la ocupación prevista).");
            if (rep.SinHistorial)
                rep.Lectura.Add($"No hubo estadías en los últimos {H} días: la estimación usa solo las reservas confirmadas.");
            return rep;
        }

        public static string Nivel(decimal ocupacion)
            => ocupacion >= 90 ? "Completo" : ocupacion >= 70 ? "Alta" : ocupacion >= 40 ? "Media" : "Baja";
    }
}
