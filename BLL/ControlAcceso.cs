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

        // ── Entrega 3: menú MAESTROS ─────────────────────────────────────────
        /// <summary>CUN13 Maestro de Clientes (ABM + serializar/des-serializar XML): Administrador y Recepcionista.</summary>
        // Maestros (Clientes e Insumos): SOLO el Supervisor. El Administrador no accede.
        public static bool PuedeGestionarClientes() => RolEn(ROL_SUPERVISOR) && !EsAdministrador();

        // ── Encriptación reversible: datos sensibles de clientes (tarjeta) ─────────
        public const string ROL_SUPERVISOR = "Supervisor";
        public const string PERMISO_DATOS_SENSIBLES = "Ver datos sensibles de clientes";

        /// <summary>
        /// Ver descifrado el número de tarjeta de un cliente. Lo puede hacer quien tenga el permiso
        /// "Ver datos sensibles de clientes" en su perfil (Composite: directo o dentro de una familia),
        /// que por defecto es solo el Supervisor. El Administrador NUNCA, aunque se le asigne el permiso
        /// y aunque tenga acceso al Maestro de Clientes.
        /// </summary>
        public static bool PuedeVerDatosSensibles()
        {
            var u = SesionManager.Instancia.UsuarioActual;
            if (u == null || EsAdministrador()) return false;
            try
            {
                var perfil = new SERVICIOS_HRS_480.PerfilServicio().ObtenerRolCompleto(u.IdRol);
                return perfil != null && perfil.ObtenerPermisos()
                    .Any(p => Normalizar(p.Nombre) == Normalizar(PERMISO_DATOS_SENSIBLES));
            }
            catch { return false; }
        }
        /// <summary>Maestro de Insumos (alta, modificación, baja lógica): Administrador, Encargado de Compras y de Depósito.</summary>
        public static bool PuedeGestionarInsumos() => RolEn(ROL_SUPERVISOR) && !EsAdministrador();
        /// <summary>CUN14 Bitácora de cambios de insumos (consultar y activar versiones): solo Administrador.</summary>
        public static bool PuedeAuditarCambios() => RolEn(ROL_ADMIN);
        /// <summary>Ver el menú Maestros.</summary>
        /// <summary>A02 Reporte Inteligente Predictivo: Administrador, Recepcionista y Gerente de Compras.</summary>
        public static bool PuedeVerReportePredictivo() => RolEn(ROL_ADMIN, ROL_RECEPCIONISTA, ROL_GERENTE);
        public static bool PuedeAccederMaestros() => PuedeGestionarClientes() || PuedeGestionarInsumos() || PuedeAuditarCambios();

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
