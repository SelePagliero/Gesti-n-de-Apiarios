using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class DashBoardController : ControladorBase
    {
        private static readonly List<string> Colores =
        [
            "#4e73df", // azul
            "#1cc88a", // verde
            "#36b9cc", // celeste
            "#f6c23e", // amarillo
            "#e74a3b", // rojo
            "#858796", // gris
            "#5a5c69", // gris oscuro
            "#20c9a6", // verde agua
            "#fd7e14", // naranja
            "#6f42c1"  // violeta
        ];

        private static readonly List<string> ColoresHover =
        [
            "#2e59d9", // azul oscuro
            "#17a673", // verde oscuro
            "#2c9faf", // celeste oscuro
            "#dda20a", // amarillo oscuro
            "#be2617", // rojo oscuro
            "#6c757d", // gris medio
            "#343a40", // gris más oscuro
            "#169b7b", // verde agua oscuro
            "#e8590c", // naranja oscuro
            "#4e2a84"  // violeta oscuro
        ];

        private readonly GestionApiariosContext _context;
        public DashBoardController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<DashBoardDto>> ObtenerTodos([FromQuery] string? usuarioId)
        {
            var dueño = DueñoAMostrar(usuarioId);
            var ultimosControles = UltimoControlDeCadaApiarioActivo(dueño);
            var apiariosActivos = _context.Apiarios.Where(a => a.FechaBaja == null);
            if (dueño is not null)
                apiariosActivos = apiariosActivos.Where(a => a.UsuarioId == dueño);

            // Como hay un solo control por apiario, contar controles equivale a contar apiarios.
            var dashBoard = new DashBoardDto
            {
                ApiariosActivos = await apiariosActivos.CountAsync(),
                CantidadTotalDeColmenas = await ultimosControles.SumAsync(c => c.CantDeColmenas) ?? 0,
                ApiariosConEnfermedades = await ultimosControles.CountAsync(c => c.CodEnfermedad != null)
            };

            return Ok(dashBoard);
        }

        [HttpGet("graficoEnfermedades")]
        public async Task<ActionResult<EnfermedadesGraficoResponse>> ObtenerTodosGraficosEnfermedades([FromQuery] string? usuarioId)
        {
            var cantidadPorEnfermedad = await UltimoControlDeCadaApiarioActivo(DueñoAMostrar(usuarioId))
                .Where(c => c.CodEnfermedad != null)
                .GroupBy(c => c.CodEnfermedadNavigation!.Nombre)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToListAsync();

            var total = cantidadPorEnfermedad.Sum(x => x.Cantidad);

            var respuesta = new EnfermedadesGraficoResponse
            {
                labels = cantidadPorEnfermedad.Select(x => x.Nombre ?? "Sin nombre").ToList()
            };
            respuesta.datasets.Add(new EnfermedadesGraficoDto
            {
                data = cantidadPorEnfermedad
                    .Select(x => total == 0 ? 0 : (int)Math.Round((double)x.Cantidad * 100 / total))
                    .ToList(),
                backgroundColor = Colores,
                hoverBackgroundColor = ColoresHover,
                hoverBorderColor = "rgba(234, 236, 244, 1)"
            });

            return Ok(respuesta);
        }

        // Dueño cuyos datos se muestran (null = todos). Un apicultor siempre ve solo lo suyo; la Administradora
        // ve todo o, si indica usuarioId, los datos de ese apicultor.
        private string? DueñoAMostrar(string? usuarioIdPedido)
        {
            if (EsAdministrador)
                return string.IsNullOrEmpty(usuarioIdPedido) ? null : usuarioIdPedido;
            return UsuarioIdActual ?? string.Empty;
        }

        // El estado actual de cada apiario es su control más reciente: entre un control y el siguiente
        // pueden morir colmenas o curarse enfermedades. Solo se consideran apiarios y controles no dados de baja
        // y, si se indica dueño, solo los apiarios de ese dueño.
        // Se ordena por Fecha (un control sin fecha cuenta como el más antiguo) y, si hay empate, por Codigo.
        private IQueryable<Controle> UltimoControlDeCadaApiarioActivo(string? dueño)
        {
            var controlesActivos = _context.Controles
                .Where(c => c.FechaBaja == null && c.CodApiario != null && c.CodApiarioNavigation!.FechaBaja == null);
            if (dueño is not null)
                controlesActivos = controlesActivos.Where(c => c.CodApiarioNavigation!.UsuarioId == dueño);

            return controlesActivos.Where(c => !controlesActivos.Any(posterior =>
                posterior.CodApiario == c.CodApiario &&
                ((posterior.Fecha ?? DateOnly.MinValue) > (c.Fecha ?? DateOnly.MinValue) ||
                 ((posterior.Fecha ?? DateOnly.MinValue) == (c.Fecha ?? DateOnly.MinValue) && posterior.Codigo > c.Codigo))));
        }
    }
}
