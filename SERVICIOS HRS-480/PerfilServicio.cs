using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL_HRS_480;
using ENTIDADES_HRS_480;

namespace SERVICIOS_HRS_480
{
    public class PerfilServicio
    {
        private readonly PerfilDAL _dal = new PerfilDAL();
        private readonly Bitacora _bitacora = new Bitacora();

        private const int ID_MOD = 1;   // Módulo Usuarios/Perfiles
        private const int EV_CREAR = 3;

        // ══════════════════════════════════════════════════════
        // PERMISOS
        // ══════════════════════════════════════════════════════
        public List<Permiso> ObtenerPermisos() => _dal.ObtenerTodosPermisos();

        // ══════════════════════════════════════════════════════
        // FAMILIAS
        // ══════════════════════════════════════════════════════
        public List<Familia> ObtenerFamilias() => _dal.ObtenerTodasFamilias();

        public Familia ObtenerFamiliaCompleta(int idFamilia)
            => _dal.ObtenerFamiliaCompleta(idFamilia);

        // ──────────────────────────────────────────────────────
        // CREAR FAMILIA
        // Validaciones:
        //   - Nombre no vacío y no duplicado
        //   - Debe tener al menos un permiso o sub-familia
        //   - No puede asignarle una sub-familia con los mismos permisos
        // ──────────────────────────────────────────────────────
        public bool CrearFamilia(string nombre, string descripcion,
                                  List<int> idsPermisos, List<int> idsSubFamilias,
                                  out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Familias", "FamiliaPermisos", "FamiliaFamilias");
            mensaje = "";

            if (string.IsNullOrWhiteSpace(nombre))
            { mensaje = "❌ El nombre de la familia es obligatorio."; return false; }

            if (_dal.ExisteFamiliaNombre(nombre.Trim()))
            { mensaje = $"❌ Ya existe una familia con el nombre '{nombre}'."; return false; }

            if ((idsPermisos == null || idsPermisos.Count == 0) &&
                (idsSubFamilias == null || idsSubFamilias.Count == 0))
            { mensaje = "❌ La familia debe tener al menos un permiso o sub-familia."; return false; }

            // Validar que las sub-familias no tengan exactamente los mismos permisos
            if (!ValidarPermisosNoRepetidosEnFamilias(idsPermisos, idsSubFamilias, out string errorPermisos))
            { mensaje = $"❌ {errorPermisos}"; return false; }

            int idNueva = _dal.InsertarFamilia(nombre.Trim(), descripcion?.Trim());
            if (idsPermisos != null) _dal.AsignarPermisosAFamilia(idNueva, idsPermisos);
            if (idsSubFamilias != null) _dal.AsignarSubFamilias(idNueva, idsSubFamilias);

            _bitacora.Registrar(null, ID_MOD, EV_CREAR, 2,
                $"Familia creada: {nombre}", "EXITOSO");

            mensaje = $"✔ Familia '{nombre}' creada exitosamente.";
            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Familias", "FamiliaPermisos", "FamiliaFamilias");
            return true;
        }

        // ──────────────────────────────────────────────────────
        // MODIFICAR FAMILIA
        // ──────────────────────────────────────────────────────
        public bool ModificarFamilia(int idFamilia, List<int> idsPermisos,
                                      List<int> idsSubFamilias, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Familias", "FamiliaPermisos", "FamiliaFamilias");
            mensaje = "";

            if (!ValidarPermisosNoRepetidosEnFamilias(idsPermisos, idsSubFamilias, out string err))
            { mensaje = $"❌ {err}"; return false; }

            _dal.AsignarPermisosAFamilia(idFamilia, idsPermisos ?? new List<int>());
            _dal.AsignarSubFamilias(idFamilia, idsSubFamilias ?? new List<int>());

            mensaje = "✔ Familia modificada correctamente.";
            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Familias", "FamiliaPermisos", "FamiliaFamilias");
            return true;
        }

