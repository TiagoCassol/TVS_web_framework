using BeachTennis.Domain.Enums;

namespace BeachAula4.Api;

public sealed record CriarClienteRequest(
    string Name,
    string Email,
    string Phone,
    TipoCliente Tipo = TipoCliente.Avulso);

public sealed record CriarQuadraRequest(string Name);

public sealed record CriarReservaRequest(
    int ClienteId,
    int QuadraId,
    DateTimeOffset Inicio,
    DateTimeOffset Fim);
