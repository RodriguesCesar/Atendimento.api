# Atendimento.api

API de atendimento com RAG simples sobre base em Markdown, usando OpenAI para embeddings e geração de resposta.

## Segurança (ação obrigatória)

Este projeto já teve chave sensível versionada no passado. Faça as ações abaixo antes de qualquer uso em produção:

1. Rotacionar/revogar imediatamente a chave OpenAI antiga.
2. Configurar a chave somente por ambiente local/seguro (`appsettings.Local.json`, User Secrets ou variável de ambiente).
3. Nunca versionar segredos no repositório.

## Configuração

1. Copie o arquivo de exemplo:

```bash
cp Atendimento.api/appsettings.Local.example.json Atendimento.api/appsettings.Local.json
```

2. Defina a chave OpenAI em `Atendimento.api/appsettings.Local.json`.

Alternativa por variável de ambiente:

```bash
OpenAI__Key=seu-segredo
```

## Executar localmente

```bash
dotnet run --project Atendimento.api/Atendimento.API.csproj
```

## Endpoints principais

- `POST /api/Atendimento`
- `GET /health/live`
- `GET /health/ready`

Exemplo de request:

```json
{
  "nome": "César",
  "mensagem": "Quais cursos de desenvolvimento vocês têm no período da noite?"
}
```

## CI

Workflow em `.github/workflows/ci.yml`:

1. `dotnet restore`
2. `dotnet build --configuration Release --no-restore`
3. `dotnet test` para todos os projetos de teste encontrados

## Testes

Projeto `Atendimento.api.Tests` com cobertura inicial para:

- carregamento/chunking da base de conhecimento
- validação do contrato de entrada do endpoint
