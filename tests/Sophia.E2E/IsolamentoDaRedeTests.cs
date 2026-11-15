using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class IsolamentoDaRedeTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        var cadastro = new Cadastro(aplicacao.Banco);
        await cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await cadastro.ProdutoAsync("Caneta azul", 2.50m);
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task A_tela_carrega_e_registra_a_venda_sem_depender_das_conexoes_do_navegador()
    {
        var caixa = new Caixa(_pagina);
        await _pagina.Context.SetOfflineAsync(true);

        await _pagina.GotoAsync("/Vendas/Nova");
        await caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        await caixa.Finalizar.ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");
        await Expect(_pagina.GetByTestId("total-da-venda")).ToHaveTextAsync("R$ 5,00");
    }
}
