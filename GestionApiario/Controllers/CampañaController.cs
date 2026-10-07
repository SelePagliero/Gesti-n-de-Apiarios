using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Cada campaña pertenece al usuario que la creó, igual que los apiarios. Un apicultor solo ve y modifica
    // las suyas; la Administradora ve y modifica todas, y es la única que puede elegir o cambiar el dueño.
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
            var (dueño, error) = await ResolverDueñoAsync(_context, nuevaCampaña.UsuarioId, dueñoActual: UsuarioIdActual);
            if (error != null) { return error; }

            Campaña campaña = new()
            {
                Año = nuevaCampaña.Año,
                Responsable = nuevaCampaña.Responsable,
                UsuarioId = dueño,
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
            // Los emails de auditoría ("creado por", "modificado por" y "dado de baja por") solo los ve la Administradora.
            var mostrarAuditoria = EsAdministrador;
            var campaña = await CampañasVisibles()
                .Where(c => c.Codigo == Codigo)
                .Select(c => new CampañaDetalleDto()
                {
                    Codigo = c.Codigo,
                    Año = c.Año,
                    Responsable = c.Responsable,
                    UsuarioAlta = mostrarAuditoria ? c.UsuarioAlta : null,
                    FechaAlta = c.FechaAlta,
                    UsuarioBaja = mostrarAuditoria ? c.UsuarioBaja : null,
                    FechaBaja = c.FechaBaja,
                    FechaModificacion = c.FechaModificacion,
                    UsuarioModificacion = mostrarAuditoria ? c.UsuarioModificacion : null,
                    UsuarioId = c.UsuarioId,
                    Apicultor = c.Usuario!.Email
                })
                .FirstOrDefaultAsync();

            if (campaña == null) { return NotFound(); }
            return Ok(campaña);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarCampaña([FromRoute] int Codigo, [FromBody] CampañaDto campañaModificar)
        {
            var campaña = await CampañasVisibles().FirstOrDefaultAsync(c => c.Codigo == Codigo);

            if (campaña == null) { return NotFound(); }

            // Si no se indica dueño se mantiene el actual.
            var (dueño, error) = await ResolverDueñoAsync(_context, campañaModificar.UsuarioId, dueñoActual: campaña.UsuarioId);
            if (error != null) { return error; }

            // Un control solo puede usar campañas del dueño de su apiario: no se puede reasignar una campaña
            // que todavía usan controles de apiarios de otro apicultor.
            if (dueño != campaña.UsuarioId && await _context.Controles.AnyAsync(c =>
                    c.CodCampaña == campaña.Codigo && c.CodApiarioNavigation!.UsuarioId != dueño))
            {
                return BadRequest("No se puede reasignar la campaña porque la usan controles de apiarios de otro apicultor. " +
                                  "Reasigná esos apiarios o cambiá la campaña de esos controles.");
            }

            campaña.Año = campañaModificar.Año;
            campaña.Responsable = campañaModificar.Responsable;
            campaña.UsuarioId = dueño;
            campaña.FechaModificacion = DateTime.Now;
            campaña.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<CampañaGrillaDto>>> ObtenerTodos()
        {
            var listaCampañas = await CampañasVisibles()
                .OrderByDescending(c => c.Año)
                .Select(c => new CampañaGrillaDto()
                {
                    Codigo = c.Codigo,
                    Año = c.Año,
                    Responsable = c.Responsable,
                    FechaAlta = c.FechaAlta,
                    FechaModificacion = c.FechaModificacion,
                    UsuarioId = c.UsuarioId,
                    Apicultor = c.Usuario!.Email
                })
                .ToListAsync();

            return Ok(listaCampañas);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var campaña = await CampañasVisibles().FirstOrDefaultAsync(c => c.Codigo == Codigo);

            if (campaña == null) { return NotFound(); }

            campaña.FechaBaja = DateTime.Now;
            campaña.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Campañas activas que el usuario actual puede ver. Las ajenas responden 404, como si no existieran.
        private IQueryable<Campaña> CampañasVisibles()
        {
            var activas = _context.Campañas.Where(c => c.FechaBaja == null);
            return EsAdministrador ? activas : activas.Where(c => c.UsuarioId == UsuarioIdActual);
        }
    }
}
