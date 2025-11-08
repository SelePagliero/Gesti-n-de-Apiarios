
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionApiario.compartido.Dto;


namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductoController : Controller
    {
        private readonly GestionApiariosContext _context;
        public ProductoController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarProducto([FromBody] ProductoDto nuevoProducto)
        {
            Producto producto = new()
            {
                Nombre = nuevoProducto.Nombre,
                FechaAlta = DateTime.Now
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return Created();
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<Producto>> ObtenerProducto([FromRoute] int Codigo)
        {

            var producto = await _context.Productos.Where(p => p.Codigo == Codigo).FirstOrDefaultAsync();

            if (producto == null) { return NotFound(); }
            return Ok(producto);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarProducto([FromRoute] int Codigo, [FromBody] ProductoDto ProductoModificar)
        {

            var producto = await _context.Productos.Where(p => p.Codigo == Codigo).FirstOrDefaultAsync();

            if (producto == null) { return NotFound(); }

            producto.Nombre = ProductoModificar.Nombre;
            producto.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok(producto);
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<Producto>>> ObtenerTodos()
        {


            var listaProductos = _context.Productos.Where(p => p.FechaBaja == null).ToList();

            return Ok(listaProductos);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> EliminarProducto([FromRoute] int Codigo)
        {

            var borrarProducto = await _context.Productos.Where(p => p.Codigo == Codigo).FirstOrDefaultAsync();

            if (borrarProducto == null) { return NotFound(); }

            borrarProducto.FechaBaja = DateTime.Now;
            borrarProducto.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }

}