namespace Atendimento.Domain.Contracts;

public interface IKnowledgeIndexAdminService
{
    Task<int> RebuildKnowledgeIndexAsync(CancellationToken cancellationToken = default);
}
