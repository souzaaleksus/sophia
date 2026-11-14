using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class CaixaNoDesktopTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;
    private Caixa _caixa = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        var cadastro = new Cadastro(aplicacao.Banco);
        await cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
        _caixa = new Caixa(_pagina);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task No_desktop_a_linha_mostra_os_botoes_de_quantidade_e_diminuir_comeca_desabilitado()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");
        var aumentar = BotaoDaLinha(0, "Aumentar a quantidade");
        var diminuir = BotaoDaLinha(0, "Diminuir a quantidade");

        await Expect(aumentar).ToBeVisibleAsync();
        await Expect(diminuir).ToBeVisibleAsync();
        await Expect(diminuir).ToBeDisabledAsync();

        await aumentar.ClickAsync();

        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("2");
        await Expect(diminuir).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Diminuir_pelo_teclado_ate_um_deixa_o_foco_na_quantidade()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        var diminuir = BotaoDaLinha(0, "Diminuir a quantidade");
        await diminuir.FocusAsync();

        await _pagina.Keyboard.PressAsync("Space");

        await Expect(diminuir).ToBeDisabledAsync();
        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("1");
        await Expect(_caixa.QuantidadeDaLinha(0)).ToBeFocusedAsync();
    }

    [Fact]
    public async Task O_valor_unitario_da_linha_tem_o_prefixo_em_reais()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        var valor = _caixa.Linha(0).GetByLabel("Valor unitário");
        await Expect(valor).ToBeVisibleAsync();

        var grupo = valor.Locator("xpath=..");

        await Expect(grupo.GetByText("R$", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Remover_o_produto_avisa_e_desfazer_devolve_a_linha_e_o_total()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "4");
        await _caixa.LancarAsync(1, "Cader", "Caderno 96 folhas", "2");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 47,80");

        await _caixa.Linha(1).GetByRole(AriaRole.Button, new() { Name = "Remover este produto" }).ClickAsync();

        await Expect(_caixa.Itens).ToHaveCountAsync(1);
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 10,00");
        await Expect(_pagina.GetByText("Produto removido.")).ToBeVisibleAsync();
        var desfazer = _pagina.GetByRole(AriaRole.Button, new() { Name = "Desfazer" });
        await Expect(desfazer).ToBeVisibleAsync();

        await desfazer.ClickAsync();

        await Expect(_caixa.Itens).ToHaveCountAsync(2);
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 47,80");
        (await LinhasLancadasAsync()).Should().Contain("Caderno 96 folhas | 18,90 | 2");
    }

    [Fact]
    public async Task O_cliente_escolhido_mostra_as_iniciais_e_o_botao_de_trocar()
    {
        await _pagina.GotoAsync("/Vendas/Nova");

        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");

        var cartao = _pagina.GetByTestId("cliente-escolhido");
        await Expect(cartao.GetByText("MS", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(cartao.GetByRole(AriaRole.Button, new() { Name = "Trocar cliente" })).ToBeVisibleAsync();
    }

    private ILocator BotaoDaLinha(int linha, string nome) =>
        _caixa.Linha(linha).GetByRole(AriaRole.Button, new() { Name = nome, Exact = true });

    private async Task<List<string>> LinhasLancadasAsync()
    {
        var linhas = new List<string>();
        var quantidade = await _caixa.Itens.CountAsync();
        for (var indice = 0; indice < quantidade; indice++)
        {
            var produto = await _caixa.ProdutoDaLinha(indice).InputValueAsync();
            var valor = await _caixa.Linha(indice).GetByLabel("Valor unitário").InputValueAsync();
            var unidades = await _caixa.QuantidadeDaLinha(indice).InputValueAsync();
            linhas.Add($"{produto} | {valor} | {unidades}");
        }

        return linhas;
    }
}
