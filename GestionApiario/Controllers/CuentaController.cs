using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionApiario.Controllers
{
    // Endpoints propios de /cuenta que no trae MapIdentityApi (ver Program.cs).
    [Route("cuenta")]
    public class CuentaController : ControladorBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public CuentaController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Cualquier usuario cambia su propia contraseña. Si tenía una temporal, deja de estar obligado a cambiarla.
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContraseña(CambioContraseñaDto cambio)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario is null)
                return Unauthorized();

            if (cambio.ContraseñaNueva == cambio.ContraseñaActual)
            {
                ModelState.AddModelError(nameof(cambio.ContraseñaNueva), "La contraseña nueva tiene que ser distinta de la actual.");
                return ValidationProblem(ModelState);
            }

            var resultado = await _userManager.ChangePasswordAsync(usuario, cambio.ContraseñaActual, cambio.ContraseñaNueva);
            if (!resultado.Succeeded)
            {
                // Mismo formato que /cuenta/register: un error por código, la web los traduce.
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Code, error.Description);
                return ValidationProblem(ModelState);
            }

            var temporales = (await _userManager.GetClaimsAsync(usuario))
                .Where(c => c.Type == ContraseñaTemporal.TipoClaim)
                .ToList();
            if (temporales.Count > 0)
                await _userManager.RemoveClaimsAsync(usuario, temporales);

            return NoContent();
        }
    }
}
