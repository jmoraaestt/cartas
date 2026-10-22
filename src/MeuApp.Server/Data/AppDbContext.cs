using MeuApp.Shared;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MeuApp.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Carta> Cartas => Set<Carta>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // Chaves do Data Protection (cifram o cookie de login); ficam no banco para sobreviver a redeploys
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carta>(e =>
        {
            e.Property(c => c.Titulo).HasMaxLength(150).IsRequired();
            e.Property(c => c.Conteudo).HasColumnType("longtext").IsRequired();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Login).HasMaxLength(50).IsRequired();
            e.HasIndex(u => u.Login).IsUnique();
            e.Property(u => u.SenhaHash).IsRequired();
            e.Property(u => u.Papel).HasMaxLength(20).IsRequired();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O MySQL não guarda o "Kind" da data; tudo é gravado em UTC,
        // então marcamos como UTC ao ler (senão o ToLocalTime() do Client erra o fuso)
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcConverter>();
    }

    private class UtcConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
