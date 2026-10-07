using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GestionApiario.PruebasE2E
{
    // Recuperar contraseñas: la pantalla Usuarios, el cambio obligatorio de la temporal, "Cambiar contraseña"
    // y el aviso del ingreso. Nunca se cambia la contraseña de la Administradora de prueba, porque la usan las demás pruebas.
    [Collection(ColeccionE2E.Nombre)]
    public class PasosContraseñasE2E : PruebaE2EBase
    {
        public PasosContraseñasE2E(EntornoE2E entorno) : base(entorno) { }

        [Fact(DisplayName = "Paso 17 - La Administradora restablece la contraseña de un apicultor desde Usuarios")]
        public async Task Paso17()
        {
            var (email, _) = await NuevoApicultorAsync("olvido");
            var pagina = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);

            await pagina.GetByRole(AriaRole.Link, new() { Name = "Usuarios" }).ClickAsync();
            await Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Usuarios" })).ToBeVisibleAsync();
            var fila = Fila(pagina, email);
            await Expect(fila).ToContainTextAsync("Apicultor");
            await Expect(fila).ToContainTextAsync("Propia");
            // A sí misma no se la puede restablecer desde acá.
            await Expect(Fila(pagina, EntornoE2E.EmailAdministradora).GetByRole(AriaRole.Button)).ToHaveCountAsync(0);

            pagina.Dialog += async (_, dialogo) => await dialogo.AcceptAsync();
            await fila.GetByRole(AriaRole.Button, new() { Name = $"Restablecer la contraseña de {email}" }).ClickAsync();

            var contraseña = pagina.Locator("#contrasena-temporal");
            await Expect(contraseña).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^[A-Za-z0-9]{12}$"));
            await Expect(pagina.Locator(".tarjeta-contrasena")).ToContainTextAsync(email);
            await Expect(fila).ToContainTextAsync("Temporal");
            await Capturas.GuardarAsync(pagina, "usuarios-contrasena-temporal");

            // La contraseña mostrada es la que sirve para ingresar.
            var temporal = await contraseña.InnerTextAsync();
            var login = await new HttpClient().PostAsJsonAsync($"{EntornoE2E.UrlApi}/cuenta/login", new { email, password = temporal });
            Assert.True(login.IsSuccessStatusCode);

            // Se muestra una sola vez: al tocar "Listo" desaparece y no hay forma de volver a verla.
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Listo" }).ClickAsync();
            await Expect(pagina.Locator(".tarjeta-contrasena")).ToHaveCountAsync(0);
            await pagina.ReloadAsync();
            await Expect(Fila(pagina, email)).ToContainTextAsync("Temporal");
            await Expect(pagina.Locator("body")).Not.ToContainTextAsync(temporal);
        }

        [Fact(DisplayName = "Paso 18 - Un apicultor no ve Usuarios en el menú ni puede entrar a la pantalla")]
        public async Task Paso18()
        {
            var (email, _) = await NuevoApicultorAsync("curioso");
            var pagina = await IniciarSesionAsync(email);

            await Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Usuarios" })).ToHaveCountAsync(0);
            await pagina.GotoAsync("/usuarios");
            await Expect(pagina.GetByText("No tenés permiso para ver esta pantalla.")).ToBeVisibleAsync();
            await Expect(pagina.Locator("table")).ToHaveCountAsync(0);
        }

        [Fact(DisplayName = "Paso 19 - Quien entra con la contraseña temporal tiene que cambiarla antes de usar el sistema")]
        public async Task Paso19()
        {
            var (email, _) = await NuevoApicultorAsync("temporal");
            var temporal = await RestablecerAsync(email);

            var pagina = await AbrirLoginAsync(email, temporal);
            await Expect(pagina).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/contrasena-temporal$"));
            await Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Elegí una contraseña nueva" })).ToBeVisibleAsync();
            await Expect(pagina.GetByText(email)).ToBeVisibleAsync();
            await Capturas.GuardarAsync(pagina, "contrasena-temporal");

            // No puede ir a otra pantalla escribiendo la dirección.
            await pagina.GotoAsync("/apiarios");
            await Expect(pagina).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/contrasena-temporal$"));
            await Expect(pagina.Locator(".sidebar")).ToHaveCountAsync(0);

            // Si se equivoca con la temporal, avisa.
            await LlenarCambioAsync(pagina, "OtraCosa123", "MielPropia2026", "MielPropia2026");
            await Expect(pagina.GetByText("La contraseña actual no es correcta.")).ToBeVisibleAsync();

            await LlenarCambioAsync(pagina, temporal, "MielPropia2026", "MielPropia2026");
            await Expect(pagina.Locator("main .top-row")).ToContainTextAsync(email);
            await Expect(pagina.Locator("#tablero-apiarios")).ToBeVisibleAsync();

            // Desde ahora ingresa con la contraseña propia, sin pasar por el cambio obligatorio.
            var otraVentana = await AbrirLoginAsync(email, "MielPropia2026");
            await Expect(otraVentana.Locator("main .top-row")).ToContainTextAsync(email);
            var vieja = await new HttpClient().PostAsJsonAsync($"{EntornoE2E.UrlApi}/cuenta/login", new { email, password = temporal });
            Assert.False(vieja.IsSuccessStatusCode);
        }

        [Fact(DisplayName = "Paso 20 - Cualquier usuario cambia su propia contraseña desde el encabezado")]
        public async Task Paso20()
        {
            var (email, _) = await NuevoApicultorAsync("cambia");
            var pagina = await IniciarSesionAsync(email);

            await pagina.Locator("main .top-row").GetByRole(AriaRole.Link, new() { Name = "Cambiar contraseña" }).ClickAsync();
            await Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Cambiar contraseña" })).ToBeVisibleAsync();

            await LlenarCambioAsync(pagina, EntornoE2E.Password, "PanalNuevo2026", "PanalDistinto2026");
            await Expect(pagina.GetByText("Las dos contraseñas nuevas no coinciden.")).ToBeVisibleAsync();

            await LlenarCambioAsync(pagina, "Incorrecta123", "PanalNuevo2026", "PanalNuevo2026");
            await Expect(pagina.GetByText("La contraseña actual no es correcta.")).ToBeVisibleAsync();

            await LlenarCambioAsync(pagina, EntornoE2E.Password, "PanalNuevo2026", "PanalNuevo2026");
            await Expect(pagina.GetByText("Tu contraseña se cambió.")).ToBeVisibleAsync();

            // Sigue con la sesión abierta y puede usar el sistema.
            await IrAListaAsync(pagina, "/apiarios");

            var otraVentana = await AbrirLoginAsync(email, "PanalNuevo2026");
            await Expect(otraVentana.Locator("main .top-row")).ToContainTextAsync(email);
        }

        [Fact(DisplayName = "Paso 21 - En el ingreso hay un aviso para quien olvidó la contraseña, sin botón ni formulario")]
        public async Task Paso21()
        {
            var sesion = await Entorno.Navegador.NewContextAsync(new BrowserNewContextOptions { BaseURL = EntornoE2E.UrlWeb });
            var pagina = await sesion.NewPageAsync();
            await pagina.GotoAsync("/login");

            var aviso = pagina.Locator("#aviso-olvido");
            await Expect(aviso).ToHaveTextAsync("¿Olvidaste tu contraseña? Pedile a la administradora que te la restablezca.");
            await Expect(aviso.Locator("a, button, input")).ToHaveCountAsync(0);
            // Está debajo del botón Ingresar.
            var boton = await pagina.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).BoundingBoxAsync();
            var texto = await aviso.BoundingBoxAsync();
            Assert.True(texto!.Y > boton!.Y + boton.Height);
            await pagina.WaitForTimeoutAsync(500);
            await Capturas.GuardarAsync(pagina, "ingreso");
        }

        private async Task<string> RestablecerAsync(string email)
        {
            var administradora = await ApiAdministradoraAsync();
            var respuesta = await administradora.PostAsync($"/usuarios/{await IdDeUsuarioAsync(email)}/restablecer-contrasena", null);
            respuesta.EnsureSuccessStatusCode();
            return (await respuesta.Content.ReadFromJsonAsync<ContraseñaTemporalDto>())!.Contraseña;
        }

        // Como IniciarSesionAsync, pero con cualquier contraseña y sin esperar el tablero.
        private async Task<IPage> AbrirLoginAsync(string email, string password)
        {
            var sesion = await Entorno.Navegador.NewContextAsync(new BrowserNewContextOptions { BaseURL = EntornoE2E.UrlWeb });
            var pagina = await sesion.NewPageAsync();
            pagina.SetDefaultTimeout(15_000);
            await pagina.GotoAsync("/login");
            await pagina.Locator("#email").FillAsync(email);
            await pagina.Locator("#password").FillAsync(password);
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();
            return pagina;
        }

        private static async Task LlenarCambioAsync(IPage pagina, string actual, string nueva, string repetida)
        {
            await pagina.Locator("#contrasena-actual").FillAsync(actual);
            await pagina.Locator("#contrasena-nueva").FillAsync(nueva);
            await pagina.Locator("#contrasena-repetida").FillAsync(repetida);
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Cambiar contraseña" }).ClickAsync();
        }
    }

    // Solo para revisar el diseño: si existe la variable E2E_CAPTURAS, guarda capturas de pantalla en esa carpeta.
    internal static class Capturas
    {
        public static async Task GuardarAsync(IPage pagina, string nombre)
        {
            var carpeta = Environment.GetEnvironmentVariable("E2E_CAPTURAS");
            if (string.IsNullOrEmpty(carpeta))
                return;
            await pagina.ScreenshotAsync(new() { Path = Path.Combine(carpeta, $"{nombre}.png"), FullPage = true });
        }
    }
}
