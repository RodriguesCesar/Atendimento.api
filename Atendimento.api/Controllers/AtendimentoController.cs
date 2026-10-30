using System.ComponentModel.DataAnnotations;
using Atendimento.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Atendimento.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AtendimentoController(IAtendimentoUseCase useCase) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request, CancellationToken cancellationToken)
    {
        var resposta = await useCase.ProcessarAsync(request.Nome, request.Mensagem, cancellationToken);
        return Ok(resposta);
    }
}

public record AtendimentoRequest(
    [property: Required(AllowEmptyStrings = false), MinLength(2), MaxLength(120)] string Nome,
    [property: Required(AllowEmptyStrings = false), MinLength(5), MaxLength(2000)] string Mensagem
);
