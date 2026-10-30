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
        public const string ROL_GERENTE = "Gerente de Compras";
        public const string ROL_DEPOSITO = "Encargado de Depósito";
        private const string ROL_DEPOSITO_ANTERIOR = "Encargado de Almacén";   // nombre viejo del rol (bases sin migrar)

        private static bool RolEn(params string[] roles)
        {
            string actual = Normalizar(SesionManager.Instancia.UsuarioActual?.NombreRol);
            foreach (var r in roles)
                if (actual == Normalizar(r)) return true;
            return false;
        }
        private static bool EsDeposito() => RolEn(ROL_DEPOSITO, ROL_DEPOSITO_ANTERIOR);

        /// <summary>Entrar a Reposición (órdenes y avisos): Administrador, Compras, Gerente y Depósito.</summary>
        public static bool PuedeAccederReposicion() => RolEn(ROL_ADMIN, ROL_COMPRAS, ROL_GERENTE) || EsDeposito();
        /// <summary>Ver stock e insumos faltantes.</summary>
        public static bool PuedeConsultarStock() => RolEn(ROL_ADMIN, ROL_COMPRAS, ROL_GERENTE) || EsDeposito();
        /// <summary>CUN06 Generar solicitud de cotización y CUN07 Pre-registrar proveedor: Encargado de Compras.</summary>
        public static bool PuedeGenerarSolicitudCotizacion() => RolEn(ROL_ADMIN, ROL_COMPRAS);
        public static bool PuedePreRegistrarProveedor() => RolEn(ROL_ADMIN, ROL_COMPRAS);
        /// <summary>CUN08 Generar pre-orden de compra (incluye registrar las cotizaciones recibidas): Encargado de Compras.</summary>
        public static bool PuedeGenerarPreOrden() => RolEn(ROL_ADMIN, ROL_COMPRAS);
        /// <summary>CUN09 Generar orden de compra, CUN10 Registrar proveedor y CUN11 Pagar orden: Gerente de Compras.</summary>
        public static bool PuedeGenerarOrden() => RolEn(ROL_ADMIN, ROL_GERENTE);
        public static bool PuedeRegistrarProveedor() => RolEn(ROL_ADMIN, ROL_GERENTE);
        public static bool PuedePagarOrden() => RolEn(ROL_ADMIN, ROL_GERENTE);
        /// <summary>CUN12 Registrar recepción de insumos: Encargado de Depósito.</summary>
        public static bool PuedeRegistrarRecepcionInsumos() => RolEn(ROL_ADMIN) || EsDeposito();

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
