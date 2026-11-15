using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// CUN13 Maestro de Clientes. Los CLIENTES del hotel son los HUÉSPEDES: trabaja sobre la tabla Huespedes.
    /// Celular = columna Telefono. La baja es lógica (Activo = 0) porque los huéspedes tienen reservas e historial.
    /// </summary>
    public class ClienteDAL
    {
        private const string SELECT = "SELECT IdHuesped, DNI, Apellido, Nombre, Email, Telefono, Direccion, Activo FROM Huespedes";

        public List<ClienteBE> ObtenerClientes()
        {
            var lista = new List<ClienteBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT + " WHERE Activo = 1 ORDER BY Apellido, Nombre", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(Mapear(rd));
            return lista;
        }

        /// <summary>Busca por DNI, esté activo o dado de baja. Devuelve también si está activo.</summary>
        public (ClienteBE? cliente, bool activo) ObtenerPorDNI(string dni)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT + " WHERE DNI = @DNI ORDER BY Activo DESC, IdHuesped DESC", con);
            cmd.Parameters.AddWithValue("@DNI", dni);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return (null, false);
            return (Mapear(rd), Convert.ToBoolean(rd["Activo"]));
        }

        public ClienteBE? ObtenerPorId(int idHuesped)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT + " WHERE IdHuesped = @Id AND Activo = 1", con);
            cmd.Parameters.AddWithValue("@Id", idHuesped);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? Mapear(rd) : null;
        }

        public int Insertar(ClienteBE c)
        {
            const string sql = @"
                INSERT INTO Huespedes (Nombre, Apellido, DNI, Email, Telefono, Direccion)
                VALUES (@Nombre, @Apellido, @DNI, @Email, @Telefono, @Direccion);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            Parametros(cmd, c);
            cmd.Parameters.AddWithValue("@DNI", c.DNI);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>Modifica los datos (el DNI no se cambia). Si estaba dado de baja, lo reactiva.</summary>
        public bool Modificar(ClienteBE c, bool reactivar = false)
        {
            string sql = @"UPDATE Huespedes SET Nombre = @Nombre, Apellido = @Apellido, Email = @Email,
                                                Telefono = @Telefono, Direccion = @Direccion"
                         + (reactivar ? ", Activo = 1" : "") +
                         " WHERE IdHuesped = @Id" + (reactivar ? "" : " AND Activo = 1");
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            Parametros(cmd, c);
            cmd.Parameters.AddWithValue("@Id", c.IdHuesped);
            return cmd.ExecuteNonQuery() == 1;
        }

        public bool BajaLogica(int idHuesped)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("UPDATE Huespedes SET Activo = 0 WHERE IdHuesped = @Id AND Activo = 1", con);
            cmd.Parameters.AddWithValue("@Id", idHuesped);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static void Parametros(SqlCommand cmd, ClienteBE c)
        {
            cmd.Parameters.AddWithValue("@Nombre", c.Nombre);
            cmd.Parameters.AddWithValue("@Apellido", c.Apellido);
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(c.Email) ? DBNull.Value : c.Email);
            cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrWhiteSpace(c.Celular) ? DBNull.Value : c.Celular);
            cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(c.Direccion) ? DBNull.Value : c.Direccion);
        }

        private static ClienteBE Mapear(SqlDataReader rd) => new ClienteBE
        {
            IdHuesped = (int)rd["IdHuesped"],
            DNI = rd["DNI"]?.ToString() ?? "",
            Apellido = rd["Apellido"]?.ToString() ?? "",
            Nombre = rd["Nombre"]?.ToString() ?? "",
            Email = rd["Email"] == DBNull.Value ? "" : rd["Email"].ToString()!,
            Celular = rd["Telefono"] == DBNull.Value ? "" : rd["Telefono"].ToString()!,
            Direccion = rd["Direccion"] == DBNull.Value ? "" : rd["Direccion"].ToString()!
        };
    }
}
