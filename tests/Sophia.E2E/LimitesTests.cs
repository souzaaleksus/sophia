using System.Globalization;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class LimitesTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private const decimal PrecoAlto = 99_999_999_999.99m;

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
    public async Task Venda_com_mais_unidades_que_cabem_num_inteiro_abre_no_detalhe_e_no_painel()
    {
        var maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await aplicacao.Banco.ExecutarAsync($"""
            INSERT INTO dbo.Produtos (Nome, ValorUnitario)
            SELECT TOP (2200) CONCAT(N'Produto ', FORMAT(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), '0000')), 0.01
            FROM sys.all_objects a CROSS JOIN sys.all_objects b;
            INSERT INTO dbo.Vendas (Data, ClienteId) SELECT '{Cadastro.Hoje:yyyy-MM-dd}', {maria} FROM dbo.Produtos;
            DECLARE @primeiraVenda INT = (SELECT MIN(Id) FROM dbo.Vendas);
            DECLARE @primeiroProduto INT = (SELECT MIN(Id) FROM dbo.Produtos);
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario)
            SELECT @primeiraVenda, Id, 999999, 0.01 FROM dbo.Produtos;
            INSERT INTO dbo.VendaItens (VendaId, ProdutoId, Quantidade, ValorUnitario)
            SELECT Id, @primeiroProduto, 999999, 0.01 FROM dbo.Vendas WHERE Id <> @primeiraVenda;
            """);
        var venda = int.Parse((await aplicacao.Banco.ConsultarAsync("SELECT CAST(MIN(Id) AS VARCHAR(10)) FROM dbo.Vendas")).Single(),
            System.Globalization.CultureInfo.InvariantCulture);

        var detalhe = await _pagina.GotoAsync($"/Vendas/Detalhe/{venda}");
        detalhe!.Status.Should().Be(200);
        await Expect(_pagina.GetByText("2.199.997.800 unidades")).ToBeVisibleAsync();

        var painel = await _pagina.GotoAsync("/");
        painel!.Status.Should().Be(200);
        await Expect(_pagina.GetByTestId("mais-vendido").First).ToContainTextAsync("2.199.997.800 unidades");
    }

    [Theory]
    [InlineData("1500000000", "1000000000", "0,01")]
    [InlineData("99999", "99999", "9.999.999.999.999.999.999.999.999")]
    public async Task Linhas_com_numeros_alem_do_que_a_conta_comporta_voltam_ao_caixa_com_o_erro_e_sem_erro_500(string primeira, string segunda, string valor)
    {
        await _cadastro.ProdutoAsync("Caneta azul", 0.01m);
        await _cadastro.ProdutoAsync("Lápis preto", 0.01m);
        var caixa = new Caixa(_pagina);
        await _pagina.GotoAsync("/Vendas/Nova");
        await caixa.LancarAsync(0, "Caneta", "Caneta azul", primeira);
        await caixa.LancarAsync(1, "Lápis", "Lápis preto", segunda);
        await caixa.Linha(0).GetByLabel("Valor unitário").FillAsync(valor);
        await caixa.Linha(1).GetByLabel("Valor unitário").FillAsync(valor);

        var resposta = await _pagina.RunAndWaitForResponseAsync(
            () => caixa.Finalizar.ClickAsync(),
            r => r.Request.Method == "POST");

        resposta.Status.Should().Be(200);
        await Expect(_pagina.Locator("[data-valmsg-for=ClienteId]")).ToHaveTextAsync("Selecione o cliente.");
        await Expect(caixa.Itens).ToHaveCountAsync(2);
    }

    [Theory]
    [InlineData(360, 740)]
    [InlineData(1280, 860)]
    public async Task O_maior_total_possivel_cabe_no_caixa_sem_rolar_para_o_lado_nem_sobrepor(int largura, int altura)
    {
        foreach (var nome in new[] { "Notebook de ouro", "Relógio de ouro", "Anel de ouro" })
        {
            await _cadastro.ProdutoAsync(nome, PrecoAlto);
        }

        var pagina = await aplicacao.NovaPaginaAsync(largura, altura);
        var caixa = new Caixa(pagina);
        await pagina.GotoAsync("/Vendas/Nova");
        await caixa.LancarAsync(0, "Notebook", "Notebook de ouro", "99999");
        await caixa.LancarAsync(1, "Relógio", "Relógio de ouro", "99999");
        await caixa.LancarAsync(2, "Anel", "Anel de ouro", "99999");
        await Expect(caixa.Total).ToHaveTextAsync("R$ 29.999.699.999.997.000,03");

        (await Medidas.RolagemLateralAsync(pagina)).Should().BeEmpty($"o caixa em {largura} px não pode rolar para o lado");
        (await Medidas.TransbordamentosAsync(pagina, ".visor-valor, .visor-detalhe, .caixa-subtotal, [data-testid=subtotal]"))
            .Should().BeEmpty($"em {largura} px o total e os subtotais aparecem inteiros");
        (await Medidas.SobreposicoesAsync(pagina, "[data-item], .caixa-fechamento"))
            .Should().BeEmpty($"em {largura} px nada da linha nem do fechamento fica por cima de outra coisa");
        await pagina.Context.CloseAsync();
    }

    [Theory]
    [InlineData(360, 740, "/")]
    [InlineData(1280, 860, "/")]
    [InlineData(360, 740, "/Vendas")]
    [InlineData(1280, 860, "/Vendas")]
    [InlineData(360, 740, "/Vendas/Detalhe/{venda}")]
    [InlineData(1280, 860, "/Vendas/Detalhe/{venda}")]
    public async Task O_maior_total_possivel_cabe_no_painel_e_nas_vendas_sem_rolar_para_o_lado_nem_cortar(int largura, int altura, string endereco)
    {
        var maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var venda = 0;
        foreach (var nome in new[] { "Notebook de ouro", "Relógio de ouro", "Anel de ouro" })
        {
            var produto = await _cadastro.ProdutoAsync(nome, PrecoAlto);
            venda = await _cadastro.VendaAsync(Cadastro.Hoje, maria, new DadosItemVenda(produto, 99_999, PrecoAlto));
        }

        var pagina = await aplicacao.NovaPaginaAsync(largura, altura);
        await pagina.GotoAsync(endereco.Replace("{venda}", venda.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
        await Expect(pagina.Locator("main")).ToContainTextAsync("9.999.899.999.999.000,01");

        (await Medidas.RolagemLateralAsync(pagina)).Should().BeEmpty($"{endereco} em {largura} px não pode rolar para o lado");
        (await Medidas.RoladoresLateraisAsync(pagina)).Should().BeEmpty($"{endereco} em {largura} px não pode esconder o total numa rolagem");
        (await Medidas.TransbordamentosAsync(pagina, ".visor-valor, .visor-rotulo, .visor-detalhe, .indicador-valor, .indicador-rotulo, .grafico-resumo, .ranking-linha, .barra-do-dia.hoje .barra-valor, td.valor, .cupom-total, .cupom-total strong"))
            .Should().BeEmpty($"em {largura} px os valores de {endereco} aparecem inteiros");
        (await Medidas.SobreposicoesAsync(pagina, ".indicadores, .cabecalho-do-grafico, .ranking-linha, .cupom-total"))
            .Should().BeEmpty($"em {largura} px nada de {endereco} fica por cima de outra coisa");
        await pagina.Context.CloseAsync();
    }

    [Theory]
    [InlineData("/Clientes/Editar/abc")]
    [InlineData("/Clientes/Excluir/abc")]
    [InlineData("/Produtos/Editar/abc")]
    [InlineData("/Produtos/Excluir/abc")]
    [InlineData("/Vendas/Detalhe/abc")]
    public async Task Endereco_com_identificador_invalido_mostra_a_pagina_de_erro(string endereco)
    {
        var resposta = await _pagina.GotoAsync(endereco);

        resposta!.Status.Should().Be(400);
        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Algo deu errado");
        await Expect(_pagina.Locator("main")).ToContainTextAsync("O endereço tem um valor que não foi entendido. Volte à lista e tente de novo.");
        await Expect(_pagina.Locator("main")).Not.ToContainTextAsync("em instantes");
    }

    [Fact]
    public async Task Endereco_invalido_fora_das_listas_manda_voltar_ao_painel_e_nao_a_lista()
    {
        var resposta = await _pagina.GotoAsync("/Erro?codigo=abc");

        resposta!.Status.Should().Be(400);
        await Expect(_pagina.Locator("main")).ToContainTextAsync("O endereço tem um valor que não foi entendido. Volte ao painel e tente de novo.");
        await Expect(_pagina.Locator("main")).Not.ToContainTextAsync("lista");
        await Expect(_pagina.Locator("main").GetByRole(AriaRole.Link, new() { Name = "Ir para o painel", Exact = true })).ToHaveAttributeAsync("href", "/");
    }

    [Fact]
    public async Task Envio_de_formulario_que_nao_passa_na_conferencia_pede_para_recarregar_e_nao_fala_do_endereco()
    {
        await _pagina.GotoAsync("/Produtos/Novo");
        await _pagina.GetByLabel("Nome").FillAsync("Caneta azul");
        await _pagina.GetByLabel("Valor unitário").FillAsync("2,50");
        await _pagina.Locator("input[name=__RequestVerificationToken]").EvaluateAsync("campo => campo.value = 'token-de-outra-sessao'");

        var resposta = await _pagina.RunAndWaitForResponseAsync(
            () => _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync(),
            resposta => resposta.Request.Method == "POST");

        resposta.Status.Should().Be(400);
        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Algo deu errado");
        await Expect(_pagina.Locator("main")).ToContainTextAsync("Não foi possível conferir o envio do formulário. Recarregue a página e tente de novo.");
        await Expect(_pagina.Locator("main")).Not.ToContainTextAsync("endereço");
        await Expect(_pagina.Locator("main").GetByRole(AriaRole.Link, new() { Name = "Ir para os produtos", Exact = true })).ToHaveAttributeAsync("href", "/Produtos");
        (await new Cadastro(aplicacao.Banco).QuantidadeDeProdutosAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("/Clientes/Editar/999999", "Ir para os clientes", "/Clientes")]
    [InlineData("/Clientes/Editar/abc", "Ir para os clientes", "/Clientes")]
    [InlineData("/Produtos/Excluir/999999", "Ir para os produtos", "/Produtos")]
    [InlineData("/Vendas/Detalhe/999999", "Ir para as vendas", "/Vendas")]
    [InlineData("/Vendas?pagina=abc", "Ir para as vendas", "/Vendas")]
    [InlineData("/NaoExiste", "Ir para o painel", "/")]
    public async Task A_pagina_de_erro_oferece_voltar_para_a_area_do_endereco(string endereco, string botao, string destino)
    {
        await _pagina.GotoAsync(endereco);

        var link = _pagina.Locator("main").GetByRole(AriaRole.Link, new() { Name = botao, Exact = true });
        await Expect(link).ToHaveAttributeAsync("href", destino);
        await Expect(_pagina.Locator("main").GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Envio_com_token_invalido_mostra_a_pagina_de_erro_e_nao_uma_tela_em_branco()
    {
        using var http = new HttpClient();
        using var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Nome"] = "Caneta azul",
            ["ValorUnitario"] = "2,50",
            ["__RequestVerificationToken"] = "token-de-outra-sessao",
        });

        using var resposta = await http.PostAsync(new Uri($"{aplicacao.Endereco}/Produtos/Novo"), corpo);

        ((int)resposta.StatusCode).Should().Be(400);
        (await resposta.Content.ReadAsStringAsync()).Should().Contain("<h1");
    }
}
