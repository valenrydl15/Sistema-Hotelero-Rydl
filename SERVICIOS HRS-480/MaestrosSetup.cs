using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using DAL_HRS_480;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Deja lista la base para el menú MAESTROS (entrega 3): columna Direccion de Huéspedes,
    /// tabla Insumos_C con sus triggers, sp_ActivarVersionInsumo y los eventos de bitácora
    /// (Maestros y Dígito Verificador). Ejecuta el script embebido
    /// Scripts\05_SQL_Entrega3_Maestros.sql, que es idempotente.
    /// Se llama al iniciar la aplicación y después de restaurar un backup.
    /// </summary>
    public static class MaestrosSetup
    {
        private const string RECURSO = "Entrega3_Maestros.sql";

        /// <summary>Devuelve null si todo quedó bien, o el detalle del error.</summary>
        public static string? Asegurar()
        {
            try
            {
                if (YaEstaListo()) return null;

                string script;
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(RECURSO)
                       ?? throw new InvalidOperationException("Falta el script embebido " + RECURSO + "."))
                using (var r = new StreamReader(s, System.Text.Encoding.UTF8))
                    script = r.ReadToEnd();

                var errores = new List<string>();
                using var con = ConexionDB.ObtenerConexion();
                con.Open();
                foreach (var lote in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    string t = lote.Trim();
                    if (t.Length == 0 || t.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        using var cmd = new SqlCommand(t, con) { CommandTimeout = 120 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex) { errores.Add(ex.Message); }
                }
                return errores.Count == 0 ? null : string.Join(Environment.NewLine, errores);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static bool YaEstaListo()
        {
            const string sql = @"
SELECT CASE WHEN
    COL_LENGTH(N'dbo.Huespedes', N'Direccion') IS NOT NULL
AND OBJECT_ID(N'dbo.Insumos_C') IS NOT NULL
AND OBJECT_ID(N'dbo.TR_Insumos_RegistrarCambio', N'TR') IS NOT NULL
AND OBJECT_ID(N'dbo.TR_Insumos_BorradoLogico', N'TR') IS NOT NULL
AND OBJECT_ID(N'dbo.TR_Insumos_C_SinBorrado', N'TR') IS NOT NULL
AND OBJECT_ID(N'dbo.TR_Insumos_C_SoloAct', N'TR') IS NOT NULL
AND OBJECT_ID(N'dbo.sp_ActivarVersionInsumo', N'P') IS NOT NULL
AND EXISTS (SELECT 1 FROM dbo.TiposEvento t JOIN dbo.Modulos m ON m.IdModulo = t.IdModulo
            WHERE m.Nombre = N'Maestros' AND t.Nombre = N'Versión de insumo activada')
AND EXISTS (SELECT 1 FROM dbo.TiposEvento t JOIN dbo.Modulos m ON m.IdModulo = t.IdModulo
            WHERE m.Nombre = N'Dígito Verificador' AND t.Nombre = N'Reparación de DV cancelada')
AND EXISTS (SELECT 1 FROM dbo.TiposEvento t JOIN dbo.Modulos m ON m.IdModulo = t.IdModulo
            WHERE m.Nombre = N'Reportes' AND t.Nombre = N'Reporte predictivo generado')
THEN 1 ELSE 0 END";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }
    }
}
