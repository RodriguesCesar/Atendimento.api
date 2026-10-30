using Atendimento.Domain.Contracts;
using Atendimento.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Atendimento.Infrastructure.Repositories;

public class AtendimentoRepository(IConfiguration configuration) : IAtendimentoRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("BDAtendimentos")
        ?? throw new InvalidOperationException("ConnectionStrings:BDAtendimentos não configurada.");

    public async Task InserirAsync(AtendimentoRegistro model, CancellationToken cancellationToken = default)
    {
        var query = @"
            INSERT INTO Atendimento
            (Id, NomeUsuario, Mensagem, Resposta, DataHora)
            VALUES
            (@Id, @NomeUsuario, @Mensagem, @Resposta, @DataHora)
        ";

        await using var connection = new SqlConnection(_connectionString);
        var command = new CommandDefinition(query, model, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyList<AtendimentoRegistro>> ObterUltimosPorUsuarioAsync(string nomeUsuario, int quantidade = 3, CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT TOP (@Quantidade) *
            FROM Atendimento
            WHERE NomeUsuario = @NomeUsuario
            ORDER BY DataHora DESC
        ";

        await using var connection = new SqlConnection(_connectionString);
        var command = new CommandDefinition(query, new { NomeUsuario = nomeUsuario, Quantidade = quantidade }, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<AtendimentoRegistro>(command);

        return result.ToList();
    }
}
