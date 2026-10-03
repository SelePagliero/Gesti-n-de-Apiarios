using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto
{
    public class CampañaDto
    {
        [Required(ErrorMessage = "El año es obligatorio.")]
        [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100.")]
        public int? Año { get; set; }

        [StringLength(100, ErrorMessage = "El responsable no puede superar los 100 caracteres.")]
        public string? Responsable { get; set; }
    }
}
