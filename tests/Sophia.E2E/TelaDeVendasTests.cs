using System.Globalization;
using Microsoft.Playwright;
using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.E2E.Apoio;
using Sophia.Infrastructure;
using Sophia.Infrastructure.Persistencia;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class TelaDeVendasTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        await PrepararCadastrosAsync();
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task A_pagina_inicial_leva_a_nova_venda_com_a_data_de_hoje()
    {
        await _pagina.GotoAsync("/");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Nova venda", Exact = true }).ClickAsync();

        var hoje = new RelogioDoSistema(TimeProvider.System, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).Hoje;
        await Expect(_pagina.GetByLabel("Data da venda")).ToHaveValueAsync(hoje.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Vendas")]
    public async Task A_tela_tem_um_so_botao_de_nova_venda(string endereco)
    {
        await _pagina.GotoAsync(endereco);

        await Expect(_pagina.GetByRole(AriaRole.Link, new() { Name = "Nova venda", Exact = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Registra_uma_venda_com_varios_produtos_vendo_os_totais()
    {
        await _pagina.GotoAsync("/Vendas/Nova");

        await EscolherClienteAsync("529.982", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "10");
        await Expect(Linha(0).GetByTestId("subtotal")).ToHaveTextAsync("R$ 25,00");
        await Linha(0).GetByLabel("Quantidade", new() { Exact = true }).PressAsync("Enter");
        await LancarAsync(1, "Cader", "Caderno 96 folhas", "2");

        await Expect(Linha(1).GetByTestId("subtotal")).ToHaveTextAsync("R$ 37,80");
        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 62,80");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");
        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Venda nº");
        await Expect(_pagina.GetByTestId("cliente-da-venda")).ToContainTextAsync("Maria Santos");
        await Expect(_pagina.GetByTestId("cliente-da-venda")).ToContainTextAsync("529.982.247-25");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" })).ToContainTextAsync("R$ 25,00");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caderno 96 folhas" })).ToContainTextAsync("R$ 37,80");
        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 62,80");
        await Expect(_pagina.GetByRole(AriaRole.Link, new() { Name = "Nova venda", Exact = true })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Encontra_o_cliente_pelo_nome()
    {
        await _pagina.GotoAsync("/Vendas/Nova");

        await EscolherClienteAsync("lima", "João Lima");

        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToContainTextAsync("111.444.777-35");
    }

    [Fact]
    public async Task O_valor_unitario_vem_do_produto_e_pode_ser_ajustado()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "4");

        await Expect(Linha(0).GetByLabel("Valor unitário")).ToHaveValueAsync("2,50");
        await Linha(0).GetByLabel("Valor unitário").FillAsync("2,00");

        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 8,00");
    }

    [Fact]
    public async Task Avisa_quando_a_venda_nao_tem_produtos()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Alert)).ToContainTextAsync("Lance ao menos um produto na venda.");
    }

    [Fact]
    public async Task Avisa_quando_a_venda_nao_tem_cliente_e_mantem_os_itens_lancados()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await LancarAsync(0, "Caneta", "Caneta azul", "3");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=ClienteId]")).ToHaveTextAsync("Selecione o cliente.");
        await Expect(ProdutoDaLinha(0)).ToHaveValueAsync("Caneta azul");
        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 7,50");
    }

    [Fact]
    public async Task Produto_excluido_antes_de_finalizar_e_apontado_na_linha_do_item()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "1");
        await LancarAsync(1, "Cader", "Caderno 96 folhas", "1");
        await aplicacao.Banco.ExecutarAsync("DELETE FROM dbo.Produtos WHERE Nome = N'Caderno 96 folhas'");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for='Itens[1].ProdutoId']")).ToHaveTextAsync("Produto não encontrado.");
        await Expect(_pagina.Locator("[data-valmsg-for='Itens[0].ProdutoId']")).ToBeEmptyAsync();
    }

    [Fact]
    public async Task Lista_a_venda_registrada()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "2");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Finalizar venda" }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");

        await _pagina.GotoAsync("/Vendas");

        var linha = _pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" });
        await Expect(linha).ToContainTextAsync("529.982.247-25");
        await Expect(linha).ToContainTextAsync("R$ 5,00");
    }

    [Fact]
    public async Task F2_leva_a_busca_de_cliente()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _pagina.GetByLabel("Data da venda").FocusAsync();

        await _pagina.Keyboard.PressAsync("F2");

        await Expect(ComboboxDeCliente()).ToBeFocusedAsync();
    }

    [Fact]
    public async Task F2_com_cliente_escolhido_volta_a_buscar_outro_sem_perder_os_itens()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "3");

        await _pagina.Keyboard.PressAsync("F2");

        await Expect(ComboboxDeCliente()).ToBeFocusedAsync();
        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToBeHiddenAsync();
        await EscolherClienteAsync("lima", "João Lima");
        await Expect(ProdutoDaLinha(0)).ToHaveValueAsync("Caneta azul");
    }

    [Fact]
    public async Task F4_abre_uma_nova_linha_de_produto_com_o_foco_nela()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "3");

        await _pagina.Keyboard.PressAsync("F4");

        await Expect(_pagina.GetByTestId("item-da-venda")).ToHaveCountAsync(2);
        await Expect(ProdutoDaLinha(1)).ToBeFocusedAsync();
    }

    [Fact]
    public async Task F4_aproveita_a_linha_vazia_em_vez_de_abrir_outra()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _pagina.GetByLabel("Data da venda").FocusAsync();

        await _pagina.Keyboard.PressAsync("F4");

        await Expect(_pagina.GetByTestId("item-da-venda")).ToHaveCountAsync(1);
        await Expect(ProdutoDaLinha(0)).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Ctrl_Enter_finaliza_a_venda()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "2");

        await _pagina.Keyboard.PressAsync("Control+Enter");

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");
    }

    [Fact]
    public async Task Esc_fecha_a_lista_de_sugestoes()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await ComboboxDeCliente().FillAsync("Maria");
        await Expect(_pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = "Maria Santos" })).ToBeVisibleAsync();

        await ComboboxDeCliente().PressAsync("Escape");

        await Expect(_pagina.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        await Expect(ComboboxDeCliente()).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task O_visor_mostra_o_total_os_itens_e_as_unidades_lancadas()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await EscolherClienteAsync("Maria", "Maria Santos");
        await LancarAsync(0, "Caneta", "Caneta azul", "10");
        await Expect(_pagina.GetByTestId("itens-da-venda")).ToHaveTextAsync("1 item");
        await Expect(_pagina.GetByTestId("unidades-da-venda")).ToHaveTextAsync("10 unidades");

        await LancarAsync(1, "Cader", "Caderno 96 folhas", "2");

        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 62,80");
        await Expect(_pagina.GetByTestId("itens-da-venda")).ToHaveTextAsync("2 itens");
        await Expect(_pagina.GetByTestId("unidades-da-venda")).ToHaveTextAsync("12 unidades");
    }

    private ILocator Linha(int indice) => _pagina.GetByTestId("item-da-venda").Nth(indice);

    private ILocator ComboboxDeCliente() => _pagina.GetByRole(AriaRole.Combobox, new() { Name = "Cliente", Exact = true });

    private ILocator ProdutoDaLinha(int indice) => Linha(indice).GetByRole(AriaRole.Combobox, new() { Name = "Produto", Exact = true });

    private async Task EscolherClienteAsync(string termo, string nome)
    {
        await ComboboxDeCliente().FillAsync(termo);
        await _pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = nome }).ClickAsync();
        await Expect(_pagina.GetByTestId("cliente-escolhido")).ToContainTextAsync(nome);
    }

    private async Task LancarAsync(int linha, string termo, string produto, string quantidade)
    {
        if (await _pagina.GetByTestId("item-da-venda").CountAsync() <= linha)
        {
            await _pagina.GetByRole(AriaRole.Button, new() { Name = "Adicionar produto" }).ClickAsync();
        }

        await ProdutoDaLinha(linha).FillAsync(termo);
        await _pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = produto }).ClickAsync();
        await Linha(linha).GetByLabel("Quantidade", new() { Exact = true }).FillAsync(quantidade);
    }

    private async Task PrepararCadastrosAsync()
    {
        await using var contexto = aplicacao.Banco.NovoContexto();
        var unidade = new UnidadeDeTrabalho(contexto);
        IRelogio relogio = new RelogioDoSistema(TimeProvider.System, TimeZoneInfo.Utc);
        var clientes = new ClienteServico(new ClienteRepositorio(contexto), unidade, relogio, new Sophia.Application.ParametrosDoSophia());
        var produtos = new ProdutoServico(new ProdutoRepositorio(contexto), unidade, new Sophia.Application.ParametrosDoSophia());

        await clientes.CadastrarAsync(new DadosCliente("529.982.247-25", "Maria Santos", null, null, null));
        await clientes.CadastrarAsync(new DadosCliente("111.444.777-35", "João Lima", null, null, null));
        await produtos.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        await produtos.CadastrarAsync(new DadosProduto("Caderno 96 folhas", 18.90m));
    }
}
