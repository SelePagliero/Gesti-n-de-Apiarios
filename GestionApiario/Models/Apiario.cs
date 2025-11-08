using System;
using System.Collections.Generic;

namespace GestionApiario.Models;

public partial class Apiario
{
    public int Codigo { get; set; }

    public string? Nombre { get; set; }

    public string? Empresa { get; set; }

    public string? UsuarioAlta { get; set; }

    public DateTime? FechaAlta { get; set; }

    public string? UsuarioBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public string? Latitud { get; set; }

    public string? Longitud { get; set; }

    public virtual ICollection<Controle> Controles { get; set; } = new List<Controle>();
}
