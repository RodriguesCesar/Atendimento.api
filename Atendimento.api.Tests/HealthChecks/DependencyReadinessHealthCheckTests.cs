using Atendimento.API.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Atendimento.api.Tests.HealthChecks;

public class DependencyReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenAllDependenciesAreConfigured()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"atendimento-healthcheck-{Guid.NewGuid():N}");
        var docsFolder = Path.Combine(tempRoot, "Documents");
        Directory.CreateDirectory(docsFolder);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BaseDeConhecimento:Path"] = "Documents",
                    ["OpenAI:Model"] = "gpt-4o-mini",
                    ["OpenAI:Key"] = "test-key",
                    ["ConnectionStrings:BDAtendimentos"] = "Server=localhost;Database=Atendimento;User Id=test;Password=test;"
                })
                .Build();

            var environment = new FakeEnvironment { ContentRootPath = tempRoot };
            var healthCheck = new DependencyReadinessHealthCheck(config, environment);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenDependenciesAreMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseDeConhecimento:Path"] = "Documents"
            })
            .Build();

        var environment = new FakeEnvironment { ContentRootPath = Path.GetTempPath() };
        var healthCheck = new DependencyReadinessHealthCheck(config, environment);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("OpenAI:Model", result.Data.Keys);
        Assert.Contains("OpenAI:Key", result.Data.Keys);
        Assert.Contains("ConnectionStrings:BDAtendimentos", result.Data.Keys);
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Atendimento.api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
