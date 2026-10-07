using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;

namespace GestionApiario.Pruebas
{
    // Restablecer contraseñas (solo la Administradora), cambio obligatorio de la temporal y cambio de la propia.
    public class ContraseñasPruebas
    {
        private const string EmailApicultor = "apicultor@ejemplo.com";

        [Fact]
        public async Task Un_apicultor_no_puede_restablecer_contraseñas_ni_ver_la_grilla_de_usuarios()
        {
            using var fabrica = new FabricaApi();
            await fabrica.CrearClienteAutenticadoAsync();
            var apicultor = await fabrica.CrearApicultorAsync(EmailApicultor);
            await fabrica.CrearApicultorAsync("otro@ejemplo.com");
            var idOtro = await fabrica.ObtenerIdUsuarioAsync("otro@ejemplo.com");
            var idAdministradora = await fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            Assert.Equal(HttpStatusCode.Forbidden, (await apicultor.PostAsync($"/usuarios/{idOtro}/restablecer-contrasena", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await apicultor.PostAsync($"/usuarios/{idAdministradora}/restablecer-contrasena", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await apicultor.GetAsync("/usuarios/grilla")).StatusCode);

            // Las contraseñas no cambiaron.
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, "otro@ejemplo.com", FabricaApi.PasswordPrueba)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, FabricaApi.EmailPrueba, FabricaApi.PasswordPrueba)).StatusCode);
        }

        [Fact]
        public async Task Sin_iniciar_sesion_no_se_puede_restablecer()
        {
            using var fabrica = new FabricaApi();
            await fabrica.CrearApicultorAsync(EmailApicultor);
            var id = await fabrica.ObtenerIdUsuarioAsync(EmailApicultor);

            var respuesta = await fabrica.CreateClient().PostAsync($"/usuarios/{id}/restablecer-contrasena", null);

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task La_administradora_restablece_y_la_contraseña_anterior_deja_de_funcionar()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();
            await fabrica.CrearApicultorAsync(EmailApicultor);

            var temporal = await RestablecerAsync(fabrica, administradora, EmailApicultor);

            Assert.Equal(EmailApicultor, temporal.Email);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(fabrica, EmailApicultor, FabricaApi.PasswordPrueba)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, EmailApicultor, temporal.Contraseña)).StatusCode);

            var grilla = await administradora.GetFromJsonAsync<List<UsuarioGrillaDto>>("/usuarios/grilla");
            var fila = grilla!.Single(u => u.Email == EmailApicultor);
            Assert.True(fila.TieneContraseñaTemporal);
            Assert.False(fila.EsAdministrador);
            Assert.True(grilla!.Single(u => u.Email == FabricaApi.EmailPrueba).EsAdministrador);
        }

        [Fact]
        public async Task La_administradora_no_puede_restablecer_su_propia_contraseña_desde_usuarios()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();
            var id = await fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            var respuesta = await administradora.PostAsync($"/usuarios/{id}/restablecer-contrasena", null);

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }

        [Fact]
        public async Task Restablecer_un_usuario_que_no_existe_responde_404()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();

            var respuesta = await administradora.PostAsync($"/usuarios/{Guid.NewGuid()}/restablecer-contrasena", null);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }

        [Theory]
        [InlineData("GET", "/apiario/vertodos")]
        [InlineData("GET", "/controles/vertodos")]
        [InlineData("GET", "/dashBoard")]
        [InlineData("GET", "/cuenta/manage/info")]
        [InlineData("POST", "/apiario")]
        public async Task Con_la_contraseña_temporal_solo_puede_cambiarla(string metodo, string url)
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();
            await fabrica.CrearApicultorAsync(EmailApicultor);
            var temporal = await RestablecerAsync(fabrica, administradora, EmailApicultor);
            var apicultor = await IniciarSesionAsync(fabrica, EmailApicultor, temporal.Contraseña);

            var solicitud = new HttpRequestMessage(new HttpMethod(metodo), url);
            if (metodo == "POST")
                solicitud.Content = JsonContent.Create(new ApiarioDto { Nombre = "No debería crearse" });
            var respuesta = await apicultor.SendAsync(solicitud);

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Contains("temporal", await respuesta.Content.ReadAsStringAsync());
            var yo = await apicultor.GetFromJsonAsync<UsuarioActualDto>("/cuenta/yo");
            Assert.True(yo!.DebeCambiarContraseña);
        }

        [Fact]
        public async Task Al_cambiar_la_temporal_puede_usar_el_sistema_con_la_nueva()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();
            await fabrica.CrearApicultorAsync(EmailApicultor);
            var temporal = await RestablecerAsync(fabrica, administradora, EmailApicultor);
            var conTemporal = await IniciarSesionAsync(fabrica, EmailApicultor, temporal.Contraseña);

            var cambio = await conTemporal.PostAsJsonAsync("/cuenta/cambiar-contrasena", Cambio(temporal.Contraseña, "MielNueva2026"));
            Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);

            // La temporal ya no sirve; con la nueva entra y usa el sistema normalmente.
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(fabrica, EmailApicultor, temporal.Contraseña)).StatusCode);
            var apicultor = await IniciarSesionAsync(fabrica, EmailApicultor, "MielNueva2026");
            Assert.Equal(HttpStatusCode.OK, (await apicultor.GetAsync("/apiario/vertodos")).StatusCode);
            Assert.False((await apicultor.GetFromJsonAsync<UsuarioActualDto>("/cuenta/yo"))!.DebeCambiarContraseña);

            var grilla = await administradora.GetFromJsonAsync<List<UsuarioGrillaDto>>("/usuarios/grilla");
            Assert.False(grilla!.Single(u => u.Email == EmailApicultor).TieneContraseñaTemporal);
        }

        [Fact]
        public async Task Cualquier_usuario_puede_cambiar_su_propia_contraseña()
        {
            using var fabrica = new FabricaApi();
            var apicultor = await fabrica.CrearApicultorAsync(EmailApicultor);

            var cambio = await apicultor.PostAsJsonAsync("/cuenta/cambiar-contrasena", Cambio(FabricaApi.PasswordPrueba, "PanalNuevo99"));

            Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(fabrica, EmailApicultor, FabricaApi.PasswordPrueba)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, EmailApicultor, "PanalNuevo99")).StatusCode);
        }

        [Fact]
        public async Task La_administradora_también_puede_cambiar_su_propia_contraseña()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();

            var cambio = await administradora.PostAsJsonAsync("/cuenta/cambiar-contrasena", Cambio(FabricaApi.PasswordPrueba, "ReinaNueva2026"));

            Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, FabricaApi.EmailPrueba, "ReinaNueva2026")).StatusCode);
        }

        [Theory]
        [InlineData("Incorrecta123", "PanalNuevo99", "PanalNuevo99", "PasswordMismatch")]
        [InlineData(FabricaApi.PasswordPrueba, "PanalNuevo99", "PanalOtro99", "no coinciden")]
        [InlineData(FabricaApi.PasswordPrueba, FabricaApi.PasswordPrueba, FabricaApi.PasswordPrueba, "distinta de la actual")]
        [InlineData(FabricaApi.PasswordPrueba, "sinmayuscula1", "sinmayuscula1", "PasswordRequiresUpper")]
        [InlineData(FabricaApi.PasswordPrueba, "Corta1", "Corta1", "al menos 8 caracteres")]
        public async Task El_cambio_de_contraseña_se_rechaza_si_los_datos_no_son_válidos(
            string actual, string nueva, string confirmacion, string error)
        {
            using var fabrica = new FabricaApi();
            var apicultor = await fabrica.CrearApicultorAsync(EmailApicultor);

            var respuesta = await apicultor.PostAsJsonAsync("/cuenta/cambiar-contrasena",
                new CambioContraseñaDto { ContraseñaActual = actual, ContraseñaNueva = nueva, ConfirmacionContraseña = confirmacion });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains(error, await respuesta.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, EmailApicultor, FabricaApi.PasswordPrueba)).StatusCode);
        }

        [Fact]
        public async Task Sin_iniciar_sesion_no_se_puede_cambiar_la_contraseña()
        {
            using var fabrica = new FabricaApi();

            var respuesta = await fabrica.CreateClient().PostAsJsonAsync("/cuenta/cambiar-contrasena", Cambio("Colmena2026", "PanalNuevo99"));

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        [Fact]
        public async Task Restablecer_desbloquea_una_cuenta_bloqueada_por_intentos_fallidos()
        {
            using var fabrica = new FabricaApi();
            var administradora = await fabrica.CrearClienteAutenticadoAsync();
            await fabrica.CrearApicultorAsync(EmailApicultor);
            for (var i = 0; i < 5; i++)
                await LoginAsync(fabrica, EmailApicultor, "Incorrecta123");
            Assert.Contains("LockedOut", await (await LoginAsync(fabrica, EmailApicultor, FabricaApi.PasswordPrueba)).Content.ReadAsStringAsync());

            var temporal = await RestablecerAsync(fabrica, administradora, EmailApicultor);

            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fabrica, EmailApicultor, temporal.Contraseña)).StatusCode);
        }

        [Fact]
        public async Task Ya_no_existe_la_recuperación_por_correo()
        {
            using var fabrica = new FabricaApi();
            await fabrica.CrearApicultorAsync(EmailApicultor);

            var respuesta = await fabrica.CreateClient().PostAsJsonAsync("/cuenta/olvide-contrasena", new { email = EmailApicultor });

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }

        [Fact]
        public void Las_contraseñas_temporales_cumplen_las_reglas_y_no_se_repiten()
        {
            var generadas = Enumerable.Range(0, 500).Select(_ => ContraseñaTemporal.Generar()).ToList();

            Assert.All(generadas, c =>
            {
                Assert.Equal(12, c.Length);
                Assert.Contains(c, char.IsUpper);
                Assert.Contains(c, char.IsLower);
                Assert.Contains(c, char.IsDigit);
                Assert.DoesNotContain(c, x => "IlO01".Contains(x));
            });
            Assert.Equal(generadas.Count, generadas.Distinct().Count());
        }

        private static CambioContraseñaDto Cambio(string actual, string nueva) =>
            new() { ContraseñaActual = actual, ContraseñaNueva = nueva, ConfirmacionContraseña = nueva };

        private static async Task<ContraseñaTemporalDto> RestablecerAsync(FabricaApi fabrica, HttpClient administradora, string email)
        {
            var id = await fabrica.ObtenerIdUsuarioAsync(email);
            var respuesta = await administradora.PostAsync($"/usuarios/{id}/restablecer-contrasena", null);
            respuesta.EnsureSuccessStatusCode();
            return (await respuesta.Content.ReadFromJsonAsync<ContraseñaTemporalDto>())!;
        }

        private static Task<HttpResponseMessage> LoginAsync(FabricaApi fabrica, string email, string password) =>
            fabrica.CreateClient().PostAsJsonAsync("/cuenta/login", new { email, password });

        private static async Task<HttpClient> IniciarSesionAsync(FabricaApi fabrica, string email, string password)
        {
            var login = await LoginAsync(fabrica, email, password);
            login.EnsureSuccessStatusCode();
            var tokens = await login.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var cliente = fabrica.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!["accessToken"].ToString());
            return cliente;
        }
    }
}
