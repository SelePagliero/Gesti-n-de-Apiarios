using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionApiario.compartido.Dto
{
    public class DashBoardDto
    {
        public int ApiariosActivos{ get; set; }

        public int CantidadTotalDeColmenas { get; set; }

        public int ApiariosConEnfermedades { get; set; }
    }
}
