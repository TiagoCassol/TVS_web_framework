using System.Text.Json.Serialization;
using BeachAula4.Data;
using BeachAula4.Infrastructure;
using BeachAula4.Api;
using BeachTennis.Application.Contracts;
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
    reservasConfig.GetValue<decimal?>("DescontoAssociado")
        ?? throw new InvalidOperationException("Reservas:DescontoAssociado não foi configurado."),
    reservasConfig.GetValue<decimal?>("DescontoProfessor")
        ?? throw new InvalidOperationException("Reservas:DescontoProfessor não foi configurado."),
    horarioConfig["FusoHorario"]
        ?? throw new InvalidOperationException("HorarioFuncionamento:FusoHorario não foi configurado."));
_ = TimeZoneInfo.FindSystemTimeZoneById(configuracaoReservas.FusoHorario);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(configuracaoReservas);
builder.Services.AddSingleton(new CalculadoraPreco(configuracaoReservas));
builder.Services.AddSingleton<IHorarioFuncionamento>(
    HorarioFuncionamento.Carregar(builder.Configuration.GetSection("HorarioFuncionamento")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ServicoReservas>();
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

api.MapPost("/clientes", async (
    CriarClienteRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var cliente = await servico.RegistrarClienteAsync(
        request.Name,
        request.Email,
        request.Phone,
        request.Tipo,
        cancellationToken);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});

api.MapGet("/clientes/{id:int}", async (
    int id,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.ObterClienteAsync(id, cancellationToken)));

api.MapPost("/quadras", async (
    CriarQuadraRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var quadra = await servico.RegistrarQuadraAsync(request.Name, cancellationToken);
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
        tipoCliente ?? BeachTennis.Domain.Enums.TipoCliente.Avulso,
        cancellationToken);
    return Results.Ok(horarios);
});

api.MapPost("/reservas", async (
    CriarReservaRequest request,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    var reserva = await servico.CriarReservaAsync(
        request.ClienteId,
        request.QuadraId,
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
    int? clienteId,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
{
    if (clienteId is null or <= 0)
        throw new BeachTennis.Application.Errors.ValidacaoException(
            "Informe um clienteId válido para consultar as reservas.");
    return Results.Ok(await servico.ListarReservasDoClienteAsync(clienteId.Value, cancellationToken));
});

api.MapPost("/reservas/{id:int}/cancelamento", async (
    int id,
    ServicoReservas servico,
    CancellationToken cancellationToken) =>
    Results.Ok(await servico.CancelarReservaAsync(id, cancellationToken)));

app.Run();

public partial class Program { }
