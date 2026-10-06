using BeachTennis.Application.Contracts;
using BeachTennis.Application.Errors;
using BeachTennis.Application.Interfaces;
using BeachTennis.Domain.Entities;
using BeachTennis.Domain.Enums;

namespace BeachTennis.Application.Services;

public sealed class ServicoReservas(
    IClienteRepository clientes,
    IQuadraRepository quadras,
    IReservaRepository reservas,
    IHorarioFuncionamento horarios,
    IUnitOfWork unitOfWork,
    CalculadoraPreco calculadora,
    TimeProvider timeProvider)
{
    private readonly TimeZoneInfo _fusoHorario =
        TimeZoneInfo.FindSystemTimeZoneById(calculadora.Configuracao.FusoHorario);

    public async Task<ClienteDto> RegistrarClienteAsync(
        string name,
        string email,
        string phone,
        TipoCliente tipo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidacaoException("O e-mail do cliente é obrigatório.");
        if (await clientes.EmailExisteAsync(email.Trim(), cancellationToken))
            throw new ConflitoException("Já existe um cliente cadastrado com este e-mail.");

        Cliente cliente;
        try
        {
            cliente = Cliente.Criar(name, email, phone, tipo);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await clientes.AdicionarAsync(cliente, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(cliente);
    }

    public async Task<ClienteDto> ObterClienteAsync(int id, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        return ToDto(cliente);
    }

    public async Task<QuadraDto> RegistrarQuadraAsync(string name, CancellationToken cancellationToken)
    {
        Quadra quadra;
        try
        {
            quadra = Quadra.Criar(name);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await quadras.AdicionarAsync(quadra, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(quadra);
    }

    public async Task<IReadOnlyList<QuadraDto>> ListarQuadrasAsync(CancellationToken cancellationToken)
    {
        var existentes = await quadras.ListarAsync(cancellationToken);
        return existentes.Select(ToDto).ToArray();
    }

    public async Task<ReservaDto> CriarReservaAsync(
        int clienteId,
        int quadraId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(clienteId, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {clienteId} não encontrado.");
        if (!cliente.Ativo)
            throw new ValidacaoException("O cliente está inativo.");

        var quadra = await quadras.ObterPorIdAsync(quadraId, cancellationToken)
            ?? throw new NaoEncontradoException($"Quadra {quadraId} não encontrada.");
        if (!quadra.Ativa)
            throw new ValidacaoException("A quadra está inativa.");

        var inicioLocal = TimeZoneInfo.ConvertTime(inicio, _fusoHorario);
        var fimLocal = TimeZoneInfo.ConvertTime(fim, _fusoHorario);
        if (fimLocal <= inicioLocal)
            throw new ValidacaoException("O horário final deve ser posterior ao horário inicial.");
        if (inicioLocal <= TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _fusoHorario))
            throw new ValidacaoException("A reserva deve começar no futuro.");
        if (inicioLocal.Date != fimLocal.Date)
            throw new ValidacaoException("A reserva deve começar e terminar no mesmo dia local.");

        ValidarDuracaoEGrade(inicioLocal, fimLocal);
        ValidarFuncionamento(inicioLocal, fimLocal);

        if (await reservas.ExisteConflitoAsync(quadraId, inicioLocal, fimLocal, cancellationToken))
            throw new ConflitoException("A quadra já possui uma reserva neste intervalo.");

        var valores = calculadora.Calcular(fimLocal - inicioLocal, cliente.Tipo);
        var criadaEm = timeProvider.GetUtcNow();
        var reserva = Reserva.Criar(
            clienteId,
            quadraId,
            inicioLocal,
            fimLocal,
            valores.ValorBruto,
            valores.Desconto,
            criadaEm);
        await reservas.AdicionarAsync(reserva, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(reserva);
    }

    public async Task<IReadOnlyList<HorarioDisponivelDto>> ConsultarDisponibilidadeAsync(
        int quadraId,
        DateOnly data,
        int duracaoMinutos,
        TipoCliente tipoCliente,
        CancellationToken cancellationToken)
    {
        var quadra = await quadras.ObterPorIdAsync(quadraId, cancellationToken)
            ?? throw new NaoEncontradoException($"Quadra {quadraId} não encontrada.");
        if (!quadra.Ativa)
            throw new ValidacaoException("A quadra está inativa.");

        var hojeLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _fusoHorario).DateTime);
        if (data < hojeLocal)
            throw new ValidacaoException("Não é possível consultar disponibilidade para uma data passada.");

        ValidarDuracao(duracaoMinutos);
        if (!horarios.TentarObter(data.DayOfWeek, out var horario))
            return [];
        if (horario.Fechamento <= horario.Abertura)
            throw new InvalidOperationException($"O horário de funcionamento de {data.DayOfWeek} é inválido.");

        var inicioLocal = CriarHorarioLocal(data, horario.Abertura);
        var fimLocal = CriarHorarioLocal(data, horario.Fechamento);
        var agoraLocal = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _fusoHorario);
        var existentes = await reservas.ListarPorQuadraEPeriodoAsync(
            quadraId,
            inicioLocal,
            fimLocal,
            cancellationToken);
        var resultados = new List<HorarioDisponivelDto>();
        var passo = TimeSpan.FromMinutes(calculadora.Configuracao.PassoMinutos);
        var duracao = TimeSpan.FromMinutes(duracaoMinutos);

        for (var slotInicio = inicioLocal; slotInicio + duracao <= fimLocal; slotInicio += passo)
        {
            var slotFim = slotInicio + duracao;
            if (slotInicio <= agoraLocal)
                continue;

            var disponivel = !existentes.Any(r =>
                r.Status == StatusReserva.Confirmada &&
                slotInicio < r.Fim &&
                slotFim > r.Inicio);
            var valores = calculadora.Calcular(duracao, tipoCliente);
            resultados.Add(new HorarioDisponivelDto(
                slotInicio,
                slotFim,
                disponivel,
                valores.ValorBruto,
                valores.Desconto,
                valores.ValorFinal));
        }

        return resultados;
    }

    public async Task<ReservaDto> ObterReservaAsync(int id, CancellationToken cancellationToken)
    {
        var reserva = await reservas.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Reserva {id} não encontrada.");
        return ToDto(reserva);
    }

    public async Task<IReadOnlyList<ReservaDto>> ListarReservasDoClienteAsync(
        int clienteId,
        CancellationToken cancellationToken)
    {
        _ = await clientes.ObterPorIdAsync(clienteId, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {clienteId} não encontrado.");
        var itens = await reservas.ListarPorClienteAsync(clienteId, cancellationToken);
        return itens.Select(ToDto).ToArray();
    }

    public async Task<ReservaDto> CancelarReservaAsync(int id, CancellationToken cancellationToken)
    {
        var reserva = await reservas.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Reserva {id} não encontrada.");
        try
        {
            reserva.Cancelar(timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(reserva);
    }

    private void ValidarFuncionamento(DateTimeOffset inicioLocal, DateTimeOffset fimLocal)
    {
        if (!horarios.TentarObter(inicioLocal.DayOfWeek, out var horario))
            throw new ValidacaoException("O clube não funciona neste dia.");

        var abertura = CriarHorarioLocal(DateOnly.FromDateTime(inicioLocal.DateTime), horario.Abertura);
        var fechamento = CriarHorarioLocal(DateOnly.FromDateTime(inicioLocal.DateTime), horario.Fechamento);
        if (inicioLocal < abertura || fimLocal > fechamento)
            throw new ValidacaoException("A reserva está fora do horário de funcionamento.");
    }

    private void ValidarDuracaoEGrade(DateTimeOffset inicioLocal, DateTimeOffset fimLocal)
    {
        var duracao = fimLocal - inicioLocal;
        ValidarDuracao((int)duracao.TotalMinutes);
        if (duracao.TotalMinutes % calculadora.Configuracao.PassoMinutos != 0)
            throw new ValidacaoException($"A duração deve ser múltipla de {calculadora.Configuracao.PassoMinutos} minutos.");

        if (!horarios.TentarObter(inicioLocal.DayOfWeek, out var horario))
            throw new ValidacaoException("O clube não funciona neste dia.");
        var abertura = horario.Abertura.ToTimeSpan();
        var desdeAbertura = inicioLocal.TimeOfDay - abertura;
        if (desdeAbertura < TimeSpan.Zero ||
            desdeAbertura.Ticks % TimeSpan.FromMinutes(calculadora.Configuracao.PassoMinutos).Ticks != 0)
        {
            throw new ValidacaoException(
                $"O início da reserva deve seguir intervalos de {calculadora.Configuracao.PassoMinutos} minutos a partir da abertura.");
        }
    }

    private void ValidarDuracao(int duracaoMinutos)
    {
        var config = calculadora.Configuracao;
        if (duracaoMinutos < config.DuracaoMinimaMinutos ||
            duracaoMinutos > config.DuracaoMaximaMinutos ||
            duracaoMinutos % config.PassoMinutos != 0)
        {
            throw new ValidacaoException(
                $"A duração deve ser entre {config.DuracaoMinimaMinutos} e {config.DuracaoMaximaMinutos} minutos, em intervalos de {config.PassoMinutos} minutos.");
        }
    }

    private DateTimeOffset CriarHorarioLocal(DateOnly data, TimeOnly horario)
    {
        var dataHoraLocal = DateTime.SpecifyKind(data.ToDateTime(horario), DateTimeKind.Unspecified);
        if (_fusoHorario.IsInvalidTime(dataHoraLocal))
            throw new ValidacaoException("O horário informado não existe devido à mudança de fuso horário.");

        var offset = _fusoHorario.IsAmbiguousTime(dataHoraLocal)
            ? _fusoHorario.GetAmbiguousTimeOffsets(dataHoraLocal).Min()
            : _fusoHorario.GetUtcOffset(dataHoraLocal);
        return new DateTimeOffset(dataHoraLocal, offset);
    }

    private static ClienteDto ToDto(Cliente cliente) =>
        new(cliente.Id, cliente.Name, cliente.Email, cliente.Phone, cliente.Tipo, cliente.Ativo);

    private static QuadraDto ToDto(Quadra quadra) =>
        new(quadra.Id, quadra.Name, quadra.Ativa);

    private static ReservaDto ToDto(Reserva reserva) =>
        new(
            reserva.Id,
            reserva.ClienteId,
            reserva.QuadraId,
            reserva.Inicio,
            reserva.Fim,
            reserva.Status,
            reserva.ValorBruto,
            reserva.Desconto,
            reserva.ValorFinal,
            reserva.CriadaEm);
}
