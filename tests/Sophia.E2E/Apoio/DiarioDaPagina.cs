using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Playwright;

namespace Sophia.E2E.Apoio;

public sealed class DiarioDaPagina
{
    private const string EstadoDoCampo = """
        campo => {
            const descrever = e => e ? `${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${e.name ? '[name=' + e.name + ']' : ''}` : 'nenhum';
            const area = campo.closest('.autocompletar');
            const lista = area?.querySelector('[role=listbox]');
            const aviso = area?.querySelector('.opcoes-aviso');
            return [
                `valor do campo: "${campo.value}"`,
                `foco: ${descrever(document.activeElement)}`,
                `campo com foco: ${document.activeElement === campo}`,
                `documento com foco: ${document.hasFocus()}`,
                `visibilidade: ${document.visibilityState}`,
                `readyState: ${document.readyState}`,
                `aria-busy: ${campo.getAttribute('aria-busy')}`,
                `aria-expanded: ${campo.getAttribute('aria-expanded')}`,
                `lista escondida: ${lista?.hidden}; opções: ${lista?.children.length}`,
                `aviso escondido: ${aviso?.hidden}; texto: "${aviso?.textContent ?? ''}"`,
                ...performance.getEntriesByType('resource')
                    .filter(r => r.initiatorType === 'script' || r.initiatorType === 'fetch' || r.name.endsWith('.js') || r.name.includes('.js?'))
                    .map(r => `recurso ${r.initiatorType} ${r.name} status=${r.responseStatus} início=${Math.round(r.startTime)}ms duração=${Math.round(r.duration)}ms`),
            ].join('\n');
        }
        """;

    private static readonly ConditionalWeakTable<IPage, DiarioDaPagina> Diarios = new();

    private readonly ConcurrentQueue<string> _eventos = new();
    private readonly Stopwatch _relogio = Stopwatch.StartNew();
    private readonly Func<IEnumerable<string>> _saidaDaAplicacao;

    private DiarioDaPagina(Func<IEnumerable<string>> saidaDaAplicacao)
    {
        _saidaDaAplicacao = saidaDaAplicacao;
    }

    public static async Task AcompanharAsync(IPage pagina, Func<IEnumerable<string>> saidaDaAplicacao)
    {
        var diario = new DiarioDaPagina(saidaDaAplicacao);
        pagina.Console += (_, mensagem) => diario.Anotar($"console {mensagem.Type}: {mensagem.Text}");
        pagina.PageError += (_, erro) => diario.Anotar($"erro na página: {erro}");
        pagina.FrameNavigated += (_, quadro) => diario.Anotar($"navegou para {quadro.Url}");
        pagina.DOMContentLoaded += (_, _) => diario.Anotar("DOMContentLoaded");
        pagina.Load += (_, _) => diario.Anotar("load");
        pagina.Request += (_, requisicao) => diario.AnotarSeForBuscaOuScript(requisicao, $"-> {requisicao.Method} {requisicao.Url}");
        pagina.Response += (_, resposta) => diario.AnotarSeForBuscaOuScript(resposta.Request, $"<- {resposta.Status} {resposta.Url}");
        pagina.RequestFailed += (_, requisicao) => diario.AnotarSeForBuscaOuScript(requisicao, $"falhou {requisicao.Url}: {requisicao.Failure}");
        pagina.RequestFinished += (_, requisicao) => diario.AnotarSeForBuscaOuScript(requisicao, $"terminou {requisicao.Url}");
        await pagina.Context.Tracing.StartAsync(new() { Snapshots = true, Screenshots = true });
        Diarios.AddOrUpdate(pagina, diario);
    }

    public static async Task<Exception> FalhaAsync(IPage pagina, ILocator campo, string esperado, TimeoutException erro)
    {
        if (!Diarios.TryGetValue(pagina, out var diario))
        {
            return erro;
        }

        var relato = new StringBuilder()
            .Append("A sugestão \"").Append(esperado).AppendLine("\" não apareceu.")
            .AppendLine("--- estado da tela ---")
            .AppendLine(await EstadoAsync(campo))
            .AppendLine("--- eventos da página ---")
            .AppendJoin(Environment.NewLine, diario._eventos).AppendLine()
            .AppendLine("--- rastro do Playwright ---")
            .AppendLine(await GuardarRastroAsync(pagina))
            .AppendLine("--- saída da aplicação ---")
            .AppendJoin(Environment.NewLine, diario._saidaDaAplicacao().TakeLast(80));
        return new TimeoutException(relato.ToString(), erro);
    }

    private static async Task<string> EstadoAsync(ILocator campo)
    {
        try
        {
            return await campo.EvaluateAsync<string>(EstadoDoCampo, null, new() { Timeout = 5_000 });
        }
        catch (PlaywrightException erro)
        {
            return $"não foi possível ler o estado: {erro.Message}";
        }
    }

    private static async Task<string> GuardarRastroAsync(IPage pagina)
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "rastros");
        Directory.CreateDirectory(pasta);
        var arquivo = Path.Combine(pasta, $"rastro-{DateTime.Now:yyyyMMdd-HHmmss-fff}.zip");
        try
        {
            await pagina.Context.Tracing.StopAsync(new() { Path = arquivo });
            return arquivo;
        }
        catch (PlaywrightException erro)
        {
            return $"não foi possível guardar o rastro: {erro.Message}";
        }
    }

    private void AnotarSeForBuscaOuScript(IRequest requisicao, string texto)
    {
        if (requisicao.Url.Contains("/Buscar", StringComparison.Ordinal) || requisicao.ResourceType == "script")
        {
            Anotar(texto);
        }
    }

    private void Anotar(string texto) =>
        _eventos.Enqueue(string.Create(CultureInfo.InvariantCulture, $"{_relogio.ElapsedMilliseconds,7} ms  {texto}"));
}
