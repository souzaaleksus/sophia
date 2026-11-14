using System.Text.RegularExpressions;
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
    public async Task Trocar_o_produto_de_uma_linha_por_um_ja_lancado_soma_na_existente_e_libera_a_linha()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");
        await _caixa.LancarAsync(1, "Caderno", "Caderno 96 folhas", "3");

        await _caixa.ProdutoDaLinha(1).FillAsync("Caneta");
        await _pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = "Caneta azul" }).ClickAsync();

        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("2");
        await Expect(_caixa.ProdutoDaLinha(1)).ToHaveValueAsync(string.Empty);
        await Expect(_caixa.Linha(1).GetByLabel("Valor unitário")).ToHaveValueAsync(string.Empty);
        await Expect(_caixa.QuantidadeDaLinha(1)).ToHaveValueAsync(string.Empty);
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 5,00");
    }

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

    [Fact]
    public async Task Valor_com_ponto_decimal_e_lido_como_centavos_ao_sair_do_campo()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");
        var valor = _caixa.Linha(0).GetByLabel("Valor unitário");

        await valor.FillAsync("12.50");
        await valor.BlurAsync();

        await Expect(valor).ToHaveValueAsync("12,50");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 12,50");
    }

    [Fact]
    public async Task Valor_com_ponto_decimal_enviado_direto_grava_a_venda_com_centavos()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        var valor = _caixa.Linha(0).GetByLabel("Valor unitário");
        await valor.FillAsync("12.50");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 25,00");

        await valor.PressAsync("Control+Enter");

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" })).ToContainTextAsync("R$ 12,50");
        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 25,00");
    }

    [Fact]
    public async Task Escolher_o_cliente_e_lancar_o_produto_tiram_os_erros_resolvidos_sem_novo_envio()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.Finalizar.ClickAsync();
        var erroDoCliente = _pagina.Locator("[data-valmsg-for=ClienteId]");
        var erroDosItens = _pagina.GetByText("Lance ao menos um produto na venda.");
        await Expect(erroDoCliente).ToHaveTextAsync("Selecione o cliente.");
        await Expect(erroDosItens).ToBeVisibleAsync();

        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");

        await Expect(erroDoCliente).ToBeEmptyAsync();
        await Expect(erroDosItens).ToBeVisibleAsync();

        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");

        await Expect(erroDosItens).ToBeHiddenAsync();
        await Expect(_pagina.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Corrigir_a_quantidade_da_linha_tira_o_erro_e_a_marca_de_invalido()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", string.Empty);
        await _caixa.Finalizar.ClickAsync();
        var erroDaQuantidade = _caixa.Linha(0).Locator("[data-erro=Quantidade]");
        await Expect(erroDaQuantidade).Not.ToBeEmptyAsync();
        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveAttributeAsync("aria-invalid", "true");
        await _caixa.ProntoAsync();

        await _caixa.QuantidadeDaLinha(0).FillAsync("2");

        await Expect(erroDaQuantidade).ToBeEmptyAsync();
        await Expect(_caixa.QuantidadeDaLinha(0)).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(_caixa.QuantidadeDaLinha(0)).Not.ToHaveAttributeAsync("aria-describedby", new Regex("erro-"));
    }

    [Fact]
    public async Task Lancar_de_novo_um_produto_ja_lancado_avisa_a_soma_e_realca_a_linha()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "1");
        await _caixa.LancarAsync(1, "Cader", "Caderno 96 folhas", "1");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Adicionar produto" }).ClickAsync();

        await _caixa.ProdutoDaLinha(2).FillAsync("Caneta");
        await _pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = "Caneta azul" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status).Filter(new() { HasText = "já estava" }))
            .ToHaveTextAsync("Caneta azul já estava na venda: quantidade somada na linha 1.");
        await Expect(_caixa.Linha(0)).ToHaveClassAsync(new Regex("caixa-item-novo"));
        await Expect(_caixa.QuantidadeDaLinha(0)).ToHaveValueAsync("2");
    }

    [Fact]
    public async Task Esc_na_busca_aberta_pelo_F2_devolve_o_cliente_escolhido()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _pagina.Keyboard.PressAsync("F2");
        await Expect(_caixa.ComboboxDeCliente).ToBeFocusedAsync();

        await _caixa.ComboboxDeCliente.PressAsync("Escape");

        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToBeVisibleAsync();
        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToContainTextAsync("Maria Santos");
        await Expect(_pagina.Locator("input[name=ClienteId]")).Not.ToHaveValueAsync(string.Empty);
        await Expect(_pagina.GetByRole(AriaRole.Button, new() { Name = "Trocar cliente" })).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Sair_da_busca_aberta_pelo_F2_sem_escolher_outro_devolve_o_cliente_escolhido()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _pagina.Keyboard.PressAsync("F2");
        await _caixa.ComboboxDeCliente.FillAsync("Cader");

        await _pagina.GetByLabel("Data da venda").ClickAsync();

        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToContainTextAsync("Maria Santos");
        await Expect(_pagina.Locator("input[name=ClienteId]")).Not.ToHaveValueAsync(string.Empty);
        await Expect(_pagina.GetByLabel("Data da venda")).ToBeFocusedAsync();
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
