using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionApiario.compartido.Dto
{
    public class ProductoDetalleDto
    {
        public int Codigo { get; set; }

        public string? Nombre { get; set; }

        public string? UsuarioAlta { get; set; }

        public DateTime? FechaAlta { get; set; }

        public string? UsuarioBaja { get; set; }

        public DateTime? FechaBaja { get; set; }

        public DateTime? FechaModificacion { get; set; }

        public string? UsuarioModificacion { get; set; }

    }
}
