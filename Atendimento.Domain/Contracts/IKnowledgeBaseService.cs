using Atendimento.Domain.Models;

namespace Atendimento.Domain.Contracts;

public interface IKnowledgeBaseService
{
    List<KnowledgeDocument> LoadDocuments();
}
