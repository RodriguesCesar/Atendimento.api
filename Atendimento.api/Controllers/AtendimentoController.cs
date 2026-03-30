using Atendimento.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Atendimento.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtendimentoController(OpenAIService service) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request)
        {
            var text = $"Meu nome é {request.Nome}, tenho a seguinte pergunta: {request.Mensagem}";
            var resposta = await service.SendMessage(text);

            return Ok(resposta);
        }
    }

    public record AtendimentoRequest(
        string Nome, //Nome da pessoa
        string Mensagem //Mensagem da pessoa
    );
}
