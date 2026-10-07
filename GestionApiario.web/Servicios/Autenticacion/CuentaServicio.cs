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

            await GuardarSesionAsync(tokens, usuario);
            return null;
        }

        // Cambia la contraseña del usuario que inició sesión. Devuelve null si salió bien, o el mensaje de error.
        // Después vuelve a ingresar con la contraseña nueva, porque el cambio invalida el token de renovación anterior.
        public async Task<string?> CambiarContraseñaAsync(CambioContraseñaDto cambio)
        {
            var sesion = await _estado.ObtenerSesionAsync();
            if (sesion is null)
                return new SesionExpiradaException().Message;

            HttpResponseMessage respuesta;
            try
            {
                respuesta = await EnviarCambioContraseñaAsync(cambio, sesion.AccessToken);

                // Si el token venció se renueva una sola vez y se reintenta.
                if (respuesta.StatusCode == HttpStatusCode.Unauthorized && await RenovarTokenAsync())
                {
                    respuesta.Dispose();
                    sesion = await _estado.ObtenerSesionAsync();
                    respuesta = await EnviarCambioContraseñaAsync(cambio, sesion!.AccessToken);
                }
            }
            catch (HttpRequestException)
            {
                return ErroresApi.SinConexion;
            }

            using (respuesta)
            {
                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await CerrarSesionAsync();
                    return new SesionExpiradaException().Message;
                }
                if (!respuesta.IsSuccessStatusCode)
                    return await LeerErroresIdentityAsync(respuesta);
            }

            return await IniciarSesionAsync(sesion.Email, cambio.ContraseñaNueva);
        }

        // Pide que se envíe por correo el link para elegir una contraseña nueva.
        // Devuelve null si el pedido se aceptó (la API no dice si el email existe), o el mensaje de error.
        public async Task<string?> SolicitarRecuperacionAsync(string email)
        {
            try
            {
                using var respuesta = await _httpClient.PostAsJsonAsync("cuenta/olvide-contrasena", new OlvideContraseñaDto { Email = email });
                return respuesta.IsSuccessStatusCode ? null : await ErroresApi.LeerMensajeAsync(respuesta);
            }
            catch (HttpRequestException)
            {
                return ErroresApi.SinConexion;
            }
        }

        // Guarda la contraseña nueva con el código del link. Devuelve null si salió bien, o el mensaje de error.
        public async Task<string?> RestablecerContraseñaAsync(RestablecimientoContraseñaDto restablecimiento)
        {
            try
            {
                using var respuesta = await _httpClient.PostAsJsonAsync("cuenta/restablecer-contrasena", restablecimiento);
                return respuesta.IsSuccessStatusCode ? null : await LeerErroresIdentityAsync(respuesta);
            }
            catch (HttpRequestException)
            {
                return ErroresApi.SinConexion;
            }
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

            return await LeerErroresIdentityAsync(respuesta);
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

            await GuardarSesionAsync(tokens, usuario);
            return true;
        }

        public Task CerrarSesionAsync() => _estado.CerrarSesionAsync();

        private Task GuardarSesionAsync(RespuestaTokens tokens, UsuarioActualDto usuario) =>
            _estado.GuardarSesionAsync(new SesionUsuario
            {
                Email = usuario.Email,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                EsAdministrador = usuario.EsAdministrador
            });

        private async Task<HttpResponseMessage> EnviarCambioContraseñaAsync(CambioContraseñaDto cambio, string accessToken)
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, "cuenta/cambiar-contrasena")
            {
                Content = JsonContent.Create(cambio)
            };
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await _httpClient.SendAsync(solicitud);
        }

        // Identity devuelve los errores con un código por cada problema (por ejemplo "PasswordTooShort").
        private static async Task<string> LeerErroresIdentityAsync(HttpResponseMessage respuesta)
        {
            var contenido = await respuesta.Content.ReadAsStringAsync();
            var mensajes = new List<string>();
            try
            {
                using var json = JsonDocument.Parse(contenido);
                if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("errors", out var errores))
                {
                    foreach (var error in errores.EnumerateObject())
                        mensajes.Add(ErroresApi.TraducirErrorIdentity(error.Name, error.Value));
                }
            }
            catch (JsonException)
            {
            }

            return mensajes.Count > 0 ? string.Join(" ", mensajes.Distinct()) : await ErroresApi.LeerMensajeAsync(respuesta);
        }

        // GET /cuenta/yo con el token recién obtenido: devuelve el email y si es la Administradora.
        private async Task<UsuarioActualDto?> ConsultarUsuarioAsync(string accessToken)
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, "cuenta/yo");
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var respuesta = await _httpClient.SendAsync(solicitud);
            return respuesta.IsSuccessStatusCode ? await respuesta.Content.ReadFromJsonAsync<UsuarioActualDto>() : null;
        }

        private class RespuestaTokens
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
