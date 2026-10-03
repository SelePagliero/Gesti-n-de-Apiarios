using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Lista de usuarios para el filtro por apicultor y el cambio de dueño de un apiario.
    [Authorize(Roles = RolesUsuario.Administrador)]
    public class UsuariosController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public UsuariosController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<UsuarioDto>>> ObtenerTodos()
        {
            var usuarios = await _context.Users
                .OrderBy(u => u.Email)
                .Select(u => new UsuarioDto { Id = u.Id, Email = u.Email ?? u.UserName ?? u.Id })
                .ToListAsync();
            return Ok(usuarios);
        }
    }
}
