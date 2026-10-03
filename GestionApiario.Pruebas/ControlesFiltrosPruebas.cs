using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;

namespace GestionApiario.Pruebas
{
    // Filtros opcionales de GET /controles/vertodos.
    //
    // Escenario (C1..C5 son controles):
    //   C1  Apiario A  campaña 2025  10/11/2025  Varroa
    //   C2  Apiario A  campaña 2026  01/03/2026  sin enfermedad
    //   C3  Apiario B  campaña 2026  15/03/2026  Nosemosis
    //   C4  Apiario B  campaña 2026  30/04/2026  Varroa
    //   C5  Apiario C  campaña 2026  01/04/2026  Varroa   (el apiario C está dado de baja)
    public class ControlesFiltrosPruebas : IDisposable
    {
        // xUnit crea una instancia de la clase por prueba, así que cada prueba tiene su propia API y su propia base.
        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Sin_filtros_devuelve_todos_los_controles_activos_ordenados_por_fecha()
        {
            var (cliente, e) = await PrepararAsync();

            var codigos = await ObtenerCodigosAsync(cliente, "");

            Assert.Equal([e.C4, e.C3, e.C2, e.C1], codigos);
        }

        [Fact]
        public async Task Filtra_por_apiario()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Equal([e.C2, e.C1], await ObtenerCodigosAsync(cliente, $"CodApiario={e.ApiarioA}"));
        }

        [Fact]
        public async Task Filtra_por_campaña_con_el_nombre_del_parametro_codificado()
        {
            var (cliente, e) = await PrepararAsync();

            var parametro = $"{Uri.EscapeDataString("CodCampaña")}={e.Campaña2025}";

            Assert.Equal([e.C1], await ObtenerCodigosAsync(cliente, parametro));
        }

