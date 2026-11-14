using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class IdentidadeVisualTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private static readonly string[] ItensDoMenu = ["Painel", "Produto", "Cliente", "Venda"];

    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task O_menu_principal_traz_painel_venda_cliente_e_produto_no_singular()
    {
        await _pagina.GotoAsync("/Produtos");

        foreach (var item in new[] { "Painel", "Venda", "Cliente", "Produto" })
        {
            await Expect(ItemDoMenu(item)).ToHaveCountAsync(1);
        }

        foreach (var antigo in new[] { "Vendas", "Clientes", "Produtos" })
        {
            await Expect(ItemDoMenu(antigo)).ToHaveCountAsync(0);
        }
    }

    [Fact]
    public async Task O_menu_principal_lista_painel_produto_cliente_e_venda_nessa_ordem()
    {
        await _pagina.GotoAsync("/");

        var nomes = await _pagina.GetByRole(AriaRole.Navigation, new() { Name = "Navegação principal" })
            .GetByRole(AriaRole.Link)
            .EvaluateAllAsync<string[]>("links => links.map(link => link.textContent.trim().replace(/\\s+/g, ' '))");

        nomes.Where(nome => ItensDoMenu.Contains(nome)).Should().Equal(ItensDoMenu);
    }

    [Theory]
    [InlineData("Venda", "Vendas")]
    [InlineData("Cliente", "Clientes")]
    [InlineData("Produto", "Produtos")]
    public async Task O_item_do_menu_no_singular_leva_a_lista_com_o_titulo_no_plural(string item, string titulo)
    {
        await _pagina.GotoAsync("/");
        await Expect(ItemDoMenu(item)).ToBeVisibleAsync();

        await ItemDoMenu(item).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync(titulo);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Vendas/Nova")]
    public async Task O_rodape_diz_que_a_sophia_e_a_forca_de_venda_da_loja(string endereco)
    {
        await _pagina.GotoAsync(endereco);

        await Expect(_pagina.GetByRole(AriaRole.Contentinfo)).ToHaveTextAsync("Sophia, a força de venda da sua loja.");
    }

    [Fact]
    public async Task O_logo_no_cabecalho_leva_ao_painel()
    {
        await _pagina.GotoAsync("/Clientes");

        var logo = _pagina.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Link)
            .Filter(new() { Has = _pagina.Locator("img[alt='Sophia']") });
        await Expect(logo).ToHaveCountAsync(1);
        await Expect(logo).ToHaveAttributeAsync("href", "/");
        var carregada = await logo.Locator("img[alt='Sophia']").EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0");
        carregada.Should().BeTrue("a imagem do logo precisa carregar");

        await logo.ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Painel");
    }

    private ILocator ItemDoMenu(string nome) =>
        _pagina.GetByRole(AriaRole.Navigation, new() { Name = "Navegação principal" })
            .GetByRole(AriaRole.Link, new() { Name = nome, Exact = true });
}
