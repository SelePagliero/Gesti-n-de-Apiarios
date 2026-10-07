using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionApiario.Pruebas
{
    // Cada apicultor ve y modifica solo sus apiarios y controles; la Administradora ve y modifica todo,
    // y es la única que puede modificar catálogos y cambiar el dueño de un apiario.
    public class PermisosPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";
        private const string EmailBeto = "beto@ejemplo.com";

        // xUnit crea una instancia de la clase por prueba, así que cada prueba tiene su propia API y su propia base.
        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Cada_apicultor_ve_solo_sus_apiarios()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var apiarioAna = await CrearApiarioAsync(ana, "De Ana");
            var apiarioBeto = await CrearApiarioAsync(beto, "De Beto");

            var deAna = await ana.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos");
            var deBeto = await beto.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos");

            Assert.Equal([apiarioAna], deAna!.Select(a => a.Codigo));
            Assert.Equal([apiarioBeto], deBeto!.Select(a => a.Codigo));
        }

        [Fact]
        public async Task Un_apicultor_no_puede_ver_editar_ni_eliminar_apiarios_ajenos()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var apiarioBeto = await CrearApiarioAsync(beto, "De Beto");

            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/apiario/{apiarioBeto}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.PutAsJsonAsync($"/apiario/{apiarioBeto}", new ApiarioDto { Nombre = "Robado" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.DeleteAsync($"/apiario/{apiarioBeto}")).StatusCode);

            var apiario = await beto.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiarioBeto}");
            Assert.Equal("De Beto", apiario!.Nombre);
        }

        [Fact]
        public async Task Un_apicultor_no_puede_cargar_controles_en_apiarios_ajenos()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(ana);
            var apiarioBeto = await CrearApiarioAsync(beto, "De Beto");

            var respuesta = await ana.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = apiarioBeto });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("El apiario no existe.", await respuesta.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Un_apicultor_solo_ve_y_modifica_sus_controles()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(ana);
            var campañaBeto = await CrearCampañaAsync(beto);
            var apiarioAna = await CrearApiarioAsync(ana, "De Ana");
            var apiarioBeto = await CrearApiarioAsync(beto, "De Beto");
            var controlAna = await CrearControlAsync(ana, apiarioAna, campaña);
            var controlBeto = await CrearControlAsync(beto, apiarioBeto, campañaBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            // La grilla muestra solo lo propio, aunque se pidan explícitamente datos de otro.
            Assert.Equal([controlAna], await CodigosControlesAsync(ana, ""));
            Assert.Equal([controlAna], await CodigosControlesAsync(ana, $"UsuarioId={idBeto}"));
            Assert.Empty(await CodigosControlesAsync(ana, $"CodApiario={apiarioBeto}"));

            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/controles/{controlBeto}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.PutAsJsonAsync($"/controles/{controlBeto}",
                new ControlDto { CodCampaña = campaña, CodApiario = apiarioAna })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.DeleteAsync($"/controles/{controlBeto}")).StatusCode);

            // Tampoco puede pasar su propio control a un apiario ajeno.
            var mover = await ana.PutAsJsonAsync($"/controles/{controlAna}", new ControlDto { CodCampaña = campaña, CodApiario = apiarioBeto });
            Assert.Equal(HttpStatusCode.BadRequest, mover.StatusCode);

            Assert.Equal([controlBeto], await CodigosControlesAsync(beto, ""));
        }

        [Fact]
        public async Task El_tablero_muestra_a_cada_usuario_solo_sus_datos()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(ana);
            var campañaBeto = await CrearCampañaAsync(beto);
            var varroa = await CrearEnfermedadAsync("Varroa");
            await CrearControlAsync(ana, await CrearApiarioAsync(ana, "De Ana"), campaña, colmenas: 10, enfermedad: varroa);
            await CrearControlAsync(beto, await CrearApiarioAsync(beto, "De Beto"), campañaBeto, colmenas: 20);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            await VerificarTableroAsync(ana, "", apiarios: 1, colmenas: 10, conEnfermedad: 1, enfermedades: ["Varroa"]);
            await VerificarTableroAsync(beto, "", apiarios: 1, colmenas: 20, conEnfermedad: 0, enfermedades: []);
            // Un apicultor no puede pedir el tablero de otro: se ignora y ve el suyo.
            await VerificarTableroAsync(ana, $"?usuarioId={idBeto}", apiarios: 1, colmenas: 10, conEnfermedad: 1, enfermedades: ["Varroa"]);

            await VerificarTableroAsync(administradora, "", apiarios: 2, colmenas: 30, conEnfermedad: 1, enfermedades: ["Varroa"]);
            await VerificarTableroAsync(administradora, $"?usuarioId={idBeto}", apiarios: 1, colmenas: 20, conEnfermedad: 0, enfermedades: []);
        }

        [Theory]
        [InlineData("alimento")]
        [InlineData("enfermedad")]
        [InlineData("producto")]
        public async Task Un_apicultor_puede_consultar_los_catalogos_compartidos_pero_no_modificarlos(string ruta)
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(await administradora.PostAsJsonAsync($"/{ruta}", CuerpoCatalogo(ruta)));

            Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync($"/{ruta}/vertodos")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync($"/{ruta}/{codigo}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ana.PostAsJsonAsync($"/{ruta}", CuerpoCatalogo(ruta))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ana.PutAsJsonAsync($"/{ruta}/{codigo}", CuerpoCatalogo(ruta))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ana.DeleteAsync($"/{ruta}/{codigo}")).StatusCode);

            // El registro sigue activo.
            Assert.Equal(HttpStatusCode.OK, (await administradora.GetAsync($"/{ruta}/{codigo}")).StatusCode);
        }

        [Fact]
        public async Task La_administradora_ve_todo_y_filtra_por_apicultor()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(ana);
            var campañaBeto = await CrearCampañaAsync(beto);
            var controlAna = await CrearControlAsync(ana, await CrearApiarioAsync(ana, "De Ana"), campaña);
            var controlBeto = await CrearControlAsync(beto, await CrearApiarioAsync(beto, "De Beto"), campañaBeto);
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);

            var apiarios = await administradora.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos");
            Assert.Equal([EmailAna, EmailBeto], apiarios!.Select(a => a.Apicultor).Order());

            Assert.Equal([controlBeto, controlAna], await CodigosControlesAsync(administradora, ""));
            Assert.Equal([controlAna], await CodigosControlesAsync(administradora, $"UsuarioId={idAna}"));

            var grilla = await administradora.GetFromJsonAsync<List<ControlGrillaDto>>("/controles/vertodos");
            Assert.Equal(EmailAna, grilla!.Single(c => c.Codigo == controlAna).Apicultor);
        }

        [Fact]
        public async Task La_administradora_puede_editar_y_eliminar_datos_de_otros_apicultores()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(beto);
            var apiarioBeto = await CrearApiarioAsync(beto, "De Beto");
            var controlBeto = await CrearControlAsync(beto, apiarioBeto, campaña);

            (await administradora.PutAsJsonAsync($"/apiario/{apiarioBeto}", new ApiarioDto { Nombre = "Corregido" })).EnsureSuccessStatusCode();
            (await administradora.PutAsJsonAsync($"/controles/{controlBeto}",
                new ControlDto { CodCampaña = campaña, CodApiario = apiarioBeto, Observaciones = "Corregido" })).EnsureSuccessStatusCode();

            // Los cambios no le quitan el apiario a Beto.
            Assert.Equal("Corregido", (await beto.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiarioBeto}"))!.Nombre);
            Assert.Equal("Corregido", (await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{controlBeto}"))!.Observaciones);

            (await administradora.DeleteAsync($"/controles/{controlBeto}")).EnsureSuccessStatusCode();
            (await administradora.DeleteAsync($"/apiario/{apiarioBeto}")).EnsureSuccessStatusCode();
            Assert.Empty((await beto.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos"))!);
        }

        [Fact]
        public async Task La_administradora_puede_crear_un_apiario_a_nombre_de_otro_apicultor()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(
                await administradora.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Para Beto", UsuarioId = idBeto }));

            var apiario = await beto.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{codigo}");
            Assert.Equal(EmailBeto, apiario!.Apicultor);
        }

        [Fact]
        public async Task Transferir_un_apiario_pasa_sus_controles_al_nuevo_dueño()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await CrearCampañaAsync(ana);
            var apiario = await CrearApiarioAsync(ana, "De Ana");
            var control = await CrearControlAsync(ana, apiario, campaña, colmenas: 12);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "De Ana", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/apiario/{apiario}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/controles/{control}")).StatusCode);
            Assert.Equal(EmailBeto, (await beto.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.Apicultor);
            Assert.Equal([control], await CodigosControlesAsync(beto, ""));
            Assert.Equal(12, (await beto.GetFromJsonAsync<DashBoardDto>("/dashBoard"))!.CantidadTotalDeColmenas);
        }

        [Fact]
        public async Task Un_apicultor_no_puede_elegir_ni_cambiar_el_dueño()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);
            var apiario = await CrearApiarioAsync(ana, "De Ana");

            var alta = await ana.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Para Beto", UsuarioId = idBeto });
            var cambio = await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "De Ana", UsuarioId = idBeto });
            // Mandar su propio Id no es un cambio de dueño, así que se acepta.
            var mismoDueño = await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "De Ana", UsuarioId = idAna });

            Assert.Equal(HttpStatusCode.Forbidden, alta.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cambio.StatusCode);
            Assert.Equal(HttpStatusCode.OK, mismoDueño.StatusCode);
            Assert.Equal(EmailAna, (await ana.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.Apicultor);
        }

        [Fact]
        public async Task Transferir_a_un_usuario_inexistente_responde_400()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var apiario = await CrearApiarioAsync(administradora, "Propio");

            var respuesta = await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Propio", UsuarioId = "no-existe" });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("El apicultor no existe.", await respuesta.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Solo_la_administradora_puede_listar_usuarios()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);

            var usuarios = await administradora.GetFromJsonAsync<List<UsuarioDto>>("/usuarios");

            Assert.Equal([FabricaApi.EmailPrueba, EmailAna], usuarios!.Select(u => u.Email).Order());
            Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync("/usuarios")).StatusCode);
        }

        [Fact]
        public async Task Cuenta_yo_informa_si_el_usuario_es_administradora()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);

            var yoAdministradora = await administradora.GetFromJsonAsync<UsuarioActualDto>("/cuenta/yo");
            var yoAna = await ana.GetFromJsonAsync<UsuarioActualDto>("/cuenta/yo");

            Assert.True(yoAdministradora!.EsAdministrador);
            Assert.Equal(FabricaApi.EmailPrueba, yoAdministradora.Email);
            Assert.False(yoAna!.EsAdministrador);
            Assert.Equal(EmailAna, yoAna.Email);
        }

        [Fact]
        public async Task La_inicializacion_asigna_el_rol_y_los_apiarios_sin_dueño_y_se_puede_repetir()
        {
            await _fabrica.CrearApicultorAsync(EmailAna);
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);
            // Apiarios "viejos" sin dueño y uno que ya tiene dueño.
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                contexto.Apiarios.AddRange(
                    new Apiario { Nombre = "Viejo 1", FechaAlta = DateTime.Now },
                    new Apiario { Nombre = "Viejo 2", FechaAlta = DateTime.Now },
                    new Apiario { Nombre = "De Ana", FechaAlta = DateTime.Now, UsuarioId = idAna });
                await contexto.SaveChangesAsync();
            });

            await _fabrica.CrearClienteAutenticadoAsync(); // registra a la Administradora y ejecuta la inicialización
            await _fabrica.EjecutarInicializadorAsync();   // una segunda vez no cambia nada
            var idAdministradora = await _fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            await _fabrica.UsarBaseAsync(async contexto =>
            {
                var dueños = await contexto.Apiarios.ToDictionaryAsync(a => a.Nombre!, a => a.UsuarioId);
                Assert.Equal(idAdministradora, dueños["Viejo 1"]);
                Assert.Equal(idAdministradora, dueños["Viejo 2"]);
                Assert.Equal(idAna, dueños["De Ana"]);
                Assert.Equal(1, await contexto.Roles.CountAsync(r => r.Name == RolesUsuario.Administrador));
                Assert.Equal(1, await contexto.UserRoles.CountAsync());
            });

            using var alcance = _fabrica.Services.CreateScope();
            var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True(await usuarios.IsInRoleAsync((await usuarios.FindByEmailAsync(FabricaApi.EmailPrueba))!, RolesUsuario.Administrador));
            Assert.False(await usuarios.IsInRoleAsync((await usuarios.FindByEmailAsync(EmailAna))!, RolesUsuario.Administrador));
        }

        // ---- Ayudas ----

        internal static object CuerpoCatalogo(string ruta) =>
            ruta == "campaña" ? new CampañaDto { Año = 2026, Responsable = "Prueba" } : new { nombre = "Prueba" };

        internal static async Task<int> CrearApiarioAsync(HttpClient cliente, string nombre) =>
            await ApiarioPruebas.ObtenerCodigoCreadoAsync(await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = nombre }));

        internal static async Task<int> CrearControlAsync(HttpClient cliente, int apiario, int campaña, int colmenas = 10, int enfermedad = 0) =>
            await ApiarioPruebas.ObtenerCodigoCreadoAsync(await cliente.PostAsJsonAsync("/controles",
                new ControlDto { CodApiario = apiario, CodCampaña = campaña, CantDeColmenas = colmenas, CodEnfermedad = enfermedad }));

        // Crea la campaña desde la API: queda a nombre de quien la crea.
        internal static async Task<int> CrearCampañaAsync(HttpClient cliente, int año = 2026, string? responsable = "Responsable") =>
            await ApiarioPruebas.ObtenerCodigoCreadoAsync(await cliente.PostAsJsonAsync("/campaña", new CampañaDto { Año = año, Responsable = responsable }));

        private async Task<int> CrearEnfermedadAsync(string nombre)
        {
            var enfermedad = new Enfermedad { Nombre = nombre, FechaAlta = DateTime.Now };
            await _fabrica.UsarBaseAsync(async contexto => { contexto.Add(enfermedad); await contexto.SaveChangesAsync(); });
            return enfermedad.Codigo;
        }

        private static async Task<List<int>> CodigosControlesAsync(HttpClient cliente, string query)
        {
            var url = string.IsNullOrEmpty(query) ? "/controles/vertodos" : $"/controles/vertodos?{query}";
            var controles = await cliente.GetFromJsonAsync<List<ControlGrillaDto>>(url);
            return controles!.Select(c => c.Codigo).ToList();
        }

        private static async Task VerificarTableroAsync(HttpClient cliente, string query, int apiarios, int colmenas, int conEnfermedad, string[] enfermedades)
        {
            var tablero = await cliente.GetFromJsonAsync<DashBoardDto>($"/dashBoard{query}");
            Assert.Equal(apiarios, tablero!.ApiariosActivos);
            Assert.Equal(colmenas, tablero.CantidadTotalDeColmenas);
            Assert.Equal(conEnfermedad, tablero.ApiariosConEnfermedades);

            var grafico = await cliente.GetFromJsonAsync<EnfermedadesGraficoResponse>($"/dashBoard/graficoEnfermedades{query}");
            Assert.Equal(enfermedades, grafico!.labels);
        }
    }
}
