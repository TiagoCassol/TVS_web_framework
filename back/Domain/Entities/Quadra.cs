namespace BeachTennis.Domain.Entities;

public sealed class Quadra
{
    private Quadra() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool Ativa { get; private set; } = true;

    public static Quadra Criar(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da quadra é obrigatório.", nameof(name));

        return new Quadra { Name = name.Trim() };
    }
}
