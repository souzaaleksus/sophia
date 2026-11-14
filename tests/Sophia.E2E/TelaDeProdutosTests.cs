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
    public async Task O_aviso_de_nome_repetido_some_quando_o_nome_e_alterado()
    {
        await CadastrarAsync("Caneta azul", "2,50");
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("3,00");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();
        var aviso = _pagina.Locator("[data-valmsg-for=Nome]");
        await Expect(aviso).ToHaveTextAsync("Já existe um produto com este nome.");
        await Expect(_pagina.GetByLabel("Nome")).ToHaveAttributeAsync("aria-invalid", "true");

        await _pagina.GetByLabel("Nome").FillAsync("Caneta vermelha");

        await Expect(aviso).ToBeEmptyAsync();
        await Expect(_pagina.GetByLabel("Nome")).Not.ToHaveAttributeAsync("aria-invalid", "true");
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
    [InlineData("12.5055", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("1.2.3", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("1.25,0", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("0.500", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("10,", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("1,234.56", "Valor inválido. Use 12,50 ou 1.250,00.")]
    [InlineData("1000000000000000000000000000000", "Valor unitário está acima do permitido.")]
    [InlineData("99999999999999999", "Valor unitário está acima do permitido.")]
    public async Task Valor_em_forma_ambigua_ou_acima_do_aceito_e_recusado_no_campo(string digitado, string aviso)
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync(digitado);
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync(aviso);
        (await new Cadastro(aplicacao.Banco).QuantidadeDeProdutosAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("10,")]
    [InlineData("1,234.56")]
    public async Task Valor_invalido_avisa_ao_sair_do_campo_sem_salvar(string digitado)
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        var valor = _pagina.GetByLabel("Valor unitário");

        await valor.FillAsync(digitado);
        await valor.BlurAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync("Valor inválido. Use 12,50 ou 1.250,00.");
        await Expect(valor).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Fact]
    public async Task Valor_lido_como_milhar_e_reescrito_ao_sair_do_campo()
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        var valor = _pagina.GetByLabel("Valor unitário");

        await valor.FillAsync("12.505");
        await valor.BlurAsync();

        await Expect(valor).ToHaveValueAsync("12.505,00");
        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToBeEmptyAsync();
    }

    [Fact]
    public async Task Valor_com_mais_digitos_do_que_o_servidor_comporta_enviado_direto_avisa_que_esta_acima_do_permitido()
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("1000000000000000000000000000000");

        await _pagina.Locator("main form").EvaluateAsync("formulario => HTMLFormElement.prototype.submit.call(formulario)");

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync("Valor unitário está acima do permitido.");
        await Expect(_pagina.GetByLabel("Valor unitário")).ToHaveValueAsync("1000000000000000000000000000000");
    }

    [Fact]
    public async Task Valor_recusado_pelo_servidor_volta_com_o_que_foi_digitado()
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("2,5x");

        await _pagina.Locator("main form").EvaluateAsync("formulario => HTMLFormElement.prototype.submit.call(formulario)");

        await Expect(_pagina.Locator("[data-valmsg-for=ValorUnitario]")).ToHaveTextAsync("Valor inválido. Use 12,50 ou 1.250,00.");
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
