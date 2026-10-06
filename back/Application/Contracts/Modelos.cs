using BeachTennis.Domain.Enums;

namespace BeachTennis.Application.Contracts;

public sealed record ConfiguracaoReservas(
    decimal PrecoPorHora,
    int DuracaoMinimaMinutos,
    int DuracaoMaximaMinutos,
    int PassoMinutos,
    decimal DescontoAssociado,
    decimal DescontoProfessor,
    string FusoHorario);

public sealed record HorarioDia(TimeOnly Abertura, TimeOnly Fechamento);

public sealed record ClienteDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    TipoCliente Tipo,
    bool Ativo);

public sealed record QuadraDto(int Id, string Name, bool Ativa);

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
