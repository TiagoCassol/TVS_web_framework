using BeachAula4.Data;
using BeachAula4.Dtos;
using BeachAula4.Entidades;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();
app.UseHttpsRedirection();

app.MapPost("/api/quadras", (Quadra quadra) =>
{

    using var db = new AppDbContext();

    db.Quadras.Add(quadra);
    db.SaveChanges();

    return Results.Created("/api/quadras/" + quadra.Id, quadra);
});

app.MapGet("/api/quadras", () =>
{
    using var db = new AppDbContext();

    return db.Quadras.ToList();
});

app.MapPost("/api/reservas", (CriarReservaDto dto) =>
{
    if (dto.Inicio <= dto.Fim)
        return Results.BadRequest("Horário Inválido");

    decimal valor = 100;

    if (dto.TipoCliente == 1)
        valor *= 0.8m;
    else if (dto.TipoCliente == 2)
        valor *= 0.7m;

    var reserva = new Reserva(dto.ClienteId, dto.QuadraId, dto.Inicio, dto.Fim);

    return Results.Ok(reserva);
});

// Configure the HTTP request pipeline.

app.Run();

