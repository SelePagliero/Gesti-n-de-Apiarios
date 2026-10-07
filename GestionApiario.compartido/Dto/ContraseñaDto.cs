using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto
{
    // POST /cuenta/cambiar-contrasena: cualquier usuario cambia su propia contraseña.
    public class CambioContraseñaDto
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
        public string ContraseñaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la contraseña nueva.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña nueva debe tener al menos 8 caracteres.")]
        public string ContraseñaNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña nueva.")]
        [Compare(nameof(ContraseñaNueva), ErrorMessage = "Las dos contraseñas nuevas no coinciden.")]
        public string ConfirmacionContraseña { get; set; } = string.Empty;
    }

    // POST /cuenta/olvide-contrasena: pide el link para elegir una contraseña nueva.
    public class OlvideContraseñaDto
    {
        [Required(ErrorMessage = "Ingresá tu email.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;
    }

    // POST /cuenta/restablecer-contrasena: la contraseña nueva, con el código que llegó en el link del correo.
    public class RestablecimientoContraseñaDto
    {
        [Required(ErrorMessage = "El link no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El link no es válido.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la contraseña nueva.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña nueva debe tener al menos 8 caracteres.")]
        public string ContraseñaNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña nueva.")]
        [Compare(nameof(ContraseñaNueva), ErrorMessage = "Las dos contraseñas nuevas no coinciden.")]
        public string ConfirmacionContraseña { get; set; } = string.Empty;
    }
}
