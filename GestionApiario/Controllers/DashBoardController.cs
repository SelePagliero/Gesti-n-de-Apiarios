using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DashBoardController:Controller
    {
        private readonly GestionApiariosContext _context;
        public DashBoardController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<DashBoardDto> ObtenerTodos()
        {
            var dashBoard = new DashBoardDto();

            dashBoard.ApiariosActivos = _context.Apiarios.Count(a => a.FechaBaja == null);
            dashBoard.CantidadTotalDeColmenas = _context.Controles.Where(c => c.FechaBaja == null).Sum(a => a.CantDeColmenas).Value;
            dashBoard.ApiariosConEnfermedades = _context.Controles.Count(a => a.CodEnfermedad != null && a.FechaBaja==null);

            return Ok(dashBoard);
        }

        [HttpGet("graficoEnfermedades")]
        public ActionResult<DashBoardDto> ObtenerTodosGraficosEnfermedades()
        {
            var dashBoardDataEnfermedades = new EnfermedadesGraficoResponse();
            dashBoardDataEnfermedades.datasets = new List<EnfermedadesGraficoDto>();
            var enfermedadesDto= new EnfermedadesGraficoDto();
            enfermedadesDto.backgroundColor = [ "#4e73df", // azul
                "#1cc88a", // verde
                "#36b9cc", // celeste
                "#f6c23e", // amarillo
                "#e74a3b", // rojo
                "#858796", // gris
                "#5a5c69", // gris oscuro
                "#20c9a6", // verde agua
                "#fd7e14", // naranja
                "#6f42c1"  ];
            enfermedadesDto.hoverBackgroundColor = ["#2e59d9", // azul oscuro
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
            enfermedadesDto.hoverBorderColor= "rgba(234, 236, 244, 1)";

            var TodasEnfermedades = _context.Controles.Include(c=> c.CodEnfermedadNavigation).Where(e => e.CodEnfermedad!=null && e.FechaBaja==null).ToList();
            var CuentaTotalEnfermedades = TodasEnfermedades.Count;
            var porcentajePorEnfermedad = TodasEnfermedades
                .GroupBy(c => c.CodEnfermedadNavigation.Nombre)
                .Select(g => new
                {
                    EnfermedadNombre = g.Key,        
                    Porcentaje = CuentaTotalEnfermedades == 0 ? 0 : (double)g.Count() * 100 / CuentaTotalEnfermedades
                })
                .OrderByDescending(x => x.Porcentaje)
                .ToList();

            enfermedadesDto.data = porcentajePorEnfermedad.Select(p => (int)Math.Round(p.Porcentaje)).ToList();
            dashBoardDataEnfermedades.labels = porcentajePorEnfermedad.Select(p => p.EnfermedadNombre).ToList();
            dashBoardDataEnfermedades.datasets.Add(enfermedadesDto);
            return Ok(dashBoardDataEnfermedades);
        }
    }
}
