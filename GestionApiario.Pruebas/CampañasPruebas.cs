using System.Net;
using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Pruebas
{
    // Cada campaña pertenece a un apicultor, igual que los apiarios. Un control solo puede usar campañas
    // del dueño de su apiario, y al reasignar un apiario sus controles pasan a campañas del nuevo dueño.
    public class CampañasPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";
        private const string EmailBeto = "beto@ejemplo.com";

        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        [Fact]
        public async Task Ana_crea_una_campaña_y_Beto_no_la_ve()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);

            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");

            Assert.Equal([campañaAna], await CodigosCampañasAsync(ana));
            Assert.Empty(await CodigosCampañasAsync(beto));
            Assert.Equal(HttpStatusCode.NotFound, (await beto.GetAsync($"/campaña/{campañaAna}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await beto.PutAsJsonAsync($"/campaña/{campañaAna}", new CampañaDto { Año = 2026 })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await beto.DeleteAsync($"/campaña/{campañaAna}")).StatusCode);

            // Ana sí puede editarla y eliminarla.
            (await ana.PutAsJsonAsync($"/campaña/{campañaAna}", new CampañaDto { Año = 2027, Responsable = "Ana" })).EnsureSuccessStatusCode();
            (await ana.DeleteAsync($"/campaña/{campañaAna}")).EnsureSuccessStatusCode();
            Assert.Empty(await CodigosCampañasAsync(ana));
        }

        [Fact]
        public async Task Al_cargar_un_control_Ana_solo_puede_elegir_sus_campañas()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var campañaBeto = await PermisosPruebas.CrearCampañaAsync(beto, 2026, "Beto");
            var apiarioAna = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");

            // El desplegable de la web se llena con /campaña/vertodos: solo trae las de Ana.
            Assert.Equal([campañaAna], await CodigosCampañasAsync(ana));

            // Aunque mande a mano la campaña de Beto, la API la rechaza como si no existiera.
            var conCampañaAjena = await ana.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campañaBeto, CodApiario = apiarioAna });
            Assert.Equal(HttpStatusCode.BadRequest, conCampañaAjena.StatusCode);
            Assert.Equal("La campaña no existe.", await conCampañaAjena.Content.ReadAsStringAsync());

            var conCampañaPropia = await ana.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campañaAna, CodApiario = apiarioAna });
            Assert.Equal(HttpStatusCode.Created, conCampañaPropia.StatusCode);
        }

        [Fact]
        public async Task La_administradora_ve_todas_las_campañas_con_su_dueño_y_las_edita_sin_cambiar_el_dueño()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var campañaBeto = await PermisosPruebas.CrearCampañaAsync(beto, 2026, "Beto");
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);

            var todas = await administradora.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos");
            Assert.Equal(EmailAna, todas!.Single(c => c.Codigo == campañaAna).Apicultor);
            Assert.Equal(EmailBeto, todas!.Single(c => c.Codigo == campañaBeto).Apicultor);

            (await administradora.PutAsJsonAsync($"/campaña/{campañaAna}", new CampañaDto { Año = 2026, Responsable = "Corregido" })).EnsureSuccessStatusCode();

            var editada = await ana.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campañaAna}");
            Assert.Equal("Corregido", editada!.Responsable);
            Assert.Equal(idAna, editada.UsuarioId);
            // Los emails de auditoría solo los ve la Administradora.
            Assert.Null(editada.UsuarioAlta);
            Assert.Null(editada.UsuarioModificacion);
            var vistaAdministradora = await administradora.GetFromJsonAsync<CampañaDetalleDto>($"/campaña/{campañaAna}");
            Assert.Equal(EmailAna, vistaAdministradora!.UsuarioAlta);
            Assert.Equal(FabricaApi.EmailPrueba, vistaAdministradora.UsuarioModificacion);
        }

        [Fact]
        public async Task La_administradora_puede_reasignar_una_campaña_que_no_usan_controles_de_otro()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            (await administradora.PutAsJsonAsync($"/campaña/{campaña}", new CampañaDto { Año = 2026, Responsable = "Ana", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            Assert.Empty(await CodigosCampañasAsync(ana));
            Assert.Equal([campaña], await CodigosCampañasAsync(beto));
        }

        [Fact]
        public async Task No_se_puede_reasignar_una_campaña_que_usan_controles_del_dueño_actual()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            await _fabrica.CrearApicultorAsync(EmailBeto);
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            await PermisosPruebas.CrearControlAsync(ana, await PermisosPruebas.CrearApiarioAsync(ana, "De Ana"), campaña);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);

            var respuesta = await administradora.PutAsJsonAsync($"/campaña/{campaña}", new CampañaDto { Año = 2026, Responsable = "Ana", UsuarioId = idBeto });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("la usan controles de apiarios de otro apicultor", await respuesta.Content.ReadAsStringAsync());
            Assert.Equal([campaña], await CodigosCampañasAsync(ana));
        }

        [Fact]
        public async Task Un_apicultor_no_puede_elegir_ni_cambiar_el_dueño_de_una_campaña()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var campaña = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");

            var alta = await ana.PostAsJsonAsync("/campaña", new CampañaDto { Año = 2026, UsuarioId = idBeto });
            var cambio = await ana.PutAsJsonAsync($"/campaña/{campaña}", new CampañaDto { Año = 2026, UsuarioId = idBeto });

            Assert.Equal(HttpStatusCode.Forbidden, alta.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cambio.StatusCode);
        }

        [Fact]
        public async Task La_administradora_no_puede_mezclar_la_campaña_de_un_apicultor_con_el_apiario_de_otro()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var campañaAna = await PermisosPruebas.CrearCampañaAsync(ana, 2026, "Ana");
            var apiarioBeto = await PermisosPruebas.CrearApiarioAsync(beto, "De Beto");

            var respuesta = await administradora.PostAsJsonAsync("/controles", new ControlDto { CodCampaña = campañaAna, CodApiario = apiarioBeto });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Equal("La campaña tiene que ser del mismo apicultor que el apiario.", await respuesta.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Al_reasignar_un_apiario_a_Beto_sus_controles_quedan_con_campañas_de_Beto_sin_duplicados()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var beto = await _fabrica.CrearApicultorAsync(EmailBeto);
            var idBeto = await _fabrica.ObtenerIdUsuarioAsync(EmailBeto);
            var idAdministradora = await _fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            // Campañas de la Administradora: dos las usa el apiario a reasignar y una no.
            var campaña2026 = await PermisosPruebas.CrearCampañaAsync(administradora, 2026, "Juan");
            var campaña2025 = await PermisosPruebas.CrearCampañaAsync(administradora, 2025, "Juan");
            var campañaSinUso = await PermisosPruebas.CrearCampañaAsync(administradora, 2024, "Juan");
            // Beto ya tiene una campaña igual a la de 2025: se tiene que reutilizar, no copiar.
            var campañaBeto2025 = await PermisosPruebas.CrearCampañaAsync(beto, 2025, "Juan");

            var apiario = await PermisosPruebas.CrearApiarioAsync(administradora, "A reasignar");
            var control1 = await PermisosPruebas.CrearControlAsync(administradora, apiario, campaña2026);
            var control2 = await PermisosPruebas.CrearControlAsync(administradora, apiario, campaña2026);
            var control3 = await PermisosPruebas.CrearControlAsync(administradora, apiario, campaña2025);
            // Otro apiario de la Administradora sigue usando la campaña 2026 original.
            var otroApiario = await PermisosPruebas.CrearApiarioAsync(administradora, "Se queda");
            var controlOtro = await PermisosPruebas.CrearControlAsync(administradora, otroApiario, campaña2026);

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "A reasignar", UsuarioId = idBeto })).EnsureSuccessStatusCode();

            await _fabrica.UsarBaseAsync(async contexto =>
            {
                var campañasBeto = await contexto.Campañas.Where(c => c.UsuarioId == idBeto).ToListAsync();
                // La de 2025 que ya tenía y una sola copia de la de 2026; la campaña sin uso no se copia.
                Assert.Equal(2, campañasBeto.Count);
                var copia2026 = campañasBeto.Single(c => c.Año == 2026 && c.Responsable == "Juan");
                Assert.Equal(FabricaApi.EmailPrueba, copia2026.UsuarioAlta);

                var controles = await contexto.Controles.Where(c => c.CodApiario == apiario).ToDictionaryAsync(c => c.Codigo, c => c.CodCampaña);
                Assert.Equal(copia2026.Codigo, controles[control1]);
                Assert.Equal(copia2026.Codigo, controles[control2]);
                Assert.Equal(campañaBeto2025, controles[control3]);

                // Las campañas originales no se tocan y el otro apiario las sigue usando.
                var originales = await contexto.Campañas.Where(c => c.Codigo == campaña2026 || c.Codigo == campaña2025 || c.Codigo == campañaSinUso).ToListAsync();
                Assert.All(originales, c => Assert.Equal(idAdministradora, c.UsuarioId));
                Assert.All(originales, c => Assert.Null(c.FechaBaja));
                Assert.Equal(campaña2026, (await contexto.Controles.SingleAsync(c => c.Codigo == controlOtro)).CodCampaña);
            });

            // Beto ve sus controles con sus propias campañas y puede editarlos.
            var controlDeBeto = await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{control3}");
            Assert.Equal(campañaBeto2025, controlDeBeto!.CodCampaña);
            (await beto.PutAsJsonAsync($"/controles/{control1}", new ControlDto
            {
                CodApiario = apiario,
                CodCampaña = (await beto.GetFromJsonAsync<ControlDetalleDto>($"/controles/{control1}"))!.CodCampaña,
                Observaciones = "Editado por Beto"
            })).EnsureSuccessStatusCode();

            // Reasignarlo de vuelta no crea más copias: usa las campañas originales de la Administradora.
            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "A reasignar", UsuarioId = idAdministradora })).EnsureSuccessStatusCode();
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                Assert.Equal(3, await contexto.Campañas.CountAsync(c => c.UsuarioId == idAdministradora)); // las mismas 3 de antes
                Assert.Equal(campaña2026, (await contexto.Controles.SingleAsync(c => c.Codigo == control1)).CodCampaña);
            });
        }

        [Fact]
        public async Task La_inicializacion_asigna_las_campañas_sin_dueño_a_la_administradora()
        {
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                contexto.Campañas.AddRange(
                    new Campaña { Año = 2024, FechaAlta = DateTime.Now },
                    new Campaña { Año = 2025, FechaAlta = DateTime.Now });
                await contexto.SaveChangesAsync();
            });

            await _fabrica.CrearClienteAutenticadoAsync(); // registra a la Administradora y ejecuta la inicialización
            var idAdministradora = await _fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            await _fabrica.UsarBaseAsync(async contexto =>
                Assert.All(await contexto.Campañas.ToListAsync(), c => Assert.Equal(idAdministradora, c.UsuarioId)));
        }

        private static async Task<List<int>> CodigosCampañasAsync(HttpClient cliente) =>
            (await cliente.GetFromJsonAsync<List<CampañaGrillaDto>>("/campaña/vertodos"))!.Select(c => c.Codigo).ToList();
    }
}
