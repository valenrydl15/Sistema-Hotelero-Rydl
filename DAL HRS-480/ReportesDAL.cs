using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>Consultas de solo lectura para los reportes PDF (no modifica datos ni dígitos verificadores).</summary>
    public class ReportesDAL
    {
        private const string SELECT_FACTURA = @"
            SELECT f.NroFactura, f.FechaEmision, f.Estado, f.MontoTotal, f.IdReserva,
                   h.Apellido, h.Nombre, h.DNI,
                   hab.Numero AS NumeroHabitacion, t.Nombre AS TipoHabitacion, t.PrecioPorNoche,
                   r.FechaIngreso, r.FechaSalida,
                   DATEDIFF(DAY, r.FechaIngreso, r.FechaSalida) AS Noches,
                   p.MetodoPago, p.UltimosCuatroTarjeta,
                   u.NombreUsuario AS EmitidaPor
            FROM Facturas f
            INNER JOIN Reservas r        ON r.IdReserva    = f.IdReserva
            INNER JOIN Huespedes h       ON h.IdHuesped    = r.IdHuesped
            INNER JOIN Habitaciones hab  ON hab.IdHabitacion = r.IdHabitacion
            INNER JOIN TiposHabitacion t ON t.IdTipoHabitacion = hab.IdTipoHabitacion
            LEFT JOIN Usuarios u         ON u.IdUsuario    = f.IdUsuarioRegistro
            OUTER APPLY (SELECT TOP 1 MetodoPago, UltimosCuatroTarjeta
                         FROM Pagos pg WHERE pg.IdReserva = f.IdReserva
                         ORDER BY pg.FechaHora DESC) p";

        private static string S(SqlDataReader r, string col)
            => r[col] == DBNull.Value ? "" : Convert.ToString(r[col]) ?? "";

        private static FacturaReporteBE Mapear(SqlDataReader r) => new FacturaReporteBE
        {
            NroFactura = S(r, "NroFactura"),
            FechaEmision = Convert.ToDateTime(r["FechaEmision"]),
            Estado = S(r, "Estado"),
            MontoTotal = Convert.ToDecimal(r["MontoTotal"]),
            IdReserva = Convert.ToInt32(r["IdReserva"]),
            Apellido = S(r, "Apellido"),
            Nombre = S(r, "Nombre"),
            DNI = S(r, "DNI"),
            NumeroHabitacion = S(r, "NumeroHabitacion"),
            TipoHabitacion = S(r, "TipoHabitacion"),
            PrecioPorNoche = Convert.ToDecimal(r["PrecioPorNoche"]),
            FechaIngreso = Convert.ToDateTime(r["FechaIngreso"]),
            FechaSalida = Convert.ToDateTime(r["FechaSalida"]),
            Noches = Convert.ToInt32(r["Noches"]),
            MetodoPago = S(r, "MetodoPago"),
            UltimosCuatroTarjeta = S(r, "UltimosCuatroTarjeta"),
            EmitidaPor = S(r, "EmitidaPor")
        };

        /// <param name="desde">Inclusive (null = sin límite).</param>
        /// <param name="hasta">Inclusive, hasta el final del día (null = sin límite).</param>
        public List<FacturaReporteBE> ObtenerFacturas(DateTime? desde, DateTime? hasta)
        {
            var lista = new List<FacturaReporteBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = SELECT_FACTURA + @"
                WHERE (@Desde IS NULL OR f.FechaEmision >= @Desde)
                  AND (@Hasta IS NULL OR f.FechaEmision <  DATEADD(DAY, 1, @Hasta))
                ORDER BY f.FechaEmision DESC, f.NroFactura DESC";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Desde", desde.HasValue ? desde.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@Hasta", hasta.HasValue ? hasta.Value.Date : DBNull.Value);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(Mapear(rd));
            return lista;
        }

        public FacturaReporteBE? ObtenerFactura(string nroFactura)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT_FACTURA + " WHERE f.NroFactura = @Nro", con);
            cmd.Parameters.AddWithValue("@Nro", nroFactura);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? Mapear(rd) : null;
        }
    }
}
