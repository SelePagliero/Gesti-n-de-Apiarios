using System.ComponentModel.DataAnnotations;

namespace GestionApiario.compartido.Dto
{
    // Filtros opcionales de GET /controles/vertodos. Los que quedan en null no se aplican.
    public class FiltroControlesDto : IValidatableObject
    {
        public const string MensajeRangoInvalido = "La fecha desde no puede ser posterior a la fecha hasta.";

        public int? CodApiario { get; set; }

        public int? CodCampaña { get; set; }

        public int? CodEnfermedad { get; set; }

        // true: solo controles que registraron alguna enfermedad (cualquiera).
        public bool? ConAlgunaEnfermedad { get; set; }

        // Rango inclusivo en los dos extremos.
        public DateOnly? FechaDesde { get; set; }

        public DateOnly? FechaHasta { get; set; }

        public bool RangoDeFechasValido => FechaDesde is null || FechaHasta is null || FechaDesde <= FechaHasta;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!RangoDeFechasValido)
                yield return new ValidationResult(MensajeRangoInvalido, [nameof(FechaDesde), nameof(FechaHasta)]);
        }
    }
}
