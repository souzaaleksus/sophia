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
    private int _maria;
    private int _caneta;

    public PainelServicoTests()
    {
        _painel = new PainelServico(_banco.Vendas, _relogio);
        _vendas = new VendaServico(_banco.Vendas, _banco.Clientes, _banco.Produtos, _banco, _relogio);
    }

    public async Task InitializeAsync()
    {
        _maria = (await new ClienteServico(_banco.Clientes, _banco, _relogio)
            .CadastrarAsync(new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        _caneta = (await new ProdutoServico(_banco.Produtos, _banco)
            .CadastrarAsync(new DadosProduto("Caneta azul", 1m))).Valor;
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

    private async Task VenderAsync(DateOnly data, int quantidade)
    {
        var resultado = await _vendas.RegistrarAsync(new DadosVenda(data, _maria, [new(_caneta, quantidade, 1m)]));
        resultado.Sucesso.Should().BeTrue();
    }
}
