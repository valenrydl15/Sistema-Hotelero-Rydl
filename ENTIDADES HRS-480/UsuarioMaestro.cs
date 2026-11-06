namespace ENTIDADES_HRS_480
{
    /// <summary>
    /// Usuario maestro (superadministrador): el primer usuario del sistema (IdUsuario 1) y el rol Administrador.
    /// Ningún otro usuario —ni otro Administrador ni un supervisor con permiso de gestión— puede desactivarlo,
    /// cambiarle el rol, resetearle la clave ni modificarle los datos; el rol Administrador tampoco se puede
    /// modificar ni eliminar. Así el sistema nunca se queda sin un usuario con control total.
    /// </summary>
    public static class UsuarioMaestro
    {
        public const int ID = 1;
        public const string ROL = "Administrador";
        public const string MENSAJE = "El usuario maestro no puede ser modificado por otros usuarios.";
        public const string MENSAJE_ROL = "El rol Administrador (rol del usuario maestro) no puede modificarse ni eliminarse.";

        public static bool Es(int? idUsuario) => idUsuario == ID;
        public static bool EsRolMaestro(string? nombreRol) => string.Equals(nombreRol?.Trim(), ROL, System.StringComparison.OrdinalIgnoreCase);
    }
}
