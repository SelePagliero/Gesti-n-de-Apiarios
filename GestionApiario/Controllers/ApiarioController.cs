using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiarioController : Controller
    {
        private readonly GestionApiariosContext _context;
        public ApiarioController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarApario([FromBody] ApiarioDto nuevoApiario)
        {
            Apiario apiario = new()
            {
                Nombre = nuevoApiario.Nombre,
                Empresa = nuevoApiario.Empresa,
                FechaAlta = DateTime.Now,
                Longitud=nuevoApiario.Longitud,
                Latitud=nuevoApiario.Latitud
            };

            _context.Apiarios.Add(apiario);
            await _context.SaveChangesAsync();
            return Created();
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<ApiarioDetalleDto>> ObtenerApario([FromRoute] int Codigo)
        {
      
           var Apiario =await _context.Apiarios.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (Apiario == null) { return NotFound();}
            ApiarioDetalleDto apiarioDto = new ApiarioDetalleDto()
            {
                Codigo = Apiario.Codigo,
                Nombre = Apiario.Nombre,
                Empresa = Apiario.Empresa,
                FechaAlta = Apiario.FechaAlta,
                FechaBaja = Apiario.FechaBaja,
                Latitud = Apiario.Latitud,
                Longitud = Apiario.Longitud,
                UsuarioAlta = Apiario.UsuarioAlta,
                UsuarioBaja = Apiario.UsuarioBaja,
                FechaModificacion = Apiario.FechaModificacion,
                UsuarioModificacion = Apiario.UsuarioModificacion
            };
            return Ok(apiarioDto);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] ApiarioDto apiarioModificar)
        {

            var Apiario = await _context.Apiarios.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (Apiario == null) { return NotFound(); }

            Apiario.Nombre= apiarioModificar.Nombre;
            Apiario.Empresa= apiarioModificar.Empresa;
            Apiario.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("vertodos")]
        public ActionResult<List<ApiarioGrillaDto>> ObtenerTodos()
        {


            var listaApiarios = _context.Apiarios.Where(a => a.FechaBaja == null).Select(apiario => new ApiarioGrillaDto()
            {
                Codigo = apiario.Codigo,
                Nombre = apiario.Nombre,
                FechaModificacion = apiario.FechaModificacion,
                FechaAlta = apiario.FechaAlta

            }).ToList();

            return Ok(listaApiarios);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {

            var Apiario = await _context.Apiarios.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (Apiario == null) { return NotFound(); }

            Apiario.FechaBaja = DateTime.Now;
            Apiario.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }
}
