using BeachTennis.Application.Contracts;
using BeachTennis.Application.Errors;
using BeachTennis.Domain.Enums;

namespace BeachTennis.Application.Services;

public sealed class CalculadoraPreco
{
    private readonly ConfiguracaoReservas _config;

    public CalculadoraPreco(ConfiguracaoReservas config)
    {
        if (config.PrecoPorHora < 0 ||
            config.DuracaoMinimaMinutos <= 0 ||
            config.DuracaoMaximaMinutos < config.DuracaoMinimaMinutos ||
            config.PassoMinutos <= 0 ||
            config.DescontoAssociado is < 0 or > 1 ||
            config.DescontoProfessor is < 0 or > 1)
        {
            throw new InvalidOperationException("A configuração de reservas contém valores inválidos.");
        }

        _config = config;
    }

    public (decimal ValorBruto, decimal Desconto, decimal ValorFinal) Calcular(
        TimeSpan duracao,
        TipoCliente tipoCliente)
    {
        var minutos = (decimal)duracao.TotalMinutes;
        if (minutos <= 0)
            throw new ValidacaoException("A duração da reserva deve ser maior que zero.");

        var valorBruto = decimal.Round(
            _config.PrecoPorHora * minutos / 60m,
            2,
            MidpointRounding.AwayFromZero);
        var percentualDesconto = tipoCliente switch
        {
            TipoCliente.Associado => _config.DescontoAssociado,
            TipoCliente.Professor => _config.DescontoProfessor,
            TipoCliente.Avulso => 0m,
            _ => throw new ValidacaoException("O tipo de cliente é inválido.")
        };
        var desconto = decimal.Round(
            valorBruto * percentualDesconto,
            2,
            MidpointRounding.AwayFromZero);

        return (valorBruto, desconto, valorBruto - desconto);
    }

    public ConfiguracaoReservas Configuracao => _config;
}
