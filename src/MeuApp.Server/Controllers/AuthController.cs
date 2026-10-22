using System.Security.Claims;
using MeuApp.Server.Data;
using MeuApp.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MeuApp.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(AppDbContext db, IPasswordHasher<Usuario> hasher) : ControllerBase
    {
        private const string LoginInvalido = "Login ou senha inválidos.";

        // Hash de uma senha qualquer: quando o login não existe, verificamos contra ele
        // para a resposta demorar o mesmo e não revelar quais logins existem
        private static readonly string HashFalso = new PasswordHasher<Usuario>().HashPassword(new Usuario(), "senha-que-nao-existe");

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<UsuarioInfoDto>> Login(LoginDto dto)
        {
            var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Login == dto.Login);

            var resultado = hasher.VerifyHashedPassword(usuario ?? new Usuario(), usuario?.SenhaHash ?? HashFalso, dto.Senha);
            if (usuario is null || resultado == PasswordVerificationResult.Failed)
                return Unauthorized(LoginInvalido);   // mesma mensagem para login e senha errados

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = hasher.HashPassword(usuario, dto.Senha);
                await db.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario.Login),
                new(ClaimTypes.Role, usuario.Papel)
            };
            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // IsPersistent: o cookie sobrevive ao fechar o navegador (até expirar)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidade),
                new AuthenticationProperties { IsPersistent = true });

            return new UsuarioInfoDto { Login = usuario.Login, Papel = usuario.Papel };
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        [Authorize]
        [HttpGet("eu")]
        public UsuarioInfoDto Eu() => new()
        {
            Login = User.Identity!.Name!,
            Papel = User.FindFirstValue(ClaimTypes.Role) ?? ""
        };
    }
}
