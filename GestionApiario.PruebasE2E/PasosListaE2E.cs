using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GestionApiario.PruebasE2E
{
    // La lista de pruebas manuales, automatizada en el navegador. Cada paso es independiente:
    // crea sus propios apicultores y datos, así que si uno falla los demás se siguen ejecutando.
    [Collection(ColeccionE2E.Nombre)]
    public class PasosListaE2E : PruebaE2EBase
    {
        public PasosListaE2E(EntornoE2E entorno) : base(entorno) { }

        [Fact(DisplayName = "Paso 01 - La Administradora ve la columna Apicultor en Apiarios, Controles y Campañas")]
        public async Task Paso01()
        {
            var (_, ana) = await NuevoApicultorAsync("ana");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, "Para columnas");
            var apiario = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Columnas"));
            await ClienteApi.CrearControlAsync(ana, apiario, campaña, 5);

            var pagina = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);

            await IrAListaAsync(pagina, "/apiarios");
            await Expect(Columna(pagina, "Apicultor")).ToBeVisibleAsync();
            await IrAListaAsync(pagina, "/controles");
            await Expect(Columna(pagina, "Apicultor")).ToBeVisibleAsync();
            await Expect(pagina.Locator("#filtroApicultor")).ToBeVisibleAsync();
            await IrAListaAsync(pagina, "/campanias");
            await Expect(Columna(pagina, "Apicultor")).ToBeVisibleAsync();
            await pagina.GotoAsync("/");
            await Expect(pagina.Locator("#tableroApicultor")).ToBeVisibleAsync();
        }

        [Fact(DisplayName = "Paso 02 - Un apicultor nuevo no ve datos de nadie y no puede editar Alimentos, Enfermedades ni Productos")]
        public async Task Paso02()
        {
            // Datos de otra persona y catálogos cargados por la Administradora.
            var (_, otro) = await NuevoApicultorAsync("otro");
            var campañaOtro = await ClienteApi.CrearCampañaAsync(otro, 2026, "Ajena");
            var apiarioOtro = NombreUnico("Ajeno");
            await ClienteApi.CrearControlAsync(otro, await ClienteApi.CrearApiarioAsync(otro, apiarioOtro), campañaOtro, 5);
            var administradora = await ApiAdministradoraAsync();
            await ClienteApi.CrearAsync(administradora, "/alimento", new AlimentoDto { Nombre = NombreUnico("Jarabe") });
            await CrearEnfermedadAsync(NombreUnico("Loque"));
            await ClienteApi.CrearAsync(administradora, "/producto", new ProductoDto { Nombre = NombreUnico("Ácido oxálico") });

            var (emailAna, _) = await NuevoApicultorAsync("ana");
            var pagina = await IniciarSesionAsync(emailAna);

            await IrAListaAsync(pagina, "/apiarios");
            await Expect(pagina.GetByText("Todavía no hay apiarios cargados.")).ToBeVisibleAsync();
            await IrAListaAsync(pagina, "/controles");
            await Expect(pagina.GetByText("Todavía no hay controles cargados.")).ToBeVisibleAsync();
            await IrAListaAsync(pagina, "/campanias");
            await Expect(pagina.GetByText("Todavía no hay campañas cargadas.")).ToBeVisibleAsync();
            // Las campañas son propias: puede crear las suyas.
            await Expect(pagina.GetByRole(AriaRole.Link, new() { Name = "Crear Campaña" })).ToBeVisibleAsync();

            foreach (var (ruta, boton) in new[] { ("/alimentos", "Crear Alimento"), ("/enfermedades", "Crear Enfermedad"), ("/productos", "Crear Producto") })
            {
                await IrAListaAsync(pagina, ruta);
                await Expect(pagina.Locator("tbody tr").First).ToBeVisibleAsync();
                await Expect(pagina.GetByRole(AriaRole.Link, new() { Name = boton })).ToHaveCountAsync(0);
                await Expect(pagina.GetByText("Editar")).ToHaveCountAsync(0);
                await Expect(pagina.GetByText("Eliminar")).ToHaveCountAsync(0);
                await Expect(Columna(pagina, "Acciones")).ToHaveCountAsync(0);
            }
        }

        [Fact(DisplayName = "Paso 03 - Ana crea una campaña, un apiario y un control desde la web")]
        public async Task Paso03()
        {
            var (emailAna, _) = await NuevoApicultorAsync("ana");
            var responsable = NombreUnico("Resp");
            var nombreApiario = NombreUnico("Apiario de Ana");
            var pagina = await IniciarSesionAsync(emailAna);

            await pagina.GotoAsync("/crear-campaña");
            await pagina.Locator("#año").FillAsync("2026");
            await pagina.Locator("#responsable").FillAsync(responsable);
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(pagina, responsable)).ToBeVisibleAsync();

            await pagina.GotoAsync("/crear-apiario");
            await pagina.Locator("#nombre").FillAsync(nombreApiario);
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(pagina, nombreApiario)).ToBeVisibleAsync();

            await pagina.GotoAsync("/crear-control");
            await pagina.Locator("#selectApiario").SelectOptionAsync(new SelectOptionValue { Label = nombreApiario });
            await pagina.Locator("#selectCampaña").SelectOptionAsync(new SelectOptionValue { Label = $"2026 - {responsable}" });
            await pagina.Locator("#cantColmenas").FillAsync("12");
            await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(pagina).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/controles"));
            await Expect(Fila(pagina, nombreApiario)).ToContainTextAsync("12");
        }

        [Fact(DisplayName = "Paso 04 - Beto no ve la campaña, el apiario ni el control de Ana")]
        public async Task Paso04()
        {
            var (_, ana) = await NuevoApicultorAsync("ana");
            var responsable = NombreUnico("Campaña de Ana");
            var apiarioAna = NombreUnico("Apiario de Ana");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, responsable);
            await ClienteApi.CrearControlAsync(ana, await ClienteApi.CrearApiarioAsync(ana, apiarioAna), campaña, 10);

            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var pagina = await IniciarSesionAsync(emailBeto);

            await IrAListaAsync(pagina, "/campanias");
            await Expect(pagina.GetByText(responsable)).ToHaveCountAsync(0);
            await IrAListaAsync(pagina, "/apiarios");
            await Expect(pagina.GetByText(apiarioAna)).ToHaveCountAsync(0);
            await IrAListaAsync(pagina, "/controles");
            await Expect(pagina.GetByText(apiarioAna)).ToHaveCountAsync(0);
            // Tampoco en los desplegables del formulario de control.
            await pagina.GotoAsync("/crear-control");
            await Expect(pagina.Locator("#selectApiario option")).ToHaveCountAsync(1);
            await Expect(pagina.Locator("#selectCampaña option")).ToHaveCountAsync(1);
        }

        [Fact(DisplayName = "Paso 05 - Al cargar un control, Ana solo puede elegir sus campañas")]
        public async Task Paso05()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (_, beto) = await NuevoApicultorAsync("beto");
            var r1 = NombreUnico("Ana uno");
            var r2 = NombreUnico("Ana dos");
            var rBeto = NombreUnico("De Beto");
            await ClienteApi.CrearCampañaAsync(ana, 2026, r1);
            await ClienteApi.CrearCampañaAsync(ana, 2025, r2);
            await ClienteApi.CrearCampañaAsync(beto, 2026, rBeto);
            await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Apiario de Ana"));

            var pagina = await IniciarSesionAsync(emailAna);
            await pagina.GotoAsync("/crear-control");

            var opciones = pagina.Locator("#selectCampaña option");
            await Expect(opciones).ToHaveCountAsync(3);
            await Expect(opciones).ToHaveTextAsync(new[] { "Seleccione una Campaña", $"2026 - {r1}", $"2025 - {r2}" });
            await Expect(pagina.Locator("#selectCampaña")).Not.ToContainTextAsync(rBeto);
        }

        [Fact(DisplayName = "Paso 06 - Beto escribe la dirección de un apiario o control de Ana y recibe \"no existe\"")]
        public async Task Paso06()
        {
            var (_, ana) = await NuevoApicultorAsync("ana");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, "Ana");
            var apiario = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Apiario de Ana"));
            var control = await ClienteApi.CrearControlAsync(ana, apiario, campaña, 10);

            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var pagina = await IniciarSesionAsync(emailBeto);

            foreach (var ruta in new[] { $"/ver-apiario/{apiario}", $"/actualizar-apiario/{apiario}", $"/ver-control/{control}", $"/actualizar-campaña/{campaña}" })
            {
                await pagina.GotoAsync(ruta);
                await Expect(pagina.GetByText("El registro no existe o fue eliminado.")).ToBeVisibleAsync();
            }
        }

        [Fact(DisplayName = "Paso 07 - Ana escribe /crear-alimento y ve \"No tenés permiso\"")]
        public async Task Paso07()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var pagina = await IniciarSesionAsync(emailAna);

            foreach (var ruta in new[] { "/crear-alimento", "/crear-enfermedad", "/crear-producto" })
            {
                await pagina.GotoAsync(ruta);
                await Expect(pagina.GetByText("No tenés permiso para realizar esta acción.")).ToBeVisibleAsync();
                await Expect(pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" })).ToHaveCountAsync(0);
            }

            // Aunque llame a la API directamente, se rechaza.
            var respuesta = await ana.PostAsJsonAsync("/alimento", new AlimentoDto { Nombre = "Intento" });
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        }

        [Fact(DisplayName = "Paso 08 - La Administradora edita una campaña de Ana sin cambiar el dueño")]
        public async Task Paso08()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, NombreUnico("Original"));
            var corregido = NombreUnico("Corregido");
            var idAna = await IdDeUsuarioAsync(emailAna);

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await admin.GotoAsync($"/actualizar-campaña/{campaña}");
            await Expect(admin.Locator("#dueño")).ToHaveValueAsync(idAna);
            await admin.Locator("#responsable").FillAsync(corregido);
            await admin.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(admin, corregido)).ToContainTextAsync(emailAna);

            var paginaAna = await IniciarSesionAsync(emailAna);
            await IrAListaAsync(paginaAna, "/campanias");
            await Expect(Fila(paginaAna, corregido)).ToBeVisibleAsync();

            var detalle = await (await ApiAdministradoraAsync()).GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}");
            Assert.Equal(idAna, detalle!.UsuarioId);
            Assert.Equal(EntornoE2E.EmailAdministradora, detalle.UsuarioModificacion);
        }

        [Fact(DisplayName = "Paso 09 - La Administradora edita un apiario de Ana sin cambiar el dueño")]
        public async Task Paso09()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var apiario = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Original"));
            var corregido = NombreUnico("Corregido");

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await admin.GotoAsync($"/actualizar-apiario/{apiario}");
            await Expect(admin.Locator("#nombre")).Not.ToHaveValueAsync("");
            await admin.Locator("#nombre").FillAsync(corregido);
            await admin.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(admin, corregido)).ToContainTextAsync(emailAna);

            var paginaAna = await IniciarSesionAsync(emailAna);
            await IrAListaAsync(paginaAna, "/apiarios");
            await Expect(Fila(paginaAna, corregido)).ToBeVisibleAsync();
        }

        [Fact(DisplayName = "Paso 10 - La Administradora filtra los Controles por apicultor")]
        public async Task Paso10()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (_, beto) = await NuevoApicultorAsync("beto");
            var apiarioAna = NombreUnico("Apiario de Ana");
            var apiarioBeto = NombreUnico("Apiario de Beto");
            await ClienteApi.CrearControlAsync(ana, await ClienteApi.CrearApiarioAsync(ana, apiarioAna), await ClienteApi.CrearCampañaAsync(ana, 2026, "Ana"), 10);
            await ClienteApi.CrearControlAsync(beto, await ClienteApi.CrearApiarioAsync(beto, apiarioBeto), await ClienteApi.CrearCampañaAsync(beto, 2026, "Beto"), 10);

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await IrAListaAsync(admin, "/controles");
            await Expect(Fila(admin, apiarioAna)).ToBeVisibleAsync();
            await Expect(Fila(admin, apiarioBeto)).ToBeVisibleAsync();

            await admin.Locator("#filtroApicultor").SelectOptionAsync(new SelectOptionValue { Label = emailAna });
            await Expect(admin).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("apicultor="));
            await Expect(Fila(admin, apiarioAna)).ToBeVisibleAsync();
            await Expect(Fila(admin, apiarioBeto)).ToHaveCountAsync(0);
            await Expect(admin.Locator("tbody tr")).ToHaveCountAsync(1);
        }

        [Fact(DisplayName = "Paso 11 - La Administradora reasigna un apiario de Ana a Beto: todo pasa a Beto y Ana deja de verlo")]
        public async Task Paso11()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var varroa = await CrearEnfermedadAsync(NombreUnico("Varroa"));
            var responsable = NombreUnico("Campaña de Ana");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, responsable);
            var norte = NombreUnico("Norte");
            var sur = NombreUnico("Sur");
            var codNorte = await ClienteApi.CrearApiarioAsync(ana, norte);
            var codSur = await ClienteApi.CrearApiarioAsync(ana, sur);
            await ClienteApi.CrearControlAsync(ana, codNorte, campaña, 20);
            var controlNorte = await ClienteApi.CrearControlAsync(ana, codNorte, campaña, 15, varroa);
            await ClienteApi.CrearControlAsync(ana, codSur, campaña, 8);

            var paginaAna = await IniciarSesionAsync(emailAna);
            await paginaAna.GotoAsync("/");
            await VerificarTableroAsync(paginaAna, apiarios: 2, colmenas: 23, conEnfermedad: 1);

            // Reasignación desde la web, como la haría la Administradora.
            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await admin.GotoAsync($"/actualizar-apiario/{codNorte}");
            await Expect(admin.Locator("#nombre")).ToHaveValueAsync(norte);
            await admin.Locator("#dueño").SelectOptionAsync(new SelectOptionValue { Label = emailBeto });
            await Expect(admin.GetByText("Al guardar, el apiario y todos sus controles pasan al nuevo dueño.")).ToBeVisibleAsync();
            await admin.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(admin, norte)).ToContainTextAsync(emailBeto);

            // Beto recibe el apiario, sus controles y una sola copia de la campaña.
            var paginaBeto = await IniciarSesionAsync(emailBeto);
            await IrAListaAsync(paginaBeto, "/apiarios");
            await Expect(Fila(paginaBeto, norte)).ToBeVisibleAsync();
            await IrAListaAsync(paginaBeto, "/controles");
            await Expect(Fila(paginaBeto, norte)).ToHaveCountAsync(2);
            await IrAListaAsync(paginaBeto, "/campanias");
            await Expect(Fila(paginaBeto, responsable)).ToHaveCountAsync(1);
            await paginaBeto.GotoAsync("/");
            await VerificarTableroAsync(paginaBeto, apiarios: 1, colmenas: 15, conEnfermedad: 1);
            await paginaBeto.GotoAsync($"/ver-control/{controlNorte}");
            await Expect(paginaBeto.Locator("#selectCampaña option:checked")).ToHaveTextAsync($"2026 - {responsable}");

            // Ana deja de ver todo lo de "Norte": listados, desplegables, dirección y tablero.
            await IrAListaAsync(paginaAna, "/apiarios");
            await Expect(Fila(paginaAna, norte)).ToHaveCountAsync(0);
            await Expect(Fila(paginaAna, sur)).ToBeVisibleAsync();
            await IrAListaAsync(paginaAna, "/controles");
            await Expect(Fila(paginaAna, norte)).ToHaveCountAsync(0);
            await paginaAna.GotoAsync("/crear-control");
            await Expect(paginaAna.Locator("#selectApiario")).Not.ToContainTextAsync(norte);
            await paginaAna.GotoAsync($"/ver-apiario/{codNorte}");
            await Expect(paginaAna.GetByText("El registro no existe o fue eliminado.")).ToBeVisibleAsync();
            await paginaAna.GotoAsync("/");
            await VerificarTableroAsync(paginaAna, apiarios: 1, colmenas: 8, conEnfermedad: 0);

            // La campaña de Ana no se borra: la sigue teniendo y usando en "Sur".
            await IrAListaAsync(paginaAna, "/campanias");
            await Expect(Fila(paginaAna, responsable)).ToHaveCountAsync(1);
        }

        [Fact(DisplayName = "Paso 12 - Reasignar a Beto otro apiario con la misma campaña no crea campañas duplicadas")]
        public async Task Paso12()
        {
            var (_, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var responsable = NombreUnico("Compartida");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, responsable);
            var apiario1 = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Uno"));
            var apiario2 = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Dos"));
            await ClienteApi.CrearControlAsync(ana, apiario1, campaña, 10);
            await ClienteApi.CrearControlAsync(ana, apiario2, campaña, 10);

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            foreach (var apiario in new[] { apiario1, apiario2 })
            {
                await admin.GotoAsync($"/actualizar-apiario/{apiario}");
                await Expect(admin.Locator("#nombre")).Not.ToHaveValueAsync("");
                await admin.Locator("#dueño").SelectOptionAsync(new SelectOptionValue { Label = emailBeto });
                await admin.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
                await Expect(admin).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/apiarios$"));
            }

            var paginaBeto = await IniciarSesionAsync(emailBeto);
            await IrAListaAsync(paginaBeto, "/campanias");
            await Expect(Fila(paginaBeto, responsable)).ToHaveCountAsync(1);
            await IrAListaAsync(paginaBeto, "/controles");
            await Expect(paginaBeto.Locator("tbody tr")).ToHaveCountAsync(2);
        }

        [Fact(DisplayName = "Paso 13 - Un apicultor no ve la opción de reasignar y la API se lo rechaza")]
        public async Task Paso13()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var apiario = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("De Ana"));

            var paginaAna = await IniciarSesionAsync(emailAna);
            await paginaAna.GotoAsync($"/actualizar-apiario/{apiario}");
            await Expect(paginaAna.Locator("#nombre")).Not.ToHaveValueAsync("");
            await Expect(paginaAna.Locator("#dueño")).ToHaveCountAsync(0);

            var intento = await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "De Ana", UsuarioId = await IdDeUsuarioAsync(emailBeto) });
            Assert.Equal(HttpStatusCode.Forbidden, intento.StatusCode);
        }

        [Fact(DisplayName = "Paso 14 - El tablero de Ana, el de Beto y el de la Administradora muestran números coherentes")]
        public async Task Paso14()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, beto) = await NuevoApicultorAsync("beto");
            var varroa = await CrearEnfermedadAsync(NombreUnico("Varroa"));
            var campañaAna = await ClienteApi.CrearCampañaAsync(ana, 2026, "Ana");
            var campañaBeto = await ClienteApi.CrearCampañaAsync(beto, 2026, "Beto");
            var a1 = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("A1"));
            var a2 = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("A2"));
            await ClienteApi.CrearControlAsync(ana, a1, campañaAna, 30);
            await ClienteApi.CrearControlAsync(ana, a1, campañaAna, 10, varroa); // el último control de A1 manda
            await ClienteApi.CrearControlAsync(ana, a2, campañaAna, 5);
            await ClienteApi.CrearControlAsync(beto, await ClienteApi.CrearApiarioAsync(beto, NombreUnico("B1")), campañaBeto, 20);

            var paginaAna = await IniciarSesionAsync(emailAna);
            await paginaAna.GotoAsync("/");
            await VerificarTableroAsync(paginaAna, apiarios: 2, colmenas: 15, conEnfermedad: 1);

            var paginaBeto = await IniciarSesionAsync(emailBeto);
            await paginaBeto.GotoAsync("/");
            await VerificarTableroAsync(paginaBeto, apiarios: 1, colmenas: 20, conEnfermedad: 0);

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await admin.GotoAsync("/");
            // "Todos" incluye los datos de las demás pruebas: tiene que coincidir con lo que calcula la API.
            var todos = await (await ApiAdministradoraAsync()).GetFromJsonAsync<DashBoardDto>("/dashBoard");
            await VerificarTableroAsync(admin, todos!.ApiariosActivos, todos.CantidadTotalDeColmenas, todos.ApiariosConEnfermedades);
            await admin.Locator("#tableroApicultor").SelectOptionAsync(new SelectOptionValue { Label = emailAna });
            await VerificarTableroAsync(admin, apiarios: 2, colmenas: 15, conEnfermedad: 1);
            await admin.Locator("#tableroApicultor").SelectOptionAsync(new SelectOptionValue { Label = emailBeto });
            await VerificarTableroAsync(admin, apiarios: 1, colmenas: 20, conEnfermedad: 0);
        }

        [Fact(DisplayName = "Paso 15 - La Administradora reasigna a Beto un apiario propio con controles y sus campañas no se tocan")]
        public async Task Paso15()
        {
            var (emailBeto, _) = await NuevoApicultorAsync("beto");
            var administradora = await ApiAdministradoraAsync();
            var responsable = NombreUnico("De la Administradora");
            var campaña = await ClienteApi.CrearCampañaAsync(administradora, 2026, responsable);
            var nombre = NombreUnico("Propio");
            var apiario = await ClienteApi.CrearApiarioAsync(administradora, nombre);
            await ClienteApi.CrearControlAsync(administradora, apiario, campaña, 12);
            await ClienteApi.CrearControlAsync(administradora, apiario, campaña, 11);

            var admin = await IniciarSesionAsync(EntornoE2E.EmailAdministradora);
            await admin.GotoAsync($"/actualizar-apiario/{apiario}");
            await Expect(admin.Locator("#nombre")).ToHaveValueAsync(nombre);
            await admin.Locator("#dueño").SelectOptionAsync(new SelectOptionValue { Label = emailBeto });
            await admin.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
            await Expect(Fila(admin, nombre)).ToContainTextAsync(emailBeto);

            var paginaBeto = await IniciarSesionAsync(emailBeto);
            await IrAListaAsync(paginaBeto, "/controles");
            await Expect(Fila(paginaBeto, nombre)).ToHaveCountAsync(2);
            await IrAListaAsync(paginaBeto, "/campanias");
            await Expect(Fila(paginaBeto, responsable)).ToHaveCountAsync(1);
            await paginaBeto.GotoAsync("/");
            await VerificarTableroAsync(paginaBeto, apiarios: 1, colmenas: 11, conEnfermedad: 0);

            // La campaña original sigue siendo de la Administradora y no se dio de baja (se revisa abajo).
            await Paso15VerificarCampañaOriginalAsync(administradora, campaña);
        }

        [Fact(DisplayName = "Paso 16 - Después de una reasignación, Beto no ve el email de Ana en ningún lado")]
        public async Task Paso16()
        {
            var (emailAna, ana) = await NuevoApicultorAsync("ana");
            var (emailBeto, beto) = await NuevoApicultorAsync("beto");
            var campaña = await ClienteApi.CrearCampañaAsync(ana, 2026, NombreUnico("Ana"));
            var apiario = await ClienteApi.CrearApiarioAsync(ana, NombreUnico("Norte"));
            await ClienteApi.CrearControlAsync(ana, apiario, campaña, 10);
            var administradora = await ApiAdministradoraAsync();
            (await administradora.PutAsJsonAsync($"/apiario/{apiario}",
                new ApiarioDto { Nombre = "Norte", UsuarioId = await IdDeUsuarioAsync(emailBeto) })).EnsureSuccessStatusCode();

            var paginaBeto = await IniciarSesionAsync(emailBeto);
            foreach (var ruta in new[] { "/apiarios", $"/ver-apiario/{apiario}", "/controles", "/campanias" })
            {
                await paginaBeto.GotoAsync(ruta);
                await paginaBeto.Locator("table, form, p.text-muted").First.WaitForAsync();
                await Expect(paginaBeto.Locator("body")).Not.ToContainTextAsync(emailAna);
            }

            // En la API tampoco: "creado por" le llega vacío.
            Assert.DoesNotContain(emailAna, await beto.GetStringAsync($"/apiario/{apiario}"));
            Assert.Null((await beto.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioAlta);
            Assert.Equal(emailAna, (await administradora.GetFromJsonAsync<ApiarioDetalleDto>($"/apiario/{apiario}"))!.UsuarioAlta);
        }

        private static async Task Paso15VerificarCampañaOriginalAsync(HttpClient administradora, int campaña)
        {
            var original = await administradora.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campaña}");
            Assert.Equal(EntornoE2E.EmailAdministradora, original!.Apicultor);
            Assert.Null(original.FechaBaja);
        }
    }
}
