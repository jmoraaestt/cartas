using System.ComponentModel.DataAnnotations;

namespace MeuApp.Shared;

public class NovaCartaDto
{
    [Required(ErrorMessage = "Dê um título à carta.")]
    [MaxLength(150)]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "A carta está vazia.")]
    public string Conteudo { get; set; } = "";
}