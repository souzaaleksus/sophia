using Sophia.Application.Clientes;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Tests.Fakes;

namespace Sophia.Application.Tests.Vendas;

public sealed class VendaServicoTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly BancoEmMemoria _banco = new();
    private readonly VendaServico _servico;
    private readonly ClienteServico _clientes;
    private readonly ProdutoServico _produtos;
    private int _maria;
    private int _caneta;
    private int _caderno;

    public VendaServicoTests()
    {
        var relogio = new RelogioFixo(Hoje);
        _servico = new VendaServico(_banco.Vendas, _banco.Clientes, _banco.Produtos, _banco, relogio);

        _clientes = new ClienteServico(_banco.Clientes, _banco, relogio);
        _produtos = new ProdutoServico(_banco.Produtos, _banco);
    }

    public async Task InitializeAsync()
    {
        _maria = (await _clientes.CadastrarAsync(new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        _caneta = (await _produtos.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;
        _caderno = (await _produtos.CadastrarAsync(new DadosProduto("Caderno 96 folhas", 18.90m))).Valor;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Sugere_a_data_de_hoje()
    {
        _servico.DataSugerida().Should().Be(Hoje);
    }

    [Fact]
    public async Task Registra_e_devolve_o_detalhe_com_cliente_itens_e_total()
    {
        var dados = new DadosVenda(Hoje, _maria, [new(_caneta, 10, 2.50m), new(_caderno, 2, 18.90m)]);

        var resultado = await _servico.RegistrarAsync(dados);

        resultado.Sucesso.Should().BeTrue();
        var venda = await _servico.ObterAsync(resultado.Valor);
        venda.Should().BeEquivalentTo(new VendaDetalheDto(
            resultado.Valor,
            Hoje,
            "529.982.247-25",
            "Maria Santos",
            [
                new ItemVendaDto("Caneta azul", 10, 2.50m, 25.00m),
                new ItemVendaDto("Caderno 96 folhas", 2, 18.90m, 37.80m),
            ],
            62.80m));
    }

    [Fact]
    public async Task Recusa_cliente_inexistente()
    {
        var resultado = await _servico.RegistrarAsync(new DadosVenda(Hoje, 999, [new(_caneta, 1, 2.50m)]));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ClienteId", "Cliente não encontrado."));
    }

    [Fact]
    public async Task Recusa_produto_inexistente_apontando_a_linha()
    {
        var resultado = await _servico.RegistrarAsync(
            new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m), new(999, 1, 1m)]));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Itens[1].ProdutoId", "Produto não encontrado."));
    }

    [Fact]
    public async Task Regras_do_dominio_chegam_ao_chamador()
    {
        var resultado = await _servico.RegistrarAsync(new DadosVenda(Hoje.AddDays(1), _maria, []));

        resultado.Erros.Select(e => e.Campo).Should().BeEquivalentTo("Data", "Itens");
    }

    [Fact]
    public async Task Lista_as_vendas_mais_recentes_primeiro_com_cliente_e_total()
    {
        await _servico.RegistrarAsync(new DadosVenda(Hoje.AddDays(-1), _maria, [new(_caneta, 2, 2.50m)]));
        await _servico.RegistrarAsync(new DadosVenda(Hoje, _maria, [new(_caderno, 1, 18.90m), new(_caneta, 1, 2.50m)]));

        var vendas = await _servico.ListarAsync();

        vendas.Select(v => (v.Data, v.ClienteNome, v.QuantidadeDeItens, v.Total)).Should().Equal(
            (Hoje, "Maria Santos", 2, 21.40m),
            (Hoje.AddDays(-1), "Maria Santos", 1, 5.00m));
    }

    [Fact]
    public async Task Venda_inexistente_devolve_nulo()
    {
        (await _servico.ObterAsync(999)).Should().BeNull();
    }
}
