using BeachAula4.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeachAula4.Migrations;

[Migration("20261006000000_AddReservationWorkflow")]
public partial class AddReservationWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Ativa",
            table: "Quadras",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "Clientes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                Phone = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Clientes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Reservas",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                QuadraId = table.Column<int>(type: "INTEGER", nullable: false),
                ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                Inicio = table.Column<long>(type: "INTEGER", nullable: false),
                Fim = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ValorBruto = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                Desconto = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                ValorFinal = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                CriadaEm = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reservas", x => x.Id);
                table.ForeignKey("FK_Reservas_Clientes_ClienteId", x => x.ClienteId, "Clientes", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Reservas_Quadras_QuadraId", x => x.QuadraId, "Quadras", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_Clientes_Email", table: "Clientes", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Reservas_ClienteId_Inicio", table: "Reservas", columns: new[] { "ClienteId", "Inicio" });
        migrationBuilder.CreateIndex(name: "IX_Reservas_QuadraId_Inicio", table: "Reservas", columns: new[] { "QuadraId", "Inicio" });

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_Reservas_NoOverlap_Insert
            BEFORE INSERT ON Reservas
            WHEN NEW.Status = 1
            AND EXISTS (
                SELECT 1 FROM Reservas r
                WHERE r.QuadraId = NEW.QuadraId
                  AND r.Status = 1
                  AND NEW.Inicio < r.Fim
                  AND NEW.Fim > r.Inicio
            )
            BEGIN
                SELECT RAISE(ABORT, 'BOOKING_TIME_CONFLICT');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_Reservas_NoOverlap_Update
            BEFORE UPDATE OF QuadraId, Inicio, Fim, Status ON Reservas
            WHEN NEW.Status = 1
            AND EXISTS (
                SELECT 1 FROM Reservas r
                WHERE r.Id <> NEW.Id
                  AND r.QuadraId = NEW.QuadraId
                  AND r.Status = 1
                  AND NEW.Inicio < r.Fim
                  AND NEW.Fim > r.Inicio
            )
            BEGIN
                SELECT RAISE(ABORT, 'BOOKING_TIME_CONFLICT');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Reservas_NoOverlap_Insert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Reservas_NoOverlap_Update;");
        migrationBuilder.DropTable(name: "Reservas");
        migrationBuilder.DropTable(name: "Clientes");
        migrationBuilder.DropColumn(name: "Ativa", table: "Quadras");
    }
}
