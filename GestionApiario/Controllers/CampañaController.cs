
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionApiario.compartido.Dto;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CampañaController : Controller
    {
        private readonly GestionApiariosContext _context;
        public CampañaController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarCampaña([FromBody] CampañaDto nuevaCampaña)
        {
            Campaña campaña = new()
            {
                Año = nuevaCampaña.Año,
                Responsable = nuevaCampaña.Responsable,
                FechaAlta = DateTime.Now
            };

            _context.Campañas.Add(campaña);
            await _context.SaveChangesAsync();
            return Created();
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<Campaña>> ObtenerCampaña([FromRoute] int Codigo)
        {

            var campaña = await _context.Campañas.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (campaña == null) { return NotFound(); }
            return Ok(campaña);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarCampaña([FromRoute] int Codigo, [FromBody] CampañaDto CampañaModificar)
        {

            var campaña = await _context.Campañas.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (campaña == null) { return NotFound(); }

            campaña.Año = CampañaModificar.Año;
            campaña.Responsable = CampañaModificar.Responsable;
            campaña.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<Campaña>>> ObtenerTodos()
        {


            var listaCampañas = _context.Campañas.Where(a => a.FechaBaja == null).ToList();

            return Ok(listaCampañas);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {

            var borrarCampaña = await _context.Campañas.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (borrarCampaña == null) { return NotFound(); }

            borrarCampaña.FechaBaja = DateTime.Now;
            borrarCampaña.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }
}

