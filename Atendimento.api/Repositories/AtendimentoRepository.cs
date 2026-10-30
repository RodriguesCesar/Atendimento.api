using Dapper;
using Microsoft.Data.SqlClient;

namespace Atendimento.API.Repositories
{
    public class AtendimentoRepository(IConfiguration configuration)
    {
        private readonly string _connectionString = configuration.GetConnectionString("BDAtendimentos")
            ?? throw new InvalidOperationException("ConnectionStrings:BDAtendimentos não configurada.");

        public async Task Inserir(AtendimentoModel model, CancellationToken cancellationToken = default)
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

        public async Task<IEnumerable<AtendimentoModel>> ObterUltimosPorUsuario(string nomeUsuario, CancellationToken cancellationToken = default)
        {
            var query = @"
                SELECT TOP 3 *
                FROM Atendimento
                WHERE NomeUsuario = @NomeUsuario
                ORDER BY DataHora DESC
            ";

            await using var connection = new SqlConnection(_connectionString);
            var command = new CommandDefinition(query, new { NomeUsuario = nomeUsuario }, cancellationToken: cancellationToken);
            return await connection.QueryAsync<AtendimentoModel>(command);
        }
    }

    public class AtendimentoModel
    {
        public Guid Id { get; set; }
        public string? NomeUsuario { get; set; }
        public string? Mensagem { get; set; }
        public string? Resposta { get; set; }
        public DateTime DataHora { get; set; }
    }
}
