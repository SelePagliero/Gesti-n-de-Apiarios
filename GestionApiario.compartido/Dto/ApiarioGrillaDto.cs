using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionApiario.compartido.Dto
{
    public class ApiarioGrillaDto
    {

        public int Codigo { get; set; }

        public string? Nombre { get; set; }

        public DateTime? FechaAlta { get; set; }

        public DateTime? FechaModificacion { get; set; }

        // Dueño del apiario.
        public string? UsuarioId { get; set; }

        // Email del dueño del apiario.
        public string? Apicultor { get; set; }
    }
}
