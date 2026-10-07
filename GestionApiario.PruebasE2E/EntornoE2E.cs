using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using GestionApiario.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace GestionApiario.PruebasE2E
{
    // Levanta, una sola vez para todas las pruebas de navegador:
    //  - una base de datos de prueba nueva en LocalDB (nunca la base real),
    //  - la API y la web como procesos reales en puertos propios,
    //  - Microsoft Edge manejado por Playwright.
    public class EntornoE2E : IAsyncLifetime
    {
        // Base de prueba: otra instancia de SQL Server (LocalDB) y otro nombre de base.
        public const string NombreBase = "GestionApiarios_PruebasE2E";
        public static readonly string CadenaConexion =
            $"Server=(localdb)\\MSSQLLocalDB;Database={NombreBase};Trusted_Connection=True;TrustServerCertificate=True;";

        public const string UrlApi = "http://localhost:5390";
        public const string UrlWeb = "http://localhost:5391";

        public const string EmailAdministradora = "administradora@prueba.local";
        public const string Password = "Colmena2026";

        // true: muestra Edge en pantalla (para mirar las pruebas). Se activa con la variable de entorno E2E_VISIBLE=1.
        private static readonly bool NavegadorVisible = Environment.GetEnvironmentVariable("E2E_VISIBLE") == "1";

        private Process? _api;
        private Process? _web;
        private IPlaywright? _playwright;

        public IBrowser Navegador { get; private set; } = null!;
        public ClienteApi Api { get; } = new(UrlApi);

        public async Task InitializeAsync()
        {
            VerificarQueEsLaBaseDePrueba();
            await CrearBaseDePruebaAsync();

            // La API asigna el rol de Administradora al arrancar, así que se registra la cuenta y se reinicia la API.
            _api = IniciarApi();
            await EsperarAsync($"{UrlApi}/cuenta/yo", "la API");
            await Api.RegistrarAsync(EmailAdministradora);
            Detener(_api);
            _api = IniciarApi();
            await EsperarAsync($"{UrlApi}/cuenta/yo", "la API");

            _web = IniciarWeb();
            await EsperarAsync($"{UrlWeb}/login", "la web");

            _playwright = await Playwright.CreateAsync();
            Navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "msedge",
                Headless = !NavegadorVisible,
                SlowMo = NavegadorVisible ? 150 : 0
            });
        }

        public async Task DisposeAsync()
        {
            if (Navegador is not null)
                await Navegador.CloseAsync();
            _playwright?.Dispose();
            Detener(_web);
            Detener(_api);
            // La base de prueba queda creada para poder revisarla si algo falló; se borra al empezar la próxima ejecución.
        }

        // Defensa extra: nunca correr contra otra base que no sea la de prueba en LocalDB.
        private static void VerificarQueEsLaBaseDePrueba()
        {
            if (!CadenaConexion.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) || !CadenaConexion.Contains(NombreBase))
                throw new InvalidOperationException("Las pruebas E2E solo pueden usar la base de prueba en LocalDB.");
        }

        private static async Task CrearBaseDePruebaAsync()
        {
            var opciones = new DbContextOptionsBuilder<GestionApiariosContext>().UseSqlServer(CadenaConexion).Options;
            await using var contexto = new GestionApiariosContext(opciones);
            await contexto.Database.EnsureDeletedAsync();
            await contexto.Database.MigrateAsync();
        }

        // Entorno "Pruebas": en ese entorno .NET no carga los secretos de usuario, así que la API
        // no puede leer la cadena de conexión real; la de prueba se pasa por línea de comandos.
        private static Process IniciarApi() => IniciarProceso("GestionApiario", "Pruebas", UrlApi,
            $"--ConnectionStrings:DefaultConnection={CadenaConexion}",
            $"--Administracion:Email={EmailAdministradora}");

        // La web no tiene secretos; corre en Development para servir sus archivos estáticos desde la compilación.
        private static Process IniciarWeb() => IniciarProceso("GestionApiario.web", "Development", UrlWeb,
            $"--Api:UrlBase={UrlApi}");

        private static Process IniciarProceso(string proyecto, string entorno, string url, params string[] argumentos)
        {
            var carpetaProyecto = Path.Combine(CarpetaSolucion(), proyecto);
            var configuracion = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}") ? "Release" : "Debug";
            var dll = Path.Combine(carpetaProyecto, "bin", configuracion, "net8.0", $"{proyecto}.dll");
            if (!File.Exists(dll))
                throw new FileNotFoundException($"No se encontró {dll}. Compilá la solución antes de correr las pruebas.");

            var inicio = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = carpetaProyecto,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            inicio.ArgumentList.Add(dll);
            foreach (var argumento in argumentos)
                inicio.ArgumentList.Add(argumento);
            inicio.Environment["ASPNETCORE_ENVIRONMENT"] = entorno;
            inicio.Environment["ASPNETCORE_URLS"] = url;

            var proceso = Process.Start(inicio)!;
            // Se descarta la salida para que el proceso no se bloquee si se llena el buffer.
            proceso.OutputDataReceived += (_, _) => { };
            proceso.ErrorDataReceived += (_, _) => { };
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();
            return proceso;
        }

        private static void Detener(Process? proceso)
        {
            if (proceso is null || proceso.HasExited)
                return;
            proceso.Kill(entireProcessTree: true);
            proceso.WaitForExit(10_000);
        }

        private static async Task EsperarAsync(string url, string nombre)
        {
            using var http = new HttpClient();
            var limite = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < limite)
            {
                try
                {
                    var respuesta = await http.GetAsync(url);
                    if (respuesta.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized)
                        return;
                }
                catch (HttpRequestException)
                {
                    // Todavía está arrancando.
                }
                await Task.Delay(500);
            }
            throw new TimeoutException($"{nombre} no respondió en {url}.");
        }

        private static string CarpetaSolucion()
        {
            var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
            while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "GestionApiario.sln")))
                carpeta = carpeta.Parent;
            return carpeta?.FullName ?? throw new DirectoryNotFoundException("No se encontró la carpeta de la solución.");
        }
    }

    [CollectionDefinition(Nombre)]
    public class ColeccionE2E : ICollectionFixture<EntornoE2E>
    {
        // Todas las pruebas de navegador comparten el mismo entorno y corren una por vez.
        public const string Nombre = "E2E";
    }
}
