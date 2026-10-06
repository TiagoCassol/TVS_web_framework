using BeachAula4.Data;
using BeachTennis.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeachAula4.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não foi configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(
            connectionString,
            sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name!)));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IQuadraRepository, QuadraRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        return services;
    }
}
