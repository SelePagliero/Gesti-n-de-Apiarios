using System.Net.Http.Json;
using GestionApiario.compartido.Dto;

namespace GestionApiario.Pruebas
{
    // El email de quien creó un registro ("creado por", UsuarioAlta) solo lo ve la Administradora.
    // A un apicultor la API se lo devuelve vacío, aunque el registro sea suyo.
    public class CreadoPorPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";
        private const string EmailBeto = "beto@ejemplo.com";

        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Un_apicultor_no_ve_el_creado_por_y_la_administradora_si()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");

            Assert.Null((await ana.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioAlta);
            Assert.Null((await ana.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}"))!.UsuarioAlta);

            Assert.Equal(EmailAna, (await administradora.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioAlta);
            Assert.Equal(EmailAna, (await administradora.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}"))!.UsuarioAlta);
        }

        [Fact]
        public async Task Despues_de_una_reasignacion_el_nuevo_dueño_no_ve_el_email_del_anterior()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "Norte");
            await PermisosPruebas.CrearControlAsync(ana, apiario, campaña);

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Norte", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            // Ni en el detalle del apiario ni en el de la campaña copiada aparece el email de Ana para Beto.
            var respuestaApiario = await beto.GetStringAsync($"/apiario/{apiario}");
            Assert.DoesNotContain(EmailAna, respuestaApiario);
            var campañaBeto = Assert.Single((await beto.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos"))!);
            Assert.DoesNotContain(EmailAna, await beto.GetStringAsync($"/campaña/{campañaBeto.Codigo}"));

            // La Administradora sí ve quién lo creó.
            Assert.Equal(EmailAna, (await administradora.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioAlta);
        }

        [Theory]
        [InlineData("alimento")]
        [InlineData("enfermedad")]
        [InlineData("producto")]
        public async Task En_los_catalogos_el_apicultor_no_ve_quien_los_creo(string ruta)
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(await administradora.PostAsJsonAsync($"/{ruta}", new { nombre = "Prueba" }));

            Assert.DoesNotContain(FabricaApi.EmailPrueba, await ana.GetStringAsync($"/{ruta}/{codigo}"));
            Assert.Contains(FabricaApi.EmailPrueba, await administradora.GetStringAsync($"/{ruta}/{codigo}"));
        }
    }
}
