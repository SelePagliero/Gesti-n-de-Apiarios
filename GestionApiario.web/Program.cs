using GestionApiario.web.Components;
using GestionApiario.web.Servicios;
using GestionApiario.web.Servicios.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IApiariosServicio, ApiarioServicio>
    (cliente => { 
        cliente.BaseAddress = new Uri("https://localhost:7035"); 
    });


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


var app = builder.Build();

// Configure the HTTP request pipeline.
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
