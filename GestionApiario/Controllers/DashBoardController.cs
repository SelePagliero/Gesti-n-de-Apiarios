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
            "#E9A825", // ámbar
            "#6B4226", // marrón
            "#5E8B3A", // verde oliva
            "#C2412D", // terracota
            "#F2C66D", // miel clara
            "#A9805B", // café con leche
            "#94B47A", // verde salvia
            "#E07B39", // naranja
            "#8E4E6B", // ciruela
            "#8C8174"  // gris piedra
        ];

        private static readonly List<string> ColoresHover =
        [
            "#CF8F12", // ámbar oscuro
            "#4F2F1A", // marrón oscuro
            "#4C7430", // verde oliva oscuro
            "#A33423", // terracota oscuro
            "#E3AE3F", // miel oscura
            "#8C6847", // café oscuro
            "#7A9A61", // salvia oscuro
            "#C4652A", // naranja oscuro
            "#723D55", // ciruela oscuro
            "#6F655A"  // piedra oscuro
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
                hoverBorderColor = "#FFFFFF"
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
