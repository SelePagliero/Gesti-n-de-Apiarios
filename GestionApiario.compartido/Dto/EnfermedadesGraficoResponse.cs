namespace GestionApiario.compartido.Dto
{
    public class EnfermedadesGraficoResponse
    {
        public List<string> labels { get; set; } = new();
        public List<EnfermedadesGraficoDto> datasets { get; set; } = new();
    }
    public class EnfermedadesGraficoDto
    {
        public List<int> data { get; set; } = new();
        public List<string> backgroundColor { get; set; } = new();
        public List<string> hoverBackgroundColor { get; set; } = new();
        public string hoverBorderColor { get; set; } = string.Empty;
    }
}
