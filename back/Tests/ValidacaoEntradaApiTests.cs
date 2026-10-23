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
}
