using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>Reglas y utilidades compartidas por las BLL del proceso de reposición.</summary>
    internal static class ReposicionComun
    {
        public const decimal CANTIDAD_MAX = 9_999_999m;
        public static readonly string[] PRIORIDADES = { "Baja", "Normal", "Alta", "Urgente" };

        public static Usuario? Actual => SesionManager.Instancia.UsuarioActual;
        public static int IdActual => Actual?.IdUsuario ?? 0;
        public static string NombreActual => Actual?.NombreUsuario ?? "(sin sesión)";

        /// <summary>Registra el rechazo por falta de permisos y devuelve el resultado listo para la GUI.</summary>
        public static ResultadoReposicion<T> Denegado<T>(string operacion)
        {
            BitacoraReposicion.Registrar(BitacoraReposicion.ACCESO_DENEGADO, 2,
                $"{NombreActual} intentó '{operacion}' sin permiso (rol: {Actual?.NombreRol ?? "-"}).", "RECHAZADO");
            return ResultadoReposicion<T>.Fallo("Su rol no tiene permiso para realizar esta operación.");
        }

        public static ResultadoReposicion<T> Rechazo<T>(string operacion, string motivo)
        {
            BitacoraReposicion.Registrar(BitacoraReposicion.OPERACION_RECH, 3,
                $"{operacion}: {motivo} (usuario {NombreActual})", "RECHAZADO");
            return ResultadoReposicion<T>.Fallo(motivo);
        }

        public static ResultadoReposicion<T> ErrorTecnico<T>(string operacion, Exception ex)
        {
            BitacoraReposicion.Registrar(BitacoraReposicion.ERROR_TECNICO, 1,
                $"{operacion}: {ex.Message}", "FALLIDO");
            return ResultadoReposicion<T>.Fallo("Ocurrió un error inesperado y la operación no se realizó. Detalle: " + ex.Message);
        }

        public static bool EsDuplicado(Microsoft.Data.SqlClient.SqlException ex) => ex.Number == 2601 || ex.Number == 2627;
    }
}
