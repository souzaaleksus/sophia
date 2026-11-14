using System.Globalization;
using Microsoft.Playwright;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class TelaResponsivaTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private const string ProdutoDeNomeLongo = "Caderno universitário 96 folhas capa dura";
    private const string Interativos = "button, input:not([type=hidden]), select, a.btn, a.acao, .navbar-toggler";
    private const int AlvoMinimo = 44;

    private IPage? _pagina;
    private int _venda;
    private int _cliente;

    public static TheoryData<int, int, string> Telas
    {
        get
        {
            var telas = new TheoryData<int, int, string>();
            foreach (var (largura, altura) in new[] { (360, 740), (768, 1024) })
            {
                foreach (var endereco in new[]
                {
                    "/", "/Vendas", "/Vendas/Nova", "/Vendas/Detalhe/{venda}",
                    "/Clientes", "/Clientes/Novo", "/Produtos", "/Clientes/Excluir/{cliente}",
                })
                {
                    telas.Add(largura, altura, endereco);
                }
            }

            return telas;
        }
    }

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        var cadastro = new Cadastro(aplicacao.Banco);
        _cliente = await cadastro.ClienteAsync("529.982.247-25", "Maria Aparecida dos Santos Oliveira");
        await cadastro.ClienteAsync("111.444.777-35", "João Lima");
        var caneta = await cadastro.ProdutoAsync("Caneta azul", 2.50m);
        var caderno = await cadastro.ProdutoAsync(ProdutoDeNomeLongo, 32.90m);
        _venda = await cadastro.VendaAsync(Cadastro.Hoje, _cliente, new DadosItemVenda(caderno, 12, 32.90m), new DadosItemVenda(caneta, 150, 2.50m));
    }

    public async Task DisposeAsync()
    {
        if (_pagina is not null)
        {
            await _pagina.Context.CloseAsync();
        }
    }

    [Theory]
    [MemberData(nameof(Telas))]
    public async Task A_tela_cabe_na_largura_sem_rolagem_lateral(int largura, int altura, string endereco)
    {
        _pagina = await aplicacao.NovaPaginaAsync(largura, altura);

        await _pagina.GotoAsync(Resolver(endereco));

        var problema = await Medidas.RolagemLateralAsync(_pagina);
        problema.Should().BeEmpty($"{endereco} em {largura}x{altura} não pode rolar para o lado");
    }

    [Theory]
    [InlineData("/Clientes")]
    [InlineData("/Produtos")]
    [InlineData("/Vendas")]
    public async Task Com_mais_de_uma_pagina_a_lista_cabe_no_menor_celular_com_o_paginador(string endereco)
    {
        var cadastro = new Cadastro(aplicacao.Banco);
        await cadastro.ClientesAsync(30);
        await cadastro.ProdutosAsync(30);
        await cadastro.VendasAsync(30);
        _pagina = await aplicacao.NovaPaginaAsync(320, 640, toque: true);

        await _pagina.GotoAsync(endereco);

        await Expect(_pagina.GetByRole(AriaRole.Navigation, new() { Name = "Páginas" })).ToBeVisibleAsync();
        var problema = await Medidas.RolagemLateralAsync(_pagina);
        problema.Should().BeEmpty($"{endereco} com paginador em 320 px não pode rolar para o lado");
    }

    [Fact]
    public async Task No_celular_o_total_do_detalhe_aparece_inteiro_sem_corte()
    {
        _pagina = await aplicacao.NovaPaginaAsync(360, 740, toque: true);
        await _pagina.GotoAsync($"/Vendas/Detalhe/{_venda}");
        var total = _pagina.GetByTestId("total-da-venda");
        await Expect(total).ToContainTextAsync("R$ 769,80");

        var caixa = await total.BoundingBoxAsync();
        caixa.Should().NotBeNull();
        caixa!.X.Should().BeGreaterThanOrEqualTo(0, "o total começa dentro da tela");
        (caixa.X + caixa.Width).Should().BeLessThanOrEqualTo(360, "o total termina dentro da tela");

        await total.ScrollIntoViewIfNeededAsync();
        var quemOcupa = await Medidas.QuemOcupaOCentroAsync(total, aceitaDescendente: true);
        quemOcupa.Should().BeEmpty("o total não pode ficar cortado por um contêiner com rolagem");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Clientes")]
    [InlineData("/Vendas")]
    [InlineData("/Vendas/Nova")]
    [InlineData("/Clientes/Novo")]
    public async Task No_celular_todo_alvo_de_toque_tem_ao_menos_44_pixels(string endereco)
    {
        _pagina = await aplicacao.NovaPaginaAsync(360, 740, toque: true);
        await _pagina.GotoAsync(endereco);
        if (endereco == "/Vendas/Nova")
        {
            var caixa = new Caixa(_pagina);
            await caixa.EscolherClienteAsync("Maria", "Maria Aparecida dos Santos Oliveira");
            await caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        }

        var pequenos = await Medidas.AlvosMenoresQueAsync(_pagina, Interativos, AlvoMinimo);

        pequenos.Should().BeEmpty($"em {endereco} todo alvo de toque precisa de {AlvoMinimo}x{AlvoMinimo} px");
    }

    private string Resolver(string endereco) =>
        endereco
            .Replace("{venda}", _venda.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{cliente}", _cliente.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
