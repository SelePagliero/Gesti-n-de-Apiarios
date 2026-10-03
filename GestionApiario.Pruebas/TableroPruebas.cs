using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;

namespace GestionApiario.Pruebas
{
    // El tablero muestra el estado actual: el último control (por fecha) de cada apiario activo.
    public class TableroPruebas
    {
        private static readonly DateOnly Marzo = new(2026, 3, 10);
        private static readonly DateOnly Abril = new(2026, 4, 10);
        private static readonly DateOnly Mayo = new(2026, 5, 10);

        [Fact]
        public async Task Sin_controles_el_tablero_devuelve_ceros()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();

            var tablero = await ObtenerTableroAsync(cliente);

            Assert.Equal(0, tablero.CantidadTotalDeColmenas);
            Assert.Equal(0, tablero.ApiariosConEnfermedades);
        }

        [Fact]
        public async Task Las_colmenas_salen_del_ultimo_control_de_cada_apiario()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);

            // En el apiario A murieron colmenas entre marzo y mayo; el control de abril se cargó después.
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Mayo, colmenas: 15);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Abril, colmenas: 18);
            await CrearControlAsync(cliente, datos, datos.ApiarioB, Marzo, colmenas: 10);

            var tablero = await ObtenerTableroAsync(cliente);

            Assert.Equal(15 + 10, tablero.CantidadTotalDeColmenas);
        }

        [Fact]
        public async Task Dar_de_baja_un_apiario_descuenta_sus_colmenas()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20, enfermedad: datos.Varroa);
            await CrearControlAsync(cliente, datos, datos.ApiarioB, Marzo, colmenas: 10);

            (await cliente.DeleteAsync($"/apiario/{datos.ApiarioA}")).EnsureSuccessStatusCode();

            var tablero = await ObtenerTableroAsync(cliente);
            Assert.Equal(2, tablero.ApiariosActivos); // quedan B y C
            Assert.Equal(10, tablero.CantidadTotalDeColmenas);
            Assert.Equal(0, tablero.ApiariosConEnfermedades);
            var grafico = await ObtenerGraficoAsync(cliente);
            Assert.Empty(grafico.labels);
        }

        [Fact]
        public async Task Dar_de_baja_el_ultimo_control_vuelve_a_tomar_el_anterior()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20);
            var controlMayo = await CrearControlAsync(cliente, datos, datos.ApiarioA, Mayo, colmenas: 15);

            (await cliente.DeleteAsync($"/controles/{controlMayo}")).EnsureSuccessStatusCode();

            var tablero = await ObtenerTableroAsync(cliente);
            Assert.Equal(20, tablero.CantidadTotalDeColmenas);
        }

        [Fact]
        public async Task Con_dos_controles_de_la_misma_fecha_vale_el_ultimo_cargado()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 17);

            var tablero = await ObtenerTableroAsync(cliente);

            Assert.Equal(17, tablero.CantidadTotalDeColmenas);
        }

        [Fact]
        public async Task Una_enfermedad_curada_en_el_ultimo_control_no_se_cuenta()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20, enfermedad: datos.Varroa);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Mayo, colmenas: 20);

            var tablero = await ObtenerTableroAsync(cliente);
            Assert.Equal(0, tablero.ApiariosConEnfermedades);
            var grafico = await ObtenerGraficoAsync(cliente);
            Assert.Empty(grafico.labels);
        }

        [Fact]
        public async Task Enfermedades_y_grafico_usan_el_ultimo_control_de_cada_apiario()
        {
            using var fabrica = new FabricaApi();
            var cliente = await fabrica.CrearClienteAutenticadoAsync();
            var datos = await CargarDatosAsync(fabrica);

            // El apiario A tuvo Nosemosis en marzo y Varroa en mayo: cuenta solo Varroa, una vez.
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Marzo, colmenas: 20, enfermedad: datos.Nosemosis);
            await CrearControlAsync(cliente, datos, datos.ApiarioA, Mayo, colmenas: 18, enfermedad: datos.Varroa);
            await CrearControlAsync(cliente, datos, datos.ApiarioB, Marzo, colmenas: 10, enfermedad: datos.Varroa);
            await CrearControlAsync(cliente, datos, datos.ApiarioC, Abril, colmenas: 5, enfermedad: datos.Nosemosis);

            var tablero = await ObtenerTableroAsync(cliente);
            Assert.Equal(3, tablero.ApiariosConEnfermedades);

            var grafico = await ObtenerGraficoAsync(cliente);
            Assert.Equal(["Varroa", "Nosemosis"], grafico.labels);
            Assert.Equal([67, 33], grafico.datasets.Single().data);
        }

        private record DatosTablero(int Campaña, int ApiarioA, int ApiarioB, int ApiarioC, int Varroa, int Nosemosis);

        private static async Task<DatosTablero> CargarDatosAsync(FabricaApi fabrica)
        {
            var campaña = new Campaña { Año = 2026, FechaAlta = DateTime.Now };
            var apiarioA = new Apiario { Nombre = "Apiario A", FechaAlta = DateTime.Now };
            var apiarioB = new Apiario { Nombre = "Apiario B", FechaAlta = DateTime.Now };
            var apiarioC = new Apiario { Nombre = "Apiario C", FechaAlta = DateTime.Now };
            var varroa = new Enfermedad { Nombre = "Varroa", FechaAlta = DateTime.Now };
            var nosemosis = new Enfermedad { Nombre = "Nosemosis", FechaAlta = DateTime.Now };

            await fabrica.UsarBaseAsync(async contexto =>
            {
                contexto.AddRange(campaña, apiarioA, apiarioB, apiarioC, varroa, nosemosis);
                await contexto.SaveChangesAsync();
            });
            return new DatosTablero(campaña.Codigo, apiarioA.Codigo, apiarioB.Codigo, apiarioC.Codigo, varroa.Codigo, nosemosis.Codigo);
        }

        private static async Task<int> CrearControlAsync(HttpClient cliente, DatosTablero datos, int apiario, DateOnly fecha, int colmenas, int enfermedad = 0)
        {
            var respuesta = await cliente.PostAsJsonAsync("/controles", new ControlDto
            {
                CodCampaña = datos.Campaña,
                CodApiario = apiario,
                Fecha = fecha,
                CantDeColmenas = colmenas,
                CodEnfermedad = enfermedad
            });
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
            return await ApiarioPruebas.ObtenerCodigoCreadoAsync(respuesta);
        }

        private static async Task<DashBoardDto> ObtenerTableroAsync(HttpClient cliente) =>
            (await cliente.GetFromJsonAsync<DashBoardDto>("/dashBoard"))!;

        private static async Task<EnfermedadesGraficoResponse> ObtenerGraficoAsync(HttpClient cliente) =>
            (await cliente.GetFromJsonAsync<EnfermedadesGraficoResponse>("/dashBoard/graficoEnfermedades"))!;
    }
}
