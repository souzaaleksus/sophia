using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class DialogoDeExclusaoTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
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
    public async Task Enquanto_a_exclusao_e_enviada_o_dialogo_nao_fecha()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
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

    private ILocator Dialogo => _pagina.GetByRole(AriaRole.Dialog);

    [Fact]
    public async Task Excluir_o_cliente_abre_o_dialogo_com_o_nome_e_o_foco_em_cancelar()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Clientes");

        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos" }).ClickAsync();

        await Expect(Dialogo).ToBeVisibleAsync();
        await Expect(Dialogo).ToContainTextAsync("Excluir cliente?");
        await Expect(Dialogo).ToContainTextAsync("Maria Santos");
        await Expect(Dialogo.GetByRole(AriaRole.Button, new() { Name = "Cancelar" })).ToBeFocusedAsync();
        await Expect(_pagina).ToHaveURLAsync(ListaDeClientes());
    }

    [Fact]
    public async Task Esc_fecha_o_dialogo_e_devolve_o_foco_ao_link()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Clientes");
        var excluir = _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos" });
        await excluir.ClickAsync();
        await Expect(Dialogo).ToBeVisibleAsync();

        await _pagina.Keyboard.PressAsync("Escape");

        await Expect(Dialogo).ToBeHiddenAsync();
        await Expect(excluir).ToBeFocusedAsync();
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Confirmar_no_dialogo_exclui_o_cliente()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _cadastro.ClienteAsync("111.444.777-35", "João Lima");
        await _pagina.GotoAsync("/Clientes");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos" }).ClickAsync();
        await Expect(Dialogo).ToBeVisibleAsync();

        await Dialogo.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente excluído.");
        await Expect(_pagina).ToHaveURLAsync(ListaDeClientes());
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" })).ToHaveCountAsync(0);
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "João Lima" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task O_cliente_com_vendas_nao_e_excluido_pelo_dialogo()
    {
        var cliente = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.VendaAsync(Cadastro.Hoje, cliente, new DadosItemVenda(produto, 1, 2.50m));
        await _pagina.GotoAsync("/Clientes");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Maria Santos" }).ClickAsync();
        await Expect(Dialogo).ToBeVisibleAsync();

        await Dialogo.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();

        await Expect(_pagina.GetByText("Este cliente possui vendas e não pode ser excluído.")).ToBeVisibleAsync();
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Excluir_o_produto_abre_o_dialogo_com_o_nome_e_o_foco_em_cancelar()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _pagina.GotoAsync("/Produtos");

        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Caneta azul" }).ClickAsync();

        await Expect(Dialogo).ToBeVisibleAsync();
        await Expect(Dialogo).ToContainTextAsync("Excluir produto?");
        await Expect(Dialogo).ToContainTextAsync("Caneta azul");
        await Expect(Dialogo.GetByRole(AriaRole.Button, new() { Name = "Cancelar" })).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Confirmar_no_dialogo_exclui_o_produto()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        await _pagina.GotoAsync("/Produtos");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Caneta azul" }).ClickAsync();
        await Expect(Dialogo).ToBeVisibleAsync();

        await Dialogo.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto excluído.");
        await Expect(_pagina).ToHaveURLAsync(ListaDeProdutos());
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" })).ToHaveCountAsync(0);
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caderno 96 folhas" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task O_produto_ja_vendido_nao_e_excluido_pelo_dialogo()
    {
        var cliente = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.VendaAsync(Cadastro.Hoje, cliente, new DadosItemVenda(produto, 1, 2.50m));
        await _pagina.GotoAsync("/Produtos");
        await _pagina.GetByRole(AriaRole.Link, new() { Name = "Excluir Caneta azul" }).ClickAsync();
        await Expect(Dialogo).ToBeVisibleAsync();

        await Dialogo.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();

        await Expect(_pagina.GetByText("Este produto já foi vendido e não pode ser excluído.")).ToBeVisibleAsync();
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Caneta azul" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task A_pagina_de_exclusao_do_cliente_continua_acessivel_pelo_endereco()
    {
        var cliente = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");

        await _pagina.GotoAsync($"/Clientes/Excluir/{cliente}");

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Excluir cliente?");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Cliente excluído.");
    }

    [Fact]
    public async Task A_pagina_de_exclusao_do_produto_continua_acessivel_pelo_endereco()
    {
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);

        await _pagina.GotoAsync($"/Produtos/Excluir/{produto}");

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Excluir produto?");
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Excluir", Exact = true }).ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Produto excluído.");
    }

    [GeneratedRegex(@"/Clientes(\?.*)?$")]
    private static partial Regex ListaDeClientes();

    [GeneratedRegex(@"/Produtos(\?.*)?$")]
    private static partial Regex ListaDeProdutos();
}
