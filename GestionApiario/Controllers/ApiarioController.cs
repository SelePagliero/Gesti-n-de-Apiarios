using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Cada apiario pertenece al usuario que lo creó. Un apicultor solo ve y modifica los suyos;
    // la Administradora ve y modifica todos, y es la única que puede elegir o cambiar el dueño.
    public class ApiarioController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public ApiarioController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarApiario([FromBody] ApiarioDto nuevoApiario)
        {
            var (dueño, error) = await ResolverDueñoAsync(nuevoApiario.UsuarioId, dueñoActual: UsuarioIdActual);
            if (error != null) { return error; }

            Apiario apiario = new()
            {
                Nombre = nuevoApiario.Nombre,
                Empresa = nuevoApiario.Empresa,
                Longitud = nuevoApiario.Longitud,
                Latitud = nuevoApiario.Latitud,
                UsuarioId = dueño,
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Apiarios.Add(apiario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerApiario), new { Codigo = apiario.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<ApiarioDetalleDto>> ObtenerApiario([FromRoute] int Codigo)
        {
            var apiario = await ApiariosVisibles()
                .Where(a => a.Codigo == Codigo)
                .Select(a => new ApiarioDetalleDto()
                {
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    Empresa = a.Empresa,
                    FechaAlta = a.FechaAlta,
                    FechaBaja = a.FechaBaja,
                    Latitud = a.Latitud,
                    Longitud = a.Longitud,
                    UsuarioAlta = a.UsuarioAlta,
                    UsuarioBaja = a.UsuarioBaja,
                    FechaModificacion = a.FechaModificacion,
                    UsuarioModificacion = a.UsuarioModificacion,
                    UsuarioId = a.UsuarioId,
                    Apicultor = a.Usuario!.Email
                })
                .FirstOrDefaultAsync();

            if (apiario == null) { return NotFound(); }
            return Ok(apiario);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] ApiarioDto apiarioModificar)
        {
            var apiario = await ApiariosVisibles().FirstOrDefaultAsync(a => a.Codigo == Codigo);

            if (apiario == null) { return NotFound(); }

            // Si no se indica dueño se mantiene el actual. Sus controles cambian de dueño junto con el apiario.
            var (dueño, error) = await ResolverDueñoAsync(apiarioModificar.UsuarioId, dueñoActual: apiario.UsuarioId);
            if (error != null) { return error; }

            apiario.Nombre = apiarioModificar.Nombre;
            apiario.Empresa = apiarioModificar.Empresa;
            apiario.Latitud = apiarioModificar.Latitud;
            apiario.Longitud = apiarioModificar.Longitud;
            apiario.UsuarioId = dueño;
            apiario.FechaModificacion = DateTime.Now;
            apiario.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ApiarioGrillaDto>>> ObtenerTodos()
        {
            var listaApiarios = await ApiariosVisibles()
                .OrderBy(a => a.Nombre)
                .Select(apiario => new ApiarioGrillaDto()
                {
                    Codigo = apiario.Codigo,
                    Nombre = apiario.Nombre,
                    FechaModificacion = apiario.FechaModificacion,
                    FechaAlta = apiario.FechaAlta,
                    Apicultor = apiario.Usuario!.Email
                })
                .ToListAsync();

            return Ok(listaApiarios);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var apiario = await ApiariosVisibles().FirstOrDefaultAsync(a => a.Codigo == Codigo);

            if (apiario == null) { return NotFound(); }

            apiario.FechaBaja = DateTime.Now;
            apiario.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Apiarios activos que el usuario actual puede ver. Los ajenos responden 404, como si no existieran.
        private IQueryable<Apiario> ApiariosVisibles()
        {
            var activos = _context.Apiarios.Where(a => a.FechaBaja == null);
            return EsAdministrador ? activos : activos.Where(a => a.UsuarioId == UsuarioIdActual);
        }

        // Decide el dueño del apiario. Solo la Administradora puede elegir un dueño distinto del que corresponde.
        private async Task<(string? Dueño, ActionResult? Error)> ResolverDueñoAsync(string? dueñoPedido, string? dueñoActual)
        {
            if (string.IsNullOrEmpty(dueñoPedido) || dueñoPedido == dueñoActual)
                return (dueñoActual, null);

            if (!EsAdministrador)
                return (null, Forbid());

            if (!await _context.Users.AnyAsync(u => u.Id == dueñoPedido))
                return (null, BadRequest("El apicultor no existe."));

            return (dueñoPedido, null);
        }
    }
}
