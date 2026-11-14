using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class TelaDeProdutosTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Cadastra_o_produto_com_valor_em_reais()
    {
        await _pagina.GotoAsync("/Produtos");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Novo produto" }).ClickAsync();

        await _pagina.GetByLabel("Nome").FillAsync("Caderno 96 folhas");
        await _pagina.GetByLabel("Valor unitário").FillAsync("18,90");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto cadastrado.");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caderno 96 folhas" }))
            .ToContainTextAsync("R$ 18,90");
    }

    [Fact]
    public async Task Avisa_no_campo_quando_o_nome_ja_existe()
    {
        await CadastrarAsync("Caneta azul", "2,50");

        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("CANETA AZUL");
        await _pagina.GetByLabel("Valor unitário").FillAsync("3,00");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Nome]")).ToHaveTextAsync("Já existe um produto com este nome.");
    }

    [Fact]
    public async Task Avisa_no_campo_quando_o_nome_ja_existe_com_espacos_a_mais()
    {
        await CadastrarAsync("Caderno Universitário", "18,90");

        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caderno  Universitário");
        await _pagina.GetByLabel("Valor unitário").FillAsync("18,90");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Nome]")).ToHaveTextAsync("Já existe um produto com este nome.");
    }

    [Fact]
    public async Task Altera_o_valor_do_produto()
    {
        await CadastrarAsync("Caneta azul", "2,50");

        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Editar Caneta azul" }).ClickAsync();
        await _pagina.GetByLabel("Valor unitário").FillAsync("2,75");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" }))
            .ToContainTextAsync("R$ 2,75");
    }

    [Theory]
    [InlineData("12.50", "R$ 12,50")]
    [InlineData("1.250", "R$ 1.250,00")]
    [InlineData("1.250,75", "R$ 1.250,75")]
    public async Task Valor_com_ponto_segue_a_mesma_regra_do_caixa(string digitado, string esperado)
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync(digitado);
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto cadastrado.");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" })).ToContainTextAsync(esperado);
    }

    [Theory]
    [InlineData("12.5055")]
    [InlineData("1.2.3")]
    [InlineData("1.25,0")]
    public async Task Valor_em_forma_ambigua_e_recusado_no_campo(string digitado)
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync(digitado);
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync("Informe um número em Valor unitário.");
        (await new Cadastro(aplicacao.Banco).QuantidadeDeProdutosAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Valor_recusado_pelo_servidor_volta_com_o_que_foi_digitado()
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("2,5x");

        await _pagina.Locator("main form").EvaluateAsync("formulario => HTMLFormElement.prototype.submit.call(formulario)");

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync("O valor \"2,5x\" não é válido para Valor unitário.");
        await Expect(_pagina.GetByLabel("Valor unitário")).ToHaveValueAsync("2,5x");
    }

    private async Task CadastrarAsync(string nome, string valor)
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync(nome);
        await _pagina.GetByLabel("Valor unitário").FillAsync(valor);
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto cadastrado.");
    }
}
