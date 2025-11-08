using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionApiario.compartido.Dto
{
    public class EnfermedadesGraficoResponse
    {

        public List<string> labels { get; set; }
        public List<EnfermedadesGraficoDto> datasets { get; set; }
    }

    public class EnfermedadesGraficoDto
    {
        public List<int> data { get; set; }
        public List<string> backgroundColor { get; set; }
        public List<string> hoverBackgroundColor { get; set; }
        public string hoverBorderColor { get; set; }
    }
}
