using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace DAL_HRS_480
{
    /// <summary>
    /// Conexión portable a SQL Server. Orden de resolución:
    ///   1) Archivo "conexion.txt" junto al .exe (opcional). Puede contener:
    ///        - solo el nombre del servidor:   PC-LAB\SQL2019
    ///        - o una cadena de conexión completa (por ejemplo, con usuario y clave de SQL).
    ///   2) Detección automática: busca las instancias instaladas en el equipo
    ///      y elige la que ya tiene la base HotelRydlDB (si ninguna la tiene,
    ///      la primera a la que se pueda conectar).
    /// El resultado se cachea mientras la aplicación está abierta.
    /// </summary>
    public static class ConexionDB
    {
        public const string NombreBD = "HotelRydlDB";

        private static readonly object _lock = new object();
        private static SqlConnectionStringBuilder _base;

        /// <summary>Servidor/instancia en uso (útil para mostrarlo en pantalla o en el log).</summary>
        public static string Servidor => Resolver().DataSource;

        /// <summary>Conexión a la base del sistema (misma firma que antes).</summary>
        public static SqlConnection ObtenerConexion()
            => new SqlConnection(Construir(NombreBD));

        /// <summary>Conexión a master, para BACKUP / RESTORE.</summary>
        public static SqlConnection ObtenerConexionMaster()
            => new SqlConnection(Construir("master"));

        // ──────────────────────────────────────────────────────
        private static string Construir(string baseDeDatos)
        {
            var b = new SqlConnectionStringBuilder(Resolver().ConnectionString)
            {
                InitialCatalog = baseDeDatos
            };
            return b.ConnectionString;
        }

        private static SqlConnectionStringBuilder Plantilla(string servidor)
            => new SqlConnectionStringBuilder
            {
                DataSource = servidor,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

        private static SqlConnectionStringBuilder Resolver()
        {
            lock (_lock)
            {
                if (_base != null) return _base;

                // 1) Archivo de configuración opcional
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conexion.txt");
                if (File.Exists(ruta))
                {
                    string txt = File.ReadAllText(ruta).Trim();
                    if (txt.Length > 0)
                    {
                        _base = txt.Contains('=')
                            ? new SqlConnectionStringBuilder(txt)
                            : Plantilla(txt);
                        return _base;
                    }
                }

                // 2) Detección automática
                _base = Plantilla(Detectar());
                return _base;
            }
        }

        private static string Detectar()
        {
            string primeraQueConecta = null;

            foreach (var candidato in Candidatos())
            {
                try
                {
                    var b = Plantilla(candidato);
                    b.InitialCatalog = "master";
                    b.ConnectTimeout = 3;   // para no demorar al probar instancias inexistentes

                    using var con = new SqlConnection(b.ConnectionString);
                    con.Open();

                    primeraQueConecta ??= candidato;

                    using var cmd = new SqlCommand("SELECT DB_ID(@n)", con);
                    cmd.Parameters.AddWithValue("@n", NombreBD);
                    if (cmd.ExecuteScalar() is int)      // la base existe en esta instancia
                        return candidato;
                }
                catch
                {
                    // instancia inexistente o sin acceso: se prueba la siguiente
                }
            }

            if (primeraQueConecta != null) return primeraQueConecta;

            throw new InvalidOperationException(
                "No se encontró ninguna instancia de SQL Server accesible en este equipo.\n\n" +
                "Verificá que el servicio SQL Server esté iniciado o creá un archivo " +
                "'conexion.txt' junto al ejecutable con el nombre del servidor " +
                @"(por ejemplo: .\SQLEXPRESS o NOMBREPC\INSTANCIA).");
        }

        private static IEnumerable<string> Candidatos()
        {
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lista = new List<string>();
            void Agregar(string s) { if (vistos.Add(s)) lista.Add(s); }

            // Instancias instaladas, leídas del registro de Windows
            try
            {
                using var k = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (k != null)
                    foreach (var nombre in k.GetValueNames())
                        Agregar(nombre.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                                ? "." : @".\" + nombre);
            }
            catch { }

            // Casos comunes por si el registro no alcanza
            Agregar(@".\SQLEXPRESS");
            Agregar(".");
            Agregar(@"(localdb)\MSSQLLocalDB");
            Agregar("localhost");

            return lista;
        }
    }
}
