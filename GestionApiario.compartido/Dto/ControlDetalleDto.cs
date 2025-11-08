namespace GestionApiario.compartido.Dto
{
    public class ControlDetalleDto
    {

        public int CodCampaña { get; set; }

        public int CodApiario { get; set; }

        public DateOnly Fecha { get; set; }

        public int CantDeColmenas { get; set; }

        public int? CodAlimento { get; set; }

        public decimal CantidadAlimento { get; set; }

        public int? CodEnfermedad { get; set; }

        public int? CodProductos { get; set; }

        public decimal CantProducto { get; set; }

        public string Obsevaciones { get; set; }

    }
}
