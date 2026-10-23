using BeachTennis.Domain.Validation;

namespace BeachTennis.Domain.Entities;

public sealed class Cliente
{
    private Cliente() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Fone { get; private set; } = string.Empty;
    public string? CPF { get; private set; }
    public bool Ativo { get; private set; } = true;

    public static Cliente Criar(string nome, string cpf, string fone, string? email)
    {
        return new Cliente
        {
            Nome = NormalizadorEntrada.NormalizarNome(nome, "nome"),
            Email = NormalizadorEntrada.NormalizarEmail(email),
            Fone = NormalizadorEntrada.NormalizarTelefone(fone),
            CPF = NormalizadorEntrada.NormalizarCpf(cpf)
        };
    }
}
