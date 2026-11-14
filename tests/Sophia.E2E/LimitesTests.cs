using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class LimitesTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
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
    [InlineData("/Clientes/Editar/abc")]
    [InlineData("/Clientes/Excluir/abc")]
    [InlineData("/Produtos/Editar/abc")]
    [InlineData("/Produtos/Excluir/abc")]
    [InlineData("/Vendas/Detalhe/abc")]
    [InlineData("/Erro?codigo=abc")]
    public async Task Endereco_com_identificador_invalido_mostra_a_pagina_de_erro(string endereco)
    {
        var resposta = await _pagina.GotoAsync(endereco);

        resposta!.Status.Should().Be(400);
        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Algo deu errado");
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
