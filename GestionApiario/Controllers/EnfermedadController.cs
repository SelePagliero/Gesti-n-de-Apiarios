using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Authorization;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class EnfermedadController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public EnfermedadController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> InsertarEnfermedad([FromBody] EnfermedadDto nuevaEnfermedad)
        {
            Enfermedad enfermedad = new()
            {
                Nombre = nuevaEnfermedad.Nombre,
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Enfermedades.Add(enfermedad);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerEnfermedad), new { Codigo = enfermedad.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<EnfermedadDetalleDto>> ObtenerEnfermedad([FromRoute] int Codigo)
        {
            // El email de quien creó el registro ("creado por") solo lo ve la Administradora.
            var mostrarCreador = EsAdministrador;
            var enfermedad = await _context.Enfermedades
                .Where(e => e.Codigo == Codigo && e.FechaBaja == null)
                .Select(e => new EnfermedadDetalleDto()
                {
                    Codigo = e.Codigo,
                    Nombre = e.Nombre,
                    UsuarioAlta = mostrarCreador ? e.UsuarioAlta : null,
                    FechaAlta = e.FechaAlta,
                    UsuarioBaja = e.UsuarioBaja,
                    FechaBaja = e.FechaBaja,
                    FechaModificacion = e.FechaModificacion,
                    UsuarioModificacion = e.UsuarioModificacion
                })
                .FirstOrDefaultAsync();

            if (enfermedad == null) { return NotFound(); }
            return Ok(enfermedad);
        }

        [HttpPut("{Codigo}")]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] EnfermedadDto enfermedadModificar)
        {
            var enfermedad = await _context.Enfermedades.FirstOrDefaultAsync(e => e.Codigo == Codigo && e.FechaBaja == null);

            if (enfermedad == null) { return NotFound(); }

            enfermedad.Nombre = enfermedadModificar.Nombre;
            enfermedad.FechaModificacion = DateTime.Now;
            enfermedad.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<EnfermedadGrillaDto>>> ObtenerTodos()
        {
            var listaEnfermedades = await _context.Enfermedades
                .Where(e => e.FechaBaja == null)
                .OrderBy(e => e.Nombre)
                .Select(e => new EnfermedadGrillaDto()
                {
                    Codigo = e.Codigo,
                    Nombre = e.Nombre,
                    FechaAlta = e.FechaAlta,
                    FechaModificacion = e.FechaModificacion
                })
                .ToListAsync();

            return Ok(listaEnfermedades);
        }

        [HttpDelete("{Codigo}")]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var enfermedad = await _context.Enfermedades.FirstOrDefaultAsync(e => e.Codigo == Codigo && e.FechaBaja == null);

            if (enfermedad == null) { return NotFound(); }

            enfermedad.FechaBaja = DateTime.Now;
            enfermedad.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
