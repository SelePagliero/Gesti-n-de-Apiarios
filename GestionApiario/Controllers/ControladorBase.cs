using System.Security.Claims;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public abstract class ControladorBase : ControllerBase
    {
        // Email del usuario que hace la operación; se guarda en UsuarioAlta, UsuarioModificacion y UsuarioBaja.
        protected string? UsuarioActual => User.Identity?.Name;

        // Id del usuario (AspNetUsers.Id); es el que se guarda como dueño de apiarios y campañas.
        protected string? UsuarioIdActual => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected bool EsAdministrador => User.IsInRole(RolesUsuario.Administrador);

        // Decide el dueño de un apiario o una campaña. Si no se pide otro, queda el actual.
        // Solo la Administradora puede elegir un dueño distinto (un apicultor recibe 403).
        protected async Task<(string? Dueño, ActionResult? Error)> ResolverDueñoAsync(
            GestionApiariosContext context, string? dueñoPedido, string? dueñoActual)
        {
            if (string.IsNullOrEmpty(dueñoPedido) || dueñoPedido == dueñoActual)
                return (dueñoActual, null);

            if (!EsAdministrador)
                return (null, Forbid());

            if (!await context.Users.AnyAsync(u => u.Id == dueñoPedido))
                return (null, BadRequest("El apicultor no existe."));

            return (dueñoPedido, null);
        }
    }
}
