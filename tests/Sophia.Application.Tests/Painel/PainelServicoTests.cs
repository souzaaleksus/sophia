using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Tests.Fakes;

namespace Sophia.Application.Tests.Painel;

public sealed class PainelServicoTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly BancoEmMemoria _banco = new();
    private readonly RelogioFixo _relogio = new(Hoje);
    private readonly PainelServico _painel;
    private readonly VendaServico _vendas;
    private readonly ProdutoServico _produtos;
    private int _maria;
    private int _caneta;

    public PainelServicoTests()
    {
        _painel = new PainelServico(_banco.Vendas, _relogio);
        _vendas = new VendaServico(_banco.Vendas, _banco.Clientes, _banco.Produtos, _banco, _relogio);
        _produtos = new ProdutoServico(_banco.Produtos, _banco);
    }

    public async Task InitializeAsync()
    {
        _maria = (await new ClienteServico(_banco.Clientes, _banco, _relogio)
            .CadastrarAsync(new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        _caneta = await CadastrarProdutoAsync("Caneta azul");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task O_mes_vai_do_dia_primeiro_ate_hoje()
    {
        await VenderAsync(new DateOnly(2026, 10, 31), 1);
        await VenderAsync(new DateOnly(2026, 11, 1), 2);
        await VenderAsync(Hoje, 4);

        var painel = await _painel.ObterAsync();

        painel.Mes.Should().Be(new ResumoDoPeriodo(2, 6m));
        painel.Dia.Should().Be(new ResumoDoPeriodo(1, 4m));
        painel.Hoje.Should().Be(Hoje);
    }

    [Fact]
    public async Task Mostra_no_maximo_as_cinco_ultimas_vendas()
    {
        for (var dia = 1; dia <= 7; dia++)
        {
            await VenderAsync(new DateOnly(2026, 11, dia), dia);
        }

        var painel = await _painel.ObterAsync();

        painel.UltimasVendas.Select(v => v.Data.Day).Should().Equal(7, 6, 5, 4, 3);
    }

    [Fact]
    public async Task Os_ultimos_sete_dias_vao_do_mais_antigo_ate_hoje_com_zero_nos_dias_sem_venda()
    {
        await VenderAsync(new DateOnly(2026, 11, 6), 9);
        await VenderAsync(new DateOnly(2026, 11, 7), 2);
        await VenderAsync(new DateOnly(2026, 11, 10), 1);
        await VenderAsync(new DateOnly(2026, 11, 10), 3);
        await VenderAsync(Hoje, 4);

        var painel = await _painel.ObterAsync();

        painel.UltimosDias.Should().Equal(
            new TotalDoDia(new DateOnly(2026, 11, 7), 2m, 1),
            new TotalDoDia(new DateOnly(2026, 11, 8), 0m, 0),
            new TotalDoDia(new DateOnly(2026, 11, 9), 0m, 0),
            new TotalDoDia(new DateOnly(2026, 11, 10), 4m, 2),
            new TotalDoDia(new DateOnly(2026, 11, 11), 0m, 0),
            new TotalDoDia(new DateOnly(2026, 11, 12), 0m, 0),
            new TotalDoDia(Hoje, 4m, 1));
    }

    [Fact]
    public async Task Sem_vendas_os_sete_dias_aparecem_zerados()
    {
        var painel = await _painel.ObterAsync();

        painel.UltimosDias.Should().HaveCount(PainelServico.DiasNoGrafico)
            .And.OnlyContain(d => d.Total == 0m && d.QuantidadeDeVendas == 0);
        painel.UltimosDias[0].Dia.Should().Be(new DateOnly(2026, 11, 7));
        painel.UltimosDias[^1].Dia.Should().Be(Hoje);
    }

    [Fact]
    public async Task Os_ultimos_sete_dias_atravessam_a_virada_do_mes()
    {
        _relogio.Hoje = new DateOnly(2026, 12, 2);
        await VenderAsync(new DateOnly(2026, 11, 26), 5);

        var painel = await _painel.ObterAsync();

        painel.UltimosDias.Select(d => d.Dia).Should().Equal(
            new DateOnly(2026, 11, 26),
            new DateOnly(2026, 11, 27),
            new DateOnly(2026, 11, 28),
            new DateOnly(2026, 11, 29),
            new DateOnly(2026, 11, 30),
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 2));
        painel.UltimosDias[0].Should().Be(new TotalDoDia(new DateOnly(2026, 11, 26), 5m, 1));
    }

    [Fact]
    public async Task Os_mais_vendidos_do_mes_seguem_a_quantidade_depois_o_total_depois_o_nome()
    {
        var caderno = await CadastrarProdutoAsync("Caderno");
        var lapis = await CadastrarProdutoAsync("Lápis");
        var borracha = await CadastrarProdutoAsync("Borracha");
        await VenderAsync(new DateOnly(2026, 10, 31), (lapis, 50, 1m));
        await VenderAsync(new DateOnly(2026, 11, 2), (_caneta, 2, 1m), (lapis, 3, 1m));
        await VenderAsync(new DateOnly(2026, 11, 9), (_caneta, 3, 1m), (caderno, 3, 18.90m));
        await VenderAsync(Hoje, (borracha, 3, 1m));

        var painel = await _painel.ObterAsync();

        painel.MaisVendidos.Should().Equal(
            new ProdutoMaisVendido(_caneta, "Caneta azul", 5, 5m),
            new ProdutoMaisVendido(caderno, "Caderno", 3, 56.70m),
            new ProdutoMaisVendido(borracha, "Borracha", 3, 3m),
            new ProdutoMaisVendido(lapis, "Lápis", 3, 3m));
    }

    [Fact]
    public async Task Mostra_no_maximo_os_cinco_mais_vendidos()
    {
        for (var quantidade = 1; quantidade <= 6; quantidade++)
        {
            var produto = await CadastrarProdutoAsync($"Produto {quantidade}");
            await VenderAsync(Hoje, (produto, quantidade, 1m));
        }

        var painel = await _painel.ObterAsync();

        painel.MaisVendidos.Should().HaveCount(PainelServico.QuantidadeDeMaisVendidos);
        painel.MaisVendidos.Select(p => p.Quantidade).Should().Equal(6, 5, 4, 3, 2);
    }

    [Fact]
    public async Task Sem_vendas_no_mes_nao_ha_mais_vendidos()
    {
        await VenderAsync(new DateOnly(2026, 10, 31), 1);

        var painel = await _painel.ObterAsync();

        painel.MaisVendidos.Should().BeEmpty();
    }

    private async Task<int> CadastrarProdutoAsync(string nome) =>
        (await _produtos.CadastrarAsync(new DadosProduto(nome, 1m))).Valor;

    private Task VenderAsync(DateOnly data, int quantidade) => VenderAsync(data, (_caneta, quantidade, 1m));

    private async Task VenderAsync(DateOnly data, params (int Produto, int Quantidade, decimal Valor)[] itens)
    {
        var dados = new DadosVenda(
            data,
            _maria,
            itens.Select(i => new DadosItemVenda(i.Produto, i.Quantidade, i.Valor)).ToList());
        var resultado = await _vendas.RegistrarAsync(dados);
        resultado.Sucesso.Should().BeTrue();
    }
}
