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

        // La Administradora le pone una contraseña nueva a un apicultor que olvidó la suya.
        // La anterior deja de funcionar y las sesiones abiertas no se pueden renovar.
        [HttpPut("{id}/contrasena")]
        public async Task<IActionResult> CambiarContraseña(string id, ContraseñaNuevaDto cambio)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return NotFound();

            if (usuario.Id == UsuarioIdActual)
                return BadRequest("Para cambiar tu propia contraseña usá la opción \"Cambiar contraseña\" del encabezado.");
            if (await _userManager.IsInRoleAsync(usuario, RolesUsuario.Administrador))
                return BadRequest("Solo se puede cambiar la contraseña de un apicultor.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, cambio.ContraseñaNueva);
            if (!resultado.Succeeded)
            {
                // Mismo formato que /cuenta/register: un error por código, la web los traduce.
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Code, error.Description);
                return ValidationProblem(ModelState);
            }

            // Si se había bloqueado por intentos fallidos, puede ingresar enseguida con la nueva.
            await _userManager.SetLockoutEndDateAsync(usuario, null);
            await _userManager.ResetAccessFailedCountAsync(usuario);
            return NoContent();
        }
    }
}
