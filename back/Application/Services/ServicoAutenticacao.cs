using BeachTennis.Application.Contracts;

namespace BeachTennis.Application.Services;

public sealed class ServicoAutenticacao(ConfiguracaoAutenticacao configuracao)
{
    public UsuarioDto? Autenticar(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return null;

        if (!string.Equals(email.Trim(), configuracao.EmailGestor, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(senha, configuracao.SenhaGestor, StringComparison.Ordinal))
        {
            return null;
        }

        return new UsuarioDto(1, configuracao.NomeGestor, configuracao.EmailGestor, "Gestor");
    }
}
