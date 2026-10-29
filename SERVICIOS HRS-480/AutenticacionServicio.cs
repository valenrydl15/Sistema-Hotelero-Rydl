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
    public enum ResultadoLogin
    {
        Exitoso,
        UsuarioNoEncontrado,
        ContrasenaIncorrecta,
        CuentaBloqueada,
        UsuarioInactivo,
        SesionYaActiva,
        DebeChangiarClave,
        InconsistenciaDV
    }

    public enum ResultadoCambioClave
    {
        Exitoso,
        UsuarioNoEncontrado,
        ClaveActualIncorrecta,
        NuevaIgualActual,
        ConfirmacionNoCoincide
    }

    public class AutenticacionServicio
    {
        private const int ID_MOD = 1;  
        private const int EV_LOGIN = 1;
        private const int EV_LOGOUT = 2;
        private const int EV_BLOQUEO = 6;
        private const int EV_DESBLOQUEO = 7;
        private const int EV_CAMBIO_CLAVE = 8;
        private const int EV_INTENTO = 9;

        private readonly UsuarioDAL _uDAL = new UsuarioDAL();
        private readonly Bitacora _bDAL = new Bitacora();

        private readonly DigitoVerificadorServicio _dvServicio = new DigitoVerificadorServicio();

       
        public ResultadoVerificacionDV UltimaVerificacionDV { get; private set; }

        public ResultadoLogin Login(string nombreUsuario, string clavePlana,
                                     out Usuario usuarioLogueado)
        {
            usuarioLogueado = null;
            var u = _uDAL.ObtenerPorNombreUsuario(nombreUsuario);

            if (u == null)
            {
                _bDAL.Registrar(null, ID_MOD, EV_INTENTO, 2,
                    $"Usuario '{nombreUsuario}' no encontrado.", "FALLIDO");
                return ResultadoLogin.UsuarioNoEncontrado;
            }
            if (!u.Activo)
            {
                _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_INTENTO, 2,
                    "Cuenta inactiva.", "FALLIDO");
                return ResultadoLogin.UsuarioInactivo;
            }
            if (u.Bloqueado)
            {
                _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_BLOQUEO, 1,
                    "Intento en cuenta bloqueada.", "FALLIDO");
                return ResultadoLogin.CuentaBloqueada;
            }
            if (u.SesionActiva)
            {
                _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_INTENTO, 2,
                    "Sesión duplicada rechazada.", "FALLIDO");
                return ResultadoLogin.SesionYaActiva;
            }
            if (!Encriptacion.Verificar(clavePlana, u.ContrasenaHash))
            {
                // IntentosFallidos / Bloqueado forman parte del DV de Usuarios
                bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
                _uDAL.RegistrarIntentoFallido(u.IdUsuario, u.MaxIntentos);
                DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Usuarios");
                var rel = _uDAL.ObtenerPorId(u.IdUsuario);
                bool bloq = rel != null && rel.Bloqueado;
                _bDAL.Registrar(u.IdUsuario, ID_MOD,
                    bloq ? EV_BLOQUEO : EV_INTENTO, bloq ? 1 : 2,
                    bloq ? $"Cuenta bloqueada tras {u.MaxIntentos} intentos."
                         : $"Contraseña incorrecta. Intento {rel?.IntentosFallidos}/{u.MaxIntentos}.",
                    "FALLIDO");
                return bloq ? ResultadoLogin.CuentaBloqueada : ResultadoLogin.ContrasenaIncorrecta;
            }

            // ── VERIFICACIÓN DEL DÍGITO VERIFICADOR ──────────────
            UltimaVerificacionDV = _dvServicio.Verificar();

            if (!UltimaVerificacionDV.Consistente)
            {
                // Registrar en bitácora
                _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_INTENTO, 1,
     "Inconsistencia de DV detectada en login. Tablas afectadas: " +
     string.Join(", ", UltimaVerificacionDV.Inconsistencias.Select(i => i.NombreTabla)),
     "FALLIDO");

                // Guardar al usuario logueado para que FrmLogin sepa si es admin
                usuarioLogueado = u;
                return ResultadoLogin.InconsistenciaDV;
            }
            // ─────────────────────────────────────────────────────

            // Login exitoso — guardar backup de DVs correctos
            _uDAL.ResetearIntentos(u.IdUsuario);
            _uDAL.SetSesionActiva(u.IdUsuario, true);
            _dvServicio.RecalcularYGuardar("Usuarios");
            SesionManager.Instancia.IniciarSesion(u);
            _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_LOGIN, 1, "Login exitoso.", "EXITOSO");

            // ── BACKUP AUTOMÁTICO post-login ──────────────────────
            // Genera un archivo .bak físico en C:\HotelRydl\Backups\
            // para que siempre haya uno disponible en FrmReparacionDV.
            // Si falla el backup no interrumpe el login.
            try
            {
                _dvServicio.EjecutarBackup();
                _dvServicio.GuardarBackupEstadoCorrecto("Backup post-login exitoso");
            }
            catch { /* el backup es opcional, no debe bloquear el acceso */ }
            // ─────────────────────────────────────────────────────

            usuarioLogueado = u;
            return u.DebeChangiarClave
                ? ResultadoLogin.DebeChangiarClave
                : ResultadoLogin.Exitoso;
        }



        public void Logout(int idUsuario)
        {
            _uDAL.SetSesionActiva(idUsuario, false);
            
            _bDAL.Registrar(idUsuario, ID_MOD, EV_LOGOUT, 1, "Logout.", "EXITOSO");
            SesionManager.Instancia.CerrarSesion();
        }

       
        public ResultadoCambioClave CambiarContrasena(string nombreUsuario,
                                                       string claveActual,
                                                       string claveNueva,
                                                       string claveConfirmacion)
        {
            var u = _uDAL.ObtenerPorNombreUsuario(nombreUsuario);
            if (u == null) return ResultadoCambioClave.UsuarioNoEncontrado;

            if (!Encriptacion.Verificar(claveActual, u.ContrasenaHash))
                return ResultadoCambioClave.ClaveActualIncorrecta;

            if (claveNueva == claveActual)
                return ResultadoCambioClave.NuevaIgualActual;

            if (claveNueva != claveConfirmacion)
                return ResultadoCambioClave.ConfirmacionNoCoincide;

            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            _uDAL.ActualizarContrasena(u.IdUsuario, Encriptacion.Encriptar(claveNueva));
            _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_CAMBIO_CLAVE, 1, "Contraseña cambiada.", "EXITOSO");

            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Usuarios");
            return ResultadoCambioClave.Exitoso;
        }

        
        public ResultadoCambioClave CambiarContrasenaForzada(Usuario u,
                                                              string claveActual,
                                                              string claveNueva,
                                                              string claveConfirmacion)
        {
            if (!Encriptacion.Verificar(claveActual, u.ContrasenaHash))
                return ResultadoCambioClave.ClaveActualIncorrecta;
            if (claveNueva == claveActual)
                return ResultadoCambioClave.NuevaIgualActual;
            if (claveNueva != claveConfirmacion)
                return ResultadoCambioClave.ConfirmacionNoCoincide;

            bool dvOk = DigitoVerificadorServicio.EstaConsistente("Usuarios");
            _uDAL.ActualizarContrasena(u.IdUsuario, Encriptacion.Encriptar(claveNueva));
            _bDAL.Registrar(u.IdUsuario, ID_MOD, EV_CAMBIO_CLAVE, 1, "Contraseña cambiada (forzada).", "EXITOSO");

            DigitoVerificadorServicio.RecalcularSiConsistente(dvOk, "Usuarios");
            return ResultadoCambioClave.Exitoso;
        }


    
    }
}
