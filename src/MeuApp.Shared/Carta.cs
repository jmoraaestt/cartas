namespace MeuApp.Shared;

public class Carta
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Conteudo { get; set; } = "";
    public bool Publicada { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? PublicadaEm { get; set; }
}