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

    // Respuesta de GET /cuenta/yo.
    public class UsuarioActualDto
    {
        public string Email { get; set; } = string.Empty;
        public bool EsAdministrador { get; set; }
    }
}
