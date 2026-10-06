using BeachTennis.Domain.Enums;

namespace BeachTennis.Domain.Entities;

public sealed class Reserva
{
    private Reserva() { }

    public int Id { get; private set; }
    public int QuadraId { get; private set; }
    public int ClienteId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public StatusReserva Status { get; private set; }
    public decimal ValorBruto { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal ValorFinal { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }

    public static Reserva Criar(
        int clienteId,
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        decimal valorBruto,
        decimal desconto,
        DateTimeOffset criadaEm)
    {
        if (clienteId <= 0)
            throw new ArgumentException("O cliente informado é inválido.", nameof(clienteId));
        if (quadraId <= 0)
            throw new ArgumentException("A quadra informada é inválida.", nameof(quadraId));
        if (fim <= inicio)
            throw new ArgumentException("O horário final deve ser posterior ao horário inicial.");
        if (valorBruto < 0 || desconto < 0 || desconto > valorBruto)
            throw new ArgumentException("Os valores da reserva são inválidos.");

        return new Reserva
        {
            ClienteId = clienteId,
            QuadraId = quadraId,
            Inicio = inicio,
            Fim = fim,
            Status = StatusReserva.Confirmada,
            ValorBruto = decimal.Round(valorBruto, 2, MidpointRounding.AwayFromZero),
            Desconto = decimal.Round(desconto, 2, MidpointRounding.AwayFromZero),
            ValorFinal = decimal.Round(valorBruto - desconto, 2, MidpointRounding.AwayFromZero),
            CriadaEm = criadaEm
        };
    }

    public void Cancelar(DateTimeOffset agora)
    {
        if (Status != StatusReserva.Confirmada)
            throw new InvalidOperationException("Somente reservas confirmadas podem ser canceladas.");
        if (Inicio <= agora)
            throw new InvalidOperationException("Reservas que já começaram não podem ser canceladas.");

        Status = StatusReserva.Cancelada;
    }
}
