namespace Atendimento.Domain.Models;

public class KnowledgeDocument
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Chunks { get; set; } = new();
}
