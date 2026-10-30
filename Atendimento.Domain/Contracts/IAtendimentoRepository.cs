using Atendimento.Domain.Models;

namespace Atendimento.Domain.Contracts;

public interface IAtendimentoRepository
{
    Task InserirAsync(AtendimentoRegistro model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtendimentoRegistro>> ObterUltimosPorUsuarioAsync(string nomeUsuario, int quantidade = 3, CancellationToken cancellationToken = default);
}
