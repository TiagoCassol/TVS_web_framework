using System.Text.Json.Serialization;
using BeachTennis.Domain.Enums;

namespace BeachAula4.Api;

public sealed record CriarClienteRequest(
    string Nome,
    [property: JsonPropertyName("cpf")] string CPF,
    [property: JsonPropertyName("telefone")] string Fone,
    string? Email = null);

public sealed record CriarQuadraRequest(string Nome);

public sealed record CriarReservaRequest(
    int ClienteId,
    int QuadraId,
    TipoCliente? TipoCliente,
    DateTimeOffset Inicio,
    DateTimeOffset Fim);

public sealed record LoginRequest(string Email, string Senha);
