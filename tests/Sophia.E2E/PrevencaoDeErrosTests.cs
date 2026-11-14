using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sophia.E2E.Apoio;
using static Microsoft.Playwright.Assertions;

namespace Sophia.E2E;

[Collection(ColecaoDaAplicacao.Nome)]
public sealed partial class PrevencaoDeErrosTests(AplicacaoEmExecucao aplicacao) : IAsyncLifetime
{
    private readonly ConcurrentQueue<string> _dialogos = new();
    private IPage _pagina = null!;
    private Caixa _caixa = null!;
    private Cadastro _cadastro = null!;

    public async Task InitializeAsync()
    {
        await aplicacao.Banco.LimparAsync();
        _cadastro = new Cadastro(aplicacao.Banco);
        await _cadastro.ClienteAsync("529.982.247-25", "Maria Santos");
        await _cadastro.ProdutoAsync("Caneta azul", 2.50m);
        _pagina = await aplicacao.NovaPaginaAsync(1280, 860);
        _caixa = new Caixa(_pagina);
    }

    public Task DisposeAsync() => _pagina.Context.CloseAsync();

    [Fact]
    public async Task Dois_envios_seguidos_registram_uma_so_venda()
    {
        await MontarVendaAsync();

        await _pagina.EvaluateAsync("""
            () => {
                const formulario = document.getElementById('nova-venda');
                formulario.requestSubmit();
                formulario.requestSubmit();
                setTimeout(() => formulario.requestSubmit(), 60);
            }
            """);

        await EsperarODetalheAsync();
        (await VendasRegistradasAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Dois_cliques_rapidos_no_botao_registram_uma_so_venda()
    {
        await MontarVendaAsync();

        await _caixa.Finalizar.ClickAsync(new() { ClickCount = 2 });

        await EsperarODetalheAsync();
        (await VendasRegistradasAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Logo_apos_o_envio_o_botao_fica_desabilitado_mostrando_registrando()
    {
        await MontarVendaAsync();
        await _pagina.EvaluateAsync("() => window.addEventListener('submit', evento => evento.preventDefault())");

        await _caixa.Finalizar.ClickAsync();

        await Expect(_caixa.BotaoDeEnvio).ToBeDisabledAsync();
        await Expect(_caixa.BotaoDeEnvio).ToContainTextAsync("Registrando…");
        await Expect(_caixa.BotaoDeEnvio).ToHaveAttributeAsync("aria-busy", "true");
        (await VendasRegistradasAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Sair_pelo_menu_com_produto_lancado_pede_confirmacao_e_recusar_mantem_a_venda()
    {
        VigiarDialogos();
        await MontarVendaAsync();

        await ItemDoMenu("Painel").ClickAsync();

        var tipo = await PrimeiroDialogoAsync();
        tipo.Should().Be("beforeunload", "sair com produto lançado pede confirmação");
        await Expect(_pagina).ToHaveURLAsync(EnderecoDaNovaVenda());
        await Expect(_caixa.ProdutoDaLinha(0)).ToHaveValueAsync("Caneta azul");
    }

    [Fact]
    public async Task Sair_pelo_menu_sem_produto_lancado_nao_pede_confirmacao()
    {
        VigiarDialogos();
        await _pagina.GotoAsync("/Vendas/Nova");
        await _pagina.GetByLabel("Data da venda").ClickAsync();

        await ItemDoMenu("Painel").ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Painel");
        _dialogos.Should().BeEmpty();
    }

    [Fact]
    public async Task Depois_de_finalizar_a_venda_sair_nao_pede_confirmacao()
    {
        VigiarDialogos();
        await MontarVendaAsync();

        await _caixa.Finalizar.ClickAsync();
        await Expect(_pagina.GetByRole(AriaRole.Status)).ToContainTextAsync("Venda registrada.");
        await ItemDoMenu("Painel").ClickAsync();

        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Painel");
        _dialogos.Should().BeEmpty();
    }

    private ILocator ItemDoMenu(string nome) =>
        _pagina.GetByRole(AriaRole.Navigation, new() { Name = "Navegação principal" })
            .GetByRole(AriaRole.Link, new() { Name = nome, Exact = true });

    private async Task MontarVendaAsync()
    {
        await _pagina.GotoAsync("/Vendas/Nova");
        await _caixa.EscolherClienteAsync("Maria", "Maria Santos");
        await _caixa.LancarAsync(0, "Caneta", "Caneta azul", "2");
        await Expect(_caixa.Total).ToHaveTextAsync("R$ 5,00");
    }

    private async Task EsperarODetalheAsync()
    {
        await Expect(_pagina).ToHaveURLAsync(EnderecoDoDetalhe(), new() { Timeout = 15000 });
        await Expect(_pagina.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Venda nº");
    }

    private async Task<int> VendasRegistradasAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        return await _cadastro.QuantidadeDeVendasAsync();
    }

    private void VigiarDialogos()
    {
        _pagina.Dialog += async (_, dialogo) =>
        {
            _dialogos.Enqueue(dialogo.Type);
            await dialogo.DismissAsync();
        };
    }

    private async Task<string> PrimeiroDialogoAsync()
    {
        var limite = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < limite)
        {
            if (_dialogos.TryPeek(out var tipo))
            {
                return tipo;
            }

            await Task.Delay(100);
        }

        return "nenhum diálogo";
    }

    [GeneratedRegex(@"/Vendas/Detalhe/\d+$")]
    private static partial Regex EnderecoDoDetalhe();

    [GeneratedRegex(@"/Vendas/Nova$")]
    private static partial Regex EnderecoDaNovaVenda();
}
