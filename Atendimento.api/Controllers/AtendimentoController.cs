using Atendimento.API.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Atendimento.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtendimentoController(OpenAIService service) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request, CancellationToken cancellationToken)
        {
            var text = $"Meu nome é {request.Nome.Trim()}, tenho a seguinte pergunta: {request.Mensagem.Trim()}";
            var resposta = await service.SendMessage(text, cancellationToken);

            return Ok(resposta);
        }
    }

    public record AtendimentoRequest(
        [property: Required(AllowEmptyStrings = false), MinLength(2), MaxLength(120)] string Nome,
        [property: Required(AllowEmptyStrings = false), MinLength(5), MaxLength(2000)] string Mensagem
    );
}
