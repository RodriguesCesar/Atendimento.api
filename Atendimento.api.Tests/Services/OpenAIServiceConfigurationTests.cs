using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;
using Atendimento.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Atendimento.API.Tests.Services;

public class OpenAIServiceConfigurationTests
{
    [Fact]
    public void Constructor_WhenModelIsMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Key"] = "test-key"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new OpenAIService(new FakeKnowledgeBaseService(), configuration));

        Assert.Contains("OpenAI:Model", exception.Message);
    }

    [Fact]
    public void Constructor_WhenKeyIsMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "gpt-4o-mini"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new OpenAIService(new FakeKnowledgeBaseService(), configuration));

        Assert.Contains("OpenAI:Key", exception.Message);
    }

    [Fact]
    public void Constructor_WhenModelAndKeyAreConfigured_DoesNotThrow()
    {
        var configuration = BuildValidConfiguration();

        var exception = Record.Exception(() =>
            new OpenAIService(new FakeKnowledgeBaseService(), configuration));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RebuildKnowledgeIndexAsync_WhenKnowledgeBaseIsEmpty_ReturnsZero()
    {
        var configuration = BuildValidConfiguration();
        var sut = new OpenAIService(new FakeKnowledgeBaseService(), configuration);

        var indexedChunks = await sut.RebuildKnowledgeIndexAsync();

        Assert.Equal(0, indexedChunks);
    }

    private static IConfiguration BuildValidConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "gpt-4o-mini",
                ["OpenAI:Key"] = "test-key"
            })
            .Build();
    }

    private sealed class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public List<KnowledgeDocument> LoadDocuments()
        {
            return [];
        }
    }
}
