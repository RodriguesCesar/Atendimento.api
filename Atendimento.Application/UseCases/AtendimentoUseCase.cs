using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;

namespace Atendimento.Application.UseCases;

public class AtendimentoUseCase(IAtendimentoAiService aiService, IAtendimentoRepository repository) : IAtendimentoUseCase
{
    public async Task<string> ProcessarAsync(string nome, string mensagem, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        if (string.IsNullOrWhiteSpace(mensagem))
            throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));

        var nomeNormalizado = nome.Trim();
        var mensagemNormalizada = mensagem.Trim();

        var resposta = await aiService.GerarRespostaAsync(nomeNormalizado, mensagemNormalizada, cancellationToken);

        var registro = new AtendimentoRegistro
        {
            Id = Guid.NewGuid(),
            NomeUsuario = nomeNormalizado,
            Mensagem = mensagemNormalizada,
            Resposta = resposta,
            DataHora = DateTime.UtcNow
        };

        await repository.InserirAsync(registro, cancellationToken);

        return resposta;
    }
}
