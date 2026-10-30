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
        activity?.SetTag("operation.name", "admin.reindex");

        try
        {
            var indexedChunks = await knowledgeIndexAdminService.RebuildKnowledgeIndexAsync(cancellationToken);
            activity?.SetTag("atendimento.reindex.indexed_chunks", indexedChunks);
            activity?.SetStatus(ActivityStatusCode.Ok);
            ReindexCounter.Add(1);

            return Ok(new
            {
                message = "Reindexação concluída com sucesso.",
                indexedChunks
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
