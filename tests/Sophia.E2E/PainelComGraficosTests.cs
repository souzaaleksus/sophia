using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class PainelComGraficosTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;
    private Cadastro _cadastro = null!;
    private int _maria;
    private int _caneta;
    private int _caderno;
    private int _lapis;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        _maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        _caneta = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        _caderno = await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        _lapis = await _cadastro.ProdutoAsync("Lápis preto", 1.00m);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task O_grafico_mostra_os_ultimos_sete_dias_com_hoje_marcado_e_uma_tabela_com_os_valores()
    {
        var hoje = Cadastro.Hoje;
        await _cadastro.VendaAsync(hoje, _maria, new DadosItemVenda(_caneta, 10, 2.50m));
        await _cadastro.VendaAsync(hoje, _maria, new DadosItemVenda(_caderno, 1, 18.90m));
        await _cadastro.VendaAsync(hoje.AddDays(-2), _maria, new DadosItemVenda(_caderno, 3, 18.90m));

        await _pagina.GotoAsync("/");

        var grafico = _pagina.GetByTestId("grafico-ultimos-dias");
        var dias = grafico.Locator("[data-dia]");
        await Expect(dias).ToHaveCountAsync(7);
        await Expect(grafico.Locator("[data-dia][aria-current=date]")).ToHaveCountAsync(1);
        await Expect(dias.Nth(6)).ToHaveAttributeAsync("aria-current", "date");

        var linhas = grafico.GetByRole(AriaRole.Table).Locator("tbody tr");
        await Expect(linhas).ToHaveCountAsync(7);
        await Expect(linhas.Nth(6)).ToContainTextAsync("R$ 43,90");
        await Expect(linhas.Nth(4)).ToContainTextAsync("R$ 56,70");
        await Expect(linhas.Filter(new() { HasText = "R$ 0,00" })).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task Os_mais_vendidos_vem_da_maior_quantidade_para_a_menor_com_nome_e_quantidade()
    {
        var hoje = Cadastro.Hoje;
        await _cadastro.VendaAsync(hoje, _maria, new DadosItemVenda(_caneta, 10, 2.50m), new DadosItemVenda(_caderno, 1, 18.90m));
        await _cadastro.VendaAsync(hoje, _maria, new DadosItemVenda(_lapis, 5, 1.00m), new DadosItemVenda(_caderno, 3, 18.90m));

        await _pagina.GotoAsync("/");

        var itens = _pagina.GetByTestId("mais-vendidos").GetByTestId("mais-vendido");
        await Expect(itens).ToHaveCountAsync(3);
        await DeveMostrarAsync(itens.Nth(0), "Caneta azul", 10);
        await DeveMostrarAsync(itens.Nth(1), "Lápis preto", 5);
        await DeveMostrarAsync(itens.Nth(2), "Caderno 96 folhas", 4);
    }

    private static async Task DeveMostrarAsync(ILocator item, string nome, int quantidade)
    {
        await Expect(item).ToContainTextAsync(nome);
        var semONome = (await item.InnerTextAsync()).Replace(nome, " ", StringComparison.Ordinal);
        Regex.IsMatch(semONome, $@"(^|\D){quantidade}(\D|$)")
            .Should().BeTrue($"o item de {nome} mostra a quantidade {quantidade}, mas mostrou \"{semONome.Trim()}\"");
    }
}
