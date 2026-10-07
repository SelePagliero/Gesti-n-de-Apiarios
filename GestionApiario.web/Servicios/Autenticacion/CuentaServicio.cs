using System.Net;
using System.Net.Http.Headers;
using GestionApiario.compartido.Dto;
using System.Text.Json;

namespace GestionApiario.web.Servicios.Autenticacion
{
    // Llama a los endpoints /cuenta de la API (ASP.NET Core Identity).
    public class CuentaServicio
    {
        private readonly HttpClient _httpClient;
        private readonly EstadoAutenticacion _estado;

        public CuentaServicio(HttpClient httpClient, EstadoAutenticacion estado)
        {
            _httpClient = httpClient;
            _estado = estado;
        }

        // Devuelve null si el ingreso fue correcto, o el mensaje de error a mostrar.
        public async Task<string?> IniciarSesionAsync(string email, string password)
        {
            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _httpClient.PostAsJsonAsync("cuenta/login", new { email, password });
            }
            catch (HttpRequestException)
            {
                return ErroresApi.SinConexion;
            }

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                var detalle = await respuesta.Content.ReadAsStringAsync();
                return detalle.Contains("LockedOut")
                    ? "La cuenta está bloqueada por demasiados intentos fallidos. Probá de nuevo en unos minutos."
                    : "El email o la contraseña son incorrectos.";
            }
            if (!respuesta.IsSuccessStatusCode)
                return await ErroresApi.LeerMensajeAsync(respuesta);

            var tokens = await respuesta.Content.ReadFromJsonAsync<RespuestaTokens>();
            if (tokens is null)
                return "La API devolvió una respuesta inesperada.";

            var usuario = await ConsultarUsuarioAsync(tokens.AccessToken);
            if (usuario is null)
                return "La API devolvió una respuesta inesperada.";

            await _estado.GuardarSesionAsync(new SesionUsuario
            {
                Email = usuario.Email,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                EsAdministrador = usuario.EsAdministrador
            });
            return null;
        }

        // Devuelve null si el registro fue correcto, o el mensaje de error a mostrar.
        public async Task<string?> RegistrarAsync(string email, string password)
        {
            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _httpClient.PostAsJsonAsync("cuenta/register", new { email, password });
            }
            catch (HttpRequestException)
            {
                return ErroresApi.SinConexion;
            }

            if (respuesta.IsSuccessStatusCode)
                return null;

            // Identity devuelve los errores con un código por cada problema (por ejemplo "PasswordTooShort").
            var contenido = await respuesta.Content.ReadAsStringAsync();
            var mensajes = new List<string>();
            try
            {
                using var json = JsonDocument.Parse(contenido);
                if (json.RootElement.TryGetProperty("errors", out var errores))
                {
                    foreach (var error in errores.EnumerateObject())
                        mensajes.Add(TraducirErrorIdentity(error.Name, error.Value));
                }
            }
            catch (JsonException)
            {
            }

            return mensajes.Count > 0 ? string.Join(" ", mensajes.Distinct()) : await ErroresApi.LeerMensajeAsync(respuesta);
        }

        // Pide un token nuevo con el refresh token. Devuelve false si la sesión ya no es válida.
        public async Task<bool> RenovarTokenAsync()
        {
            var sesion = await _estado.ObtenerSesionAsync();
            if (sesion is null || string.IsNullOrEmpty(sesion.RefreshToken))
                return false;

            var respuesta = await _httpClient.PostAsJsonAsync("cuenta/refresh", new { refreshToken = sesion.RefreshToken });
            if (!respuesta.IsSuccessStatusCode)
                return false;

            var tokens = await respuesta.Content.ReadFromJsonAsync<RespuestaTokens>();
            if (tokens is null)
                return false;

            // Se vuelve a consultar el rol: si cambió, la web lo refleja sin tener que cerrar sesión.
            var usuario = await ConsultarUsuarioAsync(tokens.AccessToken);
            if (usuario is null)
                return false;

            await _estado.GuardarSesionAsync(new SesionUsuario
            {
                Email = usuario.Email,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                EsAdministrador = usuario.EsAdministrador
            });
            return true;
        }

        public Task CerrarSesionAsync() => _estado.CerrarSesionAsync();

        // GET /cuenta/yo con el token recién obtenido: devuelve el email y si es la Administradora.
        private async Task<UsuarioActualDto?> ConsultarUsuarioAsync(string accessToken)
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, "cuenta/yo");
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var respuesta = await _httpClient.SendAsync(solicitud);
            return respuesta.IsSuccessStatusCode ? await respuesta.Content.ReadFromJsonAsync<UsuarioActualDto>() : null;
        }

        private static string TraducirErrorIdentity(string codigo, JsonElement mensajesOriginales) => codigo switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese email.",
            "InvalidEmail" or "InvalidUserName" => "El email no es válido.",
            "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
            "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
            "PasswordRequiresLower" => "La contraseña debe tener al menos una letra minúscula.",
            "PasswordRequiresUpper" => "La contraseña debe tener al menos una letra mayúscula.",
            "PasswordRequiresUniqueChars" => "La contraseña debe tener más caracteres distintos.",
            _ => string.Join(" ", mensajesOriginales.EnumerateArray().Select(m => m.GetString()))
        };

        private class RespuestaTokens
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
