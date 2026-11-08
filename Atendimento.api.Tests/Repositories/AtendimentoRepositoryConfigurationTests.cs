using Atendimento.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Atendimento.API.Tests.Repositories;

public class AtendimentoRepositoryConfigurationTests
{
    [Fact]
    public void Constructor_WhenConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new AtendimentoRepository(configuration));

        Assert.Contains("ConnectionStrings:BDAtendimentos", exception.Message);
    }
}
