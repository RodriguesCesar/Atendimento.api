using System.Text;
using System.Text.RegularExpressions;
using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;

namespace Atendimento.Infrastructure.Services;

public class BaseDeConhecimentoService(string basePath) : IKnowledgeBaseService
{
    private readonly string _basePath = basePath;

    public List<KnowledgeDocument> LoadDocuments()
    {
        if (!Directory.Exists(_basePath))
            throw new DirectoryNotFoundException($"Diretório não encontrado: {_basePath}");

        var files = Directory.GetFiles(_basePath, "*.md", SearchOption.AllDirectories);

        var documents = new List<KnowledgeDocument>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);

            var doc = new KnowledgeDocument
            {
                FileName = Path.GetFileName(file),
                Content = content,
                Chunks = SplitMarkdown(content)
            };

            documents.Add(doc);
        }

        return documents;
    }

    private static List<string> SplitMarkdown(string content, int maxChunkSize = 500)
    {
        var sections = Regex.Split(content, @"(?=^#{1,3}\s)", RegexOptions.Multiline);

        var chunks = new List<string>();

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section))
                continue;

            if (section.Length <= maxChunkSize)
            {
                chunks.Add(section.Trim());
            }
            else
            {
                chunks.AddRange(SplitLargeText(section, maxChunkSize));
            }
        }

        return chunks;
    }

    private static List<string> SplitLargeText(string text, int maxChunkSize)
    {
        var chunks = new List<string>();

        var current = 0;

        while (current < text.Length)
        {
            var length = Math.Min(maxChunkSize, text.Length - current);
            var chunk = text.Substring(current, length);

            chunks.Add(chunk.Trim());

            current += length;
        }

        return chunks;
    }
}
