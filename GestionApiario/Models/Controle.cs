using System;
using System.Collections.Generic;

namespace GestionApiario.Models;

public partial class Controle
{
    public int Codigo { get; set; }

    public int? CodCampaña { get; set; }

    public int? CodApiario { get; set; }

    public DateOnly? Fecha { get; set; }

    public int? CantDeColmenas { get; set; }

    public int? CodAlimento { get; set; }

    public decimal? CantidadAlimento { get; set; }

    public int? CodEnfermedad { get; set; }

    public int? CodProductos { get; set; }

    public decimal? CantProducto { get; set; }

    public string? Observaciones { get; set; }

    public string? UsuarioAlta { get; set; }

    public DateTime? FechaAlta { get; set; }

    public string? UsuarioBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }

    public virtual Alimento? CodAlimentoNavigation { get; set; }

    public virtual Apiario? CodApiarioNavigation { get; set; }

    public virtual Campaña? CodCampañaNavigation { get; set; }

    public virtual Enfermedad? CodEnfermedadNavigation { get; set; }

    public virtual Producto? CodProductosNavigation { get; set; }
}
