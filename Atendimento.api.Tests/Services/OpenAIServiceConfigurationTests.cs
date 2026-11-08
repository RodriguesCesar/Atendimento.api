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

    private sealed class FakeKnowledgeBaseService : IKnowledgeBaseService
    {
        public List<KnowledgeDocument> LoadDocuments()
        {
            return [];
        }
    }
}
