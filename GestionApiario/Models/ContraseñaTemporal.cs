using System.Security.Cryptography;

namespace GestionApiario.Models;

// Contraseñas temporales que genera la Administradora al restablecer la contraseña de un usuario.
// El usuario queda marcado con un claim (AspNetUserClaims) hasta que elige una contraseña propia.
public static class ContraseñaTemporal
{
    public const string TipoClaim = "debe_cambiar_contrasena";

    public const string MensajeCambioObligatorio =
        "Tenés que cambiar la contraseña temporal antes de seguir usando el sistema.";

    // Lo único que puede hacer un usuario con contraseña temporal (además de ingresar y renovar el token).
    private static readonly string[] RutasPermitidas =
    {
        "/cuenta/login", "/cuenta/refresh", "/cuenta/yo", "/cuenta/cambiar-contrasena"
    };

    public static bool RutaPermitida(PathString ruta) =>
        RutasPermitidas.Any(r => ruta.Equals(r, StringComparison.OrdinalIgnoreCase));

    // Sin letras ni números que se confunden al dictarlos o copiarlos (I, l, O, 0, 1).
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Numeros = "23456789";
    private const int Largo = 12;

    // 12 caracteres al azar con al menos una mayúscula, una minúscula y un número,
    // así cumple las reglas de Identity configuradas en Program.cs.
    public static string Generar()
    {
        var todos = Mayusculas + Minusculas + Numeros;
        var caracteres = new List<char>
        {
            Elegir(Mayusculas),
            Elegir(Minusculas),
            Elegir(Numeros)
        };
        while (caracteres.Count < Largo)
            caracteres.Add(Elegir(todos));

        // Mezcla para que los obligatorios no queden siempre al principio.
        for (var i = caracteres.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }
        return new string(caracteres.ToArray());
    }

    private static char Elegir(string opciones) => opciones[RandomNumberGenerator.GetInt32(opciones.Length)];
}
