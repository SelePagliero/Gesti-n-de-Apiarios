using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using GestionApiario.compartido.Dto;
using GestionApiario.web.Servicios.Autenticacion;
using GestionApiario.web.Servicios.Interfaces;

namespace GestionApiario.web.Servicios
{
    public class ApiarioServicio : IApiariosServicio
    {
        private readonly HttpClient _httpClient;
        private readonly EstadoAutenticacion _estado;
        private readonly CuentaServicio _cuenta;

        public ApiarioServicio(HttpClient http, EstadoAutenticacion estado, CuentaServicio cuenta)
        {
            _httpClient = http;
            _estado = estado;
            _cuenta = cuenta;
        }

        #region Apiarios
        public Task<List<ApiarioGrillaDto>> ObtenerTodosApiarios() => ObtenerListaAsync<ApiarioGrillaDto>("apiario/vertodos");
        public Task<ApiarioDetalleDto> ObtenerApiario(int codigo) => ObtenerAsync<ApiarioDetalleDto>($"apiario/{codigo}");
        public Task CrearApiario(ApiarioDto apiario) => EnviarAsync(HttpMethod.Post, "apiario", apiario);
        public Task ModificarApiario(int codigo, ApiarioDto apiario) => EnviarAsync(HttpMethod.Put, $"apiario/{codigo}", apiario);
        public Task EliminarApiario(int codigo) => EnviarAsync(HttpMethod.Delete, $"apiario/{codigo}");
        #endregion

        #region Alimentos
        public Task<List<AlimentoGrillaDto>> ObtenerTodosAlimentos() => ObtenerListaAsync<AlimentoGrillaDto>("alimento/vertodos");
        public Task<AlimentoDetalleDto> ObtenerAlimento(int codigo) => ObtenerAsync<AlimentoDetalleDto>($"alimento/{codigo}");
        public Task CrearAlimento(AlimentoDto alimento) => EnviarAsync(HttpMethod.Post, "alimento", alimento);
        public Task ModificarAlimento(int codigo, AlimentoDto alimento) => EnviarAsync(HttpMethod.Put, $"alimento/{codigo}", alimento);
        public Task EliminarAlimento(int codigo) => EnviarAsync(HttpMethod.Delete, $"alimento/{codigo}");
        #endregion

        #region Productos
        public Task<List<ProductoGrillaDto>> ObtenerTodosProductos() => ObtenerListaAsync<ProductoGrillaDto>("producto/vertodos");
        public Task<ProductoDetalleDto> ObtenerProducto(int codigo) => ObtenerAsync<ProductoDetalleDto>($"producto/{codigo}");
        public Task CrearProducto(ProductoDto producto) => EnviarAsync(HttpMethod.Post, "producto", producto);
        public Task ModificarProducto(int codigo, ProductoDto producto) => EnviarAsync(HttpMethod.Put, $"producto/{codigo}", producto);
        public Task EliminarProducto(int codigo) => EnviarAsync(HttpMethod.Delete, $"producto/{codigo}");
        #endregion

        #region Enfermedades
        public Task<List<EnfermedadGrillaDto>> ObtenerTodasEnfermedades() => ObtenerListaAsync<EnfermedadGrillaDto>("enfermedad/vertodos");
        public Task<EnfermedadDetalleDto> ObtenerEnfermedad(int codigo) => ObtenerAsync<EnfermedadDetalleDto>($"enfermedad/{codigo}");
        public Task CrearEnfermedad(EnfermedadDto enfermedad) => EnviarAsync(HttpMethod.Post, "enfermedad", enfermedad);
        public Task ModificarEnfermedad(int codigo, EnfermedadDto enfermedad) => EnviarAsync(HttpMethod.Put, $"enfermedad/{codigo}", enfermedad);
        public Task EliminarEnfermedad(int codigo) => EnviarAsync(HttpMethod.Delete, $"enfermedad/{codigo}");
        #endregion

        #region Campañas
        public Task<List<CampañaGrillaDto>> ObtenerTodasCampañas() => ObtenerListaAsync<CampañaGrillaDto>("campaña/vertodos");
        public Task<CampañaDetalleDto> ObtenerCampaña(int codigo) => ObtenerAsync<CampañaDetalleDto>($"campaña/{codigo}");
        public Task CrearCampaña(CampañaDto campaña) => EnviarAsync(HttpMethod.Post, "campaña", campaña);
        public Task ModificarCampaña(int codigo, CampañaDto campaña) => EnviarAsync(HttpMethod.Put, $"campaña/{codigo}", campaña);
        public Task EliminarCampaña(int codigo) => EnviarAsync(HttpMethod.Delete, $"campaña/{codigo}");
        #endregion

