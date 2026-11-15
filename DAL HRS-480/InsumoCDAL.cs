using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// CUN14 Bitácora de cambios de Insumos (PRODUCTOS_C_DAL del enunciado): lee Insumos_C y activa versiones.
    /// Los registros los escriben los TRIGGERS de Insumos; esta clase nunca inserta ni borra.
    /// </summary>
    public class InsumoCDAL
    {
        public List<InsumoCBE> ObtenerCambios(FiltroCambiosBE f)
        {
            string sql = @"SELECT c.IdCambio, c.IdInsumo, c.Fecha, c.Hora, c.Nombre, c.Descripcion, c.Categoria, c.UnidadMedida,
                                  c.StockActual, c.StockMinimo, c.Activo, c.Act, c.Operacion, u.NombreUsuario
                           FROM Insumos_C c
                           LEFT JOIN Usuarios u ON u.IdUsuario = c.IdUsuario
                           WHERE 1 = 1";
            if (f.IdInsumo != null) sql += " AND c.IdInsumo = @IdInsumo";
            if (!string.IsNullOrWhiteSpace(f.Nombre)) sql += " AND c.Nombre = @Nombre";
            if (f.FechaInicio != null) sql += " AND c.Fecha >= @Desde";
            if (f.FechaFin != null) sql += " AND c.Fecha <= @Hasta";
            sql += " ORDER BY c.IdInsumo, c.Fecha, c.Hora, c.IdCambio";

            var lista = new List<InsumoCBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            if (f.IdInsumo != null) cmd.Parameters.AddWithValue("@IdInsumo", f.IdInsumo.Value);
            if (!string.IsNullOrWhiteSpace(f.Nombre)) cmd.Parameters.AddWithValue("@Nombre", f.Nombre);
            if (f.FechaInicio != null) cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = f.FechaInicio.Value.Date;
            if (f.FechaFin != null) cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = f.FechaFin.Value.Date;
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new InsumoCBE
                {
                    IdCambio = (int)rd["IdCambio"],
                    IdInsumo = (int)rd["IdInsumo"],
                    Fecha = (DateTime)rd["Fecha"],
                    Hora = (TimeSpan)rd["Hora"],
                    Nombre = rd["Nombre"].ToString()!,
                    Descripcion = rd["Descripcion"] == DBNull.Value ? "" : rd["Descripcion"].ToString()!,
                    Categoria = rd["Categoria"] == DBNull.Value ? "" : rd["Categoria"].ToString()!,
                    UnidadMedida = rd["UnidadMedida"] == DBNull.Value ? "" : rd["UnidadMedida"].ToString()!,
                    StockActual = Convert.ToDecimal(rd["StockActual"]),
                    StockMinimo = Convert.ToDecimal(rd["StockMinimo"]),
                    Activo = Convert.ToBoolean(rd["Activo"]),
                    Act = Convert.ToBoolean(rd["Act"]),
                    Operacion = rd["Operacion"].ToString()!,
                    Usuario = rd["NombreUsuario"] == DBNull.Value ? "" : rd["NombreUsuario"].ToString()!
                });
            return lista;
        }

        /// <summary>Nombres distintos que tuvo cada insumo en el historial (para el filtro por Nombre).</summary>
        public List<string> ObtenerNombres()
        {
            var lista = new List<string>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT DISTINCT Nombre FROM Insumos_C ORDER BY Nombre", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(rd.GetString(0));
            return lista;
        }

        public InsumoCBE? ObtenerCambio(int idCambio)
        {
            var l = ObtenerCambiosPorId(idCambio);
            return l.Count == 0 ? null : l[0];
        }

        private List<InsumoCBE> ObtenerCambiosPorId(int idCambio)
        {
            var todos = new List<InsumoCBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT IdInsumo FROM Insumos_C WHERE IdCambio = @Id", con);
            cmd.Parameters.AddWithValue("@Id", idCambio);
            if (cmd.ExecuteScalar() is not int idInsumo) return todos;
            return ObtenerCambios(new FiltroCambiosBE { IdInsumo = idInsumo }).FindAll(c => c.IdCambio == idCambio);
        }

        /// <summary>
        /// Activa una versión: sp_ActivarVersionInsumo devuelve el insumo a esos datos
        /// y deja Act = 1 solo en esa versión del historial.
        /// </summary>
        public void ActivarVersion(int idCambio, int? idUsuario)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("dbo.sp_ActivarVersionInsumo", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdCambio", idCambio);
            cmd.Parameters.AddWithValue("@IdUsuario", (object?)idUsuario ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}
