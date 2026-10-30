using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Atendimento.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atendimento.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class AtendimentoController(IAtendimentoUseCase useCase) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("Atendimento.API");
    private static readonly Meter Meter = new("Atendimento.API");
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>("atendimento_api_requests_total");
    private static readonly Counter<long> ValidationErrorsCounter = Meter.CreateCounter<long>("atendimento_api_validation_errors_total");

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostAsync([FromBody] AtendimentoRequest request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("api.atendimento.post", ActivityKind.Server);
        RequestsCounter.Add(1);

        if (!ModelState.IsValid)
        {
            ValidationErrorsCounter.Add(1);
            return ValidationProblem(ModelState);
        }

        var resposta = await useCase.ProcessarAsync(request.Nome, request.Mensagem, cancellationToken);
        return Ok(resposta);
    }
}

public record AtendimentoRequest(
    [Required(AllowEmptyStrings = false)]
    [MinLength(2)]
    [MaxLength(120)]
    string Nome,
    [Required(AllowEmptyStrings = false)]
    [MinLength(5)]
    [MaxLength(2000)]
    string Mensagem
);
