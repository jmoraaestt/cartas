using MeuApp.Shared;
using Microsoft.AspNetCore.Mvc;

namespace MeuApp.Server.Controllers
{
    [ApiController]
    [Route("api/cartas")]
    public class CartasController : ControllerBase
    {

        // Static: o controller é criado de novo a cada requisição
        private static readonly List<Carta> _cartas = new();
        private static int _proximoId = 1;


        [HttpPost]
        public ActionResult<Carta> Criar(NovaCartaDto dto)
        {
            var carta = new Carta
            {
                Id = _proximoId++,
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo,
                Publicada = false,
                CriadaEm = DateTime.Now
            };

            _cartas.Add(carta);
            return Created($"/api/cartas/{carta.Id}", carta); // http 201
        }

        [HttpGet("rascunhos")]
        public List<Carta> Rascunhos() =>
            _cartas.Where(c => !c.Publicada)
                   .OrderByDescending(c => c.CriadaEm)
                   .ToList();

        [HttpGet("{id:int}")]
        public ActionResult<Carta> Buscar(int id)
        {
            var carta = _cartas.FirstOrDefault(c => c.Id == id);
            return carta is null ? NotFound() : carta;
        }

        [HttpPut("{id:int}")]
        public IActionResult Editar(int id, NovaCartaDto dto)
        {
            var carta = _cartas.FirstOrDefault(c => c.Id == id);
            if (carta is null) return NotFound();
            if (carta.Publicada) return Conflict("Carta publicada não pode ser editada.");

            carta.Titulo = dto.Titulo;
            carta.Conteudo = dto.Conteudo;
            return NoContent();   // 204: deu certo, sem corpo
        }

        [HttpGet]
        public List<Carta> Publicadas() =>
            _cartas.Where(c => c.Publicada)
                   .OrderByDescending(c => c.PublicadaEm)
                   .ToList();

        [HttpPost("{id:int}/publicar")]
        public IActionResult Publicar(int id)
        {
            var carta = _cartas.FirstOrDefault(c => c.Id == id);
            if (carta is null) return NotFound();
            if (carta.Publicada) return Conflict("Esta carta já foi publicada.");
            if (string.IsNullOrWhiteSpace(carta.Titulo) || string.IsNullOrWhiteSpace(carta.Conteudo))
                return BadRequest("A carta precisa de título e conteúdo para ser publicada.");

            carta.Publicada = true;
            carta.PublicadaEm = DateTime.UtcNow;
            return NoContent();
        }
    }
}
