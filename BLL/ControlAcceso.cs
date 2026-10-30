using System;
using System.Globalization;
using System.Linq;
using System.Text;
using SEGURIDAD_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// Regla de acceso al módulo de Recepción: solo Recepcionista y Administrador.
    /// Se valida en la BLL (no solo en el menú), así ninguna pantalla puede saltearla.
    /// </summary>
    public static class ControlAcceso
    {
        public const string ROL_ADMIN = "Administrador";
        public const string ROL_RECEPCIONISTA = "Recepcionista";

        public static bool EsAdministrador()
            => Normalizar(SesionManager.Instancia.UsuarioActual?.NombreRol) == Normalizar(ROL_ADMIN);

        public static bool PuedeOperarRecepcion()
        {
            string rol = Normalizar(SesionManager.Instancia.UsuarioActual?.NombreRol);
            return rol == Normalizar(ROL_ADMIN) || rol == Normalizar(ROL_RECEPCIONISTA);
        }

        // ── PN2: Reposición de insumos ────────────────────────────────────────
        public const string ROL_COMPRAS = "Encargado de Compras";
        public const string ROL_ALMACEN = "Encargado de Almacén";   // Encargado de Depósito

        private static bool RolEn(params string[] roles)
        {
            string actual = Normalizar(SesionManager.Instancia.UsuarioActual?.NombreRol);
            foreach (var r in roles)
                if (actual == Normalizar(r)) return true;
            return false;
        }

        /// <summary>Ver stock, órdenes y avisos: Administrador, Compras y Depósito (Almacén).</summary>
        public static bool PuedeAccederReposicion() => RolEn(ROL_ADMIN, ROL_COMPRAS, ROL_ALMACEN);
        /// <summary>CUN06 Realizar cotización: Encargado de Depósito.</summary>
        public static bool PuedeRealizarCotizacion() => RolEn(ROL_ADMIN, ROL_ALMACEN);
        /// <summary>CUN07 Generar pre-orden de compra: Encargado de Depósito.</summary>
        public static bool PuedeGenerarPreOrden() => RolEn(ROL_ADMIN, ROL_ALMACEN);
        /// <summary>CUN08 Registrar proveedor nuevo: Encargado de Compras.</summary>
        public static bool PuedeRegistrarProveedor() => RolEn(ROL_ADMIN, ROL_COMPRAS);
        /// <summary>CUN09 Realizar compra de insumos (pago + orden): Encargado de Compras.</summary>
        public static bool PuedeRealizarCompra() => RolEn(ROL_ADMIN, ROL_COMPRAS);
        /// <summary>CUN10 Registrar insumos recibidos: Encargado de Depósito.</summary>
        public static bool PuedeRegistrarRecepcionInsumos() => RolEn(ROL_ADMIN, ROL_ALMACEN);
        /// <summary>Tareas del área de Compras (reenviar una orden, ver pagos).</summary>
        public static bool PuedeGestionarCompras() => RolEn(ROL_ADMIN, ROL_COMPRAS);

        private static string Normalizar(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var d = s.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
    }
}
