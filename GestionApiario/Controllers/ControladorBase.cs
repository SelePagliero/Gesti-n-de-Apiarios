using Microsoft.AspNetCore.Mvc;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public abstract class ControladorBase : ControllerBase
    {
        // Las columnas UsuarioAlta, UsuarioModificacion y UsuarioBaja admiten hasta 50 caracteres.
        private const int LargoMaximoUsuario = 50;

        protected string? UsuarioActual
        {
            get
            {
                var nombre = User.Identity?.Name;
                if (string.IsNullOrEmpty(nombre))
                    return null;
                return nombre.Length <= LargoMaximoUsuario ? nombre : nombre[..LargoMaximoUsuario];
            }
        }
    }
}
