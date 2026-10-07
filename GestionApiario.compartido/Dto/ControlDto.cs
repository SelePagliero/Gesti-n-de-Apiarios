using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto
{
    public class ControlDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una campaña.")]
        public int CodCampaña { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un apiario.")]
        public int CodApiario { get; set; }

        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de colmenas no puede ser negativa.")]
        public int? CantDeColmenas { get; set; } = 0;

        public int CodAlimento { get; set; }

        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "La cantidad de alimento no puede ser negativa.")]
        public decimal? CantidadAlimento { get; set; } = 0;

        public int CodEnfermedad { get; set; }

        public int CodProductos { get; set; }

        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "La cantidad de producto no puede ser negativa.")]
        public decimal? CantProducto { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
        public string? Observaciones { get; set; }
    }
}
