using System.Net;
using System.Net.Http.Json;
using Atendimento.API;
using Atendimento.Application.UseCases;
using Atendimento.Domain.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Atendimento.API.Tests.Integration;

public class ApiSecurityAndRateLimitTests
{
    [Fact]
    public async Task AtendimentoEndpoint_WithoutApiKey_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory(permitLimit: 1);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/atendimento", new
        {
            nome = "César",
            mensagem = "Quais cursos de backend vocês têm?"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AtendimentoEndpoint_WithApiKey_ReturnsOk()
    {
        await using var factory = new TestApiFactory(permitLimit: 1);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var payload = new
        {
            nome = "César",
            mensagem = "Quais cursos de backend vocês têm?"
        };

        var response = await client.PostAsJsonAsync("/api/atendimento", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminReindex_WithApiKey_ReturnsOk()
    {
        await using var factory = new TestApiFactory(permitLimit: 1);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        var response = await client.PostAsync("/api/admin/reindex", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("indexedChunks", json);
    }

    private sealed class TestApiFactory(int permitLimit, bool autoReplenishment = true) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Security:ApiKey"] = "test-api-key",
                    ["RateLimiting:PermitLimit"] = permitLimit.ToString(),
                    ["RateLimiting:WindowSeconds"] = "60",
                    ["RateLimiting:QueueLimit"] = "0",
                    ["RateLimiting:AutoReplenishment"] = autoReplenishment.ToString(),
                    ["OpenAI:Model"] = "gpt-4o-mini",
                    ["OpenAI:Key"] = "test-openai-key",
                    ["BaseDeConhecimento:Path"] = "Documents"
                };

                configBuilder.AddInMemoryCollection(settings);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IAtendimentoUseCase, FakeUseCase>();
                services.AddSingleton<IKnowledgeIndexAdminService, FakeKnowledgeIndexAdminService>();
            });
        }
    }

    private sealed class FakeUseCase : IAtendimentoUseCase
    {
        public Task<string> ProcessarAsync(string nome, string mensagem, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"ok: {nome} - {mensagem}");
        }
    }

    private sealed class FakeKnowledgeIndexAdminService : IKnowledgeIndexAdminService
    {
        public Task<int> RebuildKnowledgeIndexAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(10);
        }
    }
}
