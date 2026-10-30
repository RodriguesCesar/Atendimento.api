namespace Atendimento.Domain.Contracts;

public interface IAtendimentoAiService
{
    Task<string> GerarRespostaAsync(string nomeUsuario, string mensagem, CancellationToken cancellationToken = default);
}
