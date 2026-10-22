using System.ComponentModel.DataAnnotations;

namespace MeuApp.Shared;

public class LoginDto
{
    [Required(ErrorMessage = "Informe o login.")]
    [MaxLength(50)]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    public string Senha { get; set; } = "";
}
