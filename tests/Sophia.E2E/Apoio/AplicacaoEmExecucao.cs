using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Sophia.Integration.Tests.Apoio;

namespace Sophia.E2E.Apoio;

public sealed class AplicacaoEmExecucao : IAsyncLifetime
{
    private readonly BancoDeTestes _banco = new();
    private readonly ConcurrentQueue<string> _saidaDaAplicacao = new();
    private Process? _processo;
    private IPlaywright? _playwright;

    public string Endereco { get; private set; } = string.Empty;

    public BancoDeTestes Banco => _banco;

    public IBrowser Navegador { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _banco.InitializeAsync();
        Endereco = $"http://127.0.0.1:{PortaLivre()}";
        _processo = IniciarAplicacao();
        await EsperarResponderAsync();

        _playwright = await Playwright.CreateAsync();
        var visivel = Environment.GetEnvironmentVariable("SOPHIA_E2E_VISIVEL") == "1";
        Navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = Environment.GetEnvironmentVariable("SOPHIA_E2E_CANAL"),
            Headless = !visivel,
            SlowMo = visivel ? 250 : 0,
        });
    }

    public async Task DisposeAsync()
    {
        if (Navegador is not null)
        {
            await Navegador.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_processo is { HasExited: false })
        {
            _processo.Kill(entireProcessTree: true);
            await _processo.WaitForExitAsync();
        }

        _processo?.Dispose();
        await _banco.DisposeAsync();
    }

    public async Task<IPage> NovaPaginaAsync()
    {
        var contexto = await Navegador.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Endereco,
            Locale = "pt-BR",
            TimezoneId = "America/Sao_Paulo",
        });
        return await contexto.NewPageAsync();
    }

    private Process IniciarAplicacao()
    {
        var pastaDoProjetoWeb = PastaDoProjetoWeb();
        var configuracao = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(pastaDoProjetoWeb, "bin", configuracao, "net8.0", "Sophia.Web.dll");

        var inicio = new ProcessStartInfo("dotnet", [dll, "--urls", Endereco])
        {
            WorkingDirectory = pastaDoProjetoWeb,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        inicio.Environment["ConnectionStrings__Sophia"] = _banco.TextoDeConexao;

        var processo = Process.Start(inicio)!;
        processo.OutputDataReceived += (_, linha) => GuardarSaida(linha.Data);
        processo.ErrorDataReceived += (_, linha) => GuardarSaida(linha.Data);
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();
        return processo;
    }

    private async Task EsperarResponderAsync()
    {
        using var http = new HttpClient();
        var limite = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < limite)
        {
            try
            {
                var resposta = await http.GetAsync(new Uri($"{Endereco}/Produtos"));
                if (resposta.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"A aplicação não respondeu em {Endereco}.{Environment.NewLine}{string.Join(Environment.NewLine, _saidaDaAplicacao)}");
    }

    private void GuardarSaida(string? linha)
    {
        if (linha is null)
        {
            return;
        }

        _saidaDaAplicacao.Enqueue(linha);
        while (_saidaDaAplicacao.Count > 200)
        {
            _saidaDaAplicacao.TryDequeue(out _);
        }
    }

    private static string PastaDoProjetoWeb()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Sophia.sln")))
        {
            pasta = pasta.Parent;
        }

        return Path.Combine(pasta!.FullName, "src", "Sophia.Web");
    }

    private static int PortaLivre()
    {
        using var ouvinte = new TcpListener(IPAddress.Loopback, 0);
        ouvinte.Start();
        return ((IPEndPoint)ouvinte.LocalEndpoint).Port;
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoDaAplicacao : ICollectionFixture<AplicacaoEmExecucao>
{
    public const string Nome = "Aplicação em execução";
}
