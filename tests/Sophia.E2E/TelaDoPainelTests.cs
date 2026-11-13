using Microsoft.Playwright;
using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.E2E.Apoio;
using Sophia.Infrastructure;
using Sophia.Infrastructure.Persistencia;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed class TelaDoPainelTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private IPage _pagina = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _pagina = await aplicacao.NovaPaginaAsync();
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Antes_da_primeira_venda_convida_a_registrar()
    {
        await _pagina.GotoAsync("/");

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Painel");
        await Expect(_pagina.GetByTestId("vendido-hoje")).ToContainTextAsync("R$ 0,00");
        await Expect(_pagina.GetByText("Nenhuma venda registrada ainda.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Mostra_o_total_de_hoje_o_do_mes_e_as_ultimas_vendas()
    {
        await RegistrarVendasDeHojeAsync();

        await _pagina.GotoAsync("/");

        await Expect(_pagina.GetByTestId("vendido-hoje")).ToContainTextAsync("R$ 43,90");
        await Expect(_pagina.GetByTestId("vendido-hoje")).ToContainTextAsync("2 vendas");
        await Expect(_pagina.GetByTestId("vendido-no-mes")).ToContainTextAsync("R$ 43,90");
        await Expect(_pagina.GetByTestId("ticket-medio")).ToContainTextAsync("R$ 21,95");
        await Expect(_pagina.GetByRole(AriaRole.Row).Filter(new() { HasText = "Maria Santos" })).ToHaveCountAsync(2);
    }

    private async Task RegistrarVendasDeHojeAsync()
    {
        await using var contexto = aplicacao.Banco.NovoContexto();
        var unidade = new UnidadeDeTrabalho(contexto);
        var relogio = new RelogioDoSistema(TimeProvider.System, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
        var clientes = new ClienteServico(new ClienteRepositorio(contexto), unidade, relogio);
        var produtos = new ProdutoServico(new ProdutoRepositorio(contexto), unidade);
        var vendas = new VendaServico(
            new VendaRepositorio(contexto), new ClienteRepositorio(contexto), new ProdutoRepositorio(contexto), unidade, relogio);

        var maria = (await clientes.CadastrarAsync(new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        var caneta = (await produtos.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;
        var caderno = (await produtos.CadastrarAsync(new DadosProduto("Caderno 96 folhas", 18.90m))).Valor;

        (await vendas.RegistrarAsync(new DadosVenda(relogio.Hoje, maria, [new(caneta, 10, 2.50m)]))).Sucesso.Should().BeTrue();
        (await vendas.RegistrarAsync(new DadosVenda(relogio.Hoje, maria, [new(caderno, 1, 18.90m)]))).Sucesso.Should().BeTrue();
    }
}
