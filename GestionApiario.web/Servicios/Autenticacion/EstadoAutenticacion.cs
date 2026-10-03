using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GestionApiario.web.Servicios.Autenticacion
{
    // Le informa a Blazor quién inició sesión y guarda los tokens de la API para el circuito actual.
    public class EstadoAutenticacion : AuthenticationStateProvider
    {
        private const string ClaveSesion = "sesion";

        private readonly ProtectedSessionStorage _almacenamiento;
        private Task<SesionUsuario?>? _cargaSesion;

        public EstadoAutenticacion(ProtectedSessionStorage almacenamiento)
        {
            _almacenamiento = almacenamiento;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var sesion = await ObtenerSesionAsync();
            if (sesion is null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var identidad = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, sesion.Email), new Claim(ClaimTypes.Email, sesion.Email)],
                authenticationType: "ApiGestionApiarios");
            return new AuthenticationState(new ClaimsPrincipal(identidad));
        }

        // La primera vez lee la sesión guardada en el navegador; después usa la que está en memoria.
        public Task<SesionUsuario?> ObtenerSesionAsync() => _cargaSesion ??= CargarSesionGuardadaAsync();

        public async Task GuardarSesionAsync(SesionUsuario sesion)
        {
            _cargaSesion = Task.FromResult<SesionUsuario?>(sesion);
            await _almacenamiento.SetAsync(ClaveSesion, sesion);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task CerrarSesionAsync()
        {
            _cargaSesion = Task.FromResult<SesionUsuario?>(null);
            try
            {
                await _almacenamiento.DeleteAsync(ClaveSesion);
            }
            catch (Exception)
            {
                // Si el navegador ya se desconectó no hay nada que borrar.
            }
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private async Task<SesionUsuario?> CargarSesionGuardadaAsync()
        {
            try
            {
                var resultado = await _almacenamiento.GetAsync<SesionUsuario>(ClaveSesion);
                return resultado.Success ? resultado.Value : null;
            }
            catch (Exception)
            {
                // Datos ilegibles (por ejemplo, de otra versión de la app): se ignora y se pide iniciar sesión.
                return null;
            }
        }
    }
}
