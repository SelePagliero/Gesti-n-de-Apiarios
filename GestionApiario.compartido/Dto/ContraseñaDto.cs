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

    // Respuesta de POST /usuarios/{id}/restablecer-contrasena. La contraseña no se guarda en ningún lado:
    // la Administradora la ve una sola vez.
    public class ContraseñaTemporalDto
    {
        public string Email { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty;
    }
}
