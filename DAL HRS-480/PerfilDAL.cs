using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTIDADES_HRS_480;
using Microsoft.Data.SqlClient;

namespace DAL_HRS_480
{
    public class PerfilDAL
    {
        public List<Permiso> ObtenerTodosPermisos()
        {
            var lista = new List<Permiso>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = "SELECT * FROM Permisos WHERE Activo=1 ORDER BY Nombre";
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Permiso
                {
                    IdPermiso = (int)rd["IdPermiso"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        // Permisos que pertenecen a una familia
        public List<Permiso> ObtenerPermisosDeFamilia(int idFamilia)
        {
            var lista = new List<Permiso>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT p.* FROM Permisos p
                INNER JOIN FamiliaPermisos fp ON p.IdPermiso = fp.IdPermiso
                WHERE fp.IdFamilia = @Id ORDER BY p.Nombre";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", idFamilia);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Permiso
                {
                    IdPermiso = (int)rd["IdPermiso"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        // Permisos directos de un Rol
        public List<Permiso> ObtenerPermisosDeRol(int idRol)
        {
            var lista = new List<Permiso>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT p.* FROM Permisos p
                INNER JOIN RolPermisos rp ON p.IdPermiso = rp.IdPermiso
                WHERE rp.IdRol = @Id ORDER BY p.Nombre";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", idRol);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Permiso
                {
                    IdPermiso = (int)rd["IdPermiso"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        // ══════════════════════════════════════════════════════
        // FAMILIAS
        // ══════════════════════════════════════════════════════

        public List<Familia> ObtenerTodasFamilias()
        {
            var lista = new List<Familia>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = "SELECT * FROM Familias WHERE Activo=1 ORDER BY Nombre";
            using var cmd = new SqlCommand(sql, con);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Familia
                {
                    IdFamilia = (int)rd["IdFamilia"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        // Carga una familia completa con sus permisos y sub-familias
        public Familia ObtenerFamiliaCompleta(int idFamilia)
        {
            Familia fam = null;
            using var con = ConexionDB.ObtenerConexion();
            con.Open();

            using (var cmd = new SqlCommand("SELECT * FROM Familias WHERE IdFamilia=@Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", idFamilia);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                    fam = new Familia
                    {
                        IdFamilia = (int)rd["IdFamilia"],
                        Nombre = rd["Nombre"].ToString(),
                        Descripcion = rd["Descripcion"]?.ToString(),
                        Activo = (bool)rd["Activo"]
                    };
            }

            if (fam == null) return null;

            fam.Permisos = ObtenerPermisosDeFamilia(idFamilia);
            fam.SubFamilias = ObtenerSubFamilias(idFamilia);
            return fam;
        }

        // Sub-familias de una familia (un nivel, sin recursión profunda)
        public List<Familia> ObtenerSubFamilias(int idFamiliaPadre)
        {
            var lista = new List<Familia>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT f.* FROM Familias f
                INNER JOIN FamiliaFamilias ff ON f.IdFamilia = ff.IdFamiliaHija
                WHERE ff.IdFamiliaPadre = @Id ORDER BY f.Nombre";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", idFamiliaPadre);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Familia
                {
                    IdFamilia = (int)rd["IdFamilia"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        public bool ExisteFamiliaNombre(string nombre, int? idExcluir = null)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            string sql = "SELECT COUNT(*) FROM Familias WHERE Nombre=@Nombre AND Activo=1";
            if (idExcluir != null) sql += " AND IdFamilia <> @IdExcluir";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            if (idExcluir != null) cmd.Parameters.AddWithValue("@IdExcluir", idExcluir.Value);
            return (int)cmd.ExecuteScalar() > 0;
        }

        public bool FamiliaEstaEnUso(int idFamilia)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT COUNT(*) FROM RolFamilias WHERE IdFamilia=@Id
                                  UNION ALL
                                 SELECT COUNT(*) FROM FamiliaFamilias WHERE IdFamiliaHija=@Id";
            // Verificamos ambas tablas
            using var cmd1 = new SqlCommand(
                "SELECT COUNT(*) FROM RolFamilias WHERE IdFamilia=@Id", con);
            cmd1.Parameters.AddWithValue("@Id", idFamilia);
            if ((int)cmd1.ExecuteScalar() > 0) return true;

            using var cmd2 = new SqlCommand(
                "SELECT COUNT(*) FROM FamiliaFamilias WHERE IdFamiliaHija=@Id", con);
            cmd2.Parameters.AddWithValue("@Id", idFamilia);
            return (int)cmd2.ExecuteScalar() > 0;
        }

        public int InsertarFamilia(string nombre, string descripcion)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"INSERT INTO Familias (Nombre, Descripcion)
                                  VALUES (@Nombre, @Desc);
                                  SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Desc", (object)descripcion ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void AsignarPermisosAFamilia(int idFamilia, List<int> idsPermisos)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            // Limpiar asignaciones anteriores
            using (var del = new SqlCommand(
                "DELETE FROM FamiliaPermisos WHERE IdFamilia=@Id", con))
            {
                del.Parameters.AddWithValue("@Id", idFamilia);
                del.ExecuteNonQuery();
            }
            // Insertar nuevas
            foreach (var idP in idsPermisos)
            {
                using var ins = new SqlCommand(
                    "INSERT INTO FamiliaPermisos(IdFamilia,IdPermiso) VALUES(@F,@P)", con);
                ins.Parameters.AddWithValue("@F", idFamilia);
                ins.Parameters.AddWithValue("@P", idP);
                ins.ExecuteNonQuery();
            }
        }

        public void AsignarSubFamilias(int idFamiliaPadre, List<int> idsHijas)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using (var del = new SqlCommand(
                "DELETE FROM FamiliaFamilias WHERE IdFamiliaPadre=@Id", con))
            {
                del.Parameters.AddWithValue("@Id", idFamiliaPadre);
                del.ExecuteNonQuery();
            }
            foreach (var idH in idsHijas)
            {
                using var ins = new SqlCommand(
                    "INSERT INTO FamiliaFamilias(IdFamiliaPadre,IdFamiliaHija) VALUES(@P,@H)", con);
                ins.Parameters.AddWithValue("@P", idFamiliaPadre);
                ins.Parameters.AddWithValue("@H", idH);
                ins.ExecuteNonQuery();
            }
        }

        public bool EliminarFamilia(int idFamilia)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Permisos directos de esta familia
                using (var d1 = new SqlCommand(
                    "DELETE FROM FamiliaPermisos WHERE IdFamilia=@Id", con, tx))
                {
                    d1.Parameters.AddWithValue("@Id", idFamilia);
                    d1.ExecuteNonQuery();
                }

                // Subfamilias que esta familia tiene asignadas (como padre)
                using (var d2 = new SqlCommand(
                    "DELETE FROM FamiliaFamilias WHERE IdFamiliaPadre=@Id", con, tx))
                {
                    d2.Parameters.AddWithValue("@Id", idFamilia);
                    d2.ExecuteNonQuery();
                }

                using var cmd = new SqlCommand(
                    "DELETE FROM Familias WHERE IdFamilia=@Id", con, tx);
                cmd.Parameters.AddWithValue("@Id", idFamilia);
                int filas = cmd.ExecuteNonQuery();

                tx.Commit();
                return filas > 0;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══════════════════════════════════════════════════════
        // ROLES (compuesto raíz)
        // ══════════════════════════════════════════════════════

        public List<Rol> ObtenerTodosRoles()
        {
            var lista = new List<Rol>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = "SELECT * FROM Roles ORDER BY Nombre";
            using var cmd = new SqlCommand(sql, con);
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

        // Carga un RolPerfil completo con sus permisos y familias
        public RolPerfil ObtenerRolCompleto(int idRol)
        {
            RolPerfil rol = null;
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using (var cmd = new SqlCommand("SELECT * FROM Roles WHERE IdRol=@Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", idRol);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                    rol = new RolPerfil
                    {
                        IdRol = (int)rd["IdRol"],
                        Nombre = rd["Nombre"].ToString(),
                        Descripcion = rd["Descripcion"]?.ToString()
                    };
            }
            if (rol == null) return null;

            rol.PermisosDirectos = ObtenerPermisosDeRol(idRol);
            rol.Familias = ObtenerFamiliasDeRol(idRol);
            return rol;
        }

        public List<Familia> ObtenerFamiliasDeRol(int idRol)
        {
            var lista = new List<Familia>();
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"SELECT f.* FROM Familias f
                INNER JOIN RolFamilias rf ON f.IdFamilia = rf.IdFamilia
                WHERE rf.IdRol=@Id ORDER BY f.Nombre";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Id", idRol);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(new Familia
                {
                    IdFamilia = (int)rd["IdFamilia"],
                    Nombre = rd["Nombre"].ToString(),
                    Descripcion = rd["Descripcion"]?.ToString(),
                    Activo = (bool)rd["Activo"]
                });
            return lista;
        }

        public bool RolEstaEnUso(int idRol, out int cantUsuarios)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Usuarios WHERE IdRol=@Id", con);
            cmd.Parameters.AddWithValue("@Id", idRol);
            cantUsuarios = (int)cmd.ExecuteScalar();
            return cantUsuarios > 0;
        }

        public int ObtenerIdRolBasico()
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "SELECT IdRol FROM Roles WHERE Nombre='Basico'", con);
            var res = cmd.ExecuteScalar();
            return res != null ? (int)res : 1;
        }

        public void MoverUsuariosARolBasico(int idRolEliminar, int idRolBasico)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using var cmd = new SqlCommand(
                "UPDATE Usuarios SET IdRol=@Basico WHERE IdRol=@Old", con);
            cmd.Parameters.AddWithValue("@Basico", idRolBasico);
            cmd.Parameters.AddWithValue("@Old", idRolEliminar);
            cmd.ExecuteNonQuery();
        }

        public int InsertarRol(string nombre, string descripcion)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            const string sql = @"INSERT INTO Roles(Nombre,Descripcion)
                                  VALUES(@Nombre,@Desc);
                                  SELECT SCOPE_IDENTITY();";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Desc", (object)descripcion ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void AsignarPermisosARol(int idRol, List<int> idsPermisos)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using (var del = new SqlCommand("DELETE FROM RolPermisos WHERE IdRol=@Id", con))
            {
                del.Parameters.AddWithValue("@Id", idRol);
                del.ExecuteNonQuery();
            }
            foreach (var idP in idsPermisos)
            {
                using var ins = new SqlCommand(
                    "INSERT INTO RolPermisos(IdRol,IdPermiso) VALUES(@R,@P)", con);
                ins.Parameters.AddWithValue("@R", idRol);
                ins.Parameters.AddWithValue("@P", idP);
                ins.ExecuteNonQuery();
            }
        }

        public void AsignarFamiliasARol(int idRol, List<int> idsFamilias)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            using (var del = new SqlCommand("DELETE FROM RolFamilias WHERE IdRol=@Id", con))
            {
                del.Parameters.AddWithValue("@Id", idRol);
                del.ExecuteNonQuery();
            }
            foreach (var idF in idsFamilias)
            {
                using var ins = new SqlCommand(
                    "INSERT INTO RolFamilias(IdRol,IdFamilia) VALUES(@R,@F)", con);
                ins.Parameters.AddWithValue("@R", idRol);
                ins.Parameters.AddWithValue("@F", idF);
                ins.ExecuteNonQuery();
            }
        }

        public bool EliminarRol(int idRol)
        {
            using var con = ConexionDB.ObtenerConexion();
            con.Open();
            // Eliminar relaciones primero
            using (var d1 = new SqlCommand("DELETE FROM RolPermisos WHERE IdRol=@Id", con))
            {
                d1.Parameters.AddWithValue("@Id", idRol);
                d1.ExecuteNonQuery();
            }
            using (var d2 = new SqlCommand("DELETE FROM RolFamilias WHERE IdRol=@Id", con))
            {
                d2.Parameters.AddWithValue("@Id", idRol);
                d2.ExecuteNonQuery();
            }
            using var cmd = new SqlCommand("DELETE FROM Roles WHERE IdRol=@Id", con);
            cmd.Parameters.AddWithValue("@Id", idRol);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
    
