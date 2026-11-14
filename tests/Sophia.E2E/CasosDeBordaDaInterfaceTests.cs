using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class CasosDeBordaDaInterfaceTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;
    private Caixa _caixa = null!;
    private Cadastro _cadastro = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Busca_abandonada_nao_abre_a_lista_quando_a_resposta_chega(bool apagando)
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await Rede.DeixarLentaAsync(_pagina, TimeSpan.FromMilliseconds(900));

        await _caixa.ComboboxDeCliente.FillAsync("Ma");
        await Task.Delay(400);
        if (apagando)
        {
            await _caixa.ComboboxDeCliente.FillAsync(string.Empty);
        }
        else
        {
            await _caixa.ComboboxDeCliente.PressAsync("Escape");
        }

        await Task.Delay(1500);
        await Expect(_pagina.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await Expect(_caixa.ComboboxDeCliente).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task O_atalho_para_cadastrar_cliente_e_alcancado_pelo_teclado()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.ComboboxDeCliente.FillAsync("zzzz");
        var cadastrar = _pagina.GetByRole(AriaRole.Link, new() { Name = "Cadastrar novo cliente" });
        await Expect(cadastrar).ToBeVisibleAsync();

        await _pagina.Keyboard.PressAsync("Tab");
        await Task.Delay(400);

        await Expect(cadastrar).ToBeVisibleAsync();
        await Expect(cadastrar).ToBeFocusedAsync();
    }

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

    [Fact]
    public async Task Apagar_um_separador_da_mascara_apaga_o_digito_antes_dele_e_mantem_o_cursor()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");
        await cpf.PressSequentiallyAsync("52998224725");
        await Expect(cpf).ToHaveValueAsync("529.982.247-25");

        await PosicionarAsync(cpf, 4);
        await cpf.PressAsync("Backspace");

        await Expect(cpf).ToHaveValueAsync("529.822.472-5");
        (await CursorAsync(cpf)).Should().Be(2);
    }

    [Fact]
    public async Task Digitar_no_meio_da_mascara_mantem_o_cursor_e_campo_cheio_nao_perde_digito()
    {
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");
        await cpf.PressSequentiallyAsync("5299822472");

        await PosicionarAsync(cpf, 4);
        await cpf.PressSequentiallyAsync("1");

        await Expect(cpf).ToHaveValueAsync("529.198.224-72");
        (await CursorAsync(cpf)).Should().Be(5);

        await PosicionarAsync(cpf, 1);
        await cpf.PressSequentiallyAsync("7");

        await Expect(cpf).ToHaveValueAsync("529.198.224-72");
        (await CursorAsync(cpf)).Should().Be(1);
    }

    [Theory]
    [InlineData("Telefone", "+55 11 98765-4321", "(11) 98765-4321")]
    [InlineData("Telefone", "+55 (11) 3456-7890", "(11) 3456-7890")]
    [InlineData("Data de nascimento", "1990-05-12", "12/05/1990")]
    public async Task Valor_colado_ou_preenchido_pelo_navegador_e_entendido_pela_mascara(string campo, string colado, string esperado)
    {
        await _pagina.GotoAsync("/Clientes/Novo");

        await _pagina.GetByLabel(campo).FillAsync(colado);

        await Expect(_pagina.GetByLabel(campo)).ToHaveValueAsync(esperado);
    }

    [Fact]
    public async Task Enquanto_a_exclusao_e_enviada_o_dialogo_nao_fecha()
    {
        await _pagina.GotoAsync("/Clientes");
        await _pagina.EvaluateAsync("() => window.addEventListener('submit', evento => evento.preventDefault())");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos" }).ClickAsync();
        var dialogo = _pagina.GetByRole(AriaRole.Dialog);
        var excluir = dialogo.Locator("[type=submit]");

        await excluir.ClickAsync();
        await Expect(excluir).ToBeDisabledAsync();
        await _pagina.Keyboard.PressAsync("Escape");

        await Expect(dialogo).ToBeVisibleAsync();
    }

    private static async Task PosicionarAsync(ILocator campo, int posicao) =>
        await campo.EvaluateAsync("(campo, posicao) => { campo.focus(); campo.setSelectionRange(posicao, posicao); }", posicao);

    private static Task<int> CursorAsync(ILocator campo) =>
        campo.EvaluateAsync<int>("campo => campo.selectionStart");
}
