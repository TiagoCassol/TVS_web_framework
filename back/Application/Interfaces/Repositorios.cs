using BeachTennis.Application.Contracts;
using BeachTennis.Domain.Entities;

namespace BeachTennis.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken);
    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
}

public interface IQuadraRepository
{
    Task<Quadra?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Quadra>> ListarAsync(CancellationToken cancellationToken);
    Task AdicionarAsync(Quadra quadra, CancellationToken cancellationToken);
}

public interface IReservaRepository
{
    Task<bool> ExisteConflitoAsync(
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken);
    Task<Reserva?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reserva>> ListarPorClienteAsync(int clienteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reserva>> ListarPorQuadraEPeriodoAsync(
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken);
    Task AdicionarAsync(Reserva reserva, CancellationToken cancellationToken);
}

public interface IHorarioFuncionamento
{
    bool TentarObter(DayOfWeek dia, out HorarioDia horario);
}

public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
