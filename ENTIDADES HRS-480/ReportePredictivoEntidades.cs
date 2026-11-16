using System;
using System.Collections.Generic;

namespace ENTIDADES_HRS_480
{
    /// <summary>Tipo de habitación con su capacidad (habitaciones activas) y precio por noche.</summary>
    public class CapacidadTipoBE
    {
        public int IdTipoHabitacion { get; set; }
        public string Tipo { get; set; } = "";
        public decimal PrecioPorNoche { get; set; }
        public int Habitaciones { get; set; }
    }

    /// <summary>Reserva reducida a lo que necesita el reporte: tipo, fechas y estado.</summary>
    public class ReservaPeriodoBE
    {
        public int IdTipoHabitacion { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Estado { get; set; } = "";
        /// <summary>¿La reserva ocupa la habitación la noche <paramref name="noche"/>?</summary>
        public bool OcupaNoche(DateTime noche) => FechaIngreso.Date <= noche.Date && noche.Date < FechaSalida.Date;
    }

    /// <summary>Predicción de un día (sección "Detalle por día").</summary>
    public class PrediccionDiaBE
    {
        public DateTime Fecha { get; set; }
        public int Confirmadas { get; set; }            // reservas ya tomadas para esa noche
        public decimal Proyeccion { get; set; }         // promedio de los mismos días de la semana (últimas 4 semanas)
        public decimal Estimadas { get; set; }          // máx(confirmadas, proyección), sin superar la capacidad
        public int Capacidad { get; set; }
        public decimal Ocupacion => Capacidad == 0 ? 0 : Estimadas / Capacidad * 100m;
        public string Nivel { get; set; } = "";         // Completo / Alta / Media / Baja
        public decimal IngresoEstimado { get; set; }    // con 21 % de impuestos, igual que la factura
        public string Recomendacion { get; set; } = "";
    }

    /// <summary>Predicción por tipo de habitación (sección "Por tipo de habitación").</summary>
    public class PrediccionTipoBE
    {
        public string Tipo { get; set; } = "";
        public int Habitaciones { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public int NochesHistoricas { get; set; }       // noches vendidas en los últimos 28 días
        public decimal PromedioDiario { get; set; }     // habitaciones ocupadas por noche (histórico)
        public decimal OcupacionHistorica { get; set; } // %
        public decimal NochesEstimadas { get; set; }    // próximos 7 días
        public decimal OcupacionPrevista { get; set; }  // %
        public string Tendencia { get; set; } = "";     // Sube / Estable / Baja
        public decimal IngresoEstimado { get; set; }
        public string Recomendacion { get; set; } = "";
    }

    /// <summary>Insumo crítico priorizado según la ocupación prevista.</summary>
    public class InsumoPrioridadBE
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal ReponerSugerido { get; set; }
        public string Prioridad { get; set; } = "";     // Urgente / Alta / Normal
    }

    /// <summary>Resultado completo del Reporte Inteligente Predictivo de ocupación.</summary>
    public class ReportePredictivoBE
    {
        public const int DIAS_HISTORICOS = 28;
        public const int DIAS_PROYECCION = 7;
        public const decimal IMPUESTOS = 0.21m;

        public DateTime HistoricoDesde { get; set; }
        public DateTime HistoricoHasta { get; set; }
        public DateTime ProyeccionDesde { get; set; }
        public DateTime ProyeccionHasta { get; set; }
        public int CapacidadTotal { get; set; }
        public int NochesHistoricas { get; set; }
        public decimal OcupacionHistorica { get; set; }
        public decimal OcupacionPrevista { get; set; }
        public decimal IngresoEstimado { get; set; }
        public bool SinHistorial { get; set; }
        public List<PrediccionDiaBE> Dias { get; set; } = new List<PrediccionDiaBE>();
        public List<PrediccionTipoBE> Tipos { get; set; } = new List<PrediccionTipoBE>();
        public List<InsumoPrioridadBE> Insumos { get; set; } = new List<InsumoPrioridadBE>();
        public List<string> Lectura { get; set; } = new List<string>();
    }
}
