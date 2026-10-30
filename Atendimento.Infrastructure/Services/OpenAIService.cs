using Atendimento.Domain.Contracts;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Atendimento.Infrastructure.Services;

public class OpenAIService : IAtendimentoAiService
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _indexingLock = new(1, 1);
    private bool _knowledgeLoaded;

    private readonly List<(string Text, float[] Vector)> _vectorDb = new();

    public OpenAIService(IKnowledgeBaseService knowledgeBaseService, IConfiguration configuration)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _configuration = configuration;

        var model = configuration["OpenAI:Model"];
        var key = configuration["OpenAI:Key"];

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Configuração OpenAI:Model não encontrada.");

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("OpenAI:Key não configurada. Configure via variável de ambiente ou appsettings.Local.json.");

        var openAIClient = new OpenAIClient(key);
        _chatClient = openAIClient.GetChatClient(model);
        _embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-3-small");
    }

    public async Task<string> GerarRespostaAsync(string nomeUsuario, string mensagem, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeUsuario))
            throw new ArgumentException("Nome do usuário é obrigatório.", nameof(nomeUsuario));

        if (string.IsNullOrWhiteSpace(mensagem))
            throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));

        await EnsureKnowledgeLoadedAsync(cancellationToken);

        var perguntaCompleta = $"Meu nome é {nomeUsuario.Trim()}, tenho a seguinte pergunta: {mensagem.Trim()}";
        var contexto = await BuscarContextoAsync(perguntaCompleta);

        var prompt = MontarPrompt(perguntaCompleta, contexto);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(prompt.System),
            new UserChatMessage(prompt.User)
        };

        var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content.FirstOrDefault()?.Text ?? "Não consegui gerar uma resposta neste momento.";
    }

    private async Task EnsureKnowledgeLoadedAsync(CancellationToken cancellationToken)
    {
        if (_knowledgeLoaded)
            return;

        await _indexingLock.WaitAsync(cancellationToken);
        try
        {
            if (_knowledgeLoaded)
                return;

            await IndexarBaseDeConhecimentoAsync();
            _knowledgeLoaded = true;
        }
        finally
        {
            _indexingLock.Release();
        }
    }

    private async Task IndexarBaseDeConhecimentoAsync()
    {
        var docs = _knowledgeBaseService.LoadDocuments();

        foreach (var doc in docs)
        {
            foreach (var chunk in doc.Chunks)
            {
                var emb = await _embeddingClient.GenerateEmbeddingAsync(chunk);
                _vectorDb.Add((chunk, emb.Value.ToFloats().ToArray()));
            }
        }
    }

    private async Task<string> BuscarContextoAsync(string pergunta)
    {
        if (_vectorDb.Count == 0)
            return string.Empty;

        var emb = await _embeddingClient.GenerateEmbeddingAsync(pergunta);
        var queryVector = emb.Value.ToFloats().ToArray();

        var top = _vectorDb
            .Select(x => new
            {
                x.Text,
                Score = CosineSimilarity(queryVector, x.Vector)
            })
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Text);

        return string.Join("\n\n", top);
    }

    private (string System, string User) MontarPrompt(string pergunta, string contexto)
    {
        var system = _configuration["EngenhariaDePrompt:AtendenteCOTI"]
            ?? throw new InvalidOperationException("Configuração EngenhariaDePrompt:AtendenteCOTI não encontrada.");

        var user = $"""
        CONTEXTO:
        {contexto}

        PERGUNTA:
        {pergunta}
        """;

        return (system, user);
    }

    private static double CosineSimilarity(float[] v1, float[] v2)
    {
        double dot = 0;
        double mag1 = 0;
        double mag2 = 0;

        for (var i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += Math.Pow(v1[i], 2);
            mag2 += Math.Pow(v2[i], 2);
        }

        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}
