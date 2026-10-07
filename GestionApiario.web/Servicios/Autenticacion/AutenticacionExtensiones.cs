using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Components.Authorization;

namespace GestionApiario.web.Servicios.Autenticacion
{
    public static class AutenticacionExtensiones
    {
        // Para decidir qué mostrar en pantalla. Los permisos reales los controla la API.
        public static async Task<bool> EsAdministradorAsync(this Task<AuthenticationState>? estado) =>
            estado is not null && (await estado).User.IsInRole(RolesUsuario.Administrador);
    }
}
