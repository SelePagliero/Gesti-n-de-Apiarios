using GestionApiario.compartido.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Models
{
    // Se ejecuta al arrancar la API. Se puede repetir sin efectos secundarios:
    //  1. Crea el rol "Administrador" si no existe.
    //  2. Se lo asigna a la cuenta configurada en "Administracion:Email" (si esa cuenta ya está registrada).
    //  3. Pasa a esa cuenta los apiarios que todavía no tienen dueño (los cargados antes de que existieran los dueños).
    public class InicializadorDatos
    {
        private readonly GestionApiariosContext _context;
        private readonly UserManager<IdentityUser> _usuarios;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly IConfiguration _configuracion;
        private readonly ILogger<InicializadorDatos> _log;

        public InicializadorDatos(GestionApiariosContext context, UserManager<IdentityUser> usuarios,
            RoleManager<IdentityRole> roles, IConfiguration configuracion, ILogger<InicializadorDatos> log)
        {
            _context = context;
            _usuarios = usuarios;
            _roles = roles;
            _configuracion = configuracion;
            _log = log;
        }

        public async Task EjecutarAsync()
        {
            if (!await _roles.RoleExistsAsync(RolesUsuario.Administrador))
                await _roles.CreateAsync(new IdentityRole(RolesUsuario.Administrador));

            var emailAdministracion = _configuracion["Administracion:Email"];
            if (string.IsNullOrWhiteSpace(emailAdministracion))
            {
                _log.LogWarning("No está configurado 'Administracion:Email': nadie tiene el rol de Administrador.");
                return;
            }

            var administradora = await _usuarios.FindByEmailAsync(emailAdministracion);
            if (administradora is null)
            {
                _log.LogWarning("La cuenta de administración {Email} todavía no está registrada. Registrala y reiniciá la API.", emailAdministracion);
                return;
            }

            if (!await _usuarios.IsInRoleAsync(administradora, RolesUsuario.Administrador))
                await _usuarios.AddToRoleAsync(administradora, RolesUsuario.Administrador);

            var apiariosSinDueño = await _context.Apiarios.Where(a => a.UsuarioId == null).ToListAsync();
            if (apiariosSinDueño.Count > 0)
            {
                foreach (var apiario in apiariosSinDueño)
                    apiario.UsuarioId = administradora.Id;
                await _context.SaveChangesAsync();
                _log.LogInformation("Se asignaron {Cantidad} apiarios sin dueño a {Email}.", apiariosSinDueño.Count, emailAdministracion);
            }
        }
    }
}
