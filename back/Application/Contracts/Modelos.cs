using BeachTennis.Domain.Enums;

namespace BeachTennis.Application.Contracts;

public sealed record ConfiguracaoReservas(
    decimal PrecoPorHora,
    int DuracaoMinimaMinutos,
    int DuracaoMaximaMinutos,
    int PassoMinutos,
    decimal DescontoSocio,
    decimal DescontoAluno,
    string FusoHorario);

public sealed record ConfiguracaoAutenticacao(
    string NomeGestor,
    string EmailGestor,
    string SenhaGestor);

public sealed record HorarioDia(TimeOnly Abertura, TimeOnly Fechamento);

public sealed record ClienteDto
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("cpf")]
    public string CPF { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("telefone")]
    public string Fone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool Ativo { get; init; }

    public ClienteDto(int id, string nome, string cpf, string fone, string email, bool ativo)
    {
        Id = id;
        Nome = nome;
        CPF = cpf;
        Fone = fone;
        Email = email;
        Ativo = ativo;
    }
}

public sealed record QuadraDto(int Id, string Nome, bool Ativa);

public sealed record UsuarioDto(int Id, string Nome, string Email, string Perfil);

public sealed record ReservaDto(
    int Id,
    int ClienteId,
    int QuadraId,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    StatusReserva Status,
    decimal ValorBruto,
    decimal Desconto,
    decimal ValorFinal,
    DateTimeOffset CriadaEm);

public sealed record HorarioDisponivelDto(
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    bool Disponivel,
    decimal ValorBruto,
    decimal Desconto,
    decimal ValorFinal);
