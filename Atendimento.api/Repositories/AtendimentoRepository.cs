using Dapper;
using Microsoft.Data.SqlClient;

namespace Atendimento.API.Repositories
{
    public class AtendimentoRepository(IConfiguration configuration)
    {
        public async Task Inserir(AtendimentoModel model)
        {
            var query = @"
                INSERT INTO Atendimento
                (Id, NomeUsuario, Mensagem, Resposta, DataHora)
                VALUES
                (@Id, @NomeUsuario, @Mensagem, @Resposta, @DataHora)
            ";

            using (var connection = new SqlConnection(configuration.GetSection("ConnectionStrings:BDAtendimentos").Value))
            {
                await connection.ExecuteAsync(query, model);
            }
        }

        public async Task<IEnumerable<AtendimentoModel>> ObterUltimosPorUsuario(string nomeUsuario)
        {
            var query = @"
                SELECT TOP 3 *
                FROM Atendimento
                WHERE NomeUsuario = @NomeUsuario
                ORDER BY DataHora DESC
            ";

            using (var connection = new SqlConnection(configuration.GetSection("ConnectionStrings:BDAtendimentos").Value))
            {
                return await connection.QueryAsync<AtendimentoModel>(query, new { NomeUsuario = nomeUsuario });
            }
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
