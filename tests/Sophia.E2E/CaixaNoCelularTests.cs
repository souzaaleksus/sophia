using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class CaixaNoCelularTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private const int Largura = 360;
    private const int Altura = 740;

    private IPage _pagina = null!;
    private Caixa _caixa = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        var cadastro = new Cadastro(aplicacao.Banco);
        await cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        await cadastro.ProdutoAsync("Lápis preto", 1.50m);
        _pagina = await aplicacao.NovaPaginaAsync(Largura, Altura, toque: true);
        _caixa = new Caixa(_pagina);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task No_celular_o_produto_novo_nao_fica_escondido_atras_da_barra_do_total()
    {
        var celular = await aplicacao.NovaPaginaAsync(375, 667, toque: true);
        try
        {
            await celular.GotoAsync("/Vendas/Nova");
            var adicionar = celular.GetByRole(AriaRole.Button, new() { Name = "Adicionar produto" });
            for (var vez = 1; vez <= 4; vez++)
            {
                await adicionar.ClickAsync();
                var produto = celular.GetByTestId("item-da-venda").Last.GetByRole(AriaRole.Combobox, new() { Name = "Produto", Exact = true });
                await Expect(produto).ToBeFocusedAsync();
                await Task.Delay(500);
                (await Medidas.QuemOcupaOCentroAsync(produto, aceitaDescendente: false))
                    .Should().BeEmpty($"o produto da linha {vez + 1} precisa ficar à vista depois de adicionado");
            }
        }
        finally
        {
            await celular.Context.CloseAsync();
        }
    }

    [Theory]
    [InlineData(375)]
    [InlineData(360)]
    [InlineData(768)]
    public async Task Valor_e_quantidade_da_linha_aparecem_inteiros_mesmo_com_a_barra_de_rolagem(int largura)
    {
        var celular = await aplicacao.NovaPaginaAsync(largura, 667);
        try
        {
            var caixa = new Caixa(celular);
            await celular.GotoAsync("/Vendas/Nova");
            await caixa.LancarAsync(0, "Caneta", "Caneta azul", "99999");
            var valor = caixa.Linha(0).GetByLabel("Valor unitário");
            await valor.FillAsync("12.345,67");

            foreach (var campo in new[] { valor, caixa.QuantidadeDaLinha(0) })
            {
                var cortado = await campo.EvaluateAsync<int>("campo => { campo.scrollLeft = 0; return campo.scrollWidth - campo.clientWidth; }");
                cortado.Should().BeLessThanOrEqualTo(0, $"o que foi digitado em {await campo.GetAttributeAsync("data-campo")} aparece inteiro em {largura} px");
            }
        }
        finally
        {
            await celular.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task Sem_rolar_o_total_e_o_botao_de_finalizar_aparecem_inteiros()
    {
        await _pagina.GotoAsync("/Vendas/Nova");

        await DeveEstarInteiroNaJanelaAsync(_caixa.Total, "o total da venda");
        await DeveEstarInteiroNaJanelaAsync(_caixa.Finalizar, "o botão Finalizar venda");
    }

    [Fact]
    public async Task Com_tres_produtos_lancados_o_total_e_o_botao_de_finalizar_continuam_na_janela()
    {
        await _pagina.GotoAsync("/Vendas/Nova");

        await LancarTresProdutosAsync();

        await Expect(_caixa.Total).ToHaveTextAsync("R$ 49,30");
        await DeveEstarInteiroNaJanelaAsync(_caixa.Total, "o total da venda");
        await DeveEstarInteiroNaJanelaAsync(_caixa.Finalizar, "o botão Finalizar venda");
    }

    [Fact]
    public async Task O_ultimo_produto_lancado_nao_fica_escondido_atras_da_barra_do_total()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await LancarTresProdutosAsync();

        var quantidade = _caixa.QuantidadeDaLinha(2);
        await quantidade.ScrollIntoViewIfNeededAsync();

        var quemOcupa = await Medidas.QuemOcupaOCentroAsync(quantidade, aceitaDescendente: false);
        quemOcupa.Should().BeEmpty("a quantidade do último produto precisa estar à vista, não coberta");
    }

    [Fact]
    public async Task Os_botoes_de_quantidade_somam_e_subtraem_sem_descer_de_um()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        var aumentar = BotaoDaLinha(0, "Aumentar a quantidade");
        var diminuir = BotaoDaLinha(0, "Diminuir a quantidade");
        await Expect(aumentar).ToBeVisibleAsync();
        await Expect(diminuir).ToBeVisibleAsync();

        await aumentar.ClickAsync();

        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("3");
        await Expect(_caixa.Linha(0).GetByTestId("subtotal")).ToHaveTextAsync("R$ 7,50");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 7,50");

        await diminuir.ClickAsync();
        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("2");
        await diminuir.ClickAsync();
        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("1");
        await diminuir.EvaluateAsync("botao => botao.click()");

        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("1");
        await Expect(_caixa.Linha(0).GetByTestId("subtotal")).ToHaveTextAsync("R$ 2,50");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 2,50");
    }

    [Fact]
    public async Task Toda_linha_de_produto_tem_os_botoes_de_quantidade()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Adicionar produto" }).ClickAsync();

        await Expect(_caixa.Itens).ToHaveCountAsync(2);
        for (var linha = 0; linha < 2; linha++)
        {
            await Expect(BotaoDaLinha(linha, "Aumentar a quantidade")).ToHaveCountAsync(1);
            await Expect(BotaoDaLinha(linha, "Diminuir a quantidade")).ToHaveCountAsync(1);
        }
    }

    private ILocator BotaoDaLinha(int linha, string nome) =>
        _caixa.Linha(linha).GetByRole(AriaRole.Button, new() { Name = nome, Exact = true });

    private async Task LancarTresProdutosAsync()
    {
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "4");
        await _caixa.LancarAsync(1, "Cader", "Caderno 96 folhas", "2");
        await _caixa.LancarAsync(2, "Lápis", "Lápis preto", "1");
    }

    private static async Task DeveEstarInteiroNaJanelaAsync(ILocator elemento, string descricao)
    {
        await Expect(elemento).ToBeVisibleAsync();
        var caixa = await elemento.BoundingBoxAsync();
        caixa.Should().NotBeNull($"{descricao} precisa ser desenhado");
        caixa!.Y.Should().BeGreaterThanOrEqualTo(0, $"{descricao} começa dentro da janela");
        (caixa.Y + caixa.Height).Should().BeLessThanOrEqualTo(Altura, $"{descricao} termina dentro da janela");
        caixa.X.Should().BeGreaterThanOrEqualTo(0, $"{descricao} começa dentro da largura");
        (caixa.X + caixa.Width).Should().BeLessThanOrEqualTo(Largura, $"{descricao} termina dentro da largura");
    }
}
