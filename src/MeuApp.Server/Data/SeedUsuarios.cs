using MeuApp.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuApp.Server.Data;

public static class SeedUsuarios
{
    // Aplica as migrations pendentes e cria os dois usuários fixos se ainda não existirem.
    // Nunca sobrescreve: trocar a senha no .env depois do primeiro start não muda nada no banco.
    public static async Task ExecutarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedUsuarios));

        await db.Database.MigrateAsync();

        await CriarSeNaoExiste(db, hasher, config, logger, "Escritora", Papeis.Escritora);
        await CriarSeNaoExiste(db, hasher, config, logger, "Leitora", Papeis.Leitora);
    }

    private static async Task CriarSeNaoExiste(AppDbContext db, IPasswordHasher<Usuario> hasher,
        IConfiguration config, ILogger logger, string prefixo, string papel)
    {
        var login = config[$"Seed:{prefixo}Login"];
        var senha = config[$"Seed:{prefixo}Senha"];

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("Seed__{Prefixo}Login/Seed__{Prefixo}Senha não configurados no .env; usuário não criado.", prefixo, prefixo);
            return;
        }

        if (await db.Usuarios.AnyAsync(u => u.Login == login))
            return;

        var usuario = new Usuario { Login = login, Papel = papel };
        usuario.SenhaHash = hasher.HashPassword(usuario, senha);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        logger.LogInformation("Usuário {Login} ({Papel}) criado.", login, papel);
    }
}
