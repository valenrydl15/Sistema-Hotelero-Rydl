using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ENTIDADES_HRS_480;

namespace DAL_HRS_480
{
    public class UsuarioDAL
    {
        private static Usuario MapearUsuario(SqlDataReader r) => new Usuario
        {
            IdUsuario = (int)r["IdUsuario"],
            Nombre = r["Nombre"].ToString(),
            Apellido = r["Apellido"].ToString(),
            DNI = r["DNI"].ToString(),
            Email = r["Email"] == DBNull.Value ? "" : r["Email"].ToString(),
            NombreUsuario = r["NombreUsuario"].ToString(),
            ContrasenaHash = r["ContrasenaHash"].ToString(),
            IdRol = (int)r["IdRol"],
            NombreRol = r["NombreRol"].ToString(),
            Activo = (bool)r["Activo"],
            Bloqueado = (bool)r["Bloqueado"],
            IntentosFallidos = (int)r["IntentosFallidos"],
            MaxIntentos = (int)r["MaxIntentos"],
            DebeChangiarClave = (bool)r["DebeChangiarClave"],
            FechaCreacion = (DateTime)r["FechaCreacion"],
            FechaUltimoAcceso = r["FechaUltimoAcceso"] == DBNull.Value ? (DateTime?)null : (DateTime)r["FechaUltimoAcceso"],
            SesionActiva = (bool)r["SesionActiva"]
        };

        private const string SELECT_BASE =
            @"SELECT u.*, r.Nombre AS NombreRol
              FROM Usuarios u
              INNER JOIN Roles r ON u.IdRol = r.IdRol";
        public Usuario ObtenerPorNombreUsuario(string nombreUsuario)
        {
            string sql = SELECT_BASE + " WHERE u.NombreUsuario = @NombreUsuario";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapearUsuario(r) : null;
        }
    


