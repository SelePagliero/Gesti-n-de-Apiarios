using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionApiario.compartido.Dto
{
    public class CampañaGrillaDto
    {
        public int Codigo { get; set; }

        public int? Año { get; set; }

        public string? Responsable { get; set; }

        public DateTime? FechaAlta { get; set; }

        public DateTime? FechaModificacion { get; set; }
    }
}
