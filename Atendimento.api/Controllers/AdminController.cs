using System.Diagnostics;
using System.Diagnostics.Metrics;
using Atendimento.Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Atendimento.API.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class AdminController(IKnowledgeIndexAdminService knowledgeIndexAdminService) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("Atendimento.API");
    private static readonly Meter Meter = new("Atendimento.API");
    private static readonly Counter<long> ReindexCounter = Meter.CreateCounter<long>("atendimento_admin_reindex_total");

    [HttpPost("reindex")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReindexAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("api.admin.reindex", ActivityKind.Internal);

        var indexedChunks = await knowledgeIndexAdminService.RebuildKnowledgeIndexAsync(cancellationToken);
        ReindexCounter.Add(1);

        return Ok(new
        {
            message = "Reindexação concluída com sucesso.",
            indexedChunks
        });
    }
}
