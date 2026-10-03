using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto
{
    public class ApiarioDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string? Nombre { get; set; }

        [StringLength(100, ErrorMessage = "La empresa no puede superar los 100 caracteres.")]
        public string? Empresa { get; set; }

        [StringLength(50, ErrorMessage = "La latitud no puede superar los 50 caracteres.")]
        public string? Latitud { get; set; }

        [StringLength(50, ErrorMessage = "La longitud no puede superar los 50 caracteres.")]
        public string? Longitud { get; set; }
    }
}
