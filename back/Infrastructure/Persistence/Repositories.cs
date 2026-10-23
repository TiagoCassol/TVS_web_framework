using BeachTennis.Application.Errors;
using BeachTennis.Application.Interfaces;
using BeachTennis.Domain.Entities;
using BeachTennis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeachAula4.Data;

public sealed class ClienteRepository(AppDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        db.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken) =>
        db.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.CPF == cpf, cancellationToken);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Clientes.AsNoTracking().OrderBy(x => x.Nome).ToArrayAsync(cancellationToken);

    public Task<bool> CpfExisteAsync(string cpf, CancellationToken cancellationToken) =>
        db.Clientes.AnyAsync(x => x.CPF == cpf, cancellationToken);

    public Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken)
    {
        var normalizado = email.Trim().ToLowerInvariant();
        return db.Clientes.AnyAsync(x => x.Email == normalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        await db.Clientes.AddAsync(cliente, cancellationToken);
    }
}

public sealed class QuadraRepository(AppDbContext db) : IQuadraRepository
{
    public Task<Quadra?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        db.Quadras.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Quadra>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Quadras.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(cancellationToken);

    public async Task AdicionarAsync(Quadra quadra, CancellationToken cancellationToken)
    {
        await db.Quadras.AddAsync(quadra, cancellationToken);
    }
}

public sealed class ReservaRepository(AppDbContext db) : IReservaRepository
{
    public Task<bool> ExisteConflitoAsync(
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken) =>
        db.Reservas.AsNoTracking().AnyAsync(r =>
            r.QuadraId == quadraId &&
            r.Status == StatusReserva.Confirmada &&
            inicio < r.Fim &&
            fim > r.Inicio,
            cancellationToken);

    public Task<Reserva?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        db.Reservas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reserva>> ListarPorClienteAsync(
        int clienteId,
        CancellationToken cancellationToken) =>
        await db.Reservas.AsNoTracking()
            .Where(x => x.ClienteId == clienteId)
            .OrderByDescending(x => x.Inicio)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Reserva>> ListarPorQuadraEPeriodoAsync(
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken) =>
        await db.Reservas.AsNoTracking()
            .Where(r =>
                r.QuadraId == quadraId &&
                r.Status == StatusReserva.Confirmada &&
                inicio < r.Fim &&
                fim > r.Inicio)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Reserva>> ListarAsync(
        DateTimeOffset? inicio,
        DateTimeOffset? fim,
        int? clienteId,
        CancellationToken cancellationToken)
    {
        var consulta = db.Reservas.AsNoTracking().AsQueryable();
        if (inicio.HasValue)
            consulta = consulta.Where(r => r.Inicio >= inicio.Value);
        if (fim.HasValue)
            consulta = consulta.Where(r => r.Inicio < fim.Value);
        if (clienteId.HasValue)
            consulta = consulta.Where(r => r.ClienteId == clienteId.Value);

        return await consulta.OrderBy(r => r.Inicio).ToArrayAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Reserva reserva, CancellationToken cancellationToken)
    {
        await db.Reservas.AddAsync(reserva, cancellationToken);
    }
}
