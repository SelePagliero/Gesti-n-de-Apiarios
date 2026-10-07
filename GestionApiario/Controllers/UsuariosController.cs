using System.Security.Claims;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Lista de usuarios para el filtro por apicultor y el cambio de dueño de un apiario,
    // y la pantalla Usuarios (restablecer contraseñas). Todo es solo para la Administradora.
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
            var conContraseñaTemporal = (await _userManager.GetUsersForClaimAsync(ClaimTemporal))
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
            {
                usuario.EsAdministrador = administradores.Contains(usuario.Id);
                usuario.TieneContraseñaTemporal = conContraseñaTemporal.Contains(usuario.Id);
            }
            return Ok(usuarios);
        }

        // Genera una contraseña temporal y la devuelve una sola vez. El usuario tiene que cambiarla al ingresar.
        [HttpPost("{id}/restablecer-contrasena")]
        public async Task<ActionResult<ContraseñaTemporalDto>> RestablecerContraseña(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return NotFound();

            if (usuario.Id == UsuarioIdActual)
                return BadRequest("Para cambiar tu propia contraseña usá la opción \"Cambiar contraseña\".");

            var contraseña = ContraseñaTemporal.Generar();
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, contraseña);
            if (!resultado.Succeeded)
                return Problem(string.Join(" ", resultado.Errors.Select(e => e.Description)));

            var claims = await _userManager.GetClaimsAsync(usuario);
            if (!claims.Any(c => c.Type == ContraseñaTemporal.TipoClaim))
                await _userManager.AddClaimAsync(usuario, ClaimTemporal);

            // Si se había bloqueado por intentos fallidos, puede ingresar enseguida con la temporal.
            await _userManager.SetLockoutEndDateAsync(usuario, null);
            await _userManager.ResetAccessFailedCountAsync(usuario);

            return Ok(new ContraseñaTemporalDto
            {
                Email = usuario.Email ?? usuario.UserName ?? usuario.Id,
                Contraseña = contraseña
            });
        }

        private static Claim ClaimTemporal => new(ContraseñaTemporal.TipoClaim, "true");
    }
}
