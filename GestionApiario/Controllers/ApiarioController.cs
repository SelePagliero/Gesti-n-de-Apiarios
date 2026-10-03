using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
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
            Apiario apiario = new()
            {
                Nombre = nuevoApiario.Nombre,
                Empresa = nuevoApiario.Empresa,
                Longitud = nuevoApiario.Longitud,
                Latitud = nuevoApiario.Latitud,
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
            var apiario = await _context.Apiarios
                .Where(a => a.Codigo == Codigo && a.FechaBaja == null)
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
                    UsuarioModificacion = a.UsuarioModificacion
                })
                .FirstOrDefaultAsync();

            if (apiario == null) { return NotFound(); }
            return Ok(apiario);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] ApiarioDto apiarioModificar)
        {
            var apiario = await _context.Apiarios.FirstOrDefaultAsync(a => a.Codigo == Codigo && a.FechaBaja == null);

            if (apiario == null) { return NotFound(); }

            apiario.Nombre = apiarioModificar.Nombre;
            apiario.Empresa = apiarioModificar.Empresa;
            apiario.Latitud = apiarioModificar.Latitud;
            apiario.Longitud = apiarioModificar.Longitud;
            apiario.FechaModificacion = DateTime.Now;
            apiario.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ApiarioGrillaDto>>> ObtenerTodos()
        {
            var listaApiarios = await _context.Apiarios
                .Where(a => a.FechaBaja == null)
                .OrderBy(a => a.Nombre)
                .Select(apiario => new ApiarioGrillaDto()
                {
                    Codigo = apiario.Codigo,
                    Nombre = apiario.Nombre,
                    FechaModificacion = apiario.FechaModificacion,
                    FechaAlta = apiario.FechaAlta
                })
                .ToListAsync();

            return Ok(listaApiarios);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var apiario = await _context.Apiarios.FirstOrDefaultAsync(a => a.Codigo == Codigo && a.FechaBaja == null);

            if (apiario == null) { return NotFound(); }

            apiario.FechaBaja = DateTime.Now;
            apiario.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
