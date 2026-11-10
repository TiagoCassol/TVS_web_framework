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

        if (await clientes.CpfExisteAsync(cliente.CPF!, null, cancellationToken))
            throw new ConflitoException("Já existe um cliente cadastrado com este CPF.");
        if (cliente.Email.Length > 0 &&
            await clientes.EmailExisteAsync(cliente.Email, null, cancellationToken))
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
        ValidarId(id, "clienteId");
        var cliente = await clientes.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        return ToDto(cliente);
    }

    public async Task<ClienteDto> AtualizarClienteAsync(
        int id,
        string? nome,
        string? cpf,
        string? fone,
        string? email,
        bool? ativo,
        CancellationToken cancellationToken)
    {
        ValidarId(id, "clienteId");
        if (ativo is null)
            throw new ValidacaoException("O campo ativo é obrigatório.");

        var cliente = await clientes.ObterParaAtualizarAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        Cliente dadosAtualizados;
        try
        {
            dadosAtualizados = Cliente.Criar(nome!, cpf!, fone!, email);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        if (await clientes.CpfExisteAsync(dadosAtualizados.CPF!, id, cancellationToken))
            throw new ConflitoException("Já existe um cliente cadastrado com este CPF.");
        if (dadosAtualizados.Email.Length > 0 &&
            await clientes.EmailExisteAsync(dadosAtualizados.Email, id, cancellationToken))
        {
            throw new ConflitoException("Já existe um cliente cadastrado com este e-mail.");
        }

        try
        {
            cliente.Atualizar(
                dadosAtualizados.Nome,
                dadosAtualizados.CPF,
                dadosAtualizados.Fone,
                dadosAtualizados.Email,
                ativo.Value);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(cliente);
    }

    public async Task DesativarClienteAsync(int id, CancellationToken cancellationToken)
    {
        ValidarId(id, "clienteId");
        var cliente = await clientes.ObterParaAtualizarAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        cliente.Desativar();
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
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

    public async Task<QuadraDto> ObterQuadraAsync(int id, CancellationToken cancellationToken)
    {
        ValidarId(id, "quadraId");
        var quadra = await quadras.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Quadra {id} não encontrada.");
        return ToDto(quadra);
    }

    public async Task<QuadraDto> AtualizarQuadraAsync(
        int id,
        string? nome,
        bool? ativa,
        CancellationToken cancellationToken)
    {
        ValidarId(id, "quadraId");
        if (ativa is null)
            throw new ValidacaoException("O campo ativa é obrigatório.");
        var quadra = await quadras.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Quadra {id} não encontrada.");

        try
        {
            quadra.Atualizar(nome, ativa.Value);
        }
        catch (ArgumentException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(quadra);
    }

    public async Task DesativarQuadraAsync(int id, CancellationToken cancellationToken)
    {
        ValidarId(id, "quadraId");
        var quadra = await quadras.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Quadra {id} não encontrada.");
        quadra.Desativar();
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<ReservaDto> CriarReservaAsync(
        int clienteId,
        int quadraId,
        TipoCliente tipoCliente,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken)
    {
        var dados = await ValidarDadosReservaAsync(
            clienteId,
            quadraId,
            tipoCliente,
            inicio,
            fim,
            cancellationToken);
        var valores = calculadora.Calcular(dados.Fim - dados.Inicio, tipoCliente);
        var criadaEm = timeProvider.GetUtcNow();
        var reserva = Reserva.Criar(
            clienteId,
            quadraId,
            dados.Inicio,
            dados.Fim,
            valores.ValorBruto,
            valores.Desconto,
            criadaEm);
        await reservas.AdicionarAsync(reserva, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        return ToDto(reserva);
    }

    public async Task<ReservaDto> AtualizarReservaAsync(
        int id,
        int? clienteId,
        int? quadraId,
        TipoCliente? tipoCliente,
        DateTimeOffset? inicio,
        DateTimeOffset? fim,
        CancellationToken cancellationToken)
    {
        ValidarId(id, "reservaId");
        var reserva = await reservas.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Reserva {id} não encontrada.");
        var agora = timeProvider.GetUtcNow();
        if (reserva.Status != StatusReserva.Confirmada || reserva.Inicio <= agora)
            throw new ValidacaoException("Somente reservas futuras confirmadas podem ser alteradas.");
        if (clienteId is null || quadraId is null || tipoCliente is null ||
            inicio is null || fim is null)
        {
            throw new ValidacaoException(
                "clienteId, quadraId, tipoCliente, inicio e fim são obrigatórios para atualizar a reserva.");
        }

        var dados = await ValidarDadosReservaAsync(
            clienteId.Value,
            quadraId.Value,
            tipoCliente.Value,
            inicio.Value,
            fim.Value,
            cancellationToken,
            id);
        var valores = calculadora.Calcular(dados.Fim - dados.Inicio, tipoCliente.Value);
        try
        {
            reserva.Atualizar(
                clienteId.Value,
                quadraId.Value,
                dados.Inicio,
                dados.Fim,
                valores.ValorBruto,
                valores.Desconto,
                agora);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

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
        ValidarId(id, "reservaId");
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
        ValidarId(id, "reservaId");
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

    public async Task ExcluirReservaAsync(int id, CancellationToken cancellationToken)
    {
        ValidarId(id, "reservaId");
        var reserva = await reservas.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Reserva {id} não encontrada.");
        if (reserva.Status == StatusReserva.Cancelada)
            return;

        try
        {
            reserva.Cancelar(timeProvider.GetUtcNow());
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidacaoException(exception.Message);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<(DateTimeOffset Inicio, DateTimeOffset Fim)> ValidarDadosReservaAsync(
        int clienteId,
        int quadraId,
        TipoCliente tipoCliente,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken cancellationToken,
        int? ignorarReservaId = null)
    {
        ValidarId(clienteId, "clienteId");
        ValidarId(quadraId, "quadraId");
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

        if (await reservas.ExisteConflitoAsync(
                quadraId,
                inicioLocal,
                fimLocal,
                cancellationToken,
                ignorarReservaId))
        {
            throw new ConflitoException("A quadra já possui uma reserva neste intervalo.");
        }

        return (inicioLocal, fimLocal);
    }

    private static void ValidarId(int id, string campo)
    {
        if (id <= 0)
            throw new ValidacaoException($"O campo {campo} deve ser maior que zero.");
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
