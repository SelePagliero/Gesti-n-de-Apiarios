using System;
using System.Collections.Generic;

namespace GestionApiario.Models;

public partial class Campaña
{
    public int Codigo { get; set; }

    public int? Año { get; set; }

    public string? Responsable { get; set; }

    public string? UsuarioAlta { get; set; }

    public DateTime? FechaAlta { get; set; }

    public string? UsuarioBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual ICollection<Controle> Controles { get; set; } = new List<Controle>();
}
