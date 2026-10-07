using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Components.Authorization;

namespace GestionApiario.web.Servicios.Autenticacion
{
    public static class AutenticacionExtensiones
    {
        // Lo agrega EstadoAutenticacion cuando la API informa que el usuario tiene una contraseña temporal.
        public const string ClaimContraseñaTemporal = "contrasena_temporal";

        public const string PaginaContraseñaTemporal = "contrasena-temporal";

        public static bool TieneContraseñaTemporal(this System.Security.Claims.ClaimsPrincipal usuario) =>
            usuario.HasClaim(c => c.Type == ClaimContraseñaTemporal);

        // Para decidir qué mostrar en pantalla. Los permisos reales los controla la API.
        public static async Task<bool> EsAdministradorAsync(this Task<AuthenticationState>? estado) =>
            estado is not null && (await estado).User.IsInRole(RolesUsuario.Administrador);
    }
}
