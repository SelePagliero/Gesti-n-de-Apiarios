namespace GestionApiario.compartido.Dto
{
    public class ControlDto
    {

        public int CodCampaña { get; set; }

        public int CodApiario { get; set; }

        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public int? CantDeColmenas { get; set; } = 0;

        public int CodAlimento { get; set; }

        public decimal? CantidadAlimento { get; set; } = 0;

        public int CodEnfermedad { get; set; }

        public int CodProductos { get; set; }

        public decimal? CantProducto { get; set; } = 0;

        public string Obsevaciones { get; set; }

    }
}
