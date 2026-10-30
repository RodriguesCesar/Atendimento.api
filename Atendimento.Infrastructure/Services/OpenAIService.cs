using System.Diagnostics;
using System.Diagnostics.Metrics;
using Atendimento.Domain.Contracts;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Polly;
using Polly.Retry;

namespace Atendimento.Infrastructure.Services;

public class OpenAIService : IAtendimentoAiService
{
    private static readonly ActivitySource ActivitySource = new("Atendimento.Infrastructure");
    private static readonly Meter Meter = new("Atendimento.Infrastructure");
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>("atendimento_ai_requests_total");
    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>("atendimento_ai_errors_total");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("atendimento_ai_retries_total");
    private static readonly Counter<long> IndexedChunksCounter = Meter.CreateCounter<long>("atendimento_rag_indexed_chunks_total");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("atendimento_ai_duration_ms");

    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IConfiguration _configuration;
    private readonly string _model;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _indexingLock = new(1, 1);
    private bool _knowledgeLoaded;

    private readonly List<(string Text, float[] Vector)> _vectorDb = new();

    public OpenAIService(IKnowledgeBaseService knowledgeBaseService, IConfiguration configuration)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _configuration = configuration;

        _model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Configuração OpenAI:Model não encontrada.");
        var key = configuration["OpenAI:Key"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("OpenAI:Key não configurada. Configure via variável de ambiente ou appsettings.Local.json.");

        var openAIClient = new OpenAIClient(key);
        _chatClient = openAIClient.GetChatClient(_model);
        _embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-3-small");

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                onRetry: (_, _, _, _) =>
                {
                    RetryCounter.Add(
                        1,
                        new KeyValuePair<string, object?>("provider", "openai"),
                        new KeyValuePair<string, object?>("model", _model)
                    );
                });
    }

    public async Task<string> GerarRespostaAsync(string nomeUsuario, string mensagem, CancellationToken cancellationToken = default)
    {
        var startedAt = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("infra.openai.generate_response", ActivityKind.Client);
        activity?.SetTag("ai.provider", "openai");
        activity?.SetTag("ai.model", _model);

        RequestsCounter.Add(
            1,
            new KeyValuePair<string, object?>("provider", "openai"),
            new KeyValuePair<string, object?>("model", _model)
        );

        try
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario))
                throw new ArgumentException("Nome do usuário é obrigatório.", nameof(nomeUsuario));

            if (string.IsNullOrWhiteSpace(mensagem))
                throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));

            await EnsureKnowledgeLoadedAsync(cancellationToken);

            var perguntaCompleta = $"Meu nome é {nomeUsuario.Trim()}, tenho a seguinte pergunta: {mensagem.Trim()}";
            var contexto = await BuscarContextoAsync(perguntaCompleta, cancellationToken);

            var prompt = MontarPrompt(perguntaCompleta, contexto);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(prompt.System),
                new UserChatMessage(prompt.User)
            };

            var response = await _retryPolicy.ExecuteAsync(async _ =>
                await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken),
                cancellationToken);

            return response.Value.Content.FirstOrDefault()?.Text ?? "Não consegui gerar uma resposta neste momento.";
        }
        catch
        {
            ErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>("provider", "openai"),
                new KeyValuePair<string, object?>("model", _model)
            );
            throw;
        }
        finally
        {
            DurationMs.Record(
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                new KeyValuePair<string, object?>("provider", "openai"),
                new KeyValuePair<string, object?>("model", _model)
            );
        }
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

            await IndexarBaseDeConhecimentoAsync(cancellationToken);
            _knowledgeLoaded = true;
        }
        finally
        {
            _indexingLock.Release();
        }
    }

    private async Task IndexarBaseDeConhecimentoAsync(CancellationToken cancellationToken)
    {
        var docs = _knowledgeBaseService.LoadDocuments();

        foreach (var doc in docs)
        {
            foreach (var chunk in doc.Chunks)
            {
                var emb = await _retryPolicy.ExecuteAsync(async _ =>
                    await _embeddingClient.GenerateEmbeddingAsync(chunk, cancellationToken: cancellationToken),
                    cancellationToken);

                _vectorDb.Add((chunk, emb.Value.ToFloats().ToArray()));
                IndexedChunksCounter.Add(1);
            }
        }
    }

    private async Task<string> BuscarContextoAsync(string pergunta, CancellationToken cancellationToken)
    {
        if (_vectorDb.Count == 0)
            return string.Empty;

        var emb = await _retryPolicy.ExecuteAsync(async _ =>
            await _embeddingClient.GenerateEmbeddingAsync(pergunta, cancellationToken: cancellationToken),
            cancellationToken);

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
