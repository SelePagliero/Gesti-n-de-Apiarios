using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionApiario.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionApiario.Pruebas
{
    // Levanta la API en memoria con una base de datos InMemory propia para cada instancia.
    public class FabricaApi : WebApplicationFactory<Program>
    {
        private readonly string _nombreBase = $"pruebas-{Guid.NewGuid()}";

        public const string EmailPrueba = "apicultor@ejemplo.com";
        public const string PasswordPrueba = "Colmena2026";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs exige una cadena de conexión; en las pruebas se reemplaza por InMemory.
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=no-se-usa");

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

        // Registra un usuario, inicia sesión y devuelve un cliente HTTP con el token puesto.
        public async Task<HttpClient> CrearClienteAutenticadoAsync()
        {
            var cliente = CreateClient();
            var registro = await cliente.PostAsJsonAsync("/cuenta/register", new { email = EmailPrueba, password = PasswordPrueba });
            registro.EnsureSuccessStatusCode();

            var login = await cliente.PostAsJsonAsync("/cuenta/login", new { email = EmailPrueba, password = PasswordPrueba });
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<RespuestaLogin>();

            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            return cliente;
        }

        private record RespuestaLogin(string AccessToken, string RefreshToken);
    }
}
