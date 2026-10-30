using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using DAL_HRS_480;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Deja lista la base para el proceso PN2 (Reposición de insumos): tablas (incluye cotizaciones,
    /// pre-órdenes, pagos y facturas de proveedor), restricciones,
    /// módulo y eventos de bitácora, roles/permisos, filas del Dígito Verificador y datos de ejemplo.
    /// Ejecuta el script embebido Scripts\02_SQL_PN2_Reposicion.sql, que es idempotente.
    /// Se llama al iniciar la aplicación y después de restaurar un backup viejo.
    /// </summary>
    public static class ReposicionSetup
    {
        private const string RECURSO = "PN2_Reposicion.sql";

        /// <summary>Devuelve null si todo quedó bien, o el detalle del error.</summary>
        public static string? Asegurar()
        {
            try
            {
                if (YaEstaListo()) return null;

                string script = LeerScript();
                var lotes = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                var errores = new List<string>();
                using var con = ConexionDB.ObtenerConexion();
                con.Open();
                foreach (var lote in lotes)
                {
                    string t = lote.Trim();
                    if (t.Length == 0) continue;
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

        private static string LeerScript()
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(RECURSO)
                ?? throw new InvalidOperationException("Falta el script PN2 embebido (" + RECURSO + ").");
            using var r = new StreamReader(s, System.Text.Encoding.UTF8);
            return r.ReadToEnd();
        }

        /// <summary>Chequeo rápido para no re-ejecutar el script en cada inicio.</summary>
        private static bool YaEstaListo()
        {
            const string sql = @"
SELECT CASE WHEN
    OBJECT_ID(N'dbo.SolicitudesCotizacion') IS NOT NULL
AND OBJECT_ID(N'dbo.SolicitudesCotizacionDetalle') IS NOT NULL
AND OBJECT_ID(N'dbo.Cotizaciones') IS NOT NULL
AND COL_LENGTH(N'dbo.Cotizaciones', N'IdSolicitudCotizacion') IS NOT NULL
AND OBJECT_ID(N'dbo.OrdenesCompraDetalle') IS NOT NULL
AND OBJECT_ID(N'dbo.PagosOrdenCompra') IS NOT NULL
AND OBJECT_ID(N'dbo.RecepcionesInsumosDetalle') IS NOT NULL
AND OBJECT_ID(N'dbo.MovimientosStock') IS NOT NULL
AND OBJECT_ID(N'dbo.Notificaciones') IS NOT NULL
AND COL_LENGTH(N'dbo.Proveedores', N'Estado') IS NOT NULL
AND COL_LENGTH(N'dbo.Insumos', N'Categoria') IS NOT NULL
AND EXISTS (SELECT 1 FROM dbo.Modulos WHERE Nombre = N'Reposición de insumos')
AND EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE Nombre = N'Pago de orden de compra aprobado')
AND EXISTS (SELECT 1 FROM dbo.TiposEvento WHERE Nombre = N'Reporte de factura exportado')
AND EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Gerente de Compras')
AND EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Depósito')
AND EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Encargado de Compras')
AND (SELECT COUNT(*) FROM dbo.DigitoVerificador WHERE NombreTabla IN
     (N'Insumos', N'Proveedores', N'SolicitudesCotizacion', N'SolicitudesCotizacionDetalle', N'Cotizaciones', N'CotizacionesDetalle',
      N'OrdenesCompra', N'OrdenesCompraDetalle', N'PagosOrdenCompra', N'RecepcionesInsumos', N'RecepcionesInsumosDetalle', N'MovimientosStock')) = 12
THEN 1 ELSE 0 END";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }
    }
}
