using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Lista de usuarios para el filtro por apicultor, el cambio de dueño de un apiario
    // y la pantalla Usuarios. Todo es solo para la Administradora.
    [Authorize(Roles = RolesUsuario.Administrador)]
    public class UsuariosController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UsuariosController(GestionApiariosContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

        [HttpGet("grilla")]
        public async Task<ActionResult<List<UsuarioGrillaDto>>> ObtenerGrilla()
        {
            var administradores = (await _userManager.GetUsersInRoleAsync(RolesUsuario.Administrador))
                .Select(u => u.Id)
                .ToHashSet();

            var usuarios = await _context.Users
                .OrderBy(u => u.Email)
                .Select(u => new UsuarioGrillaDto
                {
                    Id = u.Id,
                    Email = u.Email ?? u.UserName ?? u.Id,
                    CantidadApiarios = _context.Apiarios.Count(a => a.UsuarioId == u.Id && a.FechaBaja == null)
                })
                .ToListAsync();

            foreach (var usuario in usuarios)
                usuario.EsAdministrador = administradores.Contains(usuario.Id);
            return Ok(usuarios);
        }
    }
}