        [Fact]
        public async Task Filtra_por_enfermedad()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Equal([e.C4, e.C1], await ObtenerCodigosAsync(cliente, $"CodEnfermedad={e.Varroa}"));
        }

        [Fact]
        public async Task Con_alguna_enfermedad_devuelve_los_controles_con_cualquier_enfermedad()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Equal([e.C4, e.C3, e.C1], await ObtenerCodigosAsync(cliente, "ConAlgunaEnfermedad=true"));
        }

        [Fact]
        public async Task El_rango_de_fechas_incluye_los_dos_extremos()
        {
            var (cliente, e) = await PrepararAsync();

            var codigos = await ObtenerCodigosAsync(cliente, "FechaDesde=2026-03-01&FechaHasta=2026-03-15");

            Assert.Equal([e.C3, e.C2], codigos);
        }

        [Fact]
        public async Task Filtra_solo_con_fecha_desde()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Equal([e.C4, e.C3], await ObtenerCodigosAsync(cliente, "FechaDesde=2026-03-15"));
        }

        [Fact]
        public async Task Filtra_solo_con_fecha_hasta()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Equal([e.C2, e.C1], await ObtenerCodigosAsync(cliente, "FechaHasta=2026-03-01"));
        }

        [Fact]
        public async Task Combina_varios_filtros()
        {
            var (cliente, e) = await PrepararAsync();

            var codigos = await ObtenerCodigosAsync(cliente,
                $"CodApiario={e.ApiarioB}&ConAlgunaEnfermedad=true&FechaDesde=2026-04-01&FechaHasta=2026-12-31");

            Assert.Equal([e.C4], codigos);
        }

        [Fact]
        public async Task Fecha_desde_posterior_a_hasta_responde_400()
        {
            var (cliente, _) = await PrepararAsync();

            var respuesta = await cliente.GetAsync("/controles/vertodos?FechaDesde=2026-05-01&FechaHasta=2026-04-01");

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains(FiltroControlesDto.MensajeRangoInvalido, await respuesta.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Fecha_mal_formada_responde_400()
        {
            var (cliente, _) = await PrepararAsync();

            var respuesta = await cliente.GetAsync("/controles/vertodos?FechaDesde=hola");

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }

        [Fact]
        public async Task Filtrar_por_un_apiario_dado_de_baja_no_muestra_sus_controles()
        {
            var (cliente, e) = await PrepararAsync();

            Assert.Empty(await ObtenerCodigosAsync(cliente, $"CodApiario={e.ApiarioC}"));
            Assert.DoesNotContain(e.C5, await ObtenerCodigosAsync(cliente, $"CodEnfermedad={e.Varroa}"));
        }

        private record Escenario(
            int ApiarioA, int ApiarioB, int ApiarioC,
            int Campaña2025, int Campaña2026,
            int Varroa, int Nosemosis,
            int C1, int C2, int C3, int C4, int C5);

        private async Task<(HttpClient Cliente, Escenario Escenario)> PrepararAsync()
        {
            var fabrica = _fabrica;
            var cliente = await fabrica.CrearClienteAutenticadoAsync();

            var campaña2025 = new Campaña { Año = 2025, FechaAlta = DateTime.Now };
            var campaña2026 = new Campaña { Año = 2026, FechaAlta = DateTime.Now };
            var apiarioA = new Apiario { Nombre = "Apiario A", FechaAlta = DateTime.Now };
            var apiarioB = new Apiario { Nombre = "Apiario B", FechaAlta = DateTime.Now };
            var apiarioC = new Apiario { Nombre = "Apiario C", FechaAlta = DateTime.Now };
            var varroa = new Enfermedad { Nombre = "Varroa", FechaAlta = DateTime.Now };
            var nosemosis = new Enfermedad { Nombre = "Nosemosis", FechaAlta = DateTime.Now };
            await fabrica.UsarBaseAsync(async contexto =>
            {
                contexto.AddRange(campaña2025, campaña2026, apiarioA, apiarioB, apiarioC, varroa, nosemosis);
                await contexto.SaveChangesAsync();
            });

            var c1 = await CrearControlAsync(cliente, apiarioA.Codigo, campaña2025.Codigo, new DateOnly(2025, 11, 10), varroa.Codigo);
            var c2 = await CrearControlAsync(cliente, apiarioA.Codigo, campaña2026.Codigo, new DateOnly(2026, 3, 1), 0);
            var c3 = await CrearControlAsync(cliente, apiarioB.Codigo, campaña2026.Codigo, new DateOnly(2026, 3, 15), nosemosis.Codigo);
            var c4 = await CrearControlAsync(cliente, apiarioB.Codigo, campaña2026.Codigo, new DateOnly(2026, 4, 30), varroa.Codigo);
            var c5 = await CrearControlAsync(cliente, apiarioC.Codigo, campaña2026.Codigo, new DateOnly(2026, 4, 1), varroa.Codigo);
            (await cliente.DeleteAsync($"/apiario/{apiarioC.Codigo}")).EnsureSuccessStatusCode();

            return (cliente, new Escenario(
                apiarioA.Codigo, apiarioB.Codigo, apiarioC.Codigo,
                campaña2025.Codigo, campaña2026.Codigo,
                varroa.Codigo, nosemosis.Codigo,
                c1, c2, c3, c4, c5));
        }

        private static async Task<int> CrearControlAsync(HttpClient cliente, int apiario, int campaña, DateOnly fecha, int enfermedad)
        {
            var respuesta = await cliente.PostAsJsonAsync("/controles", new ControlDto
            {
                CodApiario = apiario,
                CodCampaña = campaña,
                Fecha = fecha,
                CantDeColmenas = 10,
                CodEnfermedad = enfermedad
            });
            return await ApiarioPruebas.ObtenerCodigoCreadoAsync(respuesta);
        }

        // Devuelve los códigos en el orden en que los devolvió la API.
        private static async Task<List<int>> ObtenerCodigosAsync(HttpClient cliente, string query)
        {
            var url = string.IsNullOrEmpty(query) ? "/controles/vertodos" : $"/controles/vertodos?{query}";
            var respuesta = await cliente.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var controles = await respuesta.Content.ReadFromJsonAsync<List<ControlGrillaDto>>();
            return controles!.Select(c => c.Codigo).ToList();
        }
    }
}