        // ──────────────────────────────────────────────────────
        // ELIMINAR FAMILIA
        // No se puede eliminar si está en uso por un rol u otra familia
        // ──────────────────────────────────────────────────────
        public bool EliminarFamilia(int idFamilia, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Familias", "FamiliaPermisos", "FamiliaFamilias");
            mensaje = "";
            if (_dal.FamiliaEstaEnUso(idFamilia))
            {
                mensaje = "❌ No se puede eliminar: la familia está siendo usada por un rol u otra familia.";
                return false;
            }
            bool ok = _dal.EliminarFamilia(idFamilia);
            mensaje = ok ? "✔ Familia eliminada." : "❌ Error al eliminar la familia.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Familias", "FamiliaPermisos", "FamiliaFamilias");
            return ok;
        }

        // ──────────────────────────────────────────────────────
        // VALIDACIÓN: permisos no repetidos entre permisos directos
        // y sub-familias asignadas
        // ──────────────────────────────────────────────────────
        private bool ValidarPermisosNoRepetidosEnFamilias(List<int> idsPermisos,
                                                           List<int> idsSubFamilias,
                                                           out string error)
        {
            error = "";
            if (idsSubFamilias == null || idsSubFamilias.Count == 0) return true;

            // Obtener todos los permisos de las sub-familias a asignar
            var permisosEnSubFamilias = new List<int>();
            foreach (var idSub in idsSubFamilias)
            {
                var fam = _dal.ObtenerFamiliaCompleta(idSub);
                if (fam != null)
                    permisosEnSubFamilias.AddRange(
                        fam.ObtenerPermisos().Select(p => p.IdPermiso));
            }

            // Verificar si hay permisos directos que ya están en las sub-familias
            if (idsPermisos != null)
            {
                var repetidos = idsPermisos.Intersect(permisosEnSubFamilias).ToList();
                if (repetidos.Count > 0)
                {
                    error = "Hay permisos repetidos entre los permisos directos y las sub-familias asignadas.";
                    return false;
                }
            }

            // Verificar permisos repetidos entre las sub-familias entre sí
            var todos = permisosEnSubFamilias;
            if (todos.Count != todos.Distinct().Count())
            {
                error = "Hay permisos repetidos entre las sub-familias seleccionadas.";
                return false;
            }

            return true;
        }

        // ══════════════════════════════════════════════════════
        // ROLES
        // ══════════════════════════════════════════════════════
        public List<Rol> ObtenerRoles() => _dal.ObtenerTodosRoles();

        public RolPerfil ObtenerRolCompleto(int idRol) => _dal.ObtenerRolCompleto(idRol);

        // ──────────────────────────────────────────────────────
        // CREAR ROL
        // ──────────────────────────────────────────────────────
        public bool CrearRol(string nombre, string descripcion,
                              List<int> idsPermisos, List<int> idsFamilias,
                              out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Roles", "RolPermisos", "RolFamilias");
            mensaje = "";

            if (string.IsNullOrWhiteSpace(nombre))
            { mensaje = "❌ El nombre del rol es obligatorio."; return false; }

            if ((idsPermisos == null || idsPermisos.Count == 0) &&
                (idsFamilias == null || idsFamilias.Count == 0))
            { mensaje = "❌ El rol debe tener al menos un permiso o familia."; return false; }

            // Validar permisos no repetidos entre permisos directos y familias
            if (!ValidarPermisosNoRepetidosEnRol(idsPermisos, idsFamilias, out string err))
            { mensaje = $"❌ {err}"; return false; }

            int idNuevo = _dal.InsertarRol(nombre.Trim(), descripcion?.Trim());
            if (idsPermisos != null) _dal.AsignarPermisosARol(idNuevo, idsPermisos);
            if (idsFamilias != null) _dal.AsignarFamiliasARol(idNuevo, idsFamilias);

            mensaje = $"✔ Rol '{nombre}' creado exitosamente.";
            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Roles", "RolPermisos", "RolFamilias");
            return true;
        }