        #region Controles
        public Task<List<ControlGrillaDto>> ObtenerTodosControles(FiltroControlesDto? filtro = null) =>
            ObtenerListaAsync<ControlGrillaDto>("controles/vertodos" + ArmarQueryFiltros(filtro));
        public Task<ControlDetalleDto> ObtenerControl(int codigo) => ObtenerAsync<ControlDetalleDto>($"controles/{codigo}");
        public Task CrearControl(ControlDto control) => EnviarAsync(HttpMethod.Post, "controles", control);
        public Task ModificarControl(int codigo, ControlDto control) => EnviarAsync(HttpMethod.Put, $"controles/{codigo}", control);
        public Task EliminarControl(int codigo) => EnviarAsync(HttpMethod.Delete, $"controles/{codigo}");
        #endregion

        #region DashBoard
        public Task<DashBoardDto> ObtenerDashBoard(string? usuarioId = null) =>
            ObtenerAsync<DashBoardDto>("dashBoard" + QueryUsuario(usuarioId));
        public Task<EnfermedadesGraficoResponse> ObtenerDatosGraficoEnfermedades(string? usuarioId = null) =>
            ObtenerAsync<EnfermedadesGraficoResponse>("dashBoard/graficoEnfermedades" + QueryUsuario(usuarioId));

        private static string QueryUsuario(string? usuarioId) =>
            string.IsNullOrEmpty(usuarioId) ? string.Empty : $"?usuarioId={Uri.EscapeDataString(usuarioId)}";
        #endregion

        #region Usuarios
        public Task<List<UsuarioDto>> ObtenerUsuarios() => ObtenerListaAsync<UsuarioDto>("usuarios");
        #endregion

        // Arma "?CodApiario=3&FechaDesde=2026-03-01..." solo con los filtros que tienen valor.
        private static string ArmarQueryFiltros(FiltroControlesDto? filtro)
        {
            if (filtro is null)
                return string.Empty;

            var parametros = new List<(string Nombre, string? Valor)>
            {
                (nameof(FiltroControlesDto.UsuarioId), string.IsNullOrEmpty(filtro.UsuarioId) ? null : filtro.UsuarioId),
                (nameof(FiltroControlesDto.CodApiario), filtro.CodApiario?.ToString(CultureInfo.InvariantCulture)),
                (nameof(FiltroControlesDto.CodCampaña), filtro.CodCampaña?.ToString(CultureInfo.InvariantCulture)),
                (nameof(FiltroControlesDto.CodEnfermedad), filtro.CodEnfermedad?.ToString(CultureInfo.InvariantCulture)),
                (nameof(FiltroControlesDto.ConAlgunaEnfermedad), filtro.ConAlgunaEnfermedad == true ? "true" : null),
                (nameof(FiltroControlesDto.FechaDesde), filtro.FechaDesde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                (nameof(FiltroControlesDto.FechaHasta), filtro.FechaHasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            };

            var conValor = parametros
                .Where(p => p.Valor is not null)
                .Select(p => $"{Uri.EscapeDataString(p.Nombre)}={Uri.EscapeDataString(p.Valor!)}")
                .ToList();
            return conValor.Count == 0 ? string.Empty : "?" + string.Join("&", conValor);
        }

        private async Task<T> ObtenerAsync<T>(string url)
        {
            using var respuesta = await EnviarAsync(HttpMethod.Get, url);
            return await respuesta.Content.ReadFromJsonAsync<T>()
                ?? throw new HttpRequestException("La API devolvió una respuesta vacía.");
        }

        private async Task<List<T>> ObtenerListaAsync<T>(string url)
        {
            using var respuesta = await EnviarAsync(HttpMethod.Get, url);
            return await respuesta.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
        }

        // Envía la solicitud con el token de la sesión. Si la API responde 401 intenta renovar el token una vez.
        // Si la API responde con error, lanza HttpRequestException con un mensaje legible.
        private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, object? cuerpo = null)
        {
            try
            {
                var respuesta = await EnviarConTokenAsync(metodo, url, cuerpo);

                if (respuesta.StatusCode == HttpStatusCode.Unauthorized && await _cuenta.RenovarTokenAsync())
                {
                    respuesta.Dispose();
                    respuesta = await EnviarConTokenAsync(metodo, url, cuerpo);
                }

                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    respuesta.Dispose();
                    await _cuenta.CerrarSesionAsync();
                    throw new SesionExpiradaException();
                }

                if (!respuesta.IsSuccessStatusCode)
                {
                    var mensaje = await ErroresApi.LeerMensajeAsync(respuesta);
                    var estado = respuesta.StatusCode;
                    respuesta.Dispose();
                    throw new HttpRequestException(mensaje, null, estado);
                }

                return respuesta;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                // La API no está levantada o no se puede alcanzar.
                throw new HttpRequestException(ErroresApi.SinConexion, ex);
            }
        }

        private async Task<HttpResponseMessage> EnviarConTokenAsync(HttpMethod metodo, string url, object? cuerpo)
        {
            using var solicitud = new HttpRequestMessage(metodo, url);
            if (cuerpo is not null)
                solicitud.Content = JsonContent.Create(cuerpo, cuerpo.GetType());

            var sesion = await _estado.ObtenerSesionAsync();
            if (sesion is not null)
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);

            return await _httpClient.SendAsync(solicitud);
        }
    }
}
