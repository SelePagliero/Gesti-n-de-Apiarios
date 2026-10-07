namespace GestionApiario.compartido.Dto
{
    public static class RolesUsuario
    {
        public const string Administrador = "Administrador";
    }

    // Un apicultor de la lista GET /usuarios (solo para la Administradora).
    public class UsuarioDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Una fila de la pantalla Usuarios (GET /usuarios/grilla, solo para la Administradora).
    public class UsuarioGrillaDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EsAdministrador { get; set; }
        public int CantidadApiarios { get; set; }
    }

    // Respuesta de GET /cuenta/yo.
    public class UsuarioActualDto
    {
        public string Email { get; set; } = string.Empty;
        public bool EsAdministrador { get; set; }
    }
}
