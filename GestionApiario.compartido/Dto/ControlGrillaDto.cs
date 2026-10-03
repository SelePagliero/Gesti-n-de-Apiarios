namespace GestionApiario.compartido.Dto;

public class ControlGrillaDto
{
    public int Codigo { get; set; }

    public int? Campaña { get; set; }

    public string? Apiario { get; set; }

    public DateOnly Fecha { get; set; }

    public int? CantDeColmenas { get; set; }

    public string? Alimento { get; set; }

    public decimal? CantidadAlimento { get; set; }

    public string? Enfermedad { get; set; }

    public string? Producto { get; set; }

    public decimal? CantProducto { get; set; }
}