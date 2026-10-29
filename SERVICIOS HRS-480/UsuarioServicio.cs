using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace SERVICIOS_HRS_480
{
    public class UsuarioServicio
    {
        private const int ID_MOD = 1;
        private const int EV_CREAR = 3;
        private const int EV_MODIFICAR = 4;
        private const int EV_ELIMINAR = 5;
        private const int EV_DESBLOQUEO = 7;

        private readonly UsuarioDAL _uDAL = new UsuarioDAL();
        private readonly Bitacora _bDAL = new Bitacora();

        private int? IdActual => SesionManager.Instancia.UsuarioActual?.IdUsuario;
        private string RolActual => SesionManager.Instancia.UsuarioActual?.NombreRol;
        public bool UsuarioActualPuedeGestionarPerfiles()
        {
            var u = SesionManager.Instancia.UsuarioActual;
            if (u == null) return false;

            // El Administrador siempre puede
            if (u.NombreRol == "Administrador") return true;

            // Cualquier otro rol que tenga el permiso GestionPerfiles
            return _uDAL.UsuarioTienePermiso(u.IdUsuario, "GestionPerfiles");
        }
        public List<Usuario> ObtenerTodos(bool soloActivos) => _uDAL.ObtenerTodos(soloActivos);

        public List<Usuario> ObtenerFiltrado(string dni, string apellido, string nombre,
                                              string login, int? idRol, bool soloActivos)
            => _uDAL.ObtenerFiltrado(dni, apellido, nombre, login, idRol, soloActivos);

        public List<Rol> ObtenerRoles() => _uDAL.ObtenerRoles();


        public bool Alta(string nombre, string apellido, string dni, string email, int idRol, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            mensaje = "";

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido)
                || string.IsNullOrWhiteSpace(dni))
            {
                mensaje = "❌ Nombre, Apellido y DNI son obligatorios.";
                return false;
            }

            
            var existente = _uDAL.ObtenerPorNombreUsuario(
                Usuario.GenerarNombreUsuario(dni, nombre));

            if (existente != null)
            {
                mensaje = $" Ya existe un usuario con el login '{existente.NombreUsuario}'.\n" +
                           "No se puede crear el mismo usuario dos veces.";
                return false;
            }

            var u = new Usuario
            {
                Nombre = nombre.Trim(),
                Apellido = apellido.Trim(),
                DNI = dni.Trim(),
                Email = email?.Trim(),
                NombreUsuario = Usuario.GenerarNombreUsuario(dni, nombre),
                ContrasenaHash = Encriptacion.Encriptar(Usuario.GenerarClaveInicial(dni, apellido)),
                IdRol = idRol,
                MaxIntentos = 3
            };

            
            bool ok = _uDAL.Insertar(u, out string errorDetalle);

            if (ok)
            {
                _bDAL.Registrar(IdActual, ID_MOD, EV_CREAR, 2,
                    $"Alta usuario: {u.NombreUsuario}", "EXITOSO");

                mensaje = $"   Usuario creado exitosamente.\n" +
                          $"   Login: {u.NombreUsuario}\n" +
                          $"   Contraseña inicial: {dni.Trim()}{apellido.Trim()}";
            }
            else
            {
                
                mensaje = $"❌ {errorDetalle}";
            }

            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
            return ok;
        }


        public bool ModificarEmail(int idUsuario, string email, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            mensaje = "";
            bool ok = _uDAL.ModificarEmail(idUsuario, email?.Trim());
            if (ok)
            {
                _bDAL.Registrar(IdActual, ID_MOD, EV_MODIFICAR, 2,
                    $"Modificación email. IdUsuario: {idUsuario}", "EXITOSO");
                mensaje = "✔ Email actualizado correctamente.";
            }
            else mensaje = "❌ Error al actualizar el email.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
            return ok;
        }
        
        
        public bool Desbloquear(int idUsuario, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            mensaje = "";
            var u = _uDAL.ObtenerPorId(idUsuario);
            if (u == null) { mensaje = "Usuario no encontrado."; return false; }

            string claveReset = Usuario.GenerarClaveInicial(u.DNI, u.Apellido);
            string hashReset = Encriptacion.Encriptar(claveReset);

            bool ok = _uDAL.Desbloquear(idUsuario, hashReset);
            if (ok)
            {
                _bDAL.Registrar(IdActual, ID_MOD, EV_DESBLOQUEO, 1,
                    $"Desbloqueo usuario: {u.NombreUsuario}. Clave reseteada.", "EXITOSO");
                mensaje = $"✔ Usuario desbloqueado.\nContraseña reseteada a: {claveReset}";
            }
            else
                mensaje = "Error al desbloquear.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
            return ok;
        }

       
        public bool SetActivo(int idUsuario, bool activo, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            mensaje = "";
            var u = _uDAL.ObtenerPorId(idUsuario);
            if (u == null) { mensaje = "Usuario no encontrado."; return false; }

            bool ok = _uDAL.SetActivo(idUsuario, activo);
            if (ok)
            {
                _bDAL.Registrar(IdActual, ID_MOD,
                    activo ? EV_CREAR : EV_ELIMINAR, 2,
                    $"Usuario {u.NombreUsuario} {(activo ? "activado" : "desactivado")}.", "EXITOSO");
                mensaje = activo ? "✔ Usuario activado." : "✔ Usuario desactivado.";
            }
            else
                mensaje = "Error al cambiar el estado.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
            return ok;
        }

        public bool AsignarRol(int idUsuarioDestino, int idRolNuevo, out string mensaje)
        {
            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            mensaje = "";

            // Validar permiso del usuario en sesión
            if (!UsuarioActualPuedeGestionarPerfiles())
            {
                mensaje = "❌ No tenés permiso para asignar roles a usuarios.";
                return false;
            }

            // No puede asignarse a sí mismo un rol diferente (opcional, podés quitarlo)
            if (idUsuarioDestino == IdActual && RolActual == "Administrador")
            {
                mensaje = "❌ El administrador no puede cambiar su propio rol.";
                return false;
            }

            var usuarioDestino = _uDAL.ObtenerPorId(idUsuarioDestino);
            if (usuarioDestino == null)
            { mensaje = "❌ Usuario no encontrado."; return false; }

            // No se puede asignar un rol si el usuario ya lo tiene
            if (usuarioDestino.IdRol == idRolNuevo)
            { mensaje = "ℹ El usuario ya tiene ese rol asignado."; return false; }

            bool ok = _uDAL.ModificarRolUsuario(idUsuarioDestino, idRolNuevo);
            if (ok)
            {
                _bDAL.Registrar(IdActual, ID_MOD, EV_MODIFICAR, 2,
                    $"Asignación de rol IdRol={idRolNuevo} a usuario: {usuarioDestino.NombreUsuario}",
                    "EXITOSO");
                mensaje = $"✔ Rol asignado correctamente al usuario {usuarioDestino.NombreUsuario}.";
            }
            else mensaje = "❌ Error al asignar el rol.";
            DigitoVerificadorServicio.RecalcularSiConsistente(ok && dvOk, "Usuarios");
            return ok;
        }

    }
}
    
