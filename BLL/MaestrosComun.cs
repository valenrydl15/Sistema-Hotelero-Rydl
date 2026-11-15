using System;
using System.Text.RegularExpressions;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>Resultados y bitácora comunes del menú Maestros.</summary>
    internal static class MaestrosComun
    {
        public static ResultadoOperacion<T> Denegado<T>(string operacion)
        {
            BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.MAESTROS_RECHAZO, 3,
                $"Acceso denegado: {operacion}.", "RECHAZADO");
            return ResultadoOperacion<T>.Fallo("No tenés permiso para esta operación.");
        }

        public static ResultadoOperacion<T> Rechazo<T>(string operacion, string motivo)
        {
            BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.MAESTROS_RECHAZO, 3,
                $"{operacion} rechazada: {motivo}", "RECHAZADO");
            return ResultadoOperacion<T>.Fallo(motivo);
        }

        public static ResultadoOperacion<T> Error<T>(string operacion, Exception ex)
        {
            BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.MAESTROS_ERROR, 1,
                $"{operacion}: {ex.Message}", "FALLIDO");
            return ResultadoOperacion<T>.Fallo("Ocurrió un error técnico. Volvé a intentar o avisá al administrador.");
        }

        public static string T(string? s) => (s ?? "").Trim();

        // ── Validaciones del cliente ──────────────────────────────────
        private static readonly Regex DNI = new Regex(@"^\d{7,8}$");
        private static readonly Regex NOMBRE = new Regex(@"^[\p{L}][\p{L} '\-.]{0,99}$");
        private static readonly Regex EMAIL = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex CELULAR = new Regex(@"^\+?[\d\s\-()]{6,25}$");

        /// <summary>Normaliza y valida un cliente. Devuelve null si está bien, o el motivo.</summary>
        public static string? ValidarCliente(ClienteBE c)
        {
            c.DNI = T(c.DNI).Replace(".", ""); c.Apellido = T(c.Apellido); c.Nombre = T(c.Nombre);
            c.Email = T(c.Email); c.Celular = T(c.Celular); c.Direccion = T(c.Direccion);
            if (!DNI.IsMatch(c.DNI)) return "El DNI debe tener 7 u 8 números.";
            if (!NOMBRE.IsMatch(c.Apellido)) return "Cargá un apellido válido (solo letras).";
            if (!NOMBRE.IsMatch(c.Nombre)) return "Cargá un nombre válido (solo letras).";
            if (c.Email.Length > 0 && (c.Email.Length > 200 || !EMAIL.IsMatch(c.Email))) return "El email no es válido.";
            if (c.Celular.Length > 0 && !CELULAR.IsMatch(c.Celular)) return "El celular no es válido.";
            if (c.Direccion.Length > 200) return "La dirección no puede superar los 200 caracteres.";
            return null;
        }
    }
}
