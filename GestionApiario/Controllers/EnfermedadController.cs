using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionApiario.compartido.Dto;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnfermedadController : Controller
    {
        private readonly GestionApiariosContext _context;
        public EnfermedadController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarEnfermedad([FromBody] EnfermedadDto nuevaEnfermedad)
        {
            Enfermedad enfermedad = new()
            {
                Nombre = nuevaEnfermedad.Nombre,
                FechaAlta = DateTime.Now
            };

            _context.Enfermedads.Add(enfermedad);
            await _context.SaveChangesAsync();
            return Created();
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<Enfermedad>> ObtenerEnfermedad([FromRoute] int Codigo)
        {


            var enfermedad = await _context.Enfermedads.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (enfermedad == null) { return NotFound(); }
            return Ok(enfermedad);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] EnfermedadDto enfermedadModificar)
        {

            var enfermedad = await _context.Enfermedads.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (enfermedad == null) { return NotFound(); }

            enfermedad.Nombre = enfermedadModificar.Nombre;
            enfermedad.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<Enfermedad>>> ObtenerTodos()
        {


            var listaEnfermedades = _context.Enfermedads.Where(a => a.FechaBaja == null).ToList();

            return Ok(listaEnfermedades);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {

            var borrarEnfermedad = await _context.Enfermedads.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (borrarEnfermedad == null) { return NotFound(); }

            borrarEnfermedad.FechaBaja = DateTime.Now;
            borrarEnfermedad.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }
}
