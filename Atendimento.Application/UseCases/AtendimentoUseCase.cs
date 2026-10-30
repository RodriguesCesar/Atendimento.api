using System.Diagnostics;
using System.Diagnostics.Metrics;
using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;

namespace Atendimento.Application.UseCases;

public class AtendimentoUseCase(IAtendimentoAiService aiService, IAtendimentoRepository repository) : IAtendimentoUseCase
{
    private static readonly ActivitySource ActivitySource = new("Atendimento.Application");
    private static readonly Meter Meter = new("Atendimento.Application");
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>("atendimento_usecase_requests_total");
    private static readonly Counter<long> PersistedCounter = Meter.CreateCounter<long>("atendimento_usecase_persisted_total");
    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>("atendimento_usecase_errors_total");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("atendimento_usecase_duration_ms");

    public async Task<string> ProcessarAsync(string nome, string mensagem, CancellationToken cancellationToken = default)
    {
        var startedAt = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("usecase.atendimento.process", ActivityKind.Internal);
        activity?.SetTag("operation.name", "atendimento.processar");
        RequestsCounter.Add(1);

        try
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório.", nameof(nome));

            if (string.IsNullOrWhiteSpace(mensagem))
                throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));

            var nomeNormalizado = nome.Trim();
            var mensagemNormalizada = mensagem.Trim();

            activity?.SetTag("atendimento.request.nome_length", nomeNormalizado.Length);
            activity?.SetTag("atendimento.request.mensagem_length", mensagemNormalizada.Length);

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
            activity?.SetTag("atendimento.persistence.saved", true);
            activity?.SetStatus(ActivityStatusCode.Ok);
            PersistedCounter.Add(1);

            return resposta;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            ErrorsCounter.Add(1);
            throw;
        }
        finally
        {
            DurationMs.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}
