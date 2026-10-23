using System.Text.Json.Serialization;
using BeachAula4.Data;
using BeachAula4.Infrastructure;
using BeachAula4.Api;
using BeachTennis.Application.Contracts;
using BeachTennis.Application.Errors;
using BeachTennis.Application.Interfaces;
using BeachTennis.Application.Services;
using BeachTennis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var reservasConfig = builder.Configuration.GetSection("Reservas");
var horarioConfig = builder.Configuration.GetSection("HorarioFuncionamento");
var configuracaoReservas = new ConfiguracaoReservas(
    reservasConfig.GetValue<decimal?>("PrecoPorHora")
        ?? throw new InvalidOperationException("Reservas:PrecoPorHora não foi configurado."),
    reservasConfig.GetValue<int?>("DuracaoMinimaMinutos")
        ?? throw new InvalidOperationException("Reservas:DuracaoMinimaMinutos não foi configurado."),
    reservasConfig.GetValue<int?>("DuracaoMaximaMinutos")
        ?? throw new InvalidOperationException("Reservas:DuracaoMaximaMinutos não foi configurado."),
    reservasConfig.GetValue<int?>("PassoMinutos")
        ?? throw new InvalidOperationException("Reservas:PassoMinutos não foi configurado."),
    reservasConfig.GetValue<decimal?>("DescontoSocio")
        ?? throw new InvalidOperationException("Reservas:DescontoSocio não foi configurado."),
    reservasConfig.GetValue<decimal?>("DescontoAluno")
        ?? throw new InvalidOperationException("Reservas:DescontoAluno não foi configurado."),
    horarioConfig["FusoHorario"]
        ?? throw new InvalidOperationException("HorarioFuncionamento:FusoHorario não foi configurado."));
_ = TimeZoneInfo.FindSystemTimeZoneById(configuracaoReservas.FusoHorario);

var autenticacaoConfig = builder.Configuration.GetSection("Autenticacao");
var configuracaoAutenticacao = new ConfiguracaoAutenticacao(
    autenticacaoConfig["NomeGestor"]
        ?? throw new InvalidOperationException("Autenticacao:NomeGestor não foi configurado."),
    autenticacaoConfig["EmailGestor"]
        ?? throw new InvalidOperationException("Autenticacao:EmailGestor não foi configurado."),
    autenticacaoConfig["SenhaGestor"]
        ?? throw new InvalidOperationException("Autenticacao:SenhaGestor não foi configurado."));
if (string.IsNullOrWhiteSpace(configuracaoAutenticacao.SenhaGestor))
    throw new InvalidOperationException("Autenticacao:SenhaGestor não pode ser vazia.");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(configuracaoReservas);
builder.Services.AddSingleton(configuracaoAutenticacao);
builder.Services.AddSingleton(new CalculadoraPreco(configuracaoReservas));
builder.Services.AddSingleton<IHorarioFuncionamento>(
    HorarioFuncionamento.Carregar(builder.Configuration.GetSection("HorarioFuncionamento")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ServicoReservas>();
builder.Services.AddScoped<ServicoAutenticacao>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var api = app.MapGroup("/api");

api.MapPost("/auth/login", (LoginRequest request, ServicoAutenticacao autenticacao) =>
{
    var usuario = autenticacao.Autenticar(request.Email, request.Senha);
    return usuario is null ? Results.Unauthorized() : Results.Ok(usuario);
});

api.MapPost("/clientes", async (
    CriarClienteRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var cliente = await servico.RegistrarClienteAsync(
        request.Nome,
        request.CPF,
        request.Fone,
        request.Email,
        cancellationToken);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});

api.MapGet("/clientes/{id:int}", async (
    int id,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ObterClienteAsync(id, cancellationToken)));

api.MapGet("/clientes", async (
    string? cpf,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ListarClientesAsync(cpf, cancellationToken)));

api.MapPost("/quadras", async (
    CriarQuadraRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var quadra = await servico.RegistrarQuadraAsync(request.Nome, cancellationToken);
    return Results.Created($"/api/quadras/{quadra.Id}", quadra);
});

api.MapGet("/quadras", async (
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ListarQuadrasAsync(cancellationToken)));

api.MapGet("/quadras/{quadraId:int}/disponibilidade", async (
    int quadraId,
    DateOnly data,
    int? duracaoMinutos,
    TipoCliente? tipoCliente,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var horarios = await servico.ConsultarDisponibilidadeAsync(
        quadraId,
        data,
        duracaoMinutos ?? configuracaoReservas.DuracaoMinimaMinutos,
        tipoCliente ?? TipoCliente.Padrao,
        cancellationToken);
    return Results.Ok(horarios);
});

api.MapPost("/reservas", async (
    CriarReservaRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var tipoCliente = request.TipoCliente
        ?? throw new ValidacaoException("O tipoCliente é obrigatório.");
    var reserva = await servico.CriarReservaAsync(
        request.ClienteId,
        request.QuadraId,
        tipoCliente,
        request.Inicio,
        request.Fim,
        cancellationToken);
    return Results.Created($"/api/reservas/{reserva.Id}", reserva);
});

api.MapGet("/reservas/{id:int}", async (
    int id,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ObterReservaAsync(id, cancellationToken)));

api.MapGet("/reservas", async (
    DateOnly? data,
    int? clienteId,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ListarReservasAsync(data, clienteId, cancellationToken)));

api.MapPost("/reservas/{id:int}/cancelamento", async (
    int id,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.CancelarReservaAsync(id, cancellationToken)));

app.Run();

public partial class Program { }
