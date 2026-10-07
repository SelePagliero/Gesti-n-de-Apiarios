using GestionApiario.compartido.Dto;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GestionApiario.PruebasE2E
{
    // Filtros de los listados de Apiarios, Campañas, Enfermedades, Alimentos y Productos.
    [Collection(ColeccionE2E.Nombre)]
    public class PasosFiltrosE2E : PruebaE2EBase
    {
        public PasosFiltrosE2E(EntornoE2E entorno) : base(entorno) { }

        [Fact(DisplayName = "Filtros 01 - Apiarios: por nombre sin importar mayúsculas ni tildes, por fecha de alta, y se conservan al recargar")]
        public async Task Filtros01()
        {
            var (email, ana) = await NuevoApicultorAsync("filtra");
            await ClienteApi.CrearApiarioAsync(ana, "El Algarróbo");
            await ClienteApi.CrearApiarioAsync(ana, "Los Talas");
            await ClienteApi.CrearApiarioAsync(ana, "Algarrobo Chico");

            var pagina = await IniciarSesionAsync(email);
            await IrAListaAsync(pagina, "/apiarios");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(3);
            await Expect(pagina.GetByText("Mostrando 3 apiarios")).ToBeVisibleAsync();

            await pagina.Locator("#filtroNombre").FillAsync("ALGARROBO");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(2);
            await Expect(Fila(pagina, "Los Talas")).ToHaveCountAsync(0);
            await Expect(pagina.GetByText("Mostrando 2 apiarios")).ToBeVisibleAsync();
            await Capturas.GuardarAsync(pagina, "filtros-apiarios");

            // Al recargar, el filtro sigue (está en la URL).
            await Expect(pagina).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("nombre=ALGARROBO"));
            await pagina.ReloadAsync();
            await Expect(pagina.Locator("#filtroNombre")).ToHaveValueAsync("ALGARROBO");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(2);

            await pagina.Locator("#filtroNombre").FillAsync("chico");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
            await Expect(pagina.GetByText("Mostrando 1 apiario")).ToBeVisibleAsync();

            await pagina.GetByRole(AriaRole.Button, new() { Name = "Limpiar filtros" }).ClickAsync();
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(3);
            await Expect(pagina.Locator("#filtroNombre")).ToHaveValueAsync("");
            await Expect(pagina.GetByRole(AriaRole.Button, new() { Name = "Limpiar filtros" })).ToBeDisabledAsync();

            // Fecha de alta: todos se crearon hoy.
            var hoy = DateTime.Today;
            await pagina.Locator("#filtroDesde").FillAsync(hoy.AddDays(1).ToString("yyyy-MM-dd"));
            await Expect(pagina.GetByText("No hay apiarios que coincidan con los filtros.")).ToBeVisibleAsync();
            await pagina.Locator("#filtroDesde").FillAsync(hoy.ToString("yyyy-MM-dd"));
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(3);
            await pagina.Locator("#filtroHasta").FillAsync(hoy.AddDays(-1).ToString("yyyy-MM-dd"));
            await Expect(pagina.GetByText("La fecha \"Desde\" no puede ser posterior a \"Hasta\".")).ToBeVisibleAsync();
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(0);
        }

        [Fact(DisplayName = "Filtros 02 - Campañas: por año y por responsable")]
        public async Task Filtros02()
        {
            var (email, ana) = await NuevoApicultorAsync("campanias");
            await ClienteApi.CrearCampañaAsync(ana, 2025, "Ana Pérez");
            await ClienteApi.CrearCampañaAsync(ana, 2026, "Ana Pérez");
            await ClienteApi.CrearCampañaAsync(ana, 2026, "Juan Gómez");

            var pagina = await IniciarSesionAsync(email);
            await IrAListaAsync(pagina, "/campanias");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(3);
            // En el desplegable de año aparecen los años que hay, del más nuevo al más viejo.
            await Expect(pagina.Locator("#filtroAnio option")).ToHaveTextAsync(new[] { "Todos", "2026", "2025" });
            await Expect(pagina.Locator("#filtroApicultor")).ToHaveCountAsync(0);

            await pagina.Locator("#filtroAnio").SelectOptionAsync("2026");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(2);

            await pagina.Locator("#filtroResponsable").FillAsync("perez");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
            await Expect(pagina.GetByText("Mostrando 1 campaña")).ToBeVisibleAsync();
            await Capturas.GuardarAsync(pagina, "filtros-campanias");

            await pagina.Locator("#filtroAnio").SelectOptionAsync("");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(2);

            // En el celular la tabla se desplaza dentro de su tarjeta, pero la página no se mueve hacia los costados.
            await pagina.SetViewportSizeAsync(390, 844);
            foreach (var ruta in new[] { "/campanias", "/apiarios" })
            {
                await IrAListaAsync(pagina, ruta);
                Assert.Equal(390, await pagina.EvaluateAsync<int>("document.documentElement.scrollWidth"));
            }
        }

        [Fact(DisplayName = "Filtros 03 - La Administradora filtra Apiarios y Campañas por apicultor; un apicultor no ve ese filtro")]
        public async Task Filtros03()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, beto) = await NuevoApicultorAsync("beto");
            var apiarioAna = NombreUnico("De Ana");
            var apiarioBeto = NombreUnico("De Beto");
            await ClienteApi.CrearApiarioAsync(ana, apiarioAna);
            await ClienteApi.CrearApiarioAsync(beto, apiarioBeto);
            await ClienteApi.CrearCampañaAsync(ana, 2026, apiarioAna);
            await ClienteApi.CrearCampañaAsync(beto, 2026, apiarioBeto);
            var idAna = await IdDeUsuarioAsync(emailAna);

            var pagina = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            foreach (var ruta in new[] { "/apiarios", "/campanias" })
            {
                await IrAListaAsync(pagina, ruta);
                await pagina.Locator("#filtroApicultor").SelectOptionAsync(idAna);
                await Expect(Fila(pagina, apiarioAna)).ToHaveCountAsync(1);
                await Expect(Fila(pagina, apiarioBeto)).ToHaveCountAsync(0);
                await Expect(pagina.Locator("tbody tr")).Not.ToHaveCountAsync(0);
                foreach (var fila in await pagina.Locator("tbody tr").AllAsync())
                    await Expect(fila).ToContainTextAsync(emailAna);
            }

            var paginaBeto = await IniciarSesionAsync(emailBeto);
            await IrAListaAsync(paginaBeto, "/apiarios");
            await Expect(paginaBeto.Locator("#filtroApicultor")).ToHaveCountAsync(0);
            // Aunque ponga el filtro en la URL, no se aplica ni ve datos ajenos.
            await IrAListaAsync(paginaBeto, $"/apiarios?apicultor={idAna}");
            await Expect(Fila(paginaBeto, apiarioBeto)).ToHaveCountAsync(1);
            await Expect(Fila(paginaBeto, apiarioAna)).ToHaveCountAsync(0);
        }

        [Fact(DisplayName = "Filtros 04 - Enfermedades, Alimentos y Productos se filtran por nombre")]
        public async Task Filtros04()
        {
            var administradora = await ApiAdministradoraAsync();
            var marca = Guid.NewGuid().ToString("N")[..6];
            await ClienteApi.CrearAsync(administradora, "/enfermedad", new EnfermedadDto { Nombre = $"Várroa {marca}" });
            await ClienteApi.CrearAsync(administradora, "/alimento", new AlimentoDto { Nombre = $"Jarabe {marca}" });
            await ClienteApi.CrearAsync(administradora, "/producto", new ProductoDto { Nombre = $"Ácido oxálico {marca}" });
            var (email, _) = await NuevoApicultorAsync("catalogos");

            var pagina = await IniciarSesionAsync(email);
            foreach (var (ruta, buscado, esperado) in new[]
            {
                ("/enfermedades", $"varroa {marca}", $"Várroa {marca}"),
                ("/alimentos", $"JARABE {marca}", $"Jarabe {marca}"),
                ("/productos", $"acido oxalico {marca}", $"Ácido oxálico {marca}")
            })
            {
                await IrAListaAsync(pagina, ruta);
                await pagina.Locator("#filtroNombre").FillAsync(buscado);
                await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
                await Expect(Fila(pagina, esperado)).ToHaveCountAsync(1);

                await pagina.Locator("#filtroNombre").FillAsync($"no existe {marca}");
                await Expect(pagina.GetByText("que coincidan con los filtros.")).ToBeVisibleAsync();
            }
            await Capturas.GuardarAsync(pagina, "filtros-productos");
        }

        [Fact(DisplayName = "Filtros 05 - Usuarios: la Administradora filtra por email, rol y contraseña")]
        public async Task Filtros05()
        {
            var marca = Guid.NewGuid().ToString("N")[..6];
            var conPropia = $"propia-{marca}@prueba.local";
            var conTemporal = $"temporal-{marca}@prueba.local";
            await Entorno.Api.RegistrarAsync(conPropia);
            await Entorno.Api.RegistrarAsync(conTemporal);
            var administradoraApi = await ApiAdministradoraAsync();
            (await administradoraApi.PostAsync($"/usuarios/{await IdDeUsuarioAsync(conTemporal)}/restablecer-contrasena", null)).EnsureSuccessStatusCode();

            var pagina = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await pagina.GetByRole(AriaRole.Link, new() { Name = "Usuarios" }).ClickAsync();

            // Email: sin distinguir mayúsculas.
            await pagina.Locator("#filtroEmail").FillAsync(marca.ToUpperInvariant());
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(2);
            await Expect(pagina.GetByText("Mostrando 2 usuarios")).ToBeVisibleAsync();

            // Contraseña temporal o propia.
            await pagina.Locator("#filtroContrasena").SelectOptionAsync("temporal");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
            await Expect(Fila(pagina, conTemporal)).ToContainTextAsync("Temporal");
            await pagina.Locator("#filtroContrasena").SelectOptionAsync("propia");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
            await Expect(Fila(pagina, conPropia)).ToContainTextAsync("Propia");
            await Capturas.GuardarAsync(pagina, "filtros-usuarios");

            // Rol: con el filtro de email de los apicultores, "Administradora" no deja ninguno.
            await pagina.Locator("#filtroRol").SelectOptionAsync("administradora");
            await Expect(pagina.GetByText("No hay usuarios que coincidan con los filtros.")).ToBeVisibleAsync();

            // Los filtros quedan en la URL y se conservan al recargar.
            await pagina.ReloadAsync();
            await Expect(pagina.Locator("#filtroRol")).ToHaveValueAsync("administradora");
            await Expect(pagina.Locator("#filtroEmail")).ToHaveValueAsync(marca.ToUpperInvariant());

            await pagina.GetByRole(AriaRole.Button, new() { Name = "Limpiar filtros" }).ClickAsync();
            await Expect(Fila(pagina, EntornoE2E.EmailAdministradora)).ToContainTextAsync("Administradora");
            await pagina.Locator("#filtroRol").SelectOptionAsync("administradora");
            await Expect(pagina.Locator("tbody tr")).ToHaveCountAsync(1);
        }
    }
}
