namespace Atendimento.Domain.Models;

public class AtendimentoRegistro
{
    public Guid Id { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
}