        // ──────────────────────────────────────────────────────
        // MODIFICAR ROL (con desempaquetado de permisos repetidos)
        // Al agregar una familia al rol, si tiene permisos que ya
        // están asignados directamente al rol, se eliminan los directos.
        // ──────────────────────────────────────────────────────
        public bool ModificarRol(int idRol, List<int> idsPermisosDirectos,
                                  List<int> idsFamilias, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Roles", "RolPermisos", "RolFamilias");
            mensaje = "";

            // Desempaquetar: obtener todos los permisos que traen las familias
            var permisosEnFamilias = new List<int>();
            if (idsFamilias != null)
                foreach (var idF in idsFamilias)
                {
                    var fam = _dal.ObtenerFamiliaCompleta(idF);
                    if (fam != null)
                        permisosEnFamilias.AddRange(
                            fam.ObtenerPermisos().Select(p => p.IdPermiso));
                }

            // Eliminar de permisos directos los que ya están en las familias
            var permisosLimpios = idsPermisosDirectos?
                .Where(idP => !permisosEnFamilias.Contains(idP))
                .ToList() ?? new List<int>();

            int eliminados = (idsPermisosDirectos?.Count ?? 0) - permisosLimpios.Count;

            _dal.AsignarPermisosARol(idRol, permisosLimpios);
            _dal.AsignarFamiliasARol(idRol, idsFamilias ?? new List<int>());

            mensaje = eliminados > 0
                ? $"✔ Rol modificado. Se eliminaron {eliminados} permiso/s repetido/s."
                : "✔ Rol modificado correctamente.";
            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Roles", "RolPermisos", "RolFamilias");
            return true;
        }

        // ──────────────────────────────────────────────────────
        // ELIMINAR ROL
        // Si está en uso, los usuarios pasan al rol Básico
        // ──────────────────────────────────────────────────────
        public bool EliminarRol(int idRol, out string mensaje, out int usuariosAfectados)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Roles", "RolPermisos", "RolFamilias", "Usuarios");
            mensaje = "";
            usuariosAfectados = 0;

            bool enUso = _dal.RolEstaEnUso(idRol, out int cantUsuarios);
            if (enUso)
            {
                usuariosAfectados = cantUsuarios;
                int idBasico = _dal.ObtenerIdRolBasico();
                _dal.MoverUsuariosARolBasico(idRol, idBasico);
            }

            bool ok = _dal.EliminarRol(idRol);
            mensaje = ok
                ? enUso
                    ? $"✔ Rol eliminado. {cantUsuarios} usuario/s fueron movidos al rol Básico."
                    : "✔ Rol eliminado correctamente."
                : "❌ Error al eliminar el rol.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Roles", "RolPermisos", "RolFamilias", "Usuarios");
            return ok;
        }

        // ──────────────────────────────────────────────────────
        // VALIDACIÓN: permisos no repetidos en un Rol
        // ──────────────────────────────────────────────────────
        private bool ValidarPermisosNoRepetidosEnRol(List<int> idsPermisos,
                                                      List<int> idsFamilias,
                                                      out string error)
        {
            error = "";
            if (idsFamilias == null || idsFamilias.Count == 0) return true;

            var permisosEnFamilias = new List<int>();
            foreach (var idF in idsFamilias)
            {
                var fam = _dal.ObtenerFamiliaCompleta(idF);
                if (fam != null)
                    permisosEnFamilias.AddRange(
                        fam.ObtenerPermisos().Select(p => p.IdPermiso));
            }

            if (idsPermisos != null)
            {
                var rep = idsPermisos.Intersect(permisosEnFamilias).ToList();
                if (rep.Count > 0)
                {
                    error = "Hay permisos directos que ya están incluidos en las familias asignadas.";
                    return false;
                }
            }

            if (permisosEnFamilias.Count != permisosEnFamilias.Distinct().Count())
            {
                error = "Hay permisos repetidos entre las familias seleccionadas.";
                return false;
            }

            return true;
        }
    }
}

