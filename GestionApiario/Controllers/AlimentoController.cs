
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionApiario.compartido.Dto;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AlimentoController : Controller
    {
        private readonly GestionApiariosContext _context;
        public AlimentoController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarAlimento([FromBody] AlimentoDto nuevoAlimento)
        {
            Alimento alimento = new()
            {
                Nombre = nuevoAlimento.Nombre,
                FechaAlta = DateTime.Now
            };

            _context.Alimentos.Add(alimento);
            await _context.SaveChangesAsync();
            return Created();
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<Alimento>> ObtenerAlimento([FromRoute] int Codigo)
        {

            var alimento = await _context.Alimentos.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (alimento == null) { return NotFound(); }
            return Ok(alimento);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarAlimento([FromRoute] int Codigo, [FromBody] AlimentoDto AlimentoModificar)
        {

            var alimento = await _context.Alimentos.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (alimento == null) { return NotFound(); }

            alimento.Nombre =AlimentoModificar.Nombre;
            alimento.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<Alimento>>> ObtenerTodos()
        {


            var listaAlimentos = _context.Alimentos.Where(a => a.FechaBaja == null).ToList();

            return Ok(listaAlimentos);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {

            var borrarAlimento = await _context.Alimentos.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (borrarAlimento == null) { return NotFound(); }

            borrarAlimento.FechaBaja = DateTime.Now;
            borrarAlimento.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }

}

