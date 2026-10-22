namespace MeuApp.Server.Data;

// Fica no Server (e não no Shared) para o SenhaHash nunca ser serializado para o Client
public class Usuario
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string Papel { get; set; } = "";   // MeuApp.Shared.Papeis
}
