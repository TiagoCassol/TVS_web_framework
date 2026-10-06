using BeachAula4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BeachAula4.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity("BeachTennis.Domain.Entities.Cliente", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<bool>("Ativo").HasColumnType("INTEGER");
            b.Property<string>("Email").IsRequired().HasMaxLength(254).HasColumnType("TEXT");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.Property<string>("Phone").IsRequired().HasMaxLength(32).HasColumnType("TEXT");
            b.Property<int>("Tipo").HasColumnType("INTEGER");
            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("Clientes");
        });

        modelBuilder.Entity("BeachTennis.Domain.Entities.Quadra", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<bool>("Ativa").HasColumnType("INTEGER");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("Quadras");
        });

        modelBuilder.Entity("BeachTennis.Domain.Entities.Reserva", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<int>("ClienteId").HasColumnType("INTEGER");
            b.Property<long>("CriadaEm").HasColumnType("INTEGER");
            b.Property<long>("Fim").HasColumnType("INTEGER");
            b.Property<long>("Inicio").HasColumnType("INTEGER");
            b.Property<int>("QuadraId").HasColumnType("INTEGER");
            b.Property<int>("Status").HasColumnType("INTEGER");
            b.Property<decimal>("Desconto").HasPrecision(10, 2).HasColumnType("TEXT");
            b.Property<decimal>("ValorBruto").HasPrecision(10, 2).HasColumnType("TEXT");
            b.Property<decimal>("ValorFinal").HasPrecision(10, 2).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("ClienteId", "Inicio");
            b.HasIndex("QuadraId", "Inicio");
            b.ToTable("Reservas");
        });

        modelBuilder.Entity("BeachTennis.Domain.Entities.Reserva", b =>
        {
            b.HasOne("BeachTennis.Domain.Entities.Cliente", null).WithMany().HasForeignKey("ClienteId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("BeachTennis.Domain.Entities.Quadra", null).WithMany().HasForeignKey("QuadraId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
        });

#pragma warning restore 612, 618
    }
}
