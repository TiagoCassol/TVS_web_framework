namespace BeachTennis.Application.Errors;

public abstract class AplicacaoException(string message) : Exception(message)
{
}

public sealed class ValidacaoException(string message) : AplicacaoException(message)
{
}

public sealed class NaoEncontradoException(string message) : AplicacaoException(message)
{
}

public sealed class ConflitoException(string message) : AplicacaoException(message)
{
}
