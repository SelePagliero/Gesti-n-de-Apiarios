using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;

namespace GestionApiario.PruebasE2E
{
    // Llamadas directas a la API de prueba, para preparar datos rápido y verificar lo que no se ve en pantalla.
    public class ClienteApi
    {
        private readonly string _url;

        public ClienteApi(string url) => _url = url;

        public async Task RegistrarAsync(string email)
        {
            using var http = new HttpClient { BaseAddress = new Uri(_url) };
            var respuesta = await http.PostAsJsonAsync("/cuenta/register", new { email, password = EntornoE2E.Password });
            respuesta.EnsureSuccessStatusCode();
        }

        // Cliente HTTP con la sesión iniciada de ese usuario.
        public async Task<HttpClient> ComoAsync(string email)
        {
            var http = new HttpClient { BaseAddress = new Uri(_url) };
            var login = await http.PostAsJsonAsync("/cuenta/login", new { email, password = EntornoE2E.Password });
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<RespuestaLogin>();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            return http;
        }

        public static async Task<int> CrearAsync(HttpClient cliente, string ruta, object cuerpo)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, cuerpo);
            respuesta.EnsureSuccessStatusCode();
            var ubicacion = respuesta.Headers.Location!.ToString();
            return int.Parse(ubicacion[(ubicacion.LastIndexOf('/') + 1)..]);
        }

        public static Task<int> CrearCampañaAsync(HttpClient cliente, int año, string responsable) =>
            CrearAsync(cliente, "/campaña", new CampañaDto { Año = año, Responsable = responsable });

        public static Task<int> CrearApiarioAsync(HttpClient cliente, string nombre) =>
            CrearAsync(cliente, "/apiario", new ApiarioDto { Nombre = nombre });

        public static Task<int> CrearControlAsync(HttpClient cliente, int apiario, int campaña, int colmenas, int enfermedad = 0) =>
            CrearAsync(cliente, "/controles", new ControlDto { CodApiario = apiario, CodCampaña = campaña, CantDeColmenas = colmenas, CodEnfermedad = enfermedad });

        private record RespuestaLogin(string AccessToken, string RefreshToken);
    }
}
