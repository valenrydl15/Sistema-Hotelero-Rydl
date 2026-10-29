using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    public class Bitacora
    {
        public void Registrar(int? idUsuario, int? idModulo, int? idTipoEvento,
                              int criticidad, string detalle, string resultado)
        {
            const string sql = @"INSERT INTO Bitacora
                (IdUsuario,Fecha,Hora,IdModulo,IdTipoEvento,Detalle,Criticidad,Resultado)
                VALUES
                (@IdUsuario,CAST(GETDATE() AS DATE),CAST(GETDATE() AS TIME(0)),
                 @IdModulo,@IdTipoEvento,@Detalle,@Criticidad,@Resultado)";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@IdUsuario", (object)idUsuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdModulo", (object)idModulo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdTipoEvento", (object)idTipoEvento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Detalle", (object)detalle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Criticidad", criticidad);
            cmd.Parameters.AddWithValue("@Resultado", resultado);
            cmd.ExecuteNonQuery();
        }

        public List<RegistroBitacora> ObtenerFiltrado(
            int? idUsuario = null, DateTime? fechaInicio = null, DateTime? fechaFin = null,
            int? idModulo = null, int? idTipoEvento = null, int? criticidad = null)
        {
            var lista = new List<RegistroBitacora>();

            if (idUsuario == null && fechaInicio == null && fechaFin == null
                && idModulo == null && idTipoEvento == null && criticidad == null)
                fechaInicio = DateTime.Today.AddDays(-3);

            string sql = @"SELECT b.*, u.NombreUsuario AS Login, u.Nombre, u.Apellido,
                                  m.Nombre AS NombreModulo, te.Nombre AS NombreEvento
                           FROM Bitacora b
                           LEFT JOIN Usuarios    u  ON b.IdUsuario    = u.IdUsuario
                           LEFT JOIN Modulos     m  ON b.IdModulo     = m.IdModulo
                           LEFT JOIN TiposEvento te ON b.IdTipoEvento = te.IdTipoEvento
                           WHERE 1=1";

            if (idUsuario != null) sql += " AND b.IdUsuario    = @IdUsuario";
            if (fechaInicio != null) sql += " AND b.Fecha       >= @FechaInicio";
            if (fechaFin != null) sql += " AND b.Fecha       <= @FechaFin";
            if (idModulo != null) sql += " AND b.IdModulo     = @IdModulo";
            if (idTipoEvento != null) sql += " AND b.IdTipoEvento = @IdTipoEvento";
            if (criticidad != null) sql += " AND b.Criticidad   = @Criticidad";
            sql += " ORDER BY b.Fecha DESC, b.Hora DESC";

            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            if (idUsuario != null) cmd.Parameters.AddWithValue("@IdUsuario", idUsuario.Value);
            if (fechaInicio != null) cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
            if (fechaFin != null) cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value);
            if (idModulo != null) cmd.Parameters.AddWithValue("@IdModulo", idModulo.Value);
            if (idTipoEvento != null) cmd.Parameters.AddWithValue("@IdTipoEvento", idTipoEvento.Value);
            if (criticidad != null) cmd.Parameters.AddWithValue("@Criticidad", criticidad.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new RegistroBitacora
                {
                    IdBitacora = (int)r["IdBitacora"],
                    IdUsuario = r["IdUsuario"] == DBNull.Value ? (int?)null : (int)r["IdUsuario"],
                    Login = r["Login"]?.ToString(),
                    Nombre = r["Nombre"]?.ToString(),
                    Apellido = r["Apellido"]?.ToString(),
                    Fecha = (DateTime)(object)r["Fecha"],
                    Hora = (TimeSpan)(object)r["Hora"],
                    NombreModulo = r["NombreModulo"]?.ToString(),
                    NombreEvento = r["NombreEvento"]?.ToString(),
                    Detalle = r["Detalle"]?.ToString(),
                    Criticidad = (int)r["Criticidad"],
                    Resultado = r["Resultado"]?.ToString()
                });
            return lista;
        }

        public List<Modulo> ObtenerModulos()
        {
            var lista = new List<Modulo>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT * FROM Modulos ORDER BY Nombre", con);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new Modulo { IdModulo = (int)r["IdModulo"], Nombre = r["Nombre"].ToString() });
            return lista;
        }

        public List<TipoEvento> ObtenerTiposEvento(int? idModulo = null)
        {
            var lista = new List<TipoEvento>();
            string sql = "SELECT te.*, m.Nombre AS NombreModulo FROM TiposEvento te INNER JOIN Modulos m ON te.IdModulo=m.IdModulo";
            if (idModulo != null) sql += " WHERE te.IdModulo=@IdModulo";
            sql += " ORDER BY te.Nombre";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            if (idModulo != null) cmd.Parameters.AddWithValue("@IdModulo", idModulo.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new TipoEvento
                {
                    IdTipoEvento = (int)r["IdTipoEvento"],
                    IdModulo = (int)r["IdModulo"],
                    NombreModulo = r["NombreModulo"].ToString(),
                    Nombre = r["Nombre"].ToString(),
                    Criticidad = (int)r["Criticidad"]
                });
            return lista;
        }
    }
}

