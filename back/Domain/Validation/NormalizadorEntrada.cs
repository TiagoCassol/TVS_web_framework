using System.Net.Mail;
using System.Text.RegularExpressions;

namespace BeachTennis.Domain.Validation;

public static class NormalizadorEntrada
{
    private static readonly Regex Espacos = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NomePessoa = new(
        @"^\p{L}[\p{L}\p{M}]*(?:(?:\s+\p{L}[\p{L}\p{M}]*)|(?:\s*-\s*\p{L}[\p{L}\p{M}]*)|(?:['\u2019]\p{L}[\p{L}\p{M}]*))*$",
        RegexOptions.Compiled);
    private static readonly Regex NomeQuadra = new(
        @"^[\p{L}\p{N}][\p{L}\p{M}\p{N}]*(?:(?:\s+[\p{L}\p{N}][\p{L}\p{M}\p{N}]*)|(?:\s*-\s*[\p{L}\p{N}][\p{L}\p{M}\p{N}]*)|(?:['\u2019][\p{L}\p{N}][\p{L}\p{M}\p{N}]*))*$",
        RegexOptions.Compiled);
    private static readonly Regex FormatoCpf = new(
        @"^(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})$",
        RegexOptions.Compiled);
    private static readonly Regex FormatoTelefone = new(
        @"^\+?(?:[0-9]{10,11}|[0-9]{2}[\s.-]?[0-9]{4,5}[\s.-]?[0-9]{4}|\([0-9]{2}\)[\s.-]?[0-9]{4,5}[\s.-]?[0-9]{4})$",
        RegexOptions.Compiled);

    public static string NormalizarNome(string? valor, string campo)
    {
        if (valor is { Length: > 512 })
            throw new ArgumentException($"{campo} excede o limite de entrada.", campo);
        var normalizado = NormalizarEspacos(valor);
        if (normalizado.Length == 0)
            throw new ArgumentException($"{campo} é obrigatório.", campo);
        if (normalizado.Length > 120)
            throw new ArgumentException($"{campo} não pode exceder 120 caracteres.", campo);
        if (!NomePessoa.IsMatch(normalizado))
            throw new ArgumentException(
                $"{campo} deve conter apenas letras, espaços, hífen ou apóstrofo.",
                campo);

        return normalizado;
    }

    public static string NormalizarNomeQuadra(string? valor)
    {
        const string campo = "nome";
        if (valor is { Length: > 512 })
            throw new ArgumentException("O nome da quadra excede o limite de entrada.", campo);
        var normalizado = NormalizarEspacos(valor);
        if (normalizado.Length == 0)
            throw new ArgumentException("O nome da quadra é obrigatório.", campo);
        if (normalizado.Length > 120)
            throw new ArgumentException("O nome da quadra não pode exceder 120 caracteres.", campo);
        if (!NomeQuadra.IsMatch(normalizado))
            throw new ArgumentException(
                "O nome da quadra deve conter apenas letras, números, espaços, hífen ou apóstrofo.",
                campo);

        return normalizado;
    }

    public static string NormalizarCpf(string? valor)
    {
        const string campo = "cpf";
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("O CPF é obrigatório.", campo);
        if (valor.Length > 32)
            throw new ArgumentException("O CPF excede o limite de entrada.", campo);
        var entrada = valor.Trim();
        if (!FormatoCpf.IsMatch(entrada))
            throw new ArgumentException("O CPF contém caracteres inválidos.", campo);

        var cpf = Regex.Replace(entrada, "[^0-9]", string.Empty);
        if (!CpfValido(cpf))
            throw new ArgumentException("Informe um CPF válido.", campo);

        return cpf;
    }

    public static string NormalizarTelefone(string? valor)
    {
        const string campo = "telefone";
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("O telefone do cliente é obrigatório.", campo);
        if (valor.Length > 40)
            throw new ArgumentException("O telefone excede o limite de entrada.", campo);
        var entrada = valor.Trim();
        if (!FormatoTelefone.IsMatch(entrada))
            throw new ArgumentException("O telefone contém caracteres inválidos.", campo);

        var telefone = Regex.Replace(entrada, "[^0-9]", string.Empty);
        if (telefone.Length is not (10 or 11))
            throw new ArgumentException("O telefone deve conter DDD e 10 ou 11 dígitos.", campo);

        return telefone;
    }

    public static string NormalizarEmail(string? valor)
    {
        if (valor is { Length: > 254 })
            throw new ArgumentException("O e-mail não pode exceder 254 caracteres.", "email");
        var email = valor?.Trim() ?? string.Empty;
        if (email.Length == 0)
            return string.Empty;
        if (!MailAddress.TryCreate(email, out var endereco) ||
            !string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Informe um e-mail válido com até 254 caracteres.", "email");
        }

        return email.ToLowerInvariant();
    }

    private static string NormalizarEspacos(string? valor) =>
        Espacos.Replace(valor?.Trim() ?? string.Empty, " ");

    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || Regex.IsMatch(cpf, @"^([0-9])\1{10}$"))
            return false;

        static int CalcularDigito(string valor, int tamanho)
        {
            var soma = 0;
            for (var indice = 0; indice < tamanho; indice++)
                soma += (valor[indice] - '0') * (tamanho + 1 - indice);

            var resto = (soma * 10) % 11;
            return resto == 10 ? 0 : resto;
        }

        return CalcularDigito(cpf, 9) == cpf[9] - '0' &&
               CalcularDigito(cpf, 10) == cpf[10] - '0';
    }
}
