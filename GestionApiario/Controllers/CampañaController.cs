using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class CampañaController : ControladorBase
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
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Campañas.Add(campaña);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerCampaña), new { Codigo = campaña.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<CampañaDetalleDto>> ObtenerCampaña([FromRoute] int Codigo)
        {
            var campaña = await _context.Campañas
                .Where(c => c.Codigo == Codigo && c.FechaBaja == null)
                .Select(c => new CampañaDetalleDto()
                {
                    Codigo = c.Codigo,
                    Año = c.Año,
                    Responsable = c.Responsable,
                    UsuarioAlta = c.UsuarioAlta,
                    FechaAlta = c.FechaAlta,
                    UsuarioBaja = c.UsuarioBaja,
                    FechaBaja = c.FechaBaja,
                    FechaModificacion = c.FechaModificacion,
                    UsuarioModificacion = c.UsuarioModificacion
                })
                .FirstOrDefaultAsync();

            if (campaña == null) { return NotFound(); }
            return Ok(campaña);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarCampaña([FromRoute] int Codigo, [FromBody] CampañaDto campañaModificar)
        {
            var campaña = await _context.Campañas.FirstOrDefaultAsync(c => c.Codigo == Codigo && c.FechaBaja == null);

            if (campaña == null) { return NotFound(); }

            campaña.Año = campañaModificar.Año;
            campaña.Responsable = campañaModificar.Responsable;
            campaña.FechaModificacion = DateTime.Now;
            campaña.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<CampañaGrillaDto>>> ObtenerTodos()
        {
            var listaCampañas = await _context.Campañas
                .Where(c => c.FechaBaja == null)
                .OrderByDescending(c => c.Año)
                .Select(c => new CampañaGrillaDto()
                {
                    Codigo = c.Codigo,
                    Año = c.Año,
                    Responsable = c.Responsable,
                    FechaAlta = c.FechaAlta,
                    FechaModificacion = c.FechaModificacion
                })
                .ToListAsync();

            return Ok(listaCampañas);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var campaña = await _context.Campañas.FirstOrDefaultAsync(c => c.Codigo == Codigo && c.FechaBaja == null);

            if (campaña == null) { return NotFound(); }

            campaña.FechaBaja = DateTime.Now;
            campaña.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
