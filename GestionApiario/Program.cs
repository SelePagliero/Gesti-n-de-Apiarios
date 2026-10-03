using System.Security.Claims;
using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    // Permite probar los endpoints protegidos desde Swagger con el botón "Authorize".
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Pegá el accessToken que devuelve POST /cuenta/login."
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// La cadena de conexión se lee de los secretos de usuario en desarrollo (ver README).
var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'DefaultConnection'. Configurala con: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=...;Database=GestionApiarios;...\" --project GestionApiario");
}

builder.Services.AddDbContext<GestionApiariosContext>(options => options.UseSqlServer(cadenaConexion));

// Usuarios: ASP.NET Core Identity con tokens de acceso (endpoints /cuenta/login, /cuenta/register, /cuenta/refresh).
builder.Services.AddAuthorization();
builder.Services
    .AddIdentityApiEndpoints<IdentityUser>(opciones =>
    {
        opciones.User.RequireUniqueEmail = true;
        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<GestionApiariosContext>();

builder.Services.AddScoped<InicializadorDatos>();

var app = builder.Build();

// Rol de Administradora y dueño de los apiarios cargados antes de que existieran los dueños.
using (var alcance = app.Services.CreateScope())
{
    await alcance.ServiceProvider.GetRequiredService<InicializadorDatos>().EjecutarAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var cuenta = app.MapGroup("/cuenta").WithTags("Cuenta");
cuenta.MapIdentityApi<IdentityUser>();

// Quién inició sesión y si es la Administradora (la web lo usa para decidir qué mostrar).
cuenta.MapGet("/yo", (ClaimsPrincipal usuario) => new UsuarioActualDto
{
    Email = usuario.Identity?.Name ?? string.Empty,
    EsAdministrador = usuario.IsInRole(RolesUsuario.Administrador)
}).RequireAuthorization();

// Todos los controladores exigen haber iniciado sesión.
app.MapControllers().RequireAuthorization();

app.Run();

// Permite que el proyecto de pruebas levante la API con WebApplicationFactory.
public partial class Program { }
