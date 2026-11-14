using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class RetornoAoOperadorTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;
    private Caixa _caixa = null!;
    private Cadastro _cadastro = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
        _caixa = new Caixa(_pagina);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Enquanto_a_busca_de_cliente_demora_a_lista_mostra_buscando()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Vendas/Nova");
        await Rede.DeixarLentaAsync(_pagina, TimeSpan.FromMilliseconds(800));

        await _caixa.ComboboxDeCliente.FillAsync("Maria");

        var buscaDeCliente = _pagina.Locator(".autocompletar").Filter(new() { Has = _caixa.ComboboxDeCliente });
        await Expect(buscaDeCliente.GetByRole(AriaRole.Status)).ToHaveTextAsync("Buscando…");
        await Expect(_pagina.GetByRole(AriaRole.Option).Filter(new() { HasText = "Maria Santos" })).ToBeVisibleAsync();
        await Expect(buscaDeCliente.GetByText("Buscando…")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Quando_a_busca_falha_avisa_que_nao_foi_possivel_e_nao_que_nada_foi_encontrado()
    {
        await _pagina.RouteAsync("**/Clientes/Buscar**", rota => rota.FulfillAsync(new() { Status = 500, Body = string.Empty }));
        await _pagina.GotoAsync("/Vendas/Nova");

        await _caixa.ComboboxDeCliente.FillAsync("Maria");

        await Expect(_pagina.GetByText("Não foi possível buscar. Tente de novo.")).ToBeVisibleAsync();
        await Expect(_pagina.GetByText("Nenhum cliente encontrado.")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Cliente_nao_encontrado_oferece_cadastrar_um_novo_em_outra_aba()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Vendas/Nova");

        await _caixa.ComboboxDeCliente.FillAsync("zzzz");

        await Expect(_pagina.GetByText("Nenhum cliente encontrado.")).ToBeVisibleAsync();
        var cadastrar = _pagina.GetByRole(AriaRole.Link, new() { Name = "Cadastrar novo cliente" });
        await Expect(cadastrar).ToBeVisibleAsync();
        await Expect(cadastrar).ToHaveAttributeAsync("href", TerminaEmNovoCliente());
        await Expect(cadastrar).ToHaveAttributeAsync("target", "_blank");
    }

    [Fact]
    public async Task Produto_nao_encontrado_avisa_na_lista()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _pagina.GotoAsync("/Vendas/Nova");

        await _caixa.ProdutoDaLinha(0).FillAsync("zzzz");

        await Expect(_pagina.GetByText("Nenhum produto encontrado.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Cpf_repetido_leva_o_foco_ao_campo_com_o_erro_ligado_a_ele()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _pagina.GotoAsync("/Clientes/Novo");
        var cpf = _pagina.GetByLabel("CPF");
        await cpf.FillAsync("529.982.247-25");
        await _pagina.GetByLabel("Nome").FillAsync("Outra Maria");

        await _pagina.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync();

        await Expect(_pagina.Locator("[data-valmsg-for=Cpf]")).ToHaveTextAsync("Já existe um cliente com este CPF.");
        await Expect(cpf).ToBeFocusedAsync();
        await Expect(cpf).ToHaveAttributeAsync("aria-invalid", "true");
        var descricao = await cpf.EvaluateAsync<string>("""
            campo => (campo.getAttribute('aria-describedby') || '')
                .split(/\s+/)
                .filter(id => id !== '')
                .map(id => document.getElementById(id)?.textContent ?? '')
                .join(' ')
            """);
        descricao.Should().Contain("Já existe um cliente com este CPF.");
    }

    [Theory]
    [InlineData("", "3 clientes")]
    [InlineData("Maria", "2 clientes encontrados")]
    [InlineData("João", "1 cliente encontrado")]
    public async Task A_lista_de_clientes_mostra_a_contagem(string busca, string contagem)
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _cadastro.ClienteAsync("111.444.777-35", "Maria Lima");
        await _cadastro.ClienteAsync("123.456.789-09", "João Pereira");

        await AbrirListaAsync("/Clientes", busca);

        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync(contagem);
    }

    [Fact]
    public async Task A_lista_com_um_so_cliente_usa_o_singular()
    {
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");

        await AbrirListaAsync("/Clientes", string.Empty);

        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync("1 cliente");
    }

    [Theory]
    [InlineData("", "3 produtos")]
    [InlineData("Caneta", "2 produtos encontrados")]
    [InlineData("Caderno", "1 produto encontrado")]
    public async Task A_lista_de_produtos_mostra_a_contagem(string busca, string contagem)
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        await _cadastro.ProdutoAsync("Caneta preta", 2.50m);
        await _cadastro.ProdutoAsync("Caderno 96 folhas", 18.90m);

        await AbrirListaAsync("/Produtos", busca);

        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync(contagem);
    }

    [Fact]
    public async Task A_lista_com_um_so_produto_usa_o_singular()
    {
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);

        await AbrirListaAsync("/Produtos", string.Empty);

        await Expect(_pagina.GetByTestId("contagem")).ToHaveTextAsync("1 produto");
    }

    private async Task AbrirListaAsync(string endereco, string busca)
    {
        await _pagina.GotoAsync(endereco);
        if (busca.Length == 0)
        {
            return;
        }

        await _pagina.GetByRole(AriaRole.Searchbox).FillAsync(busca);
        await _pagina.GetByRole(AriaRole.Searchbox).PressAsync("Enter");
        await _pagina.WaitForURLAsync(ComBusca());
    }

    [GeneratedRegex("/Clientes/Novo$")]
    private static partial Regex TerminaEmNovoCliente();

    [GeneratedRegex(@"[?&]busca=")]
    private static partial Regex ComBusca();
}
