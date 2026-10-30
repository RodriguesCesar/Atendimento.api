using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Atendimento.API.HealthChecks;

public class DependencyReadinessHealthCheck(IConfiguration configuration, IWebHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failures = new Dictionary<string, object>();

        var knowledgeBasePath = configuration["BaseDeConhecimento:Path"];
        if (string.IsNullOrWhiteSpace(knowledgeBasePath))
        {
            failures["BaseDeConhecimento:Path"] = "missing";
        }
        else
        {
            var fullKnowledgeBasePath = Path.Combine(environment.ContentRootPath, knowledgeBasePath);
            if (!Directory.Exists(fullKnowledgeBasePath))
                failures["BaseDeConhecimento:Path"] = $"directory_not_found:{fullKnowledgeBasePath}";
        }

        if (string.IsNullOrWhiteSpace(configuration["OpenAI:Model"]))
            failures["OpenAI:Model"] = "missing";

        if (string.IsNullOrWhiteSpace(configuration["OpenAI:Key"]))
            failures["OpenAI:Key"] = "missing";

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("BDAtendimentos")))
            failures["ConnectionStrings:BDAtendimentos"] = "missing";

        if (failures.Count > 0)
            return Task.FromResult(HealthCheckResult.Unhealthy("Readiness dependencies are not configured correctly.", data: failures));

        return Task.FromResult(HealthCheckResult.Healthy("Readiness dependencies are configured."));
    }
}
