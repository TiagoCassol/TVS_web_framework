using BeachTennis.Application.Contracts;
using BeachTennis.Application.Interfaces;

namespace BeachAula4.Api;

public sealed class HorarioFuncionamento : IHorarioFuncionamento
{
    private readonly IReadOnlyDictionary<DayOfWeek, HorarioDia> _dias;

    private HorarioFuncionamento(IReadOnlyDictionary<DayOfWeek, HorarioDia> dias) => _dias = dias;

    public static HorarioFuncionamento Carregar(IConfigurationSection section)
    {
        var dias = new Dictionary<DayOfWeek, HorarioDia>();
        foreach (var item in section.GetSection("Dias").GetChildren())
        {
            if (!Enum.TryParse<DayOfWeek>(item.Key, ignoreCase: true, out var dia))
                throw new InvalidOperationException($"Dia da semana inválido na configuração: {item.Key}.");

            if (!TimeOnly.TryParse(item["Abertura"], out var abertura))
                throw new InvalidOperationException($"Horário de abertura ausente ou inválido para {item.Key}.");
            if (!TimeOnly.TryParse(item["Fechamento"], out var fechamento))
                throw new InvalidOperationException($"Horário de fechamento ausente ou inválido para {item.Key}.");
            if (fechamento <= abertura)
                throw new InvalidOperationException($"O fechamento deve ser posterior à abertura em {item.Key}.");
            if (!dias.TryAdd(dia, new HorarioDia(abertura, fechamento)))
                throw new InvalidOperationException($"O dia {item.Key} está configurado mais de uma vez.");
        }

        if (dias.Count == 0)
            throw new InvalidOperationException("Configure pelo menos um dia de funcionamento.");

        return new HorarioFuncionamento(dias);
    }

    public bool TentarObter(DayOfWeek dia, out HorarioDia horario) =>
        _dias.TryGetValue(dia, out horario!);
}
