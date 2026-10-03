using System.Globalization;
using GestionApiario.web.Components;
using GestionApiario.web.Servicios;
using GestionApiario.web.Servicios.Autenticacion;
using GestionApiario.web.Servicios.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Fechas y números con formato argentino (dd/MM/yyyy).
var cultura = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var urlApi = builder.Configuration["Api:UrlBase"];
if (string.IsNullOrWhiteSpace(urlApi))
    throw new InvalidOperationException("Falta configurar 'Api:UrlBase' en appsettings.json.");

builder.Services.AddHttpClient<IApiariosServicio, ApiarioServicio>(cliente => cliente.BaseAddress = new Uri(urlApi));
builder.Services.AddHttpClient<CuentaServicio>(cliente => cliente.BaseAddress = new Uri(urlApi));

// Inicio de sesión: el estado vive por circuito (pestaña del navegador).
builder.Services.AddScoped<EstadoAutenticacion>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<EstadoAutenticacion>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
