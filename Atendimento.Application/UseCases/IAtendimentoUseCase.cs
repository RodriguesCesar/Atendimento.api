namespace Atendimento.Application.UseCases;

public interface IAtendimentoUseCase
{
    Task<string> ProcessarAsync(string nome, string mensagem, CancellationToken cancellationToken = default);
}
