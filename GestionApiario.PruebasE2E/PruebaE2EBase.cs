using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GestionApiario.PruebasE2E
{
    // Ayudas comunes para las pruebas de navegador.
    public abstract class PruebaE2EBase : IAsyncLifetime
    {
        protected readonly EntornoE2E Entorno;
        private readonly List<IBrowserContext> _sesiones = new();

        protected PruebaE2EBase(EntornoE2E entorno)
        {
            Entorno = entorno;
            // Blazor Server tarda un poco en conectar cada página; se espera hasta 15 segundos.
            SetDefaultExpectTimeout(15_000);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var sesion in _sesiones)
                await sesion.CloseAsync();
        }

        // Email único por prueba, para que los datos de una prueba no interfieran con los de otra.
        protected static string EmailNuevo(string nombre) => $"{nombre}-{Guid.NewGuid().ToString("N")[..8]}@prueba.local";

        protected static string NombreUnico(string nombre) => $"{nombre} {Guid.NewGuid().ToString("N")[..6]}";

        // Registra un apicultor por la API y devuelve su email y un cliente de API con su sesión.
        protected async Task<(string Email, HttpClient Api)> NuevoApicultorAsync(string nombre)
        {
            var email = EmailNuevo(nombre);
            await Entorno.Api.RegistrarAsync(email);
            return (email, await Entorno.Api.ComoAsync(email));
        }

        protected Task<HttpClient> ApiAdministradoraAsync() => Entorno.Api.ComoAsync(EntornoE2E.EmailAdministradora);

        protected async Task<string> IdDeUsuarioAsync(string email)
        {
            var administradora = await ApiAdministradoraAsync();
            var usuarios = await administradora.GetFromJsonAsync<List<UsuarioDto>>("/usuarios");
            return usuarios!.Single(u => u.Email == email).Id;
        }

        // Abre una ventana nueva del navegador e inicia sesión desde la pantalla de ingreso, como lo haría una persona.
        protected async Task<IPage> IniciarSesionAsync(string email)
        {
            var sesion = await Entorno.Navegador.NewContextAsync(new BrowserNewContextOptions { BaseURL = EntornoE2E.UrlWeb });
            _sesiones.Add(sesion);
            var pagina = await sesion.NewPageAsync();
            pagina.SetDefaultTimeout(15_000);

            await pagina.GotoAsync("/login");
            await pagina.Locator("#email").FillAsync(email);
            await pagina.Locator("#password").FillAsync(EntornoE2E.Password);
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();
            await Expect(pagina.Locator("main .top-row")).ToContainTextAsync(email);
            return pagina;
        }

        // Navega a una página de lista y espera a que termine de cargar (tabla o mensaje de lista vacía).
        protected static async Task IrAListaAsync(IPage pagina, string ruta)
        {
            await pagina.GotoAsync(ruta);
            await pagina.Locator("table, p.text-muted").First.WaitForAsync();
        }

        protected static ILocator Fila(IPage pagina, string texto) => pagina.Locator("tbody tr", new() { HasText = texto });

        // Encabezado de columna de la tabla, buscado por su texto exacto.
        protected static ILocator Columna(IPage pagina, string titulo) =>
            pagina.Locator("thead th", new() { HasTextRegex = new System.Text.RegularExpressions.Regex($@"^\s*{System.Text.RegularExpressions.Regex.Escape(titulo)}\s*$") });

        protected static async Task VerificarTableroAsync(IPage pagina, int apiarios, int colmenas, int conEnfermedad)
        {
            await Expect(pagina.Locator("#tablero-apiarios")).ToHaveTextAsync(apiarios.ToString());
            await Expect(pagina.Locator("#tablero-colmenas")).ToHaveTextAsync(colmenas.ToString());
            await Expect(pagina.Locator("#tablero-enfermos")).ToHaveTextAsync(conEnfermedad.ToString());
        }

        protected async Task<int> CrearEnfermedadAsync(string nombre) =>
            await ClienteApi.CrearAsync(await ApiAdministradoraAsync(), "/enfermedad", new EnfermedadDto { Nombre = nombre });
    }
}
