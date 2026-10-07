using System.Net;
using System.Text.Json;

namespace GestionApiario.web.Servicios
{
    // Convierte las respuestas de error de la API en mensajes legibles para mostrar en pantalla.
    public static class ErroresApi
    {
        public const string SinConexion = "No se pudo conectar con la API. Verificá que el proyecto GestionApiario esté en ejecución.";

        public static async Task<string> LeerMensajeAsync(HttpResponseMessage respuesta)
        {
            if (respuesta.StatusCode == HttpStatusCode.NotFound)
                return "El registro no existe o fue eliminado.";
            if (respuesta.StatusCode == HttpStatusCode.Forbidden)
                return "No tenés permiso para realizar esta acción.";

            var contenido = await respuesta.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contenido))
                return $"La operación falló ({(int)respuesta.StatusCode} {respuesta.ReasonPhrase}).";

            // Errores de validación de [ApiController]: { "title": ..., "errors": { "Campo": ["mensaje"] } }
            try
            {
                using var json = JsonDocument.Parse(contenido);
                var raiz = json.RootElement;
                if (raiz.ValueKind == JsonValueKind.String)
                    return raiz.GetString()!;

                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                    {
                        var mensajes = errores.EnumerateObject()
                            .Select(e => TraducirErrorIdentity(e.Name, e.Value))
                            .Where(m => !string.IsNullOrWhiteSpace(m));
                        return string.Join(" ", mensajes);
                    }
                    if (raiz.TryGetProperty("detail", out var detalle) && detalle.ValueKind == JsonValueKind.String)
                        return detalle.GetString()!;
                    if (raiz.TryGetProperty("title", out var titulo) && titulo.ValueKind == JsonValueKind.String)
                        return titulo.GetString()!;
                }
            }
            catch (JsonException)
            {
                // No es JSON: la API devolvió el mensaje como texto plano (por ejemplo, BadRequest("El apiario no existe.")).
            }

            return contenido;
        }

        // ASP.NET Core Identity devuelve un código por cada problema (por ejemplo "PasswordTooShort");
        // los conocidos se traducen y el resto (por ejemplo, errores de validación de un campo) se muestra tal cual.
        public static string TraducirErrorIdentity(string codigo, JsonElement mensajesOriginales) => codigo switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese email.",
            "InvalidEmail" or "InvalidUserName" => "El email no es válido.",
            "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
            "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
            "PasswordRequiresLower" => "La contraseña debe tener al menos una letra minúscula.",
            "PasswordRequiresUpper" => "La contraseña debe tener al menos una letra mayúscula.",
            "PasswordRequiresUniqueChars" => "La contraseña debe tener más caracteres distintos.",
            "PasswordMismatch" => "La contraseña actual no es correcta.",
            _ => string.Join(" ", mensajesOriginales.EnumerateArray().Select(m => m.GetString()))
        };
    }
}
