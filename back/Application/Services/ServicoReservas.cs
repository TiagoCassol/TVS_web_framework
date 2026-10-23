using BeachTennis.Application.Contracts;
using BeachTennis.Application.Errors;
using BeachTennis.Application.Interfaces;
using BeachTennis.Domain.Entities;
using BeachTennis.Domain.Enums;
using BeachTennis.Domain.Validation;

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
        string nome,
        string cpf,
        string fone,
        string? email,
        CancellationToken cancellationToken)
    {
        Cliente cliente;
        try
        {
            cliente = Cliente.Criar(nome, cpf, fone, email);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        if (await clientes.CpfExisteAsync(cliente.CPF!, cancellationToken))
            throw new ConflitoException("Já existe um cliente cadastrado com este CPF.");
        if (cliente.Email.Length > 0 && await clientes.EmailExisteAsync(cliente.Email, cancellationToken))
            throw new ConflitoException("Já existe um cliente cadastrado com este e-mail.");

        await clientes.AdicionarAsync(cliente, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(cliente);
    }

    public async Task<IReadOnlyList<ClienteDto>> ListarClientesAsync(
        string? cpf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            var todos = await clientes.ListarAsync(cancellationToken);
            return todos.Select(ToDto).ToArray();
        }

        string cpfNormalizado;
        try
        {
            cpfNormalizado = NormalizadorEntrada.NormalizarCpf(cpf);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        var cliente = await clientes.ObterPorCpfAsync(cpfNormalizado, cancellationToken);
        return cliente is null ? [] : [ToDto(cliente)];
    }

    public async Task<ClienteDto> ObterClienteAsync(int id, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        return ToDto(cliente);
    }

    public async Task<QuadraDto> RegistrarQuadraAsync(string nome, CancellationToken cancellationToken)
    {
        Quadra quadra;
        try
        {
            quadra = Quadra.Criar(nome);
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
        TipoCliente tipoCliente,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken)
    {
        if (clienteId <= 0)
            throw new ValidacaoException("O clienteId deve ser maior que zero.");
        if (quadraId <= 0)
            throw new ValidacaoException("O quadraId deve ser maior que zero.");
        ValidarTipoCliente(tipoCliente);
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

        var valores = calculadora.Calcular(fimLocal - inicioLocal, tipoCliente);
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
        ValidarTipoCliente(tipoCliente);
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

    public async Task<IReadOnlyList<ReservaDto>> ListarReservasAsync(
        DateOnly? data,
        int? clienteId,
        CancellationToken cancellationToken)
    {
        if (data is null && clienteId is null)
            throw new ValidacaoException("Informe a data ou o clienteId para consultar reservas.");
        if (data == DateOnly.MaxValue)
            throw new ValidacaoException("A data informada está fora do intervalo permitido.");
        if (clienteId is <= 0)
            throw new ValidacaoException("O clienteId deve ser maior que zero.");
        if (clienteId.HasValue &&
            await clientes.ObterPorIdAsync(clienteId.Value, cancellationToken) is null)
        {
            throw new NaoEncontradoException($"Cliente {clienteId} não encontrado.");
        }

        DateTimeOffset? inicio = null;
        DateTimeOffset? fim = null;
        if (data is { } dataReserva)
        {
            inicio = CriarHorarioLocal(dataReserva, TimeOnly.MinValue);
            fim = CriarHorarioLocal(dataReserva.AddDays(1), TimeOnly.MinValue);
        }

        var itens = await reservas.ListarAsync(inicio, fim, clienteId, cancellationToken);
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

    private static void ValidarTipoCliente(TipoCliente tipoCliente)
    {
        if (!Enum.IsDefined(tipoCliente))
            throw new ValidacaoException("O tipo de cliente é inválido.");
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
        new(
            cliente.Id,
            cliente.Nome,
            cliente.CPF ?? string.Empty,
            cliente.Fone,
            cliente.Email,
            cliente.Ativo);

    private static QuadraDto ToDto(Quadra quadra) =>
        new(quadra.Id, quadra.Nome, quadra.Ativa);

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
