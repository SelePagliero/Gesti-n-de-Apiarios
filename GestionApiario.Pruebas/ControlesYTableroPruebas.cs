using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;

namespace GestionApiario.Pruebas
{
    public class ControlesYTableroPruebas
    {
        [Fact]
        public async Task Crear_control_con_apiario_inexistente_responde_400()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var (campaña, _, _) = await CargarDatosBaseAsync(fabrica);

            var respuesta = await cliente.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = 9999 });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("El apiario no existe.", await respuesta.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Crear_control_con_apiario_dado_de_baja_responde_400()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var (campaña, apiario, _) = await CargarDatosBaseAsync(fabrica);
            (await cliente.DeleteAsync($"/apiario/{apiario}")).EnsureSuccessStatusCode();

            var respuesta = await cliente.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = apiario });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }

        [Fact]
        public async Task Modificar_control_guarda_las_observaciones()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var (campaña, apiario, _) = await CargarDatosBaseAsync(fabrica);
            var alta = await cliente.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = apiario, Observaciones = "Inicial" });
            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(alta);

            var modificacion = await cliente.PutAsJsonAsync($"/controles/{codigo}",
                new ControlDto { CodCampaña = campaña, CodApiario = apiario, Observaciones = "Reina nueva" });
            modificacion.EnsureSuccessStatusCode();

            var control = await cliente.GetFromJsonAsync<ControlDetalleDto>($"/controles/{codigo}");
            Assert.Equal("Reina nueva", control!.Observaciones);
        }

        [Fact]
        public async Task La_grilla_oculta_los_controles_de_apiarios_dados_de_baja()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var (campaña, apiario, _) = await CargarDatosBaseAsync(fabrica);
            // Los datos cargados sin dueño pasan a la Administradora, como en la base real.
            await fabrica.EjecutarInicializadorAsync();
            var otroApiario = await ApiarioPruebas.ObtenerCodigoCreadoAsync(
                await cliente.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Sigue activo" }));
            var controlOculto = await ApiarioPruebas.ObtenerCodigoCreadoAsync(
                await cliente.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = apiario }));
            var controlVisible = await ApiarioPruebas.ObtenerCodigoCreadoAsync(
                await cliente.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campaña, CodApiario = otroApiario }));

            (await cliente.DeleteAsync($"/apiario/{apiario}")).EnsureSuccessStatusCode();

            var grilla = await cliente.GetFromJsonAsync<List<ControlGrillaDto>>("/controles/vertodos");
            Assert.DoesNotContain(grilla!, c => c.Codigo == controlOculto);
            Assert.Contains(grilla!, c => c.Codigo == controlVisible);
        }

        private static async Task<(int Campaña, int Apiario, int Enfermedad)> CargarDatosBaseAsync(FabricaApi fabrica)
        {
            var campaña = new Campaña { Año = 2026, FechaAlta = DateTime.Now };
            var apiario = new Apiario { Nombre = "La Colmena", FechaAlta = DateTime.Now };
            var enfermedad = new Enfermedad { Nombre = "Varroa", FechaAlta = DateTime.Now };

            await fabrica.UsarBaseAsync(async contexto =>
            {
                contexto.AddRange(campaña, apiario, enfermedad);
                await contexto.SaveChangesAsync();
            });
            return (campaña.Codigo, apiario.Codigo, enfermedad.Codigo);
        }
    }
}
