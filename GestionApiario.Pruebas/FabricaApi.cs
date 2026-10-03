using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionApiario.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionApiario.Pruebas
{
    // Levanta la API en memoria con una base de datos InMemory propia para cada instancia.
    public class FabricaApi : WebApplicationFactory<Program>
    {
        private readonly string _nombreBase = $"pruebas-{Guid.NewGuid()}";

        // La Administradora de las pruebas (se configura como "Administracion:Email").
        public const string EmailPrueba = "administradora@ejemplo.com";
        public const string PasswordPrueba = "Colmena2026";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs exige una cadena de conexión; en las pruebas se reemplaza por InMemory.
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=no-se-usa");
            builder.UseSetting("Administracion:Email", EmailPrueba);

            builder.ConfigureServices(servicios =>
            {
                var registroSqlServer = servicios.Single(s => s.ServiceType == typeof(DbContextOptions<GestionApiariosContext>));
                servicios.Remove(registroSqlServer);
                servicios.AddDbContext<GestionApiariosContext>(opciones => opciones.UseInMemoryDatabase(_nombreBase));
            });
        }

        // Ejecuta una acción sobre la base de datos (para cargar datos o verificar resultados).
        public async Task UsarBaseAsync(Func<GestionApiariosContext, Task> accion)
        {
            using var alcance = Services.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<GestionApiariosContext>();
            await accion(contexto);
        }

        // Ejecuta la inicialización (rol de Administradora y apiarios sin dueño), como al arrancar la API.
        public async Task EjecutarInicializadorAsync()
        {
            using var alcance = Services.CreateScope();
            await alcance.ServiceProvider.GetRequiredService<InicializadorDatos>().EjecutarAsync();
        }

        public async Task<string> ObtenerIdUsuarioAsync(string email)
        {
            using var alcance = Services.CreateScope();
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            return (await usuarios.FindByEmailAsync(email))!.Id;
        }

        // La Administradora con sesión iniciada. La usan las pruebas de reglas de negocio,
        // que necesitan ver todos los datos cargados directamente en la base.
        public async Task<HttpClient> CrearClienteAutenticadoAsync()
        {
            await RegistrarAsync(EmailPrueba);
            // El rol se asigna al arrancar la API; como la cuenta se registró después, se vuelve a ejecutar.
            await EjecutarInicializadorAsync();
            return await IniciarSesionAsync(EmailPrueba);
        }

        // Un apicultor común (sin rol de Administradora) con sesión iniciada.
        public async Task<HttpClient> CrearApicultorAsync(string email)
        {
            await RegistrarAsync(email);
            return await IniciarSesionAsync(email);
        }

        private async Task RegistrarAsync(string email)
        {
            var cliente = CreateClient();
            var registro = await cliente.PostAsJsonAsync("/cuenta/register", new { email, password = PasswordPrueba });
            registro.EnsureSuccessStatusCode();
        }

        private async Task<HttpClient> IniciarSesionAsync(string email)
        {
            var cliente = CreateClient();
            var login = await cliente.PostAsJsonAsync("/cuenta/login", new { email, password = PasswordPrueba });
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<RespuestaLogin>();

            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            return cliente;
        }

        private record RespuestaLogin(string AccessToken, string RefreshToken);
    }
}
