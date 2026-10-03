using System.Net.Http.Json;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Pruebas
{
    // UsuarioAlta, UsuarioModificacion y UsuarioBaja guardan el email de quien hace cada cambio,
    // también cuando la Administradora modifica datos de otro apicultor.
    public class AuditoriaPruebas : IDisposable
    {
        private const string EmailAna = "ana@ejemplo.com";

        private readonly FabricaApi _fabrica = new();

        public void Dispose() => _fabrica.Dispose();

        private record Auditoria(string? Alta, string? Modificacion, string? Baja);

        [Theory]
        [InlineData("alimento")]
        [InlineData("enfermedad")]
        [InlineData("producto")]
        [InlineData("campaña")]
        public async Task Los_catalogos_registran_a_la_administradora(string ruta)
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var cuerpo = PermisosPruebas.CuerpoCatalogo(ruta);

            var codigo = await ApiarioPruebas.ObtenerCodigoCreadoAsync(await administradora.PostAsJsonAsync($"/{ruta}", cuerpo));
            (await administradora.PutAsJsonAsync($"/{ruta}/{codigo}", cuerpo)).EnsureSuccessStatusCode();
            (await administradora.DeleteAsync($"/{ruta}/{codigo}")).EnsureSuccessStatusCode();

            var auditoria = await LeerAuditoriaCatalogoAsync(ruta, codigo);
            Assert.Equal(new Auditoria(FabricaApi.EmailPrueba, FabricaApi.EmailPrueba, FabricaApi.EmailPrueba), auditoria);
        }

        [Fact]
        public async Task Un_apicultor_queda_registrado_en_sus_apiarios_y_controles()
        {
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var campaña = await CrearCampañaAsync();
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");
            var control = await PermisosPruebas.CrearControlAsync(ana, apiario, campaña);

            (await ana.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "De Ana II" })).EnsureSuccessStatusCode();
            (await ana.PutAsJsonAsync($"/controles/{control}", new ControlDto { CodCampaña = campaña, CodApiario = apiario })).EnsureSuccessStatusCode();
            (await ana.DeleteAsync($"/controles/{control}")).EnsureSuccessStatusCode();
            (await ana.DeleteAsync($"/apiario/{apiario}")).EnsureSuccessStatusCode();

            Assert.Equal(new Auditoria(EmailAna, EmailAna, EmailAna), await LeerAuditoriaApiarioAsync(apiario));
            Assert.Equal(new Auditoria(EmailAna, EmailAna, EmailAna), await LeerAuditoriaControlAsync(control));
        }

        [Fact]
        public async Task Si_la_administradora_modifica_datos_de_un_apicultor_queda_registrada_ella()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            var ana = await _fabrica.CrearApicultorAsync(EmailAna);
            var campaña = await CrearCampañaAsync();
            var apiario = await PermisosPruebas.CrearApiarioAsync(ana, "De Ana");
            var control = await PermisosPruebas.CrearControlAsync(ana, apiario, campaña);

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Corregido" })).EnsureSuccessStatusCode();
            (await administradora.PutAsJsonAsync($"/controles/{control}", new ControlDto { CodCampaña = campaña, CodApiario = apiario })).EnsureSuccessStatusCode();

            // El alta sigue siendo de Ana; la modificación es de la Administradora.
            Assert.Equal(new Auditoria(EmailAna, FabricaApi.EmailPrueba, null), await LeerAuditoriaApiarioAsync(apiario));
            Assert.Equal(new Auditoria(EmailAna, FabricaApi.EmailPrueba, null), await LeerAuditoriaControlAsync(control));

            (await administradora.DeleteAsync($"/controles/{control}")).EnsureSuccessStatusCode();
            (await administradora.DeleteAsync($"/apiario/{apiario}")).EnsureSuccessStatusCode();

            Assert.Equal(new Auditoria(EmailAna, FabricaApi.EmailPrueba, FabricaApi.EmailPrueba), await LeerAuditoriaApiarioAsync(apiario));
            Assert.Equal(new Auditoria(EmailAna, FabricaApi.EmailPrueba, FabricaApi.EmailPrueba), await LeerAuditoriaControlAsync(control));
        }

        [Fact]
        public async Task Crear_un_apiario_para_otro_y_transferirlo_queda_registrado()
        {
            var administradora = await _fabrica.CrearClienteAutenticadoAsync();
            await _fabrica.CrearApicultorAsync(EmailAna);
            var idAna = await _fabrica.ObtenerIdUsuarioAsync(EmailAna);
            var idAdministradora = await _fabrica.ObtenerIdUsuarioAsync(FabricaApi.EmailPrueba);

            // Lo crea la Administradora a nombre de Ana: el alta es de la Administradora, aunque el dueño sea Ana.
            var apiario = await ApiarioPruebas.ObtenerCodigoCreadoAsync(
                await administradora.PostAsJsonAsync("/apiario", new ApiarioDto { Nombre = "Para Ana", UsuarioId = idAna }));
            Assert.Equal(new Auditoria(FabricaApi.EmailPrueba, null, null), await LeerAuditoriaApiarioAsync(apiario));

            (await administradora.PutAsJsonAsync($"/apiario/{apiario}", new ApiarioDto { Nombre = "Para Ana", UsuarioId = idAdministradora })).EnsureSuccessStatusCode();
            Assert.Equal(new Auditoria(FabricaApi.EmailPrueba, FabricaApi.EmailPrueba, null), await LeerAuditoriaApiarioAsync(apiario));
        }

        [Fact]
        public async Task Un_email_largo_se_guarda_completo()
        {
            const string emailLargo = "apicultora.con.un.email.bastante.largo.para.probar@cooperativa-apicola.com.ar";
            Assert.True(emailLargo.Length > 50);
            var cliente = await _fabrica.CrearApicultorAsync(emailLargo);

            var apiario = await PermisosPruebas.CrearApiarioAsync(cliente, "Con email largo");

            Assert.Equal(emailLargo, (await LeerAuditoriaApiarioAsync(apiario)).Alta);
        }

        [Fact]
        public async Task Las_columnas_de_auditoria_admiten_emails_de_256_caracteres()
        {
            // La base InMemory no controla largos, así que se verifica la definición del modelo (la que usa SQL Server).
            await _fabrica.UsarBaseAsync(contexto =>
            {
                Type[] entidades = [typeof(Alimento), typeof(Apiario), typeof(Campaña), typeof(Controle), typeof(Enfermedad), typeof(Producto)];
                foreach (var entidad in entidades)
                {
                    foreach (var campo in new[] { "UsuarioAlta", "UsuarioModificacion", "UsuarioBaja" })
                    {
                        var propiedad = contexto.Model.FindEntityType(entidad)!.FindProperty(campo)!;
                        Assert.True(propiedad.GetMaxLength() == 256, $"{entidad.Name}.{campo} admite {propiedad.GetMaxLength()} caracteres");
                    }
                }
                return Task.CompletedTask;
            });
        }

        // ---- Ayudas ----

        private async Task<int> CrearCampañaAsync()
        {
            var campaña = new Campaña { Año = 2026, FechaAlta = DateTime.Now };
            await _fabrica.UsarBaseAsync(async contexto => { contexto.Add(campaña); await contexto.SaveChangesAsync(); });
            return campaña.Codigo;
        }

        private async Task<Auditoria> LeerAuditoriaApiarioAsync(int codigo)
        {
            Auditoria? resultado = null;
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                var a = await contexto.Apiarios.SingleAsync(x => x.Codigo == codigo);
                resultado = new Auditoria(a.UsuarioAlta, a.UsuarioModificacion, a.UsuarioBaja);
            });
            return resultado!;
        }

        private async Task<Auditoria> LeerAuditoriaControlAsync(int codigo)
        {
            Auditoria? resultado = null;
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                var c = await contexto.Controles.SingleAsync(x => x.Codigo == codigo);
                resultado = new Auditoria(c.UsuarioAlta, c.UsuarioModificacion, c.UsuarioBaja);
            });
            return resultado!;
        }

        private async Task<Auditoria> LeerAuditoriaCatalogoAsync(string ruta, int codigo)
        {
            Auditoria? resultado = null;
            await _fabrica.UsarBaseAsync(async contexto =>
            {
                resultado = ruta switch
                {
                    "alimento" => await contexto.Alimentos.Where(x => x.Codigo == codigo)
                        .Select(x => new Auditoria(x.UsuarioAlta, x.UsuarioModificacion, x.UsuarioBaja)).SingleAsync(),
                    "enfermedad" => await contexto.Enfermedades.Where(x => x.Codigo == codigo)
                        .Select(x => new Auditoria(x.UsuarioAlta, x.UsuarioModificacion, x.UsuarioBaja)).SingleAsync(),
                    "producto" => await contexto.Productos.Where(x => x.Codigo == codigo)
                        .Select(x => new Auditoria(x.UsuarioAlta, x.UsuarioModificacion, x.UsuarioBaja)).SingleAsync(),
                    "campaña" => await contexto.Campañas.Where(x => x.Codigo == codigo)
                        .Select(x => new Auditoria(x.UsuarioAlta, x.UsuarioModificacion, x.UsuarioBaja)).SingleAsync(),
                    _ => throw new ArgumentException(ruta)
                };
            });
            return resultado!;
        }
    }
}
