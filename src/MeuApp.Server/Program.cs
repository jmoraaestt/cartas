using System.Threading.RateLimiting;
using MeuApp.Server.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Local: lê o .env. Produção (Render): não há .env, as variáveis já vêm do ambiente
var arquivoEnv = DotNetEnv.Env.TraversePath().Load();
if (!arquivoEnv.Any())
    Console.WriteLine(".env não encontrado; usando só as variáveis de ambiente.");

var builder = WebApplication.CreateBuilder(args);

// O Render define PORT; sem ela vale ASPNETCORE_URLS/ASPNETCORE_HTTP_PORTS (8080 na imagem Docker)
var porta = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(porta))
    builder.WebHost.UseUrls($"http://+:{porta}");

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Chaves no banco: sem isso, cada deploy/restart do container gera chaves novas e desloga todo mundo
builder.Services.AddDataProtection()
    .SetApplicationName("MeuApp")
    .PersistKeysToDbContext<AppDbContext>();

// O Render termina o HTTPS no proxy e repassa HTTP; esses headers dizem o IP e o esquema originais.
// O IP do proxy do Render não é fixo, então aceitamos qualquer um (o app só é acessível através dele)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Só o hasher do Identity (PBKDF2 com salt), sem o Identity completo
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MeuApp.Auth";
        options.Cookie.HttpOnly = true;                              // JS não lê o cookie
        options.Cookie.SameSite = SameSiteMode.Strict;               // não vai em requisições de outros sites
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest                       // local roda em http
            : CookieSecurePolicy.Always;                             // produção: só trafega em HTTPS
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

app.UseForwardedHeaders();        // antes de tudo: corrige Request.Scheme (https) e o IP do cliente

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

app.MapGet("/health", () => Results.Ok()).AllowAnonymous();   // health check do Render
app.MapControllers();            // /api/...
app.MapFallbackToFile("index.html"); // qualquer outra rota Blazor

app.Run();
