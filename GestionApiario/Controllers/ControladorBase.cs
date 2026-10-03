using System.Security.Claims;
using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Mvc;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public abstract class ControladorBase : ControllerBase
    {
        // Email del usuario que hace la operación; se guarda en UsuarioAlta, UsuarioModificacion y UsuarioBaja.
        protected string? UsuarioActual => User.Identity?.Name;

        // Id del usuario (AspNetUsers.Id); es el que se guarda como dueño del apiario.
        protected string? UsuarioIdActual => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected bool EsAdministrador => User.IsInRole(RolesUsuario.Administrador);
    }
}
