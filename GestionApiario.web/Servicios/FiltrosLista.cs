using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace GestionApiario.web.Servicios
{
    // Ayudas para los filtros de los listados. Los filtros se guardan en la URL (?nombre=...&desde=...)
    // para que se conserven al recargar la página.
    public static class FiltrosLista
    {
        private static readonly CompareInfo Comparador = new CultureInfo("es-AR").CompareInfo;

        // true si el texto contiene lo buscado, sin distinguir mayúsculas ni tildes ("algarrobo" encuentra "El Algarróbo").
        public static bool Contiene(string? texto, string? buscado) =>
            string.IsNullOrWhiteSpace(buscado)
            || (texto is not null && Comparador.IndexOf(texto, buscado.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);

        // true si la fecha está entre desde y hasta (incluidos). Sin fecha no pasa si hay algún límite.
        public static bool EntreFechas(DateTime? fecha, DateOnly? desde, DateOnly? hasta)
        {
            if (desde is null && hasta is null)
                return true;
            if (fecha is null)
                return false;
            var dia = DateOnly.FromDateTime(fecha.Value);
            return (desde is null || dia >= desde) && (hasta is null || dia <= hasta);
        }

        // Cambia un parámetro de la URL sin agregar una entrada al historial del navegador.
        public static void CambiarParametro(this NavigationManager navegacion, string parametro, string? valor) =>
            navegacion.NavigateTo(
                navegacion.GetUriWithQueryParameter(parametro, string.IsNullOrWhiteSpace(valor) ? null : valor),
                replace: true);

        // Un valor inválido en la URL se ignora en lugar de romper la página.
        public static int? LeerEntero(string? valor) =>
            int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero > 0 ? numero : null;

        public static DateOnly? LeerFecha(string? valor) =>
            DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha) ? fecha : null;

        public static string FormatoUrl(DateOnly? fecha) => fecha?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

        public const string MensajeRangoInvalido = "La fecha \"Desde\" no puede ser posterior a \"Hasta\".";
    }
}
