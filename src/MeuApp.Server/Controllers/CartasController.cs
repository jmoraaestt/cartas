using MeuApp.Server.Data;
using MeuApp.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuApp.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/cartas")]
    public class CartasController(AppDbContext db) : ControllerBase
    {
        [Authorize(Roles = Papeis.Escritora)]
        [HttpPost]
        public async Task<ActionResult<Carta>> Criar(NovaCartaDto dto)
        {
            var carta = new Carta
            {
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo,
                Publicada = false,
                CriadaEm = DateTime.UtcNow
            };

            db.Cartas.Add(carta);
            await db.SaveChangesAsync(); // só aqui vai pro banco; o Id é preenchido pelo MySQL
            return Created($"/api/cartas/{carta.Id}", carta); // http 201
        }

        [Authorize(Roles = Papeis.Escritora)]
        [HttpGet("rascunhos")]
        public async Task<List<Carta>> Rascunhos() =>
            await db.Cartas.Where(c => !c.Publicada)
                           .OrderByDescending(c => c.CriadaEm)
                           .ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Carta>> Buscar(int id)
        {
            var carta = await db.Cartas.FindAsync(id);
            if (carta is null) return NotFound();

            if (User.IsInRole(Papeis.Leitora))
            {
                // rascunho não existe para a Leitora (404, e não 403, para nem revelar que existe)
                if (!carta.Publicada) return NotFound();

                if (carta.LidaEm is null)
                {
                    carta.LidaEm = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }

            return carta;
        }

        [Authorize(Roles = Papeis.Escritora)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, NovaCartaDto dto)
        {
            var carta = await db.Cartas.FindAsync(id);
            if (carta is null) return NotFound();
            if (carta.Publicada) return Conflict("Carta publicada não pode ser editada.");

            carta.Titulo = dto.Titulo;
            carta.Conteudo = dto.Conteudo;
            await db.SaveChangesAsync(); // o EF detecta o que mudou e gera o UPDATE
            return NoContent();   // 204: deu certo, sem corpo
        }

        [HttpGet]
        public async Task<List<Carta>> Publicadas() =>
            await db.Cartas.Where(c => c.Publicada)
                           .OrderByDescending(c => c.PublicadaEm)
                           .ToListAsync();

        [Authorize(Roles = Papeis.Escritora)]
        [HttpPost("{id:int}/publicar")]
        public async Task<IActionResult> Publicar(int id)
        {
            var carta = await db.Cartas.FindAsync(id);
            if (carta is null) return NotFound();
            if (carta.Publicada) return Conflict("Esta carta já foi publicada.");
            if (string.IsNullOrWhiteSpace(carta.Titulo) || string.IsNullOrWhiteSpace(carta.Conteudo))
                return BadRequest("A carta precisa de título e conteúdo para ser publicada.");

            carta.Publicada = true;
            carta.PublicadaEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
