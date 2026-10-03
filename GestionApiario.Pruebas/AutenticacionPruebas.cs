using System.Net;
using System.Net.Http.Json;

namespace GestionApiario.Pruebas
{
    public class AutenticacionPruebas
    {
        [Theory]
        [InlineData("/apiario/vertodos")]
        [InlineData("/controles/vertodos")]
        [InlineData("/dashBoard")]
        public async Task Sin_iniciar_sesion_la_api_responde_401(string url)
        {
            using var fabrica = new FabricaApi();
            var cliente = fabrica.CreateClient();

            var respuesta = await cliente.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task Con_sesion_iniciada_la_api_responde_200()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();

            var respuesta = await cliente.GetAsync("/apiario/vertodos");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }

        [Fact]
        public async Task Login_con_contraseña_incorrecta_responde_401()
        {
            using var fabrica = new FabricaApi();
            await fabrica.CrearClienteAutenticadoAsync();
            var cliente = fabrica.CreateClient();

            var respuesta = await cliente.PostAsJsonAsync("/cuenta/login", new { email = FabricaApi.EmailPrueba, password = "Incorrecta123" });

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task El_registro_rechaza_contraseñas_cortas()
        {
            using var fabrica = new FabricaApi();
            var cliente = fabrica.CreateClient();

            var respuesta = await cliente.PostAsJsonAsync("/cuenta/register", new { email = "otro@ejemplo.com", password = "Ab1" });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("PasswordTooShort", await respuesta.Content.ReadAsStringAsync());
        }
    }
}
