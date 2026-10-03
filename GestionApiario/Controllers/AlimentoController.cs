using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class AlimentoController : ControladorBase
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
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Alimentos.Add(alimento);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerAlimento), new { Codigo = alimento.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<AlimentoDetalleDto>> ObtenerAlimento([FromRoute] int Codigo)
        {
            var alimento = await _context.Alimentos
                .Where(a => a.Codigo == Codigo && a.FechaBaja == null)
                .Select(a => new AlimentoDetalleDto()
                {
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    UsuarioAlta = a.UsuarioAlta,
                    FechaAlta = a.FechaAlta,
                    UsuarioBaja = a.UsuarioBaja,
                    FechaBaja = a.FechaBaja,
                    FechaModificacion = a.FechaModificacion,
                    UsuarioModificacion = a.UsuarioModificacion
                })
                .FirstOrDefaultAsync();

            if (alimento == null) { return NotFound(); }
            return Ok(alimento);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarAlimento([FromRoute] int Codigo, [FromBody] AlimentoDto alimentoModificar)
        {
            var alimento = await _context.Alimentos.FirstOrDefaultAsync(a => a.Codigo == Codigo && a.FechaBaja == null);

            if (alimento == null) { return NotFound(); }

            alimento.Nombre = alimentoModificar.Nombre;
            alimento.FechaModificacion = DateTime.Now;
            alimento.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<AlimentoGrillaDto>>> ObtenerTodos()
        {
            var listaAlimentos = await _context.Alimentos
                .Where(a => a.FechaBaja == null)
                .OrderBy(a => a.Nombre)
                .Select(a => new AlimentoGrillaDto()
                {
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    FechaAlta = a.FechaAlta,
                    FechaModificacion = a.FechaModificacion
                })
                .ToListAsync();

            return Ok(listaAlimentos);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var alimento = await _context.Alimentos.FirstOrDefaultAsync(a => a.Codigo == Codigo && a.FechaBaja == null);

            if (alimento == null) { return NotFound(); }

            alimento.FechaBaja = DateTime.Now;
            alimento.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
