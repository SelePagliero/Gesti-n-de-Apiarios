using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto;
public class ProductoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
