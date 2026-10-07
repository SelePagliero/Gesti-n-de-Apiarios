using System.Net;
using System.Text;
using GestionApiario.compartido.Dto;
using GestionApiario.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace GestionApiario.Controllers
{
    // Endpoints propios de /cuenta que no trae MapIdentityApi (ver Program.cs).
    [Route("cuenta")]
    public class CuentaController : ControladorBase
    {
        public const string LimiteRecuperacion = "recuperacion";
        private const string MensajeLinkInvalido = "El link no es válido o ya venció. Pedí uno nuevo desde \"¿Olvidaste tu contraseña?\".";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEnviadorCorreo _correo;
        private readonly IConfiguration _configuracion;
        private readonly ILogger<CuentaController> _log;

        public CuentaController(UserManager<IdentityUser> userManager, IEnviadorCorreo correo,
            IConfiguration configuracion, ILogger<CuentaController> log)
        {
            _userManager = userManager;
            _correo = correo;
            _configuracion = configuracion;
            _log = log;
        }

        // Cualquier usuario cambia su propia contraseña.
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
                return ErroresIdentity(resultado);

            return NoContent();
        }

        // Envía por correo un link para elegir una contraseña nueva. Responde lo mismo exista o no el email,
        // para que no se pueda averiguar quién está registrado.
        [AllowAnonymous]
        [EnableRateLimiting(LimiteRecuperacion)]
        [HttpPost("olvide-contrasena")]
        public async Task<IActionResult> OlvideContraseña(OlvideContraseñaDto pedido)
        {
            var urlWeb = _configuracion["Web:UrlBase"]?.TrimEnd('/');
            if (!_correo.Configurado || string.IsNullOrWhiteSpace(urlWeb))
            {
                _log.LogError("No se puede recuperar la contraseña: falta configurar la sección \"Correo\" o \"Web:UrlBase\".");
                return Problem("Por ahora no se pueden enviar correos. Pedile ayuda a la Administradora.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var usuario = await _userManager.FindByEmailAsync(pedido.Email.Trim());
            if (usuario?.Email is not null)
            {
                // El código vence en una hora (Program.cs) y deja de servir apenas se cambia la contraseña.
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var codigo = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = $"{urlWeb}/restablecer-contrasena?email={Uri.EscapeDataString(usuario.Email)}&codigo={codigo}";
                try
                {
                    await _correo.EnviarAsync(usuario.Email, "Elegí una contraseña nueva", CuerpoHtml(link), CuerpoTexto(link));
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "No se pudo enviar el correo para recuperar la contraseña.");
                }
            }

            return NoContent();
        }

        [AllowAnonymous]
        [EnableRateLimiting(LimiteRecuperacion)]
        [HttpPost("restablecer-contrasena")]
        public async Task<IActionResult> RestablecerContraseña(RestablecimientoContraseñaDto restablecimiento)
        {
            var usuario = await _userManager.FindByEmailAsync(restablecimiento.Email.Trim());
            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(restablecimiento.Codigo));
            }
            catch (FormatException)
            {
                return BadRequest(MensajeLinkInvalido);
            }
            if (usuario is null)
                return BadRequest(MensajeLinkInvalido);

            var resultado = await _userManager.ResetPasswordAsync(usuario, token, restablecimiento.ContraseñaNueva);
            if (!resultado.Succeeded)
            {
                if (resultado.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                    return BadRequest(MensajeLinkInvalido);
                return ErroresIdentity(resultado);
            }

            // Si se había bloqueado por intentos fallidos, puede ingresar enseguida con la nueva.
            await _userManager.SetLockoutEndDateAsync(usuario, null);
            await _userManager.ResetAccessFailedCountAsync(usuario);
            return NoContent();
        }

        // Mismo formato que /cuenta/register: un error por código, la web los traduce.
        private IActionResult ErroresIdentity(IdentityResult resultado)
        {
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        private static string CuerpoTexto(string link) =>
            "Hola:\n\n" +
            "Pediste elegir una contraseña nueva para Gestión de Apiarios. Abrí este link (vence en una hora):\n\n" +
            $"{link}\n\n" +
            "Si no lo pediste vos, ignorá este correo: tu contraseña no cambia.";

        private static string CuerpoHtml(string link)
        {
            var href = WebUtility.HtmlEncode(link);
            return $"""
                <div style="font-family:Arial,sans-serif;color:#2E1F14;max-width:480px;margin:0 auto;padding:24px;background:#F7F2E8">
                  <div style="background:#FFFDF8;border:1px solid #E8DCC6;border-radius:14px;padding:24px">
                    <h2 style="margin:0 0 12px;font-size:20px">Elegí una contraseña nueva</h2>
                    <p style="margin:0 0 20px;color:#6E5A45">Pediste elegir una contraseña nueva para Gestión de Apiarios. El link vence en una hora.</p>
                    <p style="margin:0 0 20px"><a href="{href}" style="display:inline-block;background:#2E1F14;color:#FFF8EC;text-decoration:none;padding:12px 20px;border-radius:10px;font-weight:bold">Elegir contraseña nueva</a></p>
                    <p style="margin:0;color:#6E5A45;font-size:13px">Si no lo pediste vos, ignorá este correo: tu contraseña no cambia.</p>
                  </div>
                </div>
                """;
        }
    }
}
