using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class PoliticaDeConteudoTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private const string Vigiar = """
        window.__violacoes = [];
        document.addEventListener('securitypolicyviolation', evento => {
            if (!/kaspersky/i.test(evento.blockedURI + evento.sourceFile)) {
                window.__violacoes.push(`${evento.effectiveDirective} ${evento.blockedURI || 'inline'} ${evento.sourceFile}:${evento.lineNumber}`);
            }
        });
        """;

    private IPage _pagina = null!;
    private Cadastro _cadastro = null!;
    private int _venda;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        var maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        var caderno = await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        _venda = await _cadastro.VendaAsync(Cadastro.Hoje, maria, new DadosItemVenda(caneta, 10, 2.50m), new DadosItemVenda(caderno, 1, 18.90m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-2), maria, new DadosItemVenda(caderno, 3, 18.90m));
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
        await _pagina.AddInitScriptAsync(Vigiar);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Theory]
    [InlineData("/", false)]
    [InlineData("/", true)]
    [InlineData("/Vendas", false)]
    [InlineData("/Vendas/Nova", true)]
    [InlineData("/Clientes", false)]
    [InlineData("/Clientes/Novo", false)]
    [InlineData("/Produtos", true)]
    [InlineData("/Produtos/Novo", false)]
    [InlineData("/endereco-que-nao-existe", false)]
    public async Task Nenhuma_tela_viola_a_politica_de_conteudo(string caminho, bool escuro)
    {
        await _pagina.EmulateMediaAsync(new() { ColorScheme = escuro ? ColorScheme.Dark : ColorScheme.Light });

        await _pagina.GotoAsync(caminho);
        await _pagina.WaitForLoadStateAsync(LoadState.NetworkIdle);

        (await ViolacoesAsync()).Should().BeEmpty();
        await Expect(_pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", escuro ? "dark" : "light");
    }

    [Fact]
    public async Task O_detalhe_da_venda_e_a_busca_do_caixa_nao_violam_a_politica()
    {
        await _pagina.GotoAsync($"/Vendas/Detalhe/{_venda}");
        await _pagina.WaitForLoadStateAsync(LoadState.NetworkIdle);
        (await ViolacoesAsync()).Should().BeEmpty();

        await _pagina.GotoAsync("/Vendas/Nova");
        await new Caixa(_pagina).EscolherClienteAsync("Maria", "Maria Santos");
        (await ViolacoesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task As_barras_do_painel_seguem_os_valores_sem_estilo_embutido()
    {
        await _pagina.GotoAsync("/");

        var dias = _pagina.GetByTestId("grafico-ultimos-dias").Locator("[data-dia]");
        var hoje = await AlturaDaBarraAsync(dias.Nth(6));
        var anteontem = await AlturaDaBarraAsync(dias.Nth(4));
        var semVenda = await AlturaDaBarraAsync(dias.Nth(5));
        anteontem.Should().BeGreaterThan(hoje);
        (hoje / anteontem).Should().BeApproximately(43.90 / 56.70, 0.05);
        semVenda.Should().BeInRange(1, 3);

        var trilhos = _pagina.GetByTestId("mais-vendidos").Locator(".ranking-trilho");
        var primeiro = await LarguraPreenchidaAsync(trilhos.Nth(0));
        var segundo = await LarguraPreenchidaAsync(trilhos.Nth(1));
        primeiro.Should().BeApproximately(1, 0.02);
        segundo.Should().BeApproximately(4.0 / 10.0, 0.05);
        (await _pagina.Locator("main [style]").CountAsync()).Should().Be(0);
    }

    private async Task<string[]> ViolacoesAsync() =>
        await _pagina.EvaluateAsync<string[]>("() => window.__violacoes");

    private static async Task<double> AlturaDaBarraAsync(ILocator dia) =>
        await dia.Locator(".barra").EvaluateAsync<double>(
            "barra => [...barra.querySelectorAll('rect')].reduce((maior, retangulo) => Math.max(maior, barra.getBoundingClientRect().bottom - retangulo.getBoundingClientRect().top), 0)");

    private static async Task<double> LarguraPreenchidaAsync(ILocator trilho) =>
        await trilho.EvaluateAsync<double>(
            "trilho => trilho.querySelector('rect').getBoundingClientRect().width / trilho.getBoundingClientRect().width");
}
