using Atendimento.Application.UseCases;
using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;
using Xunit;

namespace Atendimento.API.Tests.Application;

public class AtendimentoUseCaseTests
{
    [Fact]
    public async Task ProcessarAsync_WhenValidInput_PersistsAndReturnsResponse()
    {
        var ai = new FakeAiService("resposta gerada");
        var repo = new FakeRepository();
        var sut = new AtendimentoUseCase(ai, repo);

        var resposta = await sut.ProcessarAsync("César", "Quais cursos de backend existem?");

        Assert.Equal("resposta gerada", resposta);
        Assert.Single(repo.Registros);
        Assert.Equal("César", repo.Registros[0].NomeUsuario);
    }

    [Fact]
    public async Task ProcessarAsync_WhenNameIsInvalid_ThrowsArgumentException()
    {
        var sut = new AtendimentoUseCase(new FakeAiService("ok"), new FakeRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.ProcessarAsync("", "mensagem válida"));
    }

    private sealed class FakeAiService(string response) : IAtendimentoAiService
    {
        public Task<string> GerarRespostaAsync(string nomeUsuario, string mensagem, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }
    }

    private sealed class FakeRepository : IAtendimentoRepository
    {
        public List<AtendimentoRegistro> Registros { get; } = new();

        public Task InserirAsync(AtendimentoRegistro model, CancellationToken cancellationToken = default)
        {
            Registros.Add(model);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AtendimentoRegistro>> ObterUltimosPorUsuarioAsync(string nomeUsuario, int quantidade = 3, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AtendimentoRegistro> result = Registros
                .Where(x => x.NomeUsuario == nomeUsuario)
                .Take(quantidade)
                .ToList();

            return Task.FromResult(result);
        }
    }
}
