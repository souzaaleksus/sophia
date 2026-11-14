using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class ListasTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
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

    private ILocator Linhas => _pagina.Locator("main table tbody tr");

    private ILocator Resumo => _pagina.GetByTestId("resumo-da-pagina");

    [Fact]
    public async Task A_lista_de_clientes_mostra_25_por_pagina_e_navega_para_a_proxima()
    {
        await _cadastro.ClientesAsync(30);

        await ConferirPaginacaoAsync("/Clientes");
    }

    [Fact]
    public async Task A_lista_de_produtos_mostra_25_por_pagina_e_navega_para_a_proxima()
    {
        await _cadastro.ProdutosAsync(30);

        await ConferirPaginacaoAsync("/Produtos");
    }

    [Fact]
    public async Task A_lista_de_vendas_mostra_25_por_pagina_e_navega_para_a_proxima()
    {
        await _cadastro.VendasAsync(30);

        await ConferirPaginacaoAsync("/Vendas");
    }

    [Fact]
    public async Task A_busca_de_clientes_filtra_enquanto_digita()
    {
        await TresClientesAsync();
        await _pagina.GotoAsync("/Clientes");

        await _pagina.GetByRole(AriaRole.Searchbox).PressSequentiallyAsync("lima");

        await Expect(_pagina).ToHaveURLAsync(ComBuscaPorLima(), new() { Timeout = 2000 });
        await Expect(Linhas).ToHaveCountAsync(1, new() { Timeout = 2000 });
        await Expect(Linhas.First).ToContainTextAsync("João Lima");
        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync("1 cliente encontrado");
    }

    [Fact]
    public async Task Limpar_a_busca_esvazia_o_campo_e_volta_a_lista_completa()
    {
        await TresClientesAsync();
        await _pagina.GotoAsync("/Clientes");
        var busca = _pagina.GetByRole(AriaRole.Searchbox);
        await busca.PressSequentiallyAsync("lima");
        await Expect(Linhas).ToHaveCountAsync(1, new() { Timeout = 2000 });

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Limpar a busca" }).ClickAsync();

        await Expect(busca).ToHaveValueAsync(string.Empty);
        await Expect(Linhas).ToHaveCountAsync(3);
        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync("3 clientes");
    }

    [Fact]
    public async Task Os_clientes_comecam_pelo_nome_e_o_cabecalho_inverte_a_ordem()
    {
        await TresClientesAsync();
        await _pagina.GotoAsync("/Clientes");
        var nome = _pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Nome" });
        await Expect(nome).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Linhas.First).ToContainTextAsync("Ana Souza");

        await AcionarCabecalhoAsync(nome);

        await Expect(_pagina).ToHaveURLAsync(ComOrdemPorNomeDecrescente());
        await Expect(_pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Nome" })).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(Linhas.First).ToContainTextAsync("Maria Santos");
    }

    [Fact]
    public async Task Os_produtos_ordenam_pelo_valor_unitario_do_menor_para_o_maior()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);
        await _cadastro.ProdutoAsync("Borracha", 1.00m);
        await _pagina.GotoAsync("/Produtos");

        await AcionarCabecalhoAsync(_pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Valor unitário" }));

        await Expect(_pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Valor unitário" })).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Linhas).ToHaveCountAsync(3);
        await Expect(Linhas.Nth(0)).ToContainTextAsync("Borracha");
        await Expect(Linhas.Nth(1)).ToContainTextAsync("Caneta azul");
        await Expect(Linhas.Nth(2)).ToContainTextAsync("Caderno 96 folhas");
    }

    [Fact]
    public async Task As_vendas_ordenam_pelo_total()
    {
        var cliente = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 1.00m);
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-2), cliente, new DadosItemVenda(produto, 10, 1.00m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-1), cliente, new DadosItemVenda(produto, 50, 1.00m));
        await _cadastro.VendaAsync(Cadastro.Hoje, cliente, new DadosItemVenda(produto, 30, 1.00m));
        await _pagina.GotoAsync("/Vendas");
        var total = _pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Total" });

        await AcionarCabecalhoAsync(total);

        await Expect(_pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Total" })).ToHaveAttributeAsync("aria-sort", AscendenteOuDescendente());
        var sentido = await _pagina.GetByRole(AriaRole.Columnheader, new() { Name = "Total" }).GetAttributeAsync("aria-sort");
        var esperado = sentido == "ascending"
            ? new[] { "R$ 10,00", "R$ 30,00", "R$ 50,00" }
            : new[] { "R$ 50,00", "R$ 30,00", "R$ 10,00" };
        for (var indice = 0; indice < esperado.Length; indice++)
        {
            await Expect(Linhas.Nth(indice)).ToContainTextAsync(esperado[indice]);
        }
    }

    [Fact]
    public async Task As_vendas_tem_filtro_de_periodo_com_de_ate_e_atalhos()
    {
        await _pagina.GotoAsync("/Vendas");

        await Expect(_pagina.GetByLabel("De", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(_pagina.GetByLabel("Até", new() { Exact = true })).ToBeVisibleAsync();
        foreach (var atalho in new[] { "Hoje", "7 dias", "Mês" })
        {
            await Expect(Atalho(atalho)).ToBeVisibleAsync();
        }

        await Expect(_pagina.GetByRole(AriaRole.Button, new() { Name = "Filtrar" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task O_atalho_hoje_mostra_so_as_vendas_de_hoje()
    {
        await VendasEmTresDatasAsync();
        await _pagina.GotoAsync("/Vendas");

        await Expect(Atalho("Hoje")).ToBeVisibleAsync();
        await Atalho("Hoje").ClickAsync();

        await Expect(LinhaDe("Maria Santos")).ToHaveCountAsync(1);
        await Expect(LinhaDe("Ana Souza")).ToHaveCountAsync(0);
        await Expect(LinhaDe("João Lima")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task O_atalho_7_dias_mostra_so_as_vendas_da_ultima_semana()
    {
        await VendasEmTresDatasAsync();
        await _pagina.GotoAsync("/Vendas");

        await Expect(Atalho("7 dias")).ToBeVisibleAsync();
        await Atalho("7 dias").ClickAsync();

        await Expect(LinhaDe("João Lima")).ToHaveCountAsync(0);
        await Expect(LinhaDe("Maria Santos")).ToHaveCountAsync(1);
        await Expect(LinhaDe("Ana Souza")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task O_atalho_mes_mostra_as_vendas_de_hoje()
    {
        await VendasEmTresDatasAsync();
        await _pagina.GotoAsync("/Vendas");

        await Expect(Atalho("Mês")).ToBeVisibleAsync();
        await Atalho("Mês").ClickAsync();

        await Expect(LinhaDe("Maria Santos")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task De_e_ate_preenchidos_filtram_o_intervalo()
    {
        await VendasEmTresDatasAsync();
        await _pagina.GotoAsync("/Vendas");
        var de = _pagina.GetByLabel("De", new() { Exact = true });
        var ate = _pagina.GetByLabel("Até", new() { Exact = true });
        await Expect(de).ToBeVisibleAsync();

        await de.FillAsync(Cadastro.Hoje.AddDays(-11).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await ate.FillAsync(Cadastro.Hoje.AddDays(-9).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Filtrar" }).ClickAsync();

        await Expect(LinhaDe("João Lima")).ToHaveCountAsync(1);
        await Expect(LinhaDe("Maria Santos")).ToHaveCountAsync(0);
        await Expect(LinhaDe("Ana Souza")).ToHaveCountAsync(0);
    }

    private async Task ConferirPaginacaoAsync(string endereco)
    {
        await _pagina.GotoAsync(endereco);

        await Expect(Linhas).ToHaveCountAsync(25);
        await Expect(Resumo).ToHaveTextAsync("Mostrando 1–25 de 30");
        var proxima = _pagina.GetByRole(AriaRole.Link, new() { Name = "Próxima" });
        await Expect(proxima).ToBeVisibleAsync();

        await proxima.ClickAsync();

        await Expect(Resumo).ToHaveTextAsync("Mostrando 26–30 de 30");
        await Expect(Linhas).ToHaveCountAsync(5);
        await Expect(_pagina.GetByRole(AriaRole.Link, new() { Name = "Anterior" })).ToBeVisibleAsync();
        await Expect(_pagina.GetByRole(AriaRole.Link, new() { Name = "Próxima" })).ToHaveCountAsync(0);
        await Expect(_pagina.Locator("[aria-disabled=true]", new() { HasText = "Próxima" }).First).ToBeVisibleAsync();
    }

    private ILocator Atalho(string nome) =>
        _pagina.GetByRole(AriaRole.Link, new() { Name = nome, Exact = true })
            .Or(_pagina.GetByRole(AriaRole.Button, new() { Name = nome, Exact = true }));

    private ILocator LinhaDe(string cliente) => Linhas.Filter(new() { HasText = cliente });

    private static async Task AcionarCabecalhoAsync(ILocator cabecalho)
    {
        var acao = cabecalho.GetByRole(AriaRole.Link).Or(cabecalho.GetByRole(AriaRole.Button));
        await Expect(acao).ToHaveCountAsync(1);
        await acao.ClickAsync();
    }

    private async Task TresClientesAsync()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _cadastro.ClienteAsync("111.444.777-35", "João Lima");
        await _cadastro.ClienteAsync("123.456.789-09", "Ana Souza");
    }

    private async Task VendasEmTresDatasAsync()
    {
        var maria = await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        var joao = await _cadastro.ClienteAsync("111.444.777-35", "João Lima");
        var ana = await _cadastro.ClienteAsync("123.456.789-09", "Ana Souza");
        var produto = await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.VendaAsync(Cadastro.Hoje, maria, new DadosItemVenda(produto, 1, 2.50m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-3), ana, new DadosItemVenda(produto, 2, 2.50m));
        await _cadastro.VendaAsync(Cadastro.Hoje.AddDays(-10), joao, new DadosItemVenda(produto, 3, 2.50m));
    }

    [GeneratedRegex(@"[?&]busca=lima(&|$)")]
    private static partial Regex ComBuscaPorLima();

    [GeneratedRegex(@"[?&]ordem=-nome(&|$)")]
    private static partial Regex ComOrdemPorNomeDecrescente();

    [GeneratedRegex("^(ascending|descending)$")]
    private static partial Regex AscendenteOuDescendente();
}
