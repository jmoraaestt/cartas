namespace MeuApp.Shared;

// O que o Client pode saber do usuário logado (nunca o hash da senha)
public class UsuarioInfoDto
{
    public string Login { get; set; } = "";
    public string Papel { get; set; } = "";
}
