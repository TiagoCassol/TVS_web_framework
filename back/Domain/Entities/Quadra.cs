using BeachTennis.Domain.Validation;

namespace BeachTennis.Domain.Entities;

public sealed class Quadra
{
    private Quadra() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; } = true;

    public static Quadra Criar(string nome) =>
        new() { Nome = NormalizadorEntrada.NormalizarNomeQuadra(nome) };
}
