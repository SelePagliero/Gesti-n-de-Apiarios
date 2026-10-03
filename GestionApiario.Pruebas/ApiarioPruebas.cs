using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Pruebas
{
    public class ApiarioPruebas
    {
        [Fact]
        public async Task Modificar_guarda_latitud_y_longitud()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var alta = await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Los Talas", Latitud = "-31.4", Longitud = "-64.2" });
            var codigo = await ObtenerCodigoCreadoAsync(alta);

            var modificacion = await cliente.PutAsJsonAsync($"/apiario/{codigo}", new ApiarioDto { Nombre = "Los Talas", Latitud = "-32.9", Longitud = "-60.6" });
            modificacion.EnsureSuccessStatusCode();

            var apiario = await cliente.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{codigo}");
            Assert.Equal("-32.9", apiario!.Latitud);
            Assert.Equal("-60.6", apiario.Longitud);
        }

        [Fact]
        public async Task Alta_modificacion_y_baja_registran_el_usuario()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var alta = await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "El Ceibo" });
            var codigo = await ObtenerCodigoCreadoAsync(alta);

            (await cliente.PutAsJsonAsync($"/apiario/{codigo}", new ApiarioDto { Nombre = "El Ceibo II" })).EnsureSuccessStatusCode();
            (await cliente.DeleteAsync($"/apiario/{codigo}")).EnsureSuccessStatusCode();

            await fabrica.UsarBaseAsync(async contexto =>
            {
                var apiario = await contexto.Apiarios.SingleAsync(a => a.Codigo == codigo);
                Assert.Equal(FabricaApi.EmailPrueba, apiario.UsuarioAlta);
                Assert.Equal(FabricaApi.EmailPrueba, apiario.UsuarioModificacion);
                Assert.Equal(FabricaApi.EmailPrueba, apiario.UsuarioBaja);
                Assert.NotNull(apiario.FechaBaja);
            });
        }

        [Fact]
        public async Task Un_apiario_eliminado_no_aparece_en_la_lista_ni_se_puede_editar()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var codigo = await ObtenerCodigoCreadoAsync(await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Temporal" }));

            (await cliente.DeleteAsync($"/apiario/{codigo}")).EnsureSuccessStatusCode();

            var lista = await cliente.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos");
            Assert.DoesNotContain(lista!, a => a.Codigo == codigo);
            var edicion = await cliente.PutAsJsonAsync($"/apiario/{codigo}", new ApiarioDto { Nombre = "Otro" });
            Assert.Equal(HttpStatusCode.NotFound, edicion.StatusCode);
        }

        [Fact]
        public async Task Crear_sin_nombre_responde_400_con_el_mensaje_de_validacion()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();

            var respuesta = await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "" });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("El nombre es obligatorio.", await respuesta.Content.ReadAsStringAsync());
        }

        // Las altas responden 201 con la URL del registro nuevo (por ejemplo /Apiario/5).
        internal static Task<int> ObtenerCodigoCreadoAsync(HttpResponseMessage respuesta)
        {
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
            var ubicacion = respuesta.Headers.Location!.ToString();
            return Task.FromResult(int.Parse(ubicacion[(ubicacion.LastIndexOf('/') + 1)..]));
        }
    }
}
