using System.Net.Http.Json;
using System.Security.Claims;
using MeuApp.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace MeuApp.Client.Auth;

// Quem está logado? O cookie é HttpOnly (o Client não consegue lê-lo),
// então perguntamos ao Server via /api/auth/eu e guardamos a resposta.
public class ApiAuthenticationStateProvider(HttpClient http) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private Task<AuthenticationState>? cache;   // evita uma chamada à API a cada navegação

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        cache ??= BuscarAsync();

    // Chamar depois de login/logout: descarta o cache e avisa AuthorizeView/AuthorizeRouteView
    public void NotificarMudanca()
    {
        cache = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<AuthenticationState> BuscarAsync()
    {
        try
        {
            var resposta = await http.GetAsync("api/auth/eu");
            if (!resposta.IsSuccessStatusCode)
                return Anonimo;   // 401: não logado

            var info = await resposta.Content.ReadFromJsonAsync<UsuarioInfoDto>();
            if (info is null)
                return Anonimo;

            var identidade = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, info.Login), new Claim(ClaimTypes.Role, info.Papel)],
                authenticationType: "Cookie");   // com authenticationType, IsAuthenticated = true
            return new AuthenticationState(new ClaimsPrincipal(identidade));
        }
        catch (HttpRequestException)
        {
            cache = null;   // servidor fora do ar: não guarda, tenta de novo na próxima
            return Anonimo;
        }
    }
}
