using System.Globalization;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacaoComAtalhoDe15Dias.Nome)]
public sealed class AtalhoDePeriodoConfiguradoTests(AplicacaoComAtalhoDe15Dias aplicacao) : IAsyncLifetime
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

    private static string DataDoCampo(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private ILocator Atalho(string nome) => _pagina.GetByRole(AriaRole.Link, new() { Name = nome, Exact = true });

    private ILocator LinhaDe(string cliente) =>
        _pagina.Locator("main table tbody tr").Filter(new() { HasText = cliente });
}
