using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BeachTennis.Tests;

public sealed class ValidacaoEntradaApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = factory.CreateClient();

    [Fact]
    public async Task CadastroDeClienteNormalizaEspacosCpfTelefoneEEmail()
    {
        var cpf = CriarCpfValido();
        var cpfFormatado = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var resposta = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "  João   D'Ávila  ",
            cpf = cpfFormatado,
            telefone = "(51) 99999-0001",
            email = $"  {email.ToUpperInvariant()} "
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var cliente = documento.RootElement;
        Assert.Equal("João D'Ávila", cliente.GetProperty("nome").GetString());
        Assert.Equal(cpf, cliente.GetProperty("cpf").GetString());
        Assert.Equal("51999990001", cliente.GetProperty("telefone").GetString());
        Assert.Equal(email, cliente.GetProperty("email").GetString());
    }

    [Theory]
    [InlineData("{\"nome\":\"   \",\"cpf\":\"52998224725\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana 2\",\"cpf\":\"52998224725\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"529abc24725\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"11111111111\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"51999abc001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"123\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"((51 99999-0001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"51..99999---0001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"...52998224725\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"51999990001\",\"email\":\"Ana <ana@example.com>\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\",\"telefone\":\"51999990001\",\"email\":\"ana@@example.com\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"cpf\":\"52998224725\",\"telefone\":\"51999990001\"}")]
    [InlineData("{\"nome\":\"Ana Silva\",\"cpf\":\"52998224725\"}")]
    public async Task CadastroDeClienteRejeitaCamposObrigatoriosOuFormatosInvalidos(string payload)
    {
        var resposta = await _cliente.PostAsync(
            "/api/clientes",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task CadastroDeQuadraLimpaEspacosEPermiteNumero()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/quadras", new
        {
            nome = "  Quadra   2 - Coberta  "
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Quadra 2 - Coberta", documento.RootElement.GetProperty("nome").GetString());
    }

    [Theory]
    [InlineData("{\"nome\":\"   \"}")]
    [InlineData("{\"nome\":\"Quadra <script>\"}")]
    [InlineData("{}")]
    public async Task CadastroDeQuadraRejeitaNomeVazioOuComCaracteresInvalidos(string payload)
    {
        var resposta = await _cliente.PostAsync(
            "/api/quadras",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"nome\":12,\"cpf\":\"52998224725\",\"telefone\":\"51999990001\"}")]
    public async Task RequisicaoComJsonInvalidoRetornaBadRequest(string payload)
    {
        var resposta = await _cliente.PostAsync(
            "/api/clientes",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task NomeAcimaDoLimiteEhRejeitado()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = new string('A', 121),
            cpf = CriarCpfValido(),
            telefone = "51999990001",
            email = $"campo-{Guid.NewGuid():N}@example.test"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task ConsultaDeClienteRejeitaCpfComCaracteresInvalidos()
    {
        var resposta = await _cliente.GetAsync("/api/clientes?cpf=529abc24725");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task CadastroDeClienteSemEmailMantemCampoOpcionalVazio()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Maria Silva",
            cpf = CriarCpfValido(),
            telefone = "51999990001"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(string.Empty, documento.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CadastroDeClienteRejeitaEmailDuplicadoSemDiferenciarMaiusculas()
    {
        var email = $"unico-{Guid.NewGuid():N}@example.test";
        var primeiro = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Ana Silva",
            cpf = CriarCpfValido(),
            telefone = "51999990001",
            email
        });
        var segundo = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Bia Silva",
            cpf = CriarCpfValido(),
            telefone = "51999990002",
            email = email.ToUpperInvariant()
        });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task CadastroDeClienteRejeitaCpfDuplicadoMesmoFormatadoDiferente()
    {
        var cpf = CriarCpfValido();
        var primeiro = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Ana Silva",
            cpf,
            telefone = "51999990001",
            email = $"cpf-a-{Guid.NewGuid():N}@example.test"
        });
        var segundo = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Bia Silva",
            cpf = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}",
            telefone = "51999990002",
            email = $"cpf-b-{Guid.NewGuid():N}@example.test"
        });

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task LoginRejeitaCredenciaisEmBranco()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login", new
        {
            email = "   ",
            senha = "   "
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task LoginDeDemonstracaoRetornaUsuarioEsperado()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login", new
        {
            email = "gestor@beachtennis.local",
            senha = "demo1234"
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("Gestor", documento.RootElement.GetProperty("perfil").GetString());
    }

    [Fact]
    public async Task ClientePodeSerConsultadoAtualizadoDesativadoEReativado()
    {
        var cliente = await CriarClienteAsync("Cliente CRUD");
        var url = $"/api/clientes/{cliente.Id}";
        var consulta = await _cliente.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        var atualizacao = await _cliente.PutAsJsonAsync(url, new
        {
            nome = "  Cliente   Atualizado ",
            cpf = cliente.Cpf,
            telefone = cliente.Telefone,
            email = cliente.Email,
            ativo = false
        });
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        using (var documento = JsonDocument.Parse(await atualizacao.Content.ReadAsStringAsync()))
        {
            Assert.Equal("Cliente Atualizado", documento.RootElement.GetProperty("nome").GetString());
            Assert.False(documento.RootElement.GetProperty("ativo").GetBoolean());
        }

        var exclusao = await _cliente.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var reativacao = await _cliente.PutAsJsonAsync(url, new
        {
            nome = "Cliente Atualizado",
            cpf = cliente.Cpf,
            telefone = cliente.Telefone,
            email = cliente.Email,
            ativo = true
        });
        Assert.Equal(HttpStatusCode.OK, reativacao.StatusCode);
        using var reativado = JsonDocument.Parse(await reativacao.Content.ReadAsStringAsync());
        Assert.True(reativado.RootElement.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task AtualizacaoDeClienteRejeitaCpfDuplicadoSemAlterarRegistro()
    {
        var primeiro = await CriarClienteAsync("Cliente Primeiro");
        var segundo = await CriarClienteAsync("Cliente Segundo");
        var resposta = await _cliente.PutAsJsonAsync($"/api/clientes/{segundo.Id}", new
        {
            nome = "Cliente Modificado",
            cpf = primeiro.Cpf,
            telefone = segundo.Telefone,
            email = segundo.Email,
            ativo = true
        });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var consulta = await _cliente.GetAsync($"/api/clientes/{segundo.Id}");
        using var documento = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        Assert.Equal("Cliente Segundo", documento.RootElement.GetProperty("nome").GetString());
        Assert.Equal(segundo.Cpf, documento.RootElement.GetProperty("cpf").GetString());
    }

    [Fact]
    public async Task QuadraPodeSerConsultadaAtualizadaDesativadaEReativada()
    {
        var quadra = await CriarQuadraAsync();
        var url = $"/api/quadras/{quadra.Id}";
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync(url)).StatusCode);

        var atualizacao = await _cliente.PutAsJsonAsync(url, new { nome = "  Quadra   Atualizada ", ativa = false });
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        using (var documento = JsonDocument.Parse(await atualizacao.Content.ReadAsStringAsync()))
        {
            Assert.Equal("Quadra Atualizada", documento.RootElement.GetProperty("nome").GetString());
            Assert.False(documento.RootElement.GetProperty("ativa").GetBoolean());
        }

        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync(url)).StatusCode);
        var reativacao = await _cliente.PutAsJsonAsync(url, new { nome = "Quadra Atualizada", ativa = true });
        Assert.Equal(HttpStatusCode.OK, reativacao.StatusCode);
        using var reativada = JsonDocument.Parse(await reativacao.Content.ReadAsStringAsync());
        Assert.True(reativada.RootElement.GetProperty("ativa").GetBoolean());
    }

    [Fact]
    public async Task ReservaPodeSerConsultadaReagendadaEExcluidaSemApagarHistorico()
    {
        var quadra = await CriarQuadraAsync();
        var cliente = await CriarClienteAsync("Cliente Reserva");
        var clienteAlternativo = await CriarClienteAsync("Cliente Alternativo");
        var data = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var reserva = await CriarReservaAsync(cliente.Id, quadra.Id, data, new TimeOnly(10, 0));
        var ocupanteDoDestino = await CriarReservaAsync(
            clienteAlternativo.Id,
            quadra.Id,
            data,
            new TimeOnly(12, 0));

        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/reservas/{reserva.Id}")).StatusCode);
        var lista = await _cliente.GetFromJsonAsync<JsonElement[]>("/api/reservas");
        Assert.Contains(lista!, item => item.GetProperty("id").GetInt32() == reserva.Id);

        var semAlteracao = await _cliente.PutAsJsonAsync($"/api/reservas/{reserva.Id}", new
        {
            clienteId = cliente.Id,
            quadraId = quadra.Id,
            tipoCliente = 0,
            inicio = Horario(data, new TimeOnly(10, 0)),
            fim = Horario(data, new TimeOnly(11, 0))
        });
        Assert.Equal(HttpStatusCode.OK, semAlteracao.StatusCode);

        var destinoEmConflito = await _cliente.PutAsJsonAsync($"/api/reservas/{reserva.Id}", new
        {
            clienteId = clienteAlternativo.Id,
            quadraId = quadra.Id,
            tipoCliente = 1,
            inicio = Horario(data, new TimeOnly(12, 0)),
            fim = Horario(data, new TimeOnly(13, 0))
        });
        Assert.Equal(HttpStatusCode.Conflict, destinoEmConflito.StatusCode);

        var reagendamentoData = data.AddDays(1);
        var reagendamento = await _cliente.PutAsJsonAsync($"/api/reservas/{reserva.Id}", new
        {
            clienteId = clienteAlternativo.Id,
            quadraId = quadra.Id,
            tipoCliente = 1,
            inicio = Horario(reagendamentoData, new TimeOnly(14, 0)),
            fim = Horario(reagendamentoData, new TimeOnly(15, 0))
        });
        Assert.Equal(HttpStatusCode.OK, reagendamento.StatusCode);
        using (var documento = JsonDocument.Parse(await reagendamento.Content.ReadAsStringAsync()))
        {
            Assert.Equal(clienteAlternativo.Id, documento.RootElement.GetProperty("clienteId").GetInt32());
            Assert.Equal("Confirmada", documento.RootElement.GetProperty("status").GetString());
            Assert.Equal(80m, documento.RootElement.GetProperty("valorFinal").GetDecimal());
        }

        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/reservas/{reserva.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/reservas/{reserva.Id}")).StatusCode);
        var consultaCancelada = await _cliente.GetFromJsonAsync<JsonElement>($"/api/reservas/{reserva.Id}");
        Assert.Equal("Cancelada", consultaCancelada.GetProperty("status").GetString());
        var edicaoCancelada = await _cliente.PutAsJsonAsync($"/api/reservas/{reserva.Id}", new
        {
            clienteId = cliente.Id,
            quadraId = quadra.Id,
            tipoCliente = 0,
            inicio = Horario(data.AddDays(2), new TimeOnly(10, 0)),
            fim = Horario(data.AddDays(2), new TimeOnly(11, 0))
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, edicaoCancelada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync($"/api/reservas/{ocupanteDoDestino.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("{\"clienteId\":0,\"quadraId\":1,\"tipoCliente\":0,\"inicio\":\"2030-01-01T10:00:00-03:00\",\"fim\":\"2030-01-01T11:00:00-03:00\"}")]
    [InlineData("{\"clienteId\":1,\"quadraId\":0,\"tipoCliente\":0,\"inicio\":\"2030-01-01T10:00:00-03:00\",\"fim\":\"2030-01-01T11:00:00-03:00\"}")]
    [InlineData("{\"clienteId\":1,\"quadraId\":1,\"inicio\":\"2030-01-01T10:00:00-03:00\",\"fim\":\"2030-01-01T11:00:00-03:00\"}")]
    public async Task CriacaoDeReservaRejeitaIdsInvalidosOuTipoObrigatorioAusente(string payload)
    {
        var resposta = await _cliente.PostAsync(
            "/api/reservas",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task DisponibilidadeRejeitaTipoDeClienteForaDoEnum()
    {
        Assert.True(File.Exists(factory.DatabasePath));
        var cadastroQuadra = await _cliente.PostAsJsonAsync("/api/quadras", new { nome = "Quadra Teste" });
        Assert.Equal(HttpStatusCode.Created, cadastroQuadra.StatusCode);
        using var quadraDocumento = JsonDocument.Parse(await cadastroQuadra.Content.ReadAsStringAsync());
        var quadraId = quadraDocumento.RootElement.GetProperty("id").GetInt32();
        var data = DateOnly.FromDateTime(DateTime.Today.AddDays(2)).ToString("yyyy-MM-dd");
        var resposta = await _cliente.GetAsync(
            $"/api/quadras/{quadraId}/disponibilidade?data={data}&duracaoMinutos=60&tipoCliente=99");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    private static string CriarCpfValido()
    {
        var baseCpf = Random.Shared.Next(100_000_000, 1_000_000_000).ToString();
        var cpf = baseCpf + CalcularDigitoCpf(baseCpf, 9);
        return cpf + CalcularDigitoCpf(cpf, 10);
    }

    private static int CalcularDigitoCpf(string cpf, int tamanho)
    {
        var soma = 0;
        for (var indice = 0; indice < tamanho; indice++)
            soma += (cpf[indice] - '0') * (tamanho + 1 - indice);

        var resto = (soma * 10) % 11;
        return resto == 10 ? 0 : resto;
    }

    private async Task<(int Id, string Cpf, string Telefone, string Email)> CriarClienteAsync(string nome)
    {
        var cpf = CriarCpfValido();
        var email = $"cliente-{Guid.NewGuid():N}@example.test";
        var resposta = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nome,
            cpf,
            telefone = "51999990001",
            email
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return (
            documento.RootElement.GetProperty("id").GetInt32(),
            cpf,
            "51999990001",
            email);
    }

    private async Task<(int Id, string Nome)> CriarQuadraAsync()
    {
        var nome = $"Quadra {Guid.NewGuid():N}";
        var resposta = await _cliente.PostAsJsonAsync("/api/quadras", new { nome });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return (documento.RootElement.GetProperty("id").GetInt32(), nome);
    }

    private async Task<(int Id, DateTimeOffset Inicio)> CriarReservaAsync(
        int clienteId,
        int quadraId,
        DateOnly data,
        TimeOnly inicio)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/reservas", new
        {
            clienteId,
            quadraId,
            tipoCliente = 0,
            inicio = Horario(data, inicio),
            fim = Horario(data, inicio.AddHours(1))
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return (
            documento.RootElement.GetProperty("id").GetInt32(),
            documento.RootElement.GetProperty("inicio").GetDateTimeOffset());
    }

    private static DateTimeOffset Horario(DateOnly data, TimeOnly horario) =>
        new(data.ToDateTime(horario), TimeSpan.FromHours(-3));
}
