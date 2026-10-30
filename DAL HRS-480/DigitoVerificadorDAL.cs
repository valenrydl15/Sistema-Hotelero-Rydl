using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace DAL_HRS_480
{
    public class DigitoVerificadorDAL
    {
        // Tablas de relación (clave compuesta): la 1ª columna se repite, hay que ordenar por 2 columnas
        private static readonly HashSet<string> TablasRelacion = new HashSet<string>
        { "RolFamilias", "RolPermisos", "FamiliaPermisos", "FamiliaFamilias" };

        public DataTable ObtenerDatosTabla(string nombreTabla)
        {
            var tablasPermitidas = new HashSet<string>
{
    "Usuarios","Roles","Familias","Permisos",
    "RolFamilias","RolPermisos","FamiliaPermisos","FamiliaFamilias",
    "Huespedes","Habitaciones","Reservas",
    "CheckIns","CheckOuts","Pagos","Facturas",
    "Insumos","Proveedores","SolicitudesCotizacion","SolicitudesCotizacionDetalle","Cotizaciones","CotizacionesDetalle",
    "OrdenesCompra","OrdenesCompraDetalle","PagosOrdenCompra","RecepcionesInsumos","RecepcionesInsumosDetalle","MovimientosStock"
};

            if (!tablasPermitidas.Contains(nombreTabla))
                throw new ArgumentException($"Tabla '{nombreTabla}' no permitida.");

            var dt = new DataTable();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            // ORDER BY 1 garantiza orden consistente para el cálculo
            string sql = nombreTabla == "Usuarios"
           ? @"SELECT IdUsuario, Nombre, Apellido, DNI, Email,
                   NombreUsuario, ContrasenaHash, IdRol,
                   Activo, Bloqueado, IntentosFallidos,
                   MaxIntentos, DebeChangiarClave, FechaCreacion
            FROM Usuarios ORDER BY 1"
           : $"SELECT * FROM {nombreTabla} ORDER BY {(TablasRelacion.Contains(nombreTabla) ? "1,2" : "1")}";

            using var cmd = new SqlCommand(sql, con);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        // ══════════════════════════════════════════════════════
        // GUARDAR DV ya calculado (recibe los valores de la BLL)
        // ══════════════════════════════════════════════════════
        public void GuardarDV(string nombreTabla, string dvh, string dvv)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            const string sqlUpdate = @"UPDATE DigitoVerificador
        SET DVH=@DVH, DVV=@DVV, FechaCalculo=GETDATE(), Valido=1
        WHERE NombreTabla=@Tabla";

            using (var cmd = new SqlCommand(sqlUpdate, con))
            {
                cmd.Parameters.AddWithValue("@DVH", dvh);
                cmd.Parameters.AddWithValue("@DVV", dvv);
                cmd.Parameters.AddWithValue("@Tabla", nombreTabla);
                if (cmd.ExecuteNonQuery() > 0) return;
            }

            // La tabla no estaba registrada: se inserta
            const string sqlInsert = @"INSERT INTO DigitoVerificador
        (NombreTabla, DVH, DVV, FechaCalculo, Valido)
        VALUES (@Tabla, @DVH, @DVV, GETDATE(), 1)";

            using var ins = new SqlCommand(sqlInsert, con);
            ins.Parameters.AddWithValue("@Tabla", nombreTabla);
            ins.Parameters.AddWithValue("@DVH", dvh);
            ins.Parameters.AddWithValue("@DVV", dvv);
            ins.ExecuteNonQuery();
        }

        // ══════════════════════════════════════════════════════
        // OBTENER DV almacenado
        // ══════════════════════════════════════════════════════
        public TablaDV ObtenerDVAlmacenado(string nombreTabla)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = "SELECT * FROM DigitoVerificador WHERE NombreTabla=@Tabla";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Tabla", nombreTabla);
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
                return new TablaDV
                {
                    IdDV = (int)rd["IdDV"],
                    NombreTabla = rd["NombreTabla"].ToString(),
                    DVH = rd["DVH"].ToString(),
                    DVV = rd["DVV"].ToString(),
                    FechaCalculo = (DateTime)rd["FechaCalculo"],
                    Valido = (bool)rd["Valido"]
                };
            return null;
        }

        public List<TablaDV> ObtenerTodosDV()
        {
            var lista = new List<TablaDV>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand("SELECT * FROM DigitoVerificador", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new TablaDV
                {
                    IdDV = (int)rd["IdDV"],
                    NombreTabla = rd["NombreTabla"].ToString(),
                    DVH = rd["DVH"].ToString(),
                    DVV = rd["DVV"].ToString(),
                    FechaCalculo = (DateTime)rd["FechaCalculo"],
                    Valido = (bool)rd["Valido"]
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // INCONSISTENCIAS
        // ══════════════════════════════════════════════════════
        public void RegistrarInconsistencia(string tabla,
            string dvhEsperado, string dvhCalculado,
            string dvvEsperado, string dvvCalculado)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"INSERT INTO InconsistenciasDV
                (NombreTabla,DVH_Esperado,DVH_Calculado,DVV_Esperado,DVV_Calculado)
                VALUES (@Tabla,@DVHe,@DVHc,@DVVe,@DVVc)";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Tabla", tabla);
            cmd.Parameters.AddWithValue("@DVHe", dvhEsperado ?? "");
            cmd.Parameters.AddWithValue("@DVHc", dvhCalculado ?? "");
            cmd.Parameters.AddWithValue("@DVVe", dvvEsperado ?? "");
            cmd.Parameters.AddWithValue("@DVVc", dvvCalculado ?? "");
            cmd.ExecuteNonQuery();
        }

        public List<InconsistenciaDV> ObtenerInconsistencias(bool soloNoResueltas = true)
        {
            var lista = new List<InconsistenciaDV>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = "SELECT * FROM InconsistenciasDV";
            if (soloNoResueltas) sql += " WHERE Resuelta=0";
            sql += " ORDER BY FechaDeteccion DESC";
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new InconsistenciaDV
                {
                    IdInconsistencia = (int)rd["IdInconsistencia"],
                    FechaDeteccion = (DateTime)rd["FechaDeteccion"],
                    NombreTabla = rd["NombreTabla"].ToString(),
                    DVH_Esperado = rd["DVH_Esperado"].ToString(),
                    DVH_Calculado = rd["DVH_Calculado"].ToString(),
                    DVV_Esperado = rd["DVV_Esperado"].ToString(),
                    DVV_Calculado = rd["DVV_Calculado"].ToString(),
                    Resuelta = (bool)rd["Resuelta"],
                    Resolucion = rd["Resolucion"]?.ToString()
                });
            return lista;
        }

        public void MarcarInconsistenciaResuelta(int idInconsistencia, string resolucion)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"UPDATE InconsistenciasDV
                SET Resuelta=1, Resolucion=@Res WHERE IdInconsistencia=@Id";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Res", resolucion);
            cmd.Parameters.AddWithValue("@Id", idInconsistencia);
            cmd.ExecuteNonQuery();
        }

        // ══════════════════════════════════════════════════════
        // BACKUP de DVs correctos
        // ══════════════════════════════════════════════════════
        public void GuardarBackupDV(string nombreTabla, string dvh, string dvv, string descripcion)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"INSERT INTO BackupDV (NombreTabla,DVH,DVV,Descripcion)
                VALUES (@Tabla,@DVH,@DVV,@Desc)";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Tabla", nombreTabla);
            cmd.Parameters.AddWithValue("@DVH", dvh);
            cmd.Parameters.AddWithValue("@DVV", dvv);
            cmd.Parameters.AddWithValue("@Desc", (object)descripcion ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public BackupDV ObtenerUltimoBackupDV(string nombreTabla)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT TOP 1 * FROM BackupDV
                WHERE NombreTabla=@Tabla ORDER BY FechaBackup DESC";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Tabla", nombreTabla);
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
                return new BackupDV
                {
                    IdBackup = (int)rd["IdBackup"],
                    FechaBackup = (DateTime)rd["FechaBackup"],
                    NombreTabla = rd["NombreTabla"].ToString(),
                    DVH = rd["DVH"].ToString(),
                    DVV = rd["DVV"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString()
                };
            return null;
        }

        // ══════════════════════════════════════════════════════
        // BACKUP / RESTORE físico de SQL Server
        // (conexión a master, no a HotelRydlDB)
        // ══════════════════════════════════════════════════════
        public void EjecutarBackupSQLServer(string rutaArchivo)
        {
            using var con = ConexionDB.ObtenerConexionMaster();
            con.Open();
            string sql = $@"BACKUP DATABASE {ConexionDB.NombreBD} TO DISK='{rutaArchivo}'
                WITH FORMAT, NAME='{ConexionDB.NombreBD}-{DateTime.Now:yyyyMMdd_HHmmss}'";
            using var cmd = new SqlCommand(sql, con);
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
        }

        public void EjecutarRestoreSQLServer(string rutaArchivo)
        {
            using var con = ConexionDB.ObtenerConexionMaster();
            con.Open();

            using (var c = new SqlCommand(
                $"ALTER DATABASE {ConexionDB.NombreBD} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", con))
            {
                c.CommandTimeout = 120;
                c.ExecuteNonQuery();
            }

            using (var c = new SqlCommand(
                $"RESTORE DATABASE {ConexionDB.NombreBD} FROM DISK='{rutaArchivo}' WITH REPLACE, RECOVERY", con))
            {
                c.CommandTimeout = 600;
                c.ExecuteNonQuery();
            }

            System.Threading.Thread.Sleep(3000);

            using (var c = new SqlCommand(
                $"ALTER DATABASE {ConexionDB.NombreBD} SET MULTI_USER", con))
            {
                c.CommandTimeout = 120;
                c.ExecuteNonQuery();
            }
        }

        /// <summary>Carpeta de backup configurada en la instancia de SQL Server (o null).</summary>
        public string? ObtenerCarpetaBackupPorDefecto()
        {
            using var con = ConexionDB.ObtenerConexionMaster();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(500))", con);
            var r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? null : r.ToString();
        }
    }
}
