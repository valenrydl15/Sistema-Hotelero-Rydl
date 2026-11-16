using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// Datos del Reporte Inteligente Predictivo (solo lectura): capacidad por tipo de habitación
    /// y reservas que se superponen con el período analizado (histórico + proyección).
    /// </summary>
    public class ReportePredictivoDAL
    {
        public List<CapacidadTipoBE> ObtenerCapacidadPorTipo()
        {
            var lista = new List<CapacidadTipoBE>();
            const string sql = @"
                SELECT t.IdTipoHabitacion, t.Nombre, t.PrecioPorNoche, COUNT(h.IdHabitacion) AS Habitaciones
                FROM TiposHabitacion t
                INNER JOIN Habitaciones h ON h.IdTipoHabitacion = t.IdTipoHabitacion AND h.Activa = 1
                WHERE t.Activo = 1
                GROUP BY t.IdTipoHabitacion, t.Nombre, t.PrecioPorNoche
                ORDER BY t.PrecioPorNoche";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new CapacidadTipoBE
                {
                    IdTipoHabitacion = Convert.ToInt32(rd["IdTipoHabitacion"]),
                    Tipo = rd["Nombre"].ToString() ?? "",
                    PrecioPorNoche = Convert.ToDecimal(rd["PrecioPorNoche"]),
                    Habitaciones = Convert.ToInt32(rd["Habitaciones"])
                });
            return lista;
        }

        /// <summary>Reservas no canceladas que ocupan alguna noche entre <paramref name="desde"/> y <paramref name="hasta"/> (exclusivo).</summary>
        public List<ReservaPeriodoBE> ObtenerReservas(DateTime desde, DateTime hasta)
        {
            var lista = new List<ReservaPeriodoBE>();
            const string sql = @"
                SELECT hab.IdTipoHabitacion, r.FechaIngreso, r.FechaSalida, r.Estado
                FROM Reservas r
                INNER JOIN Habitaciones hab ON hab.IdHabitacion = r.IdHabitacion
                WHERE r.Estado <> 'Cancelada'
                  AND r.FechaIngreso < @Hasta
                  AND r.FechaSalida  > @Desde";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new ReservaPeriodoBE
                {
                    IdTipoHabitacion = Convert.ToInt32(rd["IdTipoHabitacion"]),
                    FechaIngreso = Convert.ToDateTime(rd["FechaIngreso"]),
                    FechaSalida = Convert.ToDateTime(rd["FechaSalida"]),
                    Estado = rd["Estado"].ToString() ?? ""
                });
            return lista;
        }
    }
}
