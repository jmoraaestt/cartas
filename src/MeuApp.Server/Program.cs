using System.Threading.RateLimiting;
using MeuApp.Server.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Só o hasher do Identity (PBKDF2 com salt), sem o Identity completo
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MeuApp.Auth";
        options.Cookie.HttpOnly = true;                              // JS não lê o cookie
        options.Cookie.SameSite = SameSiteMode.Strict;               // não vai em requisições de outros sites
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;                            // renova enquanto a pessoa usa
        options.LoginPath = "/login";

        // A API é chamada via fetch: redirecionar para /login devolveria HTML, então respondemos o status
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
        await context.HttpContext.Response.WriteAsync("Muitas tentativas. Aguarde um minuto e tente de novo.", token);

    // 5 tentativas de login por minuto por IP
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await SeedUsuarios.ExecutarAsync(app.Services);   // migrations + usuários fixos

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();   // serve os arquivos do Client
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();         // lê o cookie e monta o User
app.UseAuthorization();          // aplica os [Authorize]
app.UseRateLimiter();

app.MapControllers();            // /api/...
app.MapFallbackToFile("index.html"); // qualquer outra rota Blazor

app.Run();
