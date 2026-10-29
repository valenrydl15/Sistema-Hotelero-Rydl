using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace DAL_HRS_480
{
    public class CheckInDAL
    {
        public List<HabitacionBE> ObtenerHabitacionesDisponibles(DateTime fechaIngreso, DateTime fechaSalida)
        {
            var lista = new List<HabitacionBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            // Devuelve habitaciones sin reservas activas en el período solicitado
            const string sql = @"
                SELECT h.*, t.Nombre AS NombreTipo, t.PrecioPorNoche
                FROM Habitaciones h
                INNER JOIN TiposHabitacion t ON h.IdTipoHabitacion = t.IdTipoHabitacion
                WHERE h.Estado = 'Disponible' AND h.Activa = 1
                  AND h.IdHabitacion NOT IN (
                    SELECT r.IdHabitacion FROM Reservas r
                    WHERE r.Estado IN ('Pendiente','Confirmada','CheckIn')
                      AND r.FechaIngreso < @FechaSalida
                      AND r.FechaSalida  > @FechaIngreso
                  )
                ORDER BY h.Piso, h.Numero";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@FechaIngreso", fechaIngreso);
            cmd.Parameters.AddWithValue("@FechaSalida", fechaSalida);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(MapearHabitacion(rd));
            return lista;
        }

        public HabitacionBE ObtenerHabitacionPorId(int id)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                SELECT h.*, t.Nombre AS NombreTipo, t.PrecioPorNoche
                FROM Habitaciones h
                INNER JOIN TiposHabitacion t ON h.IdTipoHabitacion = t.IdTipoHabitacion
                WHERE h.IdHabitacion = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearHabitacion(rd) : null;
        }

        public List<TipoHabitacionBE> ObtenerTiposHabitacion()
        {
            var lista = new List<TipoHabitacionBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT * FROM TiposHabitacion WHERE Activo=1 ORDER BY PrecioPorNoche", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new TipoHabitacionBE
                {
                    IdTipoHabitacion = (int)rd["IdTipoHabitacion"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    PrecioPorNoche = (decimal)rd["PrecioPorNoche"],
                    Capacidad = (int)rd["Capacidad"],
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        public void ActualizarEstadoHabitacion(int idHabitacion, string nuevoEstado)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "UPDATE Habitaciones SET Estado=@Estado WHERE IdHabitacion=@Id", con);
            cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", idHabitacion);
            cmd.ExecuteNonQuery();
        }

        private static HabitacionBE MapearHabitacion(SqlDataReader rd) => new HabitacionBE
        {
            IdHabitacion = (int)rd["IdHabitacion"],
            Numero = rd["Numero"].ToString(),
            IdTipoHabitacion = (int)rd["IdTipoHabitacion"],
            NombreTipo = rd["NombreTipo"].ToString(),
            PrecioPorNoche = (decimal)rd["PrecioPorNoche"],
            Piso = (int)rd["Piso"],
            Estado = rd["Estado"].ToString(),
            Observaciones = rd["Observaciones"]?.ToString(),
            Activa = (bool)rd["Activa"]
        };

        // ══════════════════════════════════════════════════════
        // HUÉSPEDES
        // ══════════════════════════════════════════════════════
        public HuespedBE ObtenerHuespedPorDNI(string dni)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT * FROM Huespedes WHERE DNI=@DNI AND Activo=1", con);
            cmd.Parameters.AddWithValue("@DNI", dni);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearHuesped(rd) : null;
        }

        public int InsertarHuesped(HuespedBE h)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                INSERT INTO Huespedes (Nombre, Apellido, DNI, Email, Telefono, Nacionalidad)
                VALUES (@Nombre, @Apellido, @DNI, @Email, @Telefono, @Nacionalidad);
                SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", h.Nombre);
            cmd.Parameters.AddWithValue("@Apellido", h.Apellido);
            cmd.Parameters.AddWithValue("@DNI", h.DNI);
            cmd.Parameters.AddWithValue("@Email", (object)h.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Telefono", (object)h.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Nacionalidad", (object)h.Nacionalidad ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static HuespedBE MapearHuesped(SqlDataReader rd) => new HuespedBE
        {
            IdHuesped = (int)rd["IdHuesped"],
            Nombre = rd["Nombre"].ToString(),
            Apellido = rd["Apellido"].ToString(),
            DNI = rd["DNI"].ToString(),
            Email = rd["Email"]?.ToString(),
            Telefono = rd["Telefono"]?.ToString(),
            Nacionalidad = rd["Nacionalidad"]?.ToString(),
            FechaRegistro = (DateTime)rd["FechaRegistro"],
            Activo = (bool)rd["Activo"]
        };

        // ══════════════════════════════════════════════════════
        // RESERVAS
        // ══════════════════════════════════════════════════════
        public int InsertarReserva(ReservaBE r, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sql = @"
        INSERT INTO Reservas
        (
            IdHuesped,
            IdHabitacion,
            FechaIngreso,
            FechaSalida,
            Estado,
            FechaReserva,
            IdUsuarioRegistro,
            Observaciones
        )
        VALUES
        (
            @IdHuesped,
            @IdHabitacion,
            @FechaIngreso,
            @FechaSalida,
            'Pendiente',
            GETDATE(),
            @IdUsuarioRegistro,
            @Observaciones
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@IdHuesped", r.IdHuesped);
            cmd.Parameters.AddWithValue("@IdHabitacion", r.IdHabitacion);
            cmd.Parameters.AddWithValue("@FechaIngreso", r.FechaIngreso);
            cmd.Parameters.AddWithValue("@FechaSalida", r.FechaSalida);
            cmd.Parameters.AddWithValue("@IdUsuarioRegistro", idUsuario);
            cmd.Parameters.AddWithValue(
                "@Observaciones",
                (object?)r.Observaciones ?? DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }

        public int InsertarCheckIn(
    int idReserva,
    DateTime fechaHoraIngreso,
    int idUsuario,
    string observaciones)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sql = @"
        INSERT INTO CheckIns
        (
            IdReserva,
            FechaHoraIngreso,
            IdUsuarioRegistro,
            Observaciones
        )
        VALUES
        (
            @IdReserva,
            @FechaHoraIngreso,
            @IdUsuarioRegistro,
            @Observaciones
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@IdReserva", idReserva);
            cmd.Parameters.AddWithValue(
                "@FechaHoraIngreso",
                fechaHoraIngreso);
            cmd.Parameters.AddWithValue(
                "@IdUsuarioRegistro",
                idUsuario);

            cmd.Parameters.AddWithValue(
                "@Observaciones",
                (object?)observaciones ?? DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }

        public int InsertarPago(PagoBE pago)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sql = @"
        INSERT INTO Pagos
        (
            IdReserva,
            MetodoPago,
            Monto,
            FechaHora,
            Estado,
            UltimosCuatroTarjeta,
            IdUsuarioRegistro
        )
        VALUES
        (
            @IdReserva,
            @MetodoPago,
            @Monto,
            GETDATE(),
            @Estado,
            @UltimosCuatroTarjeta,
            @IdUsuarioRegistro
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@IdReserva", pago.IdReserva);
            cmd.Parameters.AddWithValue("@MetodoPago", pago.MetodoPago);
            cmd.Parameters.AddWithValue("@Monto", pago.Monto);
            cmd.Parameters.AddWithValue("@Estado", pago.Estado);

            cmd.Parameters.AddWithValue(
                "@UltimosCuatroTarjeta",
                (object?)pago.UltimosCuatroTarjeta ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "@IdUsuarioRegistro",
                (object?)pago.IdUsuarioRegistro ?? DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
        public ReservaBE ObtenerReservaPorId(int id)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                SELECT r.*, h.Nombre AS NombreHuesped, h.Apellido AS ApellidoHuesped,
                       h.DNI AS DNIHuesped, hab.Numero AS NumeroHabitacion,
                       t.Nombre AS TipoHabitacion, t.PrecioPorNoche,
                       DATEDIFF(DAY, r.FechaIngreso, r.FechaSalida) AS CantNoches
                FROM Reservas r
                INNER JOIN Huespedes h       ON r.IdHuesped    = h.IdHuesped
                INNER JOIN Habitaciones hab  ON r.IdHabitacion = hab.IdHabitacion
                INNER JOIN TiposHabitacion t ON hab.IdTipoHabitacion = t.IdTipoHabitacion
                WHERE r.IdReserva = @Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? MapearReserva(rd) : null;
        }

        public List<ReservaBE> ObtenerReservasConCheckIn()
        {
            var lista = new List<ReservaBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"
                SELECT r.*, h.Nombre AS NombreHuesped, h.Apellido AS ApellidoHuesped,
                       h.DNI AS DNIHuesped, hab.Numero AS NumeroHabitacion,
                       t.Nombre AS TipoHabitacion, t.PrecioPorNoche,
                       DATEDIFF(DAY, r.FechaIngreso, r.FechaSalida) AS CantNoches
                FROM Reservas r
                INNER JOIN Huespedes h       ON r.IdHuesped    = h.IdHuesped
                INNER JOIN Habitaciones hab  ON r.IdHabitacion = hab.IdHabitacion
                INNER JOIN TiposHabitacion t ON hab.IdTipoHabitacion = t.IdTipoHabitacion
                WHERE r.Estado = 'CheckIn'
                ORDER BY r.FechaSalida";
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(MapearReserva(rd));
            return lista;
        }

        public void ActualizarEstadoReserva(int idReserva, string nuevoEstado)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "UPDATE Reservas SET Estado=@Estado WHERE IdReserva=@Id", con);
            cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", idReserva);
            cmd.ExecuteNonQuery();
        }

        private static ReservaBE MapearReserva(SqlDataReader rd) => new ReservaBE
        {
            IdReserva = (int)rd["IdReserva"],
            IdHuesped = (int)rd["IdHuesped"],
            NombreHuesped = rd["NombreHuesped"].ToString(),
            ApellidoHuesped = rd["ApellidoHuesped"].ToString(),
            DNIHuesped = rd["DNIHuesped"].ToString(),
            IdHabitacion = (int)rd["IdHabitacion"],
            NumeroHabitacion = rd["NumeroHabitacion"].ToString(),
            TipoHabitacion = rd["TipoHabitacion"].ToString(),
            PrecioPorNoche = (decimal)rd["PrecioPorNoche"],
            FechaIngreso = (DateTime)rd["FechaIngreso"],
            FechaSalida = (DateTime)rd["FechaSalida"],
            CantNoches = (int)rd["CantNoches"],
            Estado = rd["Estado"].ToString(),
            FechaReserva = (DateTime)rd["FechaReserva"],
            Observaciones = rd["Observaciones"]?.ToString()
        };

        // ══════════════════════════════════════════════════════
        // CONSUMOS
        // ══════════════════════════════════════════════════════




        // ══════════════════════════════════════════════════════
        // CHECK-OUT y FACTURA
        // ══════════════════════════════════════════════════════
        public int InsertarCheckOut(ResumenCheckOutBE resumen, int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sql = @"
        INSERT INTO CheckOuts
        (
            IdReserva,
            FechaHoraSalida,
            MontoHabitacion,
            Impuestos,
            MontoTotal,
            IdUsuarioRegistro
        )
        VALUES
        (
            @IdReserva,
            GETDATE(),
            @MHab,
            @Imp,
            @Total,
            @IdUsuario
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@IdReserva", resumen.IdReserva);
            cmd.Parameters.AddWithValue("@MHab", resumen.MontoHabitacion);
       
            cmd.Parameters.AddWithValue("@Imp", resumen.Impuestos);
            cmd.Parameters.AddWithValue("@Total", resumen.MontoTotal);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            return (int)cmd.ExecuteScalar();
        }

        public string GenerarNroFactura()
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT ISNULL(MAX(CAST(SUBSTRING(NroFactura,3,LEN(NroFactura)) AS INT)),0)+1 FROM Facturas", con);
            int nro = (int)cmd.ExecuteScalar();
            return $"F-{nro:D5}";
        }

        public int InsertarFactura(
    int idReserva,
    string nroFactura,
    decimal monto,
    int idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sql = @"
        INSERT INTO Facturas
        (
            NroFactura,
            IdReserva,
            FechaEmision,
            MontoTotal,
            Estado,
            IdUsuarioRegistro
        )
        VALUES
        (
            @NroFactura,
            @IdReserva,
            GETDATE(),
            @MontoTotal,
            'Emitida',
            @IdUsuarioRegistro
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@NroFactura", nroFactura);
            cmd.Parameters.AddWithValue("@IdReserva", idReserva);
            cmd.Parameters.AddWithValue("@MontoTotal", monto);
            cmd.Parameters.AddWithValue("@IdUsuarioRegistro", idUsuario);

            return (int)cmd.ExecuteScalar();
        }
    }
}
    

