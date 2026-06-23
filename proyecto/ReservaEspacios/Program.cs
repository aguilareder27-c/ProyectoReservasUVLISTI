using System.Threading;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Data;
using ReservaEspacios.Services;

var builder = WebApplication.CreateBuilder(args);

// Cadena de conexión a MySQL. En Docker se inyecta por variable de entorno
// ConnectionStrings__Default (apuntando al contenedor de la base de datos).
var connString = builder.Configuration.GetConnectionString("Default")
                 ?? "Server=localhost;Port=3306;Database=reservas_fei;User=root;Password=rootpassword;AllowPublicKeyRetrieval=True;SslMode=None;";

// Versión del servidor MySQL (ajústala si usas otra).
var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseMySql(connString, serverVersion));

builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<InasistenciaService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EsAdmin", p => p.RequireRole("Admin"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "EsAdmin");
    options.Conventions.AuthorizeFolder("/Reservaciones");
    options.Conventions.AuthorizeFolder("/Espacios");
    options.Conventions.AllowAnonymousToFolder("/Account");
});

var app = builder.Build();

// La base de datos vive en otro contenedor y puede tardar en aceptar
// conexiones, por eso reintentamos antes de crear el esquema y sembrar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int maxRetries = 12;
    for (int intento = 1; ; intento++)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex) when (intento < maxRetries)
        {
            Console.WriteLine($"Esperando a la base de datos (intento {intento}/{maxRetries}): {ex.Message}");
            Thread.Sleep(3000);
        }
    }

    DbSeeder.Seed(db);
    scope.ServiceProvider.GetRequiredService<ReservaService>().ProcesarExpiraciones();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
