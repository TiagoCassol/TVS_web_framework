using BeachTennis.Application.Interfaces;
using BeachTennis.Application.Errors;
using BeachTennis.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BeachAula4.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Quadra> Quadras => Set<Quadra>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Tipo).HasConversion<int>().IsRequired();
            entity.Property(x => x.Ativo).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Quadra>(entity =>
        {
            entity.ToTable("Quadras");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Ativa).IsRequired();
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reservas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Inicio).HasConversion(dateTimeOffsetConverter).IsRequired();
            entity.Property(x => x.Fim).HasConversion(dateTimeOffsetConverter).IsRequired();
            entity.Property(x => x.CriadaEm).HasConversion(dateTimeOffsetConverter).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.ValorBruto).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Desconto).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.ValorFinal).HasPrecision(10, 2).IsRequired();
            entity.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Quadra>().WithMany().HasForeignKey(x => x.QuadraId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.QuadraId, x.Inicio });
            entity.HasIndex(x => new { x.ClienteId, x.Inicio });
        });

    }

    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException sqliteException &&
            sqliteException.SqliteErrorCode == 19)
        {
            if (sqliteException.Message.Contains("Clientes.Email", StringComparison.Ordinal))
                throw new ConflitoException("Já existe um cliente cadastrado com este e-mail.");
            if (sqliteException.Message.Contains("BOOKING_TIME_CONFLICT", StringComparison.Ordinal))
                throw new ConflitoException("A quadra já possui uma reserva neste intervalo.");

            throw;
        }
    }
}
