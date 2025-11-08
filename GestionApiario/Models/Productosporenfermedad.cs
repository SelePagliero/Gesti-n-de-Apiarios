using System;
using System.Collections.Generic;

namespace GestionApiario.Models;

public partial class Productosporenfermedad
{
    public int CodEnfermedad { get; set; }

    public int CodProducto { get; set; }

    public string? UsuarioAlta { get; set; }

    public DateTime? FechaAlta { get; set; }

    public string? UsuarioBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual Enfermedad CodEnfermedadNavigation { get; set; } = null!;

    public virtual Producto CodProductoNavigation { get; set; } = null!;
}
