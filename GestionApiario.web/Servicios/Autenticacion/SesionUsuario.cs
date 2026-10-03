namespace GestionApiario.web.Servicios.Autenticacion
{
    // Datos de la sesión que se guardan (cifrados) en el sessionStorage del navegador.
    public class SesionUsuario
    {
        public string Email { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        // Lo informa la API en GET /cuenta/yo. Solo decide qué muestra la web: los permisos los controla la API.
        public bool EsAdministrador { get; set; }
    }

    public class SesionExpiradaException : Exception
    {
        public SesionExpiradaException() : base("Tu sesión expiró. Volvé a iniciar sesión.") { }
    }
}
