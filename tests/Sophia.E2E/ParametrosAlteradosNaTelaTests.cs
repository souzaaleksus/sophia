using System.Globalization;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacaoComParametrosAlterados.Nome)]
public sealed class ParametrosAlteradosNaTelaTests(AplicacaoComParametrosAlterados aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;
    private Cadastro _cadastro = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task O_grafico_do_painel_diz_quantos_dias_mostra()
    {
        await _pagina.GotoAsync("/");

        var grafico = _pagina.GetByTestId("grafico-ultimos-dias");
        await Expect(grafico.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("Últimos 5 dias");
        await Expect(grafico.Locator("caption")).ToHaveTextAsync("Total vendido em cada um dos últimos 5 dias");
        await Expect(grafico.Locator("[data-dia]")).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task O_atalho_segue_os_dias_configurados_no_rotulo_e_no_periodo()
    {
        var maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var joao = await _cadastro.ClienteAsync("111.444.777-35", "João Lima");
        var ana = await _cadastro.ClienteAsync("123.456.789-09", "Ana Souza");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.VendaAsync(Cadastro.Hoje, maria, new DadosItemVenda(produto, 1, 2.50m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-14), joao, new DadosItemVenda(produto, 2, 2.50m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-15), ana, new DadosItemVenda(produto, 3, 2.50m));
        await _pagina.GotoAsync("/Vendas");

        await Expect(Atalho("7 dias")).ToHaveCountAsync(0);
        await Atalho("15 dias").ClickAsync();

        await Expect(_pagina.GetByLabel("De", new() { Exact = true })).ToHaveValueAsync(DataDoCampo(Cadastro.Hoje.AddDays(-14)));
        await Expect(_pagina.GetByLabel("Até", new() { Exact = true })).ToHaveValueAsync(DataDoCampo(Cadastro.Hoje));
        await Expect(Atalho("15 dias")).ToHaveAttributeAsync("aria-current", "true");
        await Expect(LinhaDe("Maria Santos")).ToHaveCountAsync(1);
        await Expect(LinhaDe("João Lima")).ToHaveCountAsync(1);
        await Expect(LinhaDe("Ana Souza")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task A_quantidade_do_caixa_para_no_maximo_configurado()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        var caixa = new Caixa(_pagina);
        await _pagina.GotoAsync("/Vendas/Nova");
        await caixa.LancarAsync(0, "Caneta", "Caneta azul", "501");

        await caixa.QuantidadeDaLinha(0).BlurAsync();
        await Expect(caixa.Linha(0).Locator("[data-erro=Quantidade]")).ToHaveTextAsync("A quantidade aceita no máximo 500 por item.");
        await caixa.Linha(0).GetByRole(AriaRole.Button, new() { Name = "Diminuir a quantidade", Exact = true }).ClickAsync();

        await Expect(caixa.QuantidadeDaLinha(0)).ToHaveAttributeAsync("max", "500");
        await Expect(caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("500");
        await Expect(caixa.Linha(0).GetByRole(AriaRole.Button, new() { Name = "Aumentar a quantidade", Exact = true })).ToBeDisabledAsync();
    }

    private static string DataDoCampo(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private ILocator Atalho(string nome) => _pagina.GetByRole(AriaRole.Link, new() { Name = nome, Exact = true });

    private ILocator LinhaDe(string cliente) =>
        _pagina.Locator("main table tbody tr").Filter(new() { HasText = cliente });
}
