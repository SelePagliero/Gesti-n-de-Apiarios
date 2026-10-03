using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Authorization;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class ProductoController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public ProductoController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> InsertarProducto([FromBody] ProductoDto nuevoProducto)
        {
            Producto producto = new()
            {
                Nombre = nuevoProducto.Nombre,
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerProducto), new { Codigo = producto.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<ProductoDetalleDto>> ObtenerProducto([FromRoute] int Codigo)
        {
            var producto = await _context.Productos
                .Where(p => p.Codigo == Codigo && p.FechaBaja == null)
                .Select(p => new ProductoDetalleDto()
                {
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    UsuarioAlta = p.UsuarioAlta,
                    FechaAlta = p.FechaAlta,
                    UsuarioBaja = p.UsuarioBaja,
                    FechaBaja = p.FechaBaja,
                    FechaModificacion = p.FechaModificacion,
                    UsuarioModificacion = p.UsuarioModificacion
                })
                .FirstOrDefaultAsync();

            if (producto == null) { return NotFound(); }
            return Ok(producto);
        }

        [HttpPut("{Codigo}")]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> ModificarProducto([FromRoute] int Codigo, [FromBody] ProductoDto productoModificar)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Codigo == Codigo && p.FechaBaja == null);

            if (producto == null) { return NotFound(); }

            producto.Nombre = productoModificar.Nombre;
            producto.FechaModificacion = DateTime.Now;
            producto.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ProductoGrillaDto>>> ObtenerTodos()
        {
            var listaProductos = await _context.Productos
                .Where(p => p.FechaBaja == null)
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoGrillaDto()
                {
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    FechaAlta = p.FechaAlta,
                    FechaModificacion = p.FechaModificacion
                })
                .ToListAsync();

            return Ok(listaProductos);
        }

        [HttpDelete("{Codigo}")]
        [Authorize(Roles = RolesUsuario.Administrador)]
        public async Task<ActionResult> EliminarProducto([FromRoute] int Codigo)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Codigo == Codigo && p.FechaBaja == null);

            if (producto == null) { return NotFound(); }

            producto.FechaBaja = DateTime.Now;
            producto.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
