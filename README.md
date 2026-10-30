# Atendimento.api

API de atendimento com RAG simples sobre base em Markdown, usando OpenAI para embeddings e geração de resposta.

## Segurança (ação obrigatória)

Este projeto já teve chave sensível versionada no passado. Faça as ações abaixo antes de qualquer uso em produção:

1. Rotacionar/revogar imediatamente a chave OpenAI antiga.
2. Configurar a chave somente por ambiente local/seguro (`appsettings.Local.json`, User Secrets ou variável de ambiente).
3. Nunca versionar segredos no repositório.

## Arquitetura

Estrutura em camadas:

- `Atendimento.Domain`: contratos e modelos de domínio.
- `Atendimento.Application`: caso de uso de atendimento (`IAtendimentoUseCase`).
- `Atendimento.Infrastructure`: integrações com OpenAI, arquivos markdown e SQL Server (Dapper).
- `Atendimento.api`: host HTTP, controller, DI, health checks e tratamento global de erros.

Fluxo principal:

1. `POST /api/Atendimento` recebe nome e mensagem.
2. `AtendimentoUseCase` coordena geração de resposta e persistência.
3. `OpenAIService` aplica RAG com base em markdown.
4. `AtendimentoRepository` salva histórico de atendimento.

## Configuração

1. Copie o arquivo de exemplo:

```bash
cp Atendimento.api/appsettings.Local.example.json Atendimento.api/appsettings.Local.json
```

2. Defina `Security:ApiKey` e `OpenAI:Key` em `Atendimento.api/appsettings.Local.json`.

Alternativas por variáveis de ambiente:

```bash
Security__ApiKey=api-key-segura
OpenAI__Key=seu-segredo
```

## Executar localmente

```bash
dotnet run --project Atendimento.api/Atendimento.API.csproj
```

## Endpoints principais

- `POST /api/Atendimento` (protegido por `X-API-Key`)
- `POST /api/admin/reindex` (protegido por `X-API-Key`)
- `GET /health/live`
- `GET /health/ready`

Exemplo de request:

```json
{
  "nome": "César",
  "mensagem": "Quais cursos de desenvolvimento vocês têm no período da noite?"
}
```

## Segurança e resiliência (Fase 4)

### Autenticação

- Header obrigatório para endpoints de atendimento:
  - `X-API-Key`
- Configuração:
  - `Security:ApiKey`

### Rate limiting

Limite global com janela fixa:
- quando há API key: partição por API key
- sem API key: partição por IP
- `RateLimiting:PermitLimit` (default 30)
- `RateLimiting:WindowSeconds` (default 60)
- `RateLimiting:QueueLimit` (default 0)
- `RateLimiting:AutoReplenishment` (default `true`)

Quando excede o limite, retorna `429 Too Many Requests`.

### Resiliência para chamadas OpenAI

`OpenAIService` usa retry com backoff exponencial para falhas transitórias em chamadas de chat e embedding:
- tentativas: 3
- falhas tratadas: `HttpRequestException`, `TaskCanceledException`, `TimeoutException`

Métrica de retry:
- `atendimento_ai_retries_total`

## Governança e operação (Fase 5)

### Endpoint administrativo de reindexação

Endpoint para rebuild da base vetorial em runtime:
- `POST /api/admin/reindex`

Requer autenticação por API key e retorna quantidade de chunks indexados.

### Testes

Foram adicionados testes cobrindo:
- integração HTTP de autenticação e endpoint admin (`401` e `200`)
- comportamento determinístico de rate limiting em nível de componente (janela fixa)
- health check de prontidão para dependências obrigatórias

Arquivos:
- `Atendimento.api.Tests/Integration/ApiSecurityAndRateLimitTests.cs`
- `Atendimento.api.Tests/RateLimiting/FixedWindowRateLimiterTests.cs`
- `Atendimento.api.Tests/HealthChecks/DependencyReadinessHealthCheckTests.cs`

## Observabilidade (Fase 3)

### Instrumentação OpenTelemetry

A API exporta traces e métricas via OTLP.

Service name padrão:
- `Atendimento.API`

Métricas customizadas principais:
- `atendimento_api_requests_total`
- `atendimento_api_validation_errors_total`
- `atendimento_usecase_requests_total`
- `atendimento_usecase_persisted_total`
- `atendimento_usecase_errors_total`
- `atendimento_usecase_duration_ms`
- `atendimento_ai_requests_total` (tags `provider` e `model`)
- `atendimento_ai_errors_total` (tags `provider` e `model`)
- `atendimento_ai_retries_total` (tags `provider` e `model`)
- `atendimento_ai_duration_ms` (tags `provider` e `model`)
- `atendimento_rag_indexed_chunks_total`
- `atendimento_admin_reindex_total`

Tracing por operação (novos atributos):
- `operation.name`
- `atendimento.request.nome_length`
- `atendimento.request.mensagem_length`
- `atendimento.response.length`
- `atendimento.persistence.saved`
- `atendimento.reindex.indexed_chunks`
- `error.type`

Configuração por appsettings/env:
- `OpenTelemetry__ServiceName`
- `OpenTelemetry__Otlp__Endpoint`
- `OpenTelemetry__Otlp__Protocol` (`grpc` ou `http/protobuf`)
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL`

### Health checks de liveness/readiness

- `GET /health/live`: valida somente que o processo está de pé.
- `GET /health/ready`: valida dependências essenciais de configuração e disponibilidade local:
  - `BaseDeConhecimento:Path` configurado e diretório existente
  - `OpenAI:Model` configurado
  - `OpenAI:Key` configurada
  - `ConnectionStrings:BDAtendimentos` configurada

### Stack local de observabilidade

Arquivos:
- `docker-compose.observability.yml`
- `observability/otel-collector-config.yaml`
- `observability/prometheus.yml`
- `observability/grafana/provisioning/datasources/datasources.yml`
- `observability/grafana/provisioning/dashboards/dashboards.yml`
- `observability/grafana/dashboards/atendimento-overview.json`

Suba observabilidade:

```bash
docker compose -f docker-compose.observability.yml up -d
```

Acessos locais:
- Grafana: `http://localhost:3000` (admin/admin)
- Jaeger: `http://localhost:16686`
- Prometheus: `http://localhost:9090`

Dashboard provisionado:
- `Atendimento / Atendimento API - Overview`

## CI

Workflow em `.github/workflows/ci.yml`:

1. `dotnet restore`
2. `dotnet build --configuration Release --no-restore`
3. `dotnet test` para todos os projetos de teste encontrados com coleta de cobertura
4. quality gate de cobertura de linhas (threshold atual: 70%)

## Cobertura de testes

Projeto `Atendimento.api.Tests` com cobertura para:

- carregamento/chunking da base de conhecimento
- validação do contrato de entrada do endpoint
- caso de uso de atendimento (orquestração e persistência)
- validação de API key
- integração HTTP para auth e reindex
- comportamento determinístico do limiter de janela fixa
- readiness health check de dependências obrigatórias
