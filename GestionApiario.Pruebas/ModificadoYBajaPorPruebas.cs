using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Pruebas
{
    // "Modificado por" (UsuarioModificacion) y "dado de baja por" (UsuarioBaja) solo los ve la Administradora,
    // igual que "creado por". A un apicultor la API se los devuelve vacíos, aunque el registro sea suyo.
    public class ModificadoYBajaPorPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";
        private const string EmailBeto = "beto@ejemplo.com";

        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Si_la_administradora_modifica_algo_de_Ana_Ana_no_ve_su_email()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Corregido" })).EnsureSuccessStatusCode();
            (await administradora.PutAsJsonAsync($"/campaña/{campaña}", new CampañaDto { Año = 2026, Responsable = "Corregido" })).EnsureSuccessStatusCode();

            var apiarioAna = await ana.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}");
            var campañaAna = await ana.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}");
            Assert.Equal("Corregido", apiarioAna!.Nombre);
            Assert.Null(apiarioAna.UsuarioModificacion);
            Assert.Null(apiarioAna.UsuarioBaja);
            Assert.Null(campañaAna!.UsuarioModificacion);
            Assert.Null(campañaAna.UsuarioBaja);
            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync($"/apiario/{apiario}"));
            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync($"/campaña/{campaña}"));

            // La Administradora sí lo ve.
            Assert.Equal(FabricaApi.EmailPrueba, (await administradora.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioModificacion);
            Assert.Equal(FabricaApi.EmailPrueba, (await administradora.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}"))!.UsuarioModificacion);
        }

        [Fact]
        public async Task Un_apicultor_no_ve_el_modificado_por_ni_siquiera_de_sus_propios_cambios()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");

            (await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Cambiado por Ana" })).EnsureSuccessStatusCode();

            var detalle = await ana.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}");
            Assert.NotNull(detalle!.FechaModificacion);
            Assert.Null(detalle.UsuarioModificacion);
            // El dato se guarda igual: solo se oculta al responder.
            await _fabrica.UsarBaseAsync(async contexto =>
                Assert.Equal(EmailAna, (await contexto.Apiarios.SingleAsync(a => a.Codigo == apiario)).UsuarioModificacion));
        }

        [Fact]
        public async Task Despues_de_una_reasignacion_Beto_no_ve_quien_modifico_el_apiario()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "Norte");
            (await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Norte editado por Ana" })).EnsureSuccessStatusCode();

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Norte", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            var respuesta = await beto.GetStringAsync($"/apiario/{apiario}");
            Assert.DoesNotContain(EmailAna, respuesta);
            Assert.DoesNotContain(FabricaApi.EmailPrueba, respuesta);
        }

        [Theory]
        [InlineData("alimento")]
        [InlineData("enfermedad")]
        [InlineData("producto")]
        public async Task En_los_catalogos_el_apicultor_no_ve_quien_los_modifico(string ruta)
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(await administradora.PostAsJsonAsync($"/{ruta}", new { nombre = "Prueba" }));
            (await administradora.PutAsJsonAsync($"/{ruta}/{codigo}", new { nombre = "Prueba modificada" })).EnsureSuccessStatusCode();

            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync($"/{ruta}/{codigo}"));
            Assert.Contains(FabricaApi.EmailPrueba, await administradora.GetStringAsync($"/{ruta}/{codigo}"));
        }

        [Fact]
        public async Task Dado_de_baja_por_se_guarda_pero_ningun_apicultor_lo_puede_ver()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");
            var alimento = await ApiarioPruebas.ObtenerCodigoCreadoAsync(await administradora.PostAsJsonAsync("/alimento", new { nombre = "Jarabe" }));

            (await administradora.DeleteAsync($"/apiario/{apiario}")).EnsureSuccessStatusCode();
            (await administradora.DeleteAsync($"/alimento/{alimento}")).EnsureSuccessStatusCode();

            // Lo dado de baja no se devuelve: no hay forma de ver quién lo dio de baja.
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/apiario/{apiario}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/alimento/{alimento}")).StatusCode);
            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync("/apiario/vertodos"));
            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync("/alimento/vertodos"));

            // Pero queda registrado en la base.
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                Assert.Equal(FabricaApi.EmailPrueba, (await contexto.Apiarios.SingleAsync(a => a.Codigo == apiario)).UsuarioBaja);
                Assert.Equal(FabricaApi.EmailPrueba, (await contexto.Alimentos.SingleAsync(a => a.Codigo == alimento)).UsuarioBaja);
            });
        }
    }
}
