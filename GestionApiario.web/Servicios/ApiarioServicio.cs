using GestionApiario.compartido.Dto;
using GestionApiario.web.Servicios.Interfaces;

namespace GestionApiario.web.Servicios
{
    public class ApiarioServicio : IApiariosServicio
    {
        private readonly HttpClient _httpClient;
        public ApiarioServicio(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task CrearAlimento(AlimentoDto alimento)
        {
            await _httpClient.PostAsJsonAsync<AlimentoDto>("alimento", alimento);
        }

        public async Task CrearApiario(ApiarioDto apiario)
        {
            await _httpClient.PostAsJsonAsync<ApiarioDto>("apiario", apiario);
        }

        public async Task CrearCampaña(CampañaDto campaña)
        {
            await _httpClient.PostAsJsonAsync<CampañaDto>("campaña", campaña);
        }

        public async Task CrearControl(ControlDto control)
        {
            await _httpClient.PostAsJsonAsync<ControlDto>("controles", control);
        }

        public async Task CrearEnfermedad(EnfermedadDto enfermedad)
        {
            await _httpClient.PostAsJsonAsync<EnfermedadDto>("enfermedad", enfermedad);
        }

        public async Task CrearProducto(ProductoDto producto)
        {
            await _httpClient.PostAsJsonAsync<ProductoDto>("producto", producto);
        }

        public async Task EliminarAlimento(int codigo)
        {
            await _httpClient.DeleteAsync($"alimento/{codigo}");
        }

        public async Task EliminarApiario(int codigo)
        {
            await _httpClient.DeleteAsync($"apiario/{codigo}");
        }

        public async Task EliminarCampaña(int codigo)
        {
            await _httpClient.DeleteAsync($"campaña/{codigo}");
        }

        public async Task EliminarControl(int codigo)
        {
            await _httpClient.DeleteAsync($"controles/{codigo}");
        }

        public async Task EliminarEnfermedad(int codigo)
        {
            await _httpClient.DeleteAsync($"enfermedad/{codigo}");
        }

        public async Task EliminarProducto(int codigo)
        {
            await _httpClient.DeleteAsync($"producto/{codigo}");
        }

        public async Task ModificarAlimento(int codigo, AlimentoDto alimento)
        {
            await _httpClient.PutAsJsonAsync<AlimentoDto>($"alimento/{codigo}", alimento);
        }

        public async Task ModificarApiario(int codigo, ApiarioDto apiario)
        {
            await _httpClient.PutAsJsonAsync<ApiarioDto>($"apiario/{codigo}", apiario);
            
        }

        public async Task ModificarCamapaña(int codigo, CampañaDto campaña)
        {
            await _httpClient.PutAsJsonAsync<CampañaDto>($"campaña/{codigo}", campaña);
        }

        public async Task ModificarControl(int codigo, ControlDto control)
        {
            await _httpClient.PutAsJsonAsync<ControlDto>($"controles/{codigo}", control);
        }

        public async Task ModificarEnfermedad(int codigo,EnfermedadDto enfermedad)
        {
            await _httpClient.PutAsJsonAsync<EnfermedadDto>($"enfermedad/{codigo}", enfermedad);
        }

        public async Task ModificarProducto(int codigo, ProductoDto producto)
        {
            await _httpClient.PutAsJsonAsync<ProductoDto>($"producto/{codigo}", producto);
        }

        public async Task<AlimentoDetalleDto> ObtenerAlimento(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<AlimentoDetalleDto>($"alimento/{codigo}");
        }

        public async Task<ApiarioDetalleDto> ObtenerApiario(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<ApiarioDetalleDto>($"apiario/{codigo}");
            
        }

        public async Task<CampañaDetalleDto> ObtenerCampañas(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<CampañaDetalleDto>($"campaña/{codigo}");
        }

        public async Task<ControlDetalleDto> ObtenerControl(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<ControlDetalleDto>($"controles/{codigo}");
        }

        public async Task<DashBoardDto> ObtenerDashBoard()
        {
            return await _httpClient.GetFromJsonAsync<DashBoardDto>("dashBoard");
        }

        public async Task<EnfermedadesGraficoResponse> ObtenerDatosGraficoEnfermedades()
        {
            return await _httpClient.GetFromJsonAsync<EnfermedadesGraficoResponse>("dashBoard/graficoEnfermedades");
        }
        public async Task<EnfermedadDetalleDto> ObtenerEnfermedades(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<EnfermedadDetalleDto>($"enfermedad/{codigo}");
        }

        public async Task<ProductoDetalleDto> ObtenerProducto(int codigo)
        {
            return await _httpClient.GetFromJsonAsync<ProductoDetalleDto>($"producto/{codigo}");
        }

        public async Task<List<CampañaGrillaDto>?> ObtenerTodasCampañas()
        {
            return await _httpClient.GetFromJsonAsync<List<CampañaGrillaDto>>("campaña/vertodos");
        }

        public async Task<List<EnfermedadGrillaDto>?> ObtenerTodasEnfermedades()
        {
            return await _httpClient.GetFromJsonAsync<List<EnfermedadGrillaDto>>("enfermedad/vertodos");
        }

        public async Task<List<AlimentoGrillaDto>?> ObtenerTodosAlimentos()
        {
            return await _httpClient.GetFromJsonAsync<List<AlimentoGrillaDto>>("alimento/vertodos");
        }

        public async Task<List<ApiarioGrillaDto>?> ObtenerTodosApiarios()
        {
           return await _httpClient.GetFromJsonAsync<List<ApiarioGrillaDto>>("apiario/vertodos");   
        }

        public async Task<List<ControlGrillaDto>?> ObtenerTodosControles()
        {
            return await _httpClient.GetFromJsonAsync<List<ControlGrillaDto>>("controles/vertodos");
        }

        public async Task<List<ProductoGrillaDto>?> ObtenerTodosProductos()
        {
            return await _httpClient.GetFromJsonAsync<List<ProductoGrillaDto>>("producto/vertodos");
        }
    }
}