        public Usuario ObtenerPorId(int id)
        {
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                const string sql = @"SELECT u.*, r.Nombre AS NombreRol FROM Usuarios u
                                     INNER JOIN Roles r ON u.IdRol = r.IdRol
                                     WHERE u.IdUsuario = @Id";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var rd = cmd.ExecuteReader())
                        return rd.Read() ? MapearUsuario(rd) : null;
                }
            }
        }

        
        public List<Usuario> ObtenerTodos(bool soloActivos = true)
        {
            var lista = new List<Usuario>();
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                string sql = @"SELECT u.*, r.Nombre AS NombreRol FROM Usuarios u
                               INNER JOIN Roles r ON u.IdRol = r.IdRol";
                if (soloActivos) sql += " WHERE u.Activo = 1";
                sql += " ORDER BY u.Apellido, u.Nombre";

                using (var cmd = new SqlCommand(sql, con))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read()) lista.Add(MapearUsuario(rd));
                }
            }
            return lista;
        }

        
        public List<Usuario> ObtenerFiltrado(string dni, string apellidos, string nombres,
                                              string login, int? idRol, bool soloActivos)
        {
            var lista = new List<Usuario>();
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                string sql = @"SELECT u.*, r.Nombre AS NombreRol FROM Usuarios u
                               INNER JOIN Roles r ON u.IdRol = r.IdRol WHERE 1=1";

                if (soloActivos) sql += " AND u.Activo = 1";
                if (!string.IsNullOrWhiteSpace(dni)) sql += " AND u.DNI LIKE @DNI";
                if (!string.IsNullOrWhiteSpace(apellidos)) sql += " AND u.Apellido LIKE @Apellidos";
                if (!string.IsNullOrWhiteSpace(nombres)) sql += " AND u.Nombre LIKE @Nombres";
                if (!string.IsNullOrWhiteSpace(login)) sql += " AND u.NombreUsuario LIKE @Login";
                if (idRol != null) sql += " AND u.IdRol = @IdRol";

                sql += " ORDER BY u.Apellido, u.Nombre";

                using (var cmd = new SqlCommand(sql, con))
                {
                    if (!string.IsNullOrWhiteSpace(dni)) cmd.Parameters.AddWithValue("@DNI", "%" + dni + "%");
                    if (!string.IsNullOrWhiteSpace(apellidos)) cmd.Parameters.AddWithValue("@Apellidos", "%" + apellidos + "%");
                    if (!string.IsNullOrWhiteSpace(nombres)) cmd.Parameters.AddWithValue("@Nombres", "%" + nombres + "%");
                    if (!string.IsNullOrWhiteSpace(login)) cmd.Parameters.AddWithValue("@Login", "%" + login + "%");
                    if (idRol != null) cmd.Parameters.AddWithValue("@IdRol", idRol.Value);

                    using (var rd = cmd.ExecuteReader())
                        while (rd.Read()) lista.Add(MapearUsuario(rd));
                }
            }
            return lista;
        }

        public bool Insertar(Usuario u, out string errorDetalle)
        {
            errorDetalle = "";
            const string sql = @"INSERT INTO Usuarios
                (Nombre,Apellido,DNI,Email,NombreUsuario,ContrasenaHash,
                 IdRol,Activo,Bloqueado,IntentosFallidos,MaxIntentos,DebeChangiarClave,SesionActiva)
                VALUES
                (@Nombre,@Apellido,@DNI,@Email,@NombreUsuario,@ContrasenaHash,
                 @IdRol,1,0,0,@MaxIntentos,0,0)";
            try
            {
                using var con = ConexionDB.ObtenerConexion();
                con.Open();
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Nombre", u.Nombre);
                cmd.Parameters.AddWithValue("@Apellido", u.Apellido);
                cmd.Parameters.AddWithValue("@DNI", u.DNI);
                cmd.Parameters.AddWithValue("@Email", (object)u.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NombreUsuario", u.NombreUsuario);
                cmd.Parameters.AddWithValue("@ContrasenaHash", u.ContrasenaHash);
                cmd.Parameters.AddWithValue("@IdRol", u.IdRol);
                cmd.Parameters.AddWithValue("@MaxIntentos", u.MaxIntentos);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                errorDetalle = "Ya existe un usuario con ese DNI o nombre de usuario.";
                return false;
            }
            catch (Exception ex)
            {
                errorDetalle = $"Error inesperado: {ex.Message}";
                return false;
            }
        }
        public bool ModificarEmail(int idUsuario, string email)
        {
            const string sql = "UPDATE Usuarios SET Email=@Email WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", idUsuario);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ModificarRolUsuario(int idUsuario, int idRolNuevo)
        {
            const string sql = "UPDATE Usuarios SET IdRol=@IdRol WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@IdRol", idRolNuevo);
            cmd.Parameters.AddWithValue("@Id", idUsuario);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UsuarioTienePermiso(int idUsuario, string nombrePermiso)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            // Permiso directo en el rol
            const string sqlDirecto = @"
                SELECT COUNT(*) FROM RolPermisos rp
                INNER JOIN Permisos p ON rp.IdPermiso = p.IdPermiso
                INNER JOIN Usuarios u ON u.IdRol = rp.IdRol
                WHERE u.IdUsuario = @IdUsuario AND p.Nombre = @Permiso";

            using (var cmd = new SqlCommand(sqlDirecto, con))
            {
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.Parameters.AddWithValue("@Permiso", nombrePermiso);
                if ((int)cmd.ExecuteScalar() > 0) return true;
            }

            // Permiso dentro de una familia del rol
            const string sqlFamilia = @"
                SELECT COUNT(*) FROM RolFamilias rf
                INNER JOIN FamiliaPermisos fp ON rf.IdFamilia = fp.IdFamilia
                INNER JOIN Permisos p ON fp.IdPermiso = p.IdPermiso
                INNER JOIN Usuarios u ON u.IdRol = rf.IdRol
                WHERE u.IdUsuario = @IdUsuario AND p.Nombre = @Permiso";

            using (var cmd = new SqlCommand(sqlFamilia, con))
            {
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmd.Parameters.AddWithValue("@Permiso", nombrePermiso);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }


        public bool ActualizarContrasena(int id, string hash)
        {
            const string sql = "UPDATE Usuarios SET ContrasenaHash=@H, DebeChangiarClave=0 WHERE IdUsuario=@Id";
            using (var con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE Usuarios SET ContrasenaHash=@H, DebeChangiarClave=0 WHERE IdUsuario=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@H", hash);
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public void RegistrarIntentoFallido(int id, int maxIntentos)
        {
            const string sql = @"UPDATE Usuarios
                SET IntentosFallidos = IntentosFallidos + 1,
                    Bloqueado = CASE WHEN IntentosFallidos+1 >= @Max THEN 1 ELSE 0 END
                WHERE IdUsuario = @Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Max", maxIntentos);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        
        }

        public bool Desbloquear(int id, string nuevoHash)
        {
            const string sql = @"UPDATE Usuarios
                SET Bloqueado=0, IntentosFallidos=0,
                    ContrasenaHash=@Hash, DebeChangiarClave=1, SesionActiva=0
                WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Hash", nuevoHash);
            cmd.Parameters.AddWithValue("@Id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public void SetSesionActiva(int id, bool activa)
        {
            const string sql = @"UPDATE Usuarios
                SET SesionActiva=@A,
                    FechaUltimoAcceso = CASE WHEN @A=1 THEN GETDATE() ELSE FechaUltimoAcceso END
                WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@A", activa ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public void ResetearIntentos(int id)
        {
            const string sql = "UPDATE Usuarios SET IntentosFallidos=0 WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public bool SetActivo(int id, bool activo)
        {
            const string sql = "UPDATE Usuarios SET Activo=@A WHERE IdUsuario=@Id";
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@A", activo ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<Rol> ObtenerRoles()
        {
            var lista = new List<Rol>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            // Carga TODOS los roles incluyendo los creados en el Generador de Roles
            using var cmd = new SqlCommand("SELECT * FROM Roles ORDER BY Nombre", con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Rol
                {
                    IdRol = (int)rd["IdRol"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString()
                });
            return lista;
        }
    }
}

