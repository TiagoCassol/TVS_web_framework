using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BeachTennis.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _diretorioTemporario =
        Path.Combine(Path.GetTempPath(), $"beachtennis-tests-{Guid.NewGuid():N}");
    private readonly string? _connectionStringAnterior =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    public string DatabasePath => Path.Combine(_diretorioTemporario, "beachtennis.db");

    public ApiFactory()
    {
        Directory.CreateDirectory(_diretorioTemporario);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            $"Data Source={DatabasePath};Pooling=False");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={DatabasePath};Pooling=False"
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                _connectionStringAnterior);
            if (Directory.Exists(_diretorioTemporario))
                Directory.Delete(_diretorioTemporario, recursive: true);
        }
    }
}
