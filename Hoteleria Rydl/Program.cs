using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;
using DAL_HRS_480;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                InicializarDB();
                ResetearSesionesYSincronizarDV();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar la base de datos:\n{ex.Message}",
                    "Error crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FrmLogin());
        }

        private static void ResetearSesionesYSincronizarDV()
        {
            // Sesiones que quedaron activas por un cierre inesperado
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                const string sqlReset = "UPDATE Usuarios SET SesionActiva = 0 WHERE SesionActiva = 1";
                using var cmdReset = new SqlCommand(sqlReset, con);
                cmdReset.ExecuteNonQuery();
                // SesionActiva NO forma parte del DV de Usuarios, así que este reseteo no lo afecta
                // (antes se blanqueaba el DV acá, lo que dejaba la tabla sin control).
            }
        }

        // ══════════════════════════════════════════════════════
        // Inicialización de la base en CUALQUIER instancia de SQL Server
        // (la instancia la resuelve ConexionDB: conexion.txt o autodetección)
        // ══════════════════════════════════════════════════════
        private static void InicializarDB()
        {
            using (var con = ConexionDB.ObtenerConexionMaster())
            {
                con.Open();

                using (var cmd = new SqlCommand("SELECT DB_ID(@n)", con))
                {
                    cmd.Parameters.AddWithValue("@n", ConexionDB.NombreBD);
                    object id = cmd.ExecuteScalar();
                    if (id == null || id is DBNull)
                        CrearBaseDeDatos(con);
                }
            }

            AsegurarEventosYDV();
            AsegurarReposicion();
            AsegurarHashAdmin();

            // Una sola vez por base: pasa el DV al formato independiente de la configuración regional
            try { new DigitoVerificadorServicio().MigrarFormatoSiCorresponde(); }
            catch { /* si falla, el DV se detecta/repara desde el login */ }
        }

        private static void CrearBaseDeDatos(SqlConnection con)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Opción 1: script HotelRydl_DB.sql junto al ejecutable
            string script = Path.Combine(baseDir, "HotelRydl_DB.sql");
            if (File.Exists(script))
            {
                EjecutarScript(con, File.ReadAllText(script));
                return;
            }

            // Opción 2: último backup .bak de la carpeta Backups (del proyecto o del .exe)
            string bak = BuscarUltimoBackup(baseDir);
            if (bak == null)
                throw new FileNotFoundException(
                    "La base 'HotelRydlDB' no existe en esta instancia y no se encontró " +
                    "ni HotelRydl_DB.sql ni un backup (.bak) en la carpeta Backups.");

            RestaurarDesdeBackup(con, bak);
        }

        private static string BuscarUltimoBackup(string baseDir)
        {
            var carpetas = new List<string>();
            var d = new DirectoryInfo(baseDir);
            for (int i = 0; i < 7 && d != null; i++, d = d.Parent)
                carpetas.Add(Path.Combine(d.FullName, "Backups"));

            return carpetas.Where(Directory.Exists)
                           .SelectMany(c => Directory.GetFiles(c, "HotelRydl*.bak"))
                           .OrderByDescending(f => File.GetLastWriteTime(f))
                           .FirstOrDefault();
        }

        private static void RestaurarDesdeBackup(SqlConnection con, string bak)
        {
            // El servicio de SQL Server es quien lee el .bak: si no tiene acceso a la carpeta
            // del proyecto, se copia a la carpeta de backups de la instancia.
            string rutaParaSql = bak;
            try
            {
                EjecutarRestore(con, rutaParaSql);
                return;
            }
            catch (SqlException)
            {
                string carpetaInst = null;
                using (var cmd = new SqlCommand(
                    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(500))", con))
                {
                    var r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value) carpetaInst = r.ToString();
                }
                if (string.IsNullOrWhiteSpace(carpetaInst)) throw;

                rutaParaSql = Path.Combine(carpetaInst, Path.GetFileName(bak));
                File.Copy(bak, rutaParaSql, true);
                EjecutarRestore(con, rutaParaSql);
            }
        }

        private static void EjecutarRestore(SqlConnection con, string rutaBak)
        {
            // Rutas de datos/log de la instancia actual
            string dataPath = ValorServidor(con, "InstanceDefaultDataPath");
            string logPath = ValorServidor(con, "InstanceDefaultLogPath");
            if (string.IsNullOrWhiteSpace(dataPath))
                dataPath = Path.GetDirectoryName(ValorServidorSql(con,
                    "SELECT TOP 1 physical_name FROM master.sys.master_files WHERE database_id = 1 AND type = 0")) + "\\";
            if (string.IsNullOrWhiteSpace(logPath)) logPath = dataPath;

            var movs = new List<string>();
            using (var cmd = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = N'{rutaBak.Replace("'", "''")}'", con))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    string logico = rd.GetString(0);
                    string tipo = rd.GetString(2);           // D = datos, L = log
                    string carpeta = tipo == "L" ? logPath : dataPath;
                    string ext = tipo == "L" ? ".ldf" : ".mdf";
                    string destino = Path.Combine(carpeta, ConexionDB.NombreBD + "_" + logico + ext);
                    movs.Add($"MOVE N'{logico.Replace("'", "''")}' TO N'{destino.Replace("'", "''")}'");
                }
            }

            string sql = $"RESTORE DATABASE [{ConexionDB.NombreBD}] FROM DISK = N'{rutaBak.Replace("'", "''")}' " +
                         $"WITH {string.Join(", ", movs)}, REPLACE, RECOVERY";
            using (var cmd = new SqlCommand(sql, con) { CommandTimeout = 600 })
                cmd.ExecuteNonQuery();
        }

        private static string ValorServidor(SqlConnection con, string propiedad)
            => ValorServidorSql(con, $"SELECT CAST(SERVERPROPERTY('{propiedad}') AS NVARCHAR(500))");

        private static string ValorServidorSql(SqlConnection con, string sql)
        {
            using var cmd = new SqlCommand(sql, con);
            var r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? null : r.ToString();
        }

        private static void EjecutarScript(SqlConnection con, string script)
        {
            var bloques = script.Split(new[] { "\r\nGO", "\nGO", " GO\r", " GO\n" },
                                       StringSplitOptions.RemoveEmptyEntries);
            foreach (var bloque in bloques)
            {
                string t = bloque.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                try
                {
                    using var c = new SqlCommand(t, con);
                    c.ExecuteNonQuery();
                }
                catch { }
            }
        }

        // ══════════════════════════════════════════════════════
        // Eventos de bitácora del módulo 2 (Recepción) y tablas nuevas del DV.
        // Es idempotente: si ya existen, no hace nada. Equivale a 01_SQL_Bitacora_y_DV.sql
        // ══════════════════════════════════════════════════════
        private static void AsegurarEventosYDV()
        {
            try
            {
                using var con = ConexionDB.ObtenerConexion();
                con.Open();

                const string sql = @"
IF EXISTS (SELECT 1 FROM Modulos WHERE IdModulo = 2)
BEGIN
    DECLARE @ev TABLE (Nombre NVARCHAR(100), Criticidad INT);
    INSERT INTO @ev VALUES
     (N'Consulta de disponibilidad', 5), (N'Alta de huésped', 4), (N'Reserva creada', 4),
     (N'Pago autorizado', 3), (N'Pago rechazado', 2), (N'Reserva cancelada', 3),
     (N'Factura emitida', 4), (N'Check-in realizado', 3), (N'Check-in rechazado', 3),
     (N'Check-out realizado', 3), (N'Check-out rechazado', 3), (N'Error técnico check-in/out', 1);

    INSERT INTO TiposEvento (IdModulo, Nombre, Criticidad)
    SELECT 2, e.Nombre, e.Criticidad FROM @ev e
    WHERE NOT EXISTS (SELECT 1 FROM TiposEvento t WHERE t.IdModulo = 2 AND t.Nombre = e.Nombre);
END;

INSERT INTO DigitoVerificador (NombreTabla, DVH, DVV, FechaCalculo, Valido)
SELECT v.t, '0', '0', GETDATE(), 1
FROM (VALUES ('Huespedes'),('Habitaciones'),('Reservas'),
             ('CheckIns'),('CheckOuts'),('Pagos'),('Facturas')) v(t)
WHERE v.t NOT IN (SELECT NombreTabla FROM DigitoVerificador);";

                using var cmd = new SqlCommand(sql, con);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Si el esquema es distinto, no se bloquea el arranque: usar el script SQL manual
            }
        }

        // PN2 (Reposición de insumos): tablas, roles, eventos de bitácora y DV. Idempotente.
        private static void AsegurarReposicion()
        {
            string? error = ReposicionSetup.Asegurar();
            if (error != null)
                MessageBox.Show(
                    "No se pudo preparar la base para el módulo de Reposición de insumos.\n\n" + error +
                    "\n\nEjecutá a mano el script Scripts\\02_SQL_PN2_Reposicion.sql en SQL Server. " +
                    "El resto del sistema funciona con normalidad.",
                    "Reposición de insumos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void AsegurarHashAdmin()
        {
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                const string sql = "SELECT IdUsuario, DNI, Apellido, ContrasenaHash FROM Usuarios WHERE IdRol=1";
                int id = 0;
                string hash = "";
                string dni = "", apellido = "";

                using (var cmd = new SqlCommand(sql, con))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        if (r["ContrasenaHash"].ToString() == "##PENDING_HASH##")
                        {
                            id = (int)r["IdUsuario"];
                            dni = r["DNI"].ToString();
                            apellido = r["Apellido"].ToString();
                            hash = Encriptacion.Encriptar($"{dni}{apellido}");
                        }
                    }
                }

                if (id > 0)
                {
                    using (var cmd = new SqlCommand(
                        "UPDATE Usuarios SET ContrasenaHash=@H WHERE IdUsuario=@Id", con))
                    {
                        cmd.Parameters.AddWithValue("@H", hash);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
