using GestionApiario.compartido.Dto;

namespace GestionApiario.web.Servicios.Interfaces
{
    // Todas las operaciones lanzan HttpRequestException con un mensaje en español si la API responde con error,
    // o SesionExpiradaException si la sesión ya no es válida.
    public interface IApiariosServicio
    {
        #region Apiarios
        Task<List<ApiarioGrillaDto>> ObtenerTodosApiarios();
        Task<ApiarioDetalleDto> ObtenerApiario(int codigo);
        Task EliminarApiario(int codigo);
        Task CrearApiario(ApiarioDto apiario);
        Task ModificarApiario(int codigo, ApiarioDto apiario);
        #endregion

        #region Alimentos
        Task<List<AlimentoGrillaDto>> ObtenerTodosAlimentos();
        Task<AlimentoDetalleDto> ObtenerAlimento(int codigo);
        Task EliminarAlimento(int codigo);
        Task CrearAlimento(AlimentoDto alimento);
        Task ModificarAlimento(int codigo, AlimentoDto alimento);
        #endregion

        #region Productos
        Task<List<ProductoGrillaDto>> ObtenerTodosProductos();
        Task<ProductoDetalleDto> ObtenerProducto(int codigo);
        Task EliminarProducto(int codigo);
        Task CrearProducto(ProductoDto producto);
        Task ModificarProducto(int codigo, ProductoDto producto);
        #endregion

        #region Enfermedades
        Task<List<EnfermedadGrillaDto>> ObtenerTodasEnfermedades();
        Task<EnfermedadDetalleDto> ObtenerEnfermedad(int codigo);
        Task EliminarEnfermedad(int codigo);
        Task CrearEnfermedad(EnfermedadDto enfermedad);
        Task ModificarEnfermedad(int codigo, EnfermedadDto enfermedad);
        #endregion

        #region Campañas
        Task<List<CampañaGrillaDto>> ObtenerTodasCampañas();
        Task<CampañaDetalleDto> ObtenerCampaña(int codigo);
        Task EliminarCampaña(int codigo);
        Task CrearCampaña(CampañaDto campaña);
        Task ModificarCampaña(int codigo, CampañaDto campaña);
        #endregion

        #region Controles
        Task<List<ControlGrillaDto>> ObtenerTodosControles(FiltroControlesDto? filtro = null);
        Task<ControlDetalleDto> ObtenerControl(int codigo);
        Task EliminarControl(int codigo);
        Task CrearControl(ControlDto control);
        Task ModificarControl(int codigo, ControlDto control);
        #endregion

        #region DashBoard
        // usuarioId solo lo tiene en cuenta la API si consulta la Administradora.
        Task<DashBoardDto> ObtenerDashBoard(string? usuarioId = null);
        Task<EnfermedadesGraficoResponse> ObtenerDatosGraficoEnfermedades(string? usuarioId = null);
        #endregion

        #region Usuarios
        // Solo para la Administradora (la API responde 403 a los apicultores).
        Task<List<UsuarioDto>> ObtenerUsuarios();
        Task<List<UsuarioGrillaDto>> ObtenerGrillaUsuarios();
        #endregion
    }
}
