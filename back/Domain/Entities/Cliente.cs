using BeachTennis.Domain.Enums;
using System.Net.Mail;

namespace BeachTennis.Domain.Entities;

public sealed class Cliente
{
    private Cliente() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public TipoCliente Tipo { get; private set; }
    public bool Ativo { get; private set; } = true;

    public static Cliente Criar(string name, string email, string phone, TipoCliente tipo)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
            throw new ArgumentException("Informe um e-mail válido.", nameof(email));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("O telefone do cliente é obrigatório.", nameof(phone));
        var quantidadeDigitosTelefone = phone.Count(char.IsDigit);
        if (quantidadeDigitosTelefone is < 7 or > 15)
            throw new ArgumentException("O telefone deve conter entre 7 e 15 dígitos.", nameof(phone));
        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("O tipo de cliente é inválido.", nameof(tipo));

        return new Cliente
        {
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone.Trim(),
            Tipo = tipo
        };
    }
}
