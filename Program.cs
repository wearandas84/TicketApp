using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TicketApp.Data;

namespace TicketApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        
        // Se inyecta el repositorio Ticket para el manejo de estos en SQLite.
        builder.Services.AddSingleton<TicketRepository>();

        // Se usa autenticacion por cookies para efectos de la prueba.
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.LoginPath = "/login";
            });
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAuthorization();

        //Se crea un endpoint de api para la descarga de evidencias utilizando seguridad para evitar descargas no autorizadas.
        app.MapGet("/api/files/{id:int}", async (int id, TicketRepository repo, HttpContext context, IConfiguration config, IWebHostEnvironment env) =>
        {
            if (!context.User.Identity?.IsAuthenticated ?? true) return Results.Unauthorized();

            var archivo = await repo.ObtenerArchivoAsync(id);
            if (archivo == null) return Results.NotFound();

            // Leemos la ruta base desde el appsettings
            var basePath = config["FileStorage:AttachmentsPath"] ?? "App_Data/Attachments";
            var fullPath = Path.Combine(env.ContentRootPath, basePath, Path.GetFileName(archivo.RutaAlmacenamiento));

            if (!File.Exists(fullPath)) return Results.NotFound();

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Results.File(fileStream, archivo.ContentType, archivo.NombreOriginal);
        });
        // Endpoint para cerrar sesión
        app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
        // Endpoint para procesar el Login fuera de Blazor
        app.MapPost("/api/login", async (HttpContext context, TicketRepository repo) =>
        {
            var form = await context.Request.ReadFormAsync();
            string username = form["Username"]!;
            string password = form["Password"]!;

            if (await repo.ValidarUsuarioAsync(username, password))
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                return Results.Redirect("/dashboard");
            }
            
            // Si falla, lo regresamos al login con un parámetro de error
            return Results.Redirect("/login?error=true");
        }).DisableAntiforgery();

        app.MapRazorComponents<Components.App>().AddInteractiveServerRenderMode();

        app.Run();
    }
}
