using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Pruebas
{
    // Reasignación de un apiario de un apicultor (Ana) a otro (Beto) hecha por la Administradora:
    // todo lo del apiario pasa a Beto y Ana deja de verlo en cualquier lugar.
    public class ReasignacionPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";
        private const string EmailBeto = "beto@ejemplo.com";

        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Reasignar_un_apiario_de_Ana_a_Beto_le_pasa_todo_a_Beto_y_Ana_deja_de_verlo()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var varroa = await CrearEnfermedadAsync("Varroa");

            // Ana tiene dos apiarios que usan la misma campaña. Se reasigna solo "Norte".
            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var norte = await PermisosPruebas.CrearApiarioAsync(ana, "Norte");
            var sur = await PermisosPruebas.CrearApiarioAsync(ana, "Sur");
            var controlNorte1 = await PermisosPruebas.CrearControlAsync(ana, norte, campañaAna, colmenas: 20);
            var controlNorte2 = await PermisosPruebas.CrearControlAsync(ana, norte, campañaAna, colmenas: 15, enfermedad: varroa);
            var controlSur = await PermisosPruebas.CrearControlAsync(ana, sur, campañaAna, colmenas: 8);

            // Antes: el tablero de Ana cuenta los dos apiarios (último control de Norte: 15 colmenas con Varroa).
            var tableroAnaAntes = await ana.GetFromJsonAsync<DashBoardDto>("/dashBoard");
            Assert.Equal((2, 15 + 8, 1), (tableroAnaAntes!.ApiariosActivos, tableroAnaAntes.CantidadTotalDeColmenas, tableroAnaAntes.ApiariosConEnfermedades));

            (await administradora.PutAsJsonAsync($"/apiario/{norte}", new ApiarioDto { Nombre = "Norte", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            // ---- Ana ya no ve nada de "Norte" ----
            // Listados (y los desplegables de la web, que se llenan con estos mismos listados).
            Assert.Equal([sur], (await ana.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos"))!.Select(a => a.Codigo));
            Assert.Equal([controlSur], (await ana.GetFromJsonAsync<List<ControlGrillaDto>>("/controles/vertodos"))!.Select(c => c.Codigo));
            Assert.Empty((await ana.GetFromJsonAsync<List<ControlGrillaDto>>($"/controles/vertodos?CodApiario={norte}"))!);
            // Entrando por la dirección.
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/apiario/{norte}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync($"/controles/{controlNorte1}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.PutAsJsonAsync($"/apiario/{norte}", new ApiarioDto { Nombre = "Mío" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ana.DeleteAsync($"/controles/{controlNorte2}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await ana.PostAsJsonAsync("/controles", new ControlDto { CodApiario = norte, CodCampaña = campañaAna })).StatusCode);
            // Tablero y gráfico: deja de contar "Norte" y sus colmenas.
            var tableroAna = await ana.GetFromJsonAsync<DashBoardDto>("/dashBoard");
            Assert.Equal((1, 8, 0), (tableroAna!.ApiariosActivos, tableroAna.CantidadTotalDeColmenas, tableroAna.ApiariosConEnfermedades));
            Assert.Empty((await ana.GetFromJsonAsync<EnfermedadesGraficoResponse>("/dashBoard/graficoEnfermedades"))!.labels);

            // ---- La campaña de Ana no se borra ni cambia: la sigue usando "Sur" ----
            var campañaDeAna = await ana.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campañaAna}");
            Assert.Equal(idAna, campañaDeAna!.UsuarioId);
            Assert.Equal(campañaAna, (await ana.GetFromJsonAsync<ControlDetalleDto>($"/controles/{controlSur}"))!.CodCampaña);

            // ---- Beto recibe todo ----
            Assert.Equal([norte], (await beto.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos"))!.Select(a => a.Codigo));
            Assert.Equal([controlNorte2, controlNorte1], (await beto.GetFromJsonAsync<List<ControlGrillaDto>>("/controles/vertodos"))!.Select(c => c.Codigo));
            var tableroBeto = await beto.GetFromJsonAsync<DashBoardDto>("/dashBoard");
            Assert.Equal((1, 15, 1), (tableroBeto!.ApiariosActivos, tableroBeto.CantidadTotalDeColmenas, tableroBeto.ApiariosConEnfermedades));
            Assert.Equal(["Varroa"], (await beto.GetFromJsonAsync<EnfermedadesGraficoResponse>("/dashBoard/graficoEnfermedades"))!.labels);

            // Una sola copia de la campaña, y los dos controles apuntan a ella.
            var campañasBeto = (await beto.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos"))!;
            var copia = Assert.Single(campañasBeto);
            Assert.Equal((2026, "Ana"), (copia.Año!.Value, copia.Responsable!));
            Assert.Equal(copia.Codigo, (await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{controlNorte1}"))!.CodCampaña);
            Assert.Equal(copia.Codigo, (await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{controlNorte2}"))!.CodCampaña);

            // Beto puede trabajar con lo que recibió: editar un control y cargar uno nuevo con su campaña.
            (await beto.PutAsJsonAsync($"/controles/{controlNorte1}", new ControlDto { CodApiario = norte, CodCampaña = copia.Codigo, CantDeColmenas = 18 })).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, (await beto.PostAsJsonAsync("/controles", new ControlDto { CodApiario = norte, CodCampaña = copia.Codigo })).StatusCode);

            // ---- En la base: Ana sigue teniendo su campaña, sin borrar, y Beto tiene exactamente una ----
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                Assert.Equal(1, await contexto.Campañas.CountAsync(c => c.UsuarioId == idAna && c.FechaBaja == null));
                Assert.Equal(1, await contexto.Campañas.CountAsync(c => c.UsuarioId == idBeto));
                Assert.Equal(idBeto, (await contexto.Apiarios.SingleAsync(a => a.Codigo == norte)).UsuarioId);
            });
        }

        [Fact]
        public async Task Reasignar_otra_vez_a_Beto_un_apiario_que_usa_la_misma_campaña_no_crea_duplicados()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var norte = await PermisosPruebas.CrearApiarioAsync(ana, "Norte");
            var sur = await PermisosPruebas.CrearApiarioAsync(ana, "Sur");
            await PermisosPruebas.CrearControlAsync(ana, norte, campañaAna);
            var controlSur = await PermisosPruebas.CrearControlAsync(ana, sur, campañaAna);

            (await administradora.PutAsJsonAsync($"/apiario/{norte}", new ApiarioDto { Nombre = "Norte", UsuarioId = idBeto })).EnsureSuccessStatusCode();
            (await administradora.PutAsJsonAsync($"/apiario/{sur}", new ApiarioDto { Nombre = "Sur", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            // Beto tiene una sola campaña "2026 - Ana", usada por los controles de los dos apiarios.
            var campañasBeto = (await beto.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos"))!;
            var copia = Assert.Single(campañasBeto);
            Assert.Equal(copia.Codigo, (await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{controlSur}"))!.CodCampaña);
            // Ana conserva su campaña aunque ya no tenga apiarios que la usen.
            Assert.Equal([campañaAna], (await ana.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos"))!.Select(c => c.Codigo));
        }

        [Fact]
        public async Task Un_apicultor_no_puede_reasignar_ni_siquiera_sus_propios_apiarios()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var norte = await PermisosPruebas.CrearApiarioAsync(ana, "Norte");

            var respuesta = await ana.PutAsJsonAsync($"/apiario/{norte}", new ApiarioDto { Nombre = "Norte", UsuarioId = idBeto });

            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
            Assert.Equal([norte], (await ana.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos"))!.Select(a => a.Codigo));
            Assert.Empty((await beto.GetFromJsonAsync<List<ApiarioGrillaDto>>("/apiario/vertodos"))!);
        }

        private async Task<int> CrearEnfermedadAsync(string nombre)
        {
            var enfermedad = new Enfermedad { Nombre = nombre, FechaAlta = DateTime.Now };
            await _fabrica.UsarBaseAsync(async contexto => { contexto.Add(enfermedad); await contexto.SaveChangesAsync(); });
            return enfermedad.Codigo;
        }
    }
}
