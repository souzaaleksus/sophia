using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Sophia.Testes.Banco;

namespace Sophia.E2E.Apoio;

public class AplicacaoEmExecucao : IAsyncLifetime
{
    private readonly BancoDeTestes _banco = new();
    private readonly IReadOnlyDictionary<string, string> _configuracao;
    private readonly ConcurrentQueue<string> _saidaDaAplicacao = new();
    private Process? _processo;
    private IPlaywright? _playwright;

    public AplicacaoEmExecucao()
        : this(new Dictionary<string, string>())
    {
    }

    protected AplicacaoEmExecucao(IReadOnlyDictionary<string, string> configuracao)
    {
        _configuracao = configuracao;
    }

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

    public Task<IPage> NovaPaginaAsync() => AbrirPaginaAsync(new BrowserNewContextOptions());

    public Task<IPage> NovaPaginaAsync(int largura, int altura, bool toque = false) =>
        AbrirPaginaAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = largura, Height = altura },
            HasTouch = toque,
            IsMobile = toque,
        });

    private async Task<IPage> AbrirPaginaAsync(BrowserNewContextOptions opcoes)
    {
        opcoes.BaseURL = Endereco;
        opcoes.Locale = "pt-BR";
        opcoes.TimezoneId = "America/Sao_Paulo";
        var contexto = await Navegador.NewContextAsync(opcoes);
        await Rede.BuscarForaDoNavegadorAsync(contexto);
        var pagina = await contexto.NewPageAsync();
        await DiarioDaPagina.AcompanharAsync(pagina, () => _saidaDaAplicacao);
        await LimitarCpuAsync(pagina);
        return pagina;
    }

    private static async Task LimitarCpuAsync(IPage pagina)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("SOPHIA_E2E_CPU_LENTA"), out var vezes) || vezes <= 1)
        {
            return;
        }

        var protocolo = await pagina.Context.NewCDPSessionAsync(pagina);
        await protocolo.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = vezes });
    }

    private Process IniciarAplicacao()
    {
        var pastaDoProjetoWeb = PastaDoProjetoWeb();
        var pastaDoTeste = new DirectoryInfo(AppContext.BaseDirectory);
        var dll = Path.Combine(pastaDoProjetoWeb, "bin", pastaDoTeste.Parent!.Name, pastaDoTeste.Name, "Sophia.Web.dll");

        var inicio = new ProcessStartInfo("dotnet", [dll, "--urls", Endereco])
        {
            WorkingDirectory = pastaDoProjetoWeb,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        inicio.Environment["ConnectionStrings__Sophia"] = _banco.TextoDeConexao;
        foreach (var (chave, valor) in _configuracao)
        {
            inicio.Environment[chave] = valor;
        }

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
            if (await RespondeAsync(http))
            {
                return;
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

    private async Task<bool> RespondeAsync(HttpClient http)
    {
        try
        {
            var resposta = await http.GetAsync(new Uri($"{Endereco}/Produtos"));
            return resposta.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
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
