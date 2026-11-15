using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    /// <summary>
    /// Maestro de Insumos (PRODUCTOS_DAL del enunciado). Alta, modificación y baja lógica.
    /// Cada INSERT / UPDATE / DELETE sobre Insumos dispara los triggers que registran
    /// la versión en Insumos_C (bitácora de cambios). El DELETE no borra: el trigger
    /// INSTEAD OF DELETE lo convierte en Activo = 0.
    /// </summary>
    public class InsumoDAL
    {
        private const string SELECT = @"SELECT IdInsumo, Nombre, Descripcion, Categoria, UnidadMedida,
                                               StockActual, StockMinimo, Activo FROM Insumos";

        public List<InsumoBE> ObtenerInsumos(bool incluirBajas)
        {
            var lista = new List<InsumoBE>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT + (incluirBajas ? "" : " WHERE Activo = 1") + " ORDER BY Nombre", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) lista.Add(Mapear(rd));
            return lista;
        }

        public InsumoBE? ObtenerPorId(int idInsumo)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(SELECT + " WHERE IdInsumo = @Id", con);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            using var rd = cmd.ExecuteReader();
            return rd.Read() ? Mapear(rd) : null;
        }

        public bool ExisteNombre(string nombre, int excluirId)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Insumos WHERE Nombre = @N AND IdInsumo <> @Id", con);
            cmd.Parameters.AddWithValue("@N", nombre);
            cmd.Parameters.AddWithValue("@Id", excluirId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public int Insertar(InsumoBE i)
        {
            const string sql = @"
                INSERT INTO Insumos (Nombre, Descripcion, Categoria, UnidadMedida, StockActual, StockMinimo, Activo)
                VALUES (@Nombre, @Descripcion, @Categoria, @Unidad, @Stock, @Minimo, 1);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            Parametros(cmd, i);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool Modificar(InsumoBE i)
        {
            const string sql = @"UPDATE Insumos SET Nombre = @Nombre, Descripcion = @Descripcion, Categoria = @Categoria,
                                        UnidadMedida = @Unidad, StockActual = @Stock, StockMinimo = @Minimo
                                 WHERE IdInsumo = @Id AND Activo = 1";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            Parametros(cmd, i);
            cmd.Parameters.AddWithValue("@Id", i.IdInsumo);
            return cmd.ExecuteNonQuery() == 1;
        }

        /// <summary>Baja: se ejecuta un DELETE y el trigger TR_Insumos_BorradoLogico lo convierte en Activo = 0.</summary>
        public bool Eliminar(int idInsumo)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("DELETE FROM Insumos WHERE IdInsumo = @Id AND Activo = 1", con);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            cmd.ExecuteNonQuery();   // con INSTEAD OF el conteo no es confiable: se verifica el estado
            using var chk = new SqlCommand("SELECT Activo FROM Insumos WHERE IdInsumo = @Id", con);
            chk.Parameters.AddWithValue("@Id", idInsumo);
            return chk.ExecuteScalar() is bool activo && !activo;
        }

        public bool Reactivar(int idInsumo)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("UPDATE Insumos SET Activo = 1 WHERE IdInsumo = @Id AND Activo = 0", con);
            cmd.Parameters.AddWithValue("@Id", idInsumo);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static void Parametros(SqlCommand cmd, InsumoBE i)
        {
            cmd.Parameters.AddWithValue("@Nombre", i.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(i.Descripcion) ? DBNull.Value : i.Descripcion);
            cmd.Parameters.AddWithValue("@Categoria", string.IsNullOrWhiteSpace(i.Categoria) ? "General" : i.Categoria);
            cmd.Parameters.AddWithValue("@Unidad", i.UnidadMedida);
            cmd.Parameters.AddWithValue("@Stock", i.StockActual);
            cmd.Parameters.AddWithValue("@Minimo", i.StockMinimo);
        }

        private static InsumoBE Mapear(SqlDataReader rd) => new InsumoBE
        {
            IdInsumo = (int)rd["IdInsumo"],
            Nombre = rd["Nombre"].ToString()!,
            Descripcion = rd["Descripcion"] == DBNull.Value ? "" : rd["Descripcion"].ToString()!,
            Categoria = rd["Categoria"] == DBNull.Value ? "" : rd["Categoria"].ToString()!,
            UnidadMedida = rd["UnidadMedida"] == DBNull.Value ? "" : rd["UnidadMedida"].ToString()!,
            StockActual = Convert.ToDecimal(rd["StockActual"]),
            StockMinimo = Convert.ToDecimal(rd["StockMinimo"]),
            Activo = Convert.ToBoolean(rd["Activo"])
        };
    }
}
