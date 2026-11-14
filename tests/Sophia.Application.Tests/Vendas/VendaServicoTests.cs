using Sophia.Application.Abstracoes;
using Sophia.Application.Tests.Apoio;
using Sophia.Application.Vendas;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Testes.Banco;

namespace Sophia.Application.Tests.Vendas;

[Collection(ColecaoDoBanco.Nome)]
public sealed class VendaServicoTests(BancoDeTestes banco) : IAsyncLifetime, IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly SistemaEmTeste _sistema = new(banco, Hoje);
    private int _maria;
    private int _caneta;
    private int _caderno;

    private IVendaServico Servico => _sistema.Vendas;

    public async Task InitializeAsync()
    {
        await banco.LimparAsync();
        _maria = (await _sistema.Clientes.CadastrarAsync(
            new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        _caneta = (await _sistema.Produtos.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;
        _caderno = (await _sistema.Produtos.CadastrarAsync(new DadosProduto("Caderno 96 folhas", 18.90m))).Valor;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _sistema.Dispose();

    [Fact]
    public void Sugere_a_data_de_hoje()
    {
        Servico.DataSugerida().Should().Be(Hoje);
    }

    [Fact]
    public async Task Registra_e_devolve_o_detalhe_com_cliente_itens_e_total()
    {
        var dados = new DadosVenda(Hoje, _maria, [new(_caneta, 10, 2.50m), new(_caderno, 2, 18.90m)]);

        var resultado = await Servico.RegistrarAsync(dados);

        resultado.Sucesso.Should().BeTrue();
        var venda = await Servico.ObterAsync(resultado.Valor);
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
        var resultado = await Servico.RegistrarAsync(new DadosVenda(Hoje, 999, [new(_caneta, 1, 2.50m)]));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ClienteId", "Cliente não encontrado."));
    }

    [Fact]
    public async Task Recusa_produto_inexistente_apontando_a_linha()
    {
        var resultado = await Servico.RegistrarAsync(
            new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m), new(999, 1, 1m)]));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Itens[1].ProdutoId", "Produto não encontrado."));
    }

    [Fact]
    public async Task Regras_do_dominio_chegam_ao_chamador()
    {
        var resultado = await Servico.RegistrarAsync(new DadosVenda(Hoje.AddDays(1), _maria, []));

        resultado.Erros.Select(e => e.Campo).Should().BeEquivalentTo("Data", "Itens");
    }

    [Fact]
    public async Task Lista_as_vendas_mais_recentes_primeiro_com_cliente_e_total()
    {
        await Servico.RegistrarAsync(new DadosVenda(Hoje.AddDays(-1), _maria, [new(_caneta, 2, 2.50m)]));
        await Servico.RegistrarAsync(new DadosVenda(Hoje, _maria, [new(_caderno, 1, 18.90m), new(_caneta, 1, 2.50m)]));

        var vendas = await PrimeiraPaginaAsync();

        vendas.Select(v => (v.Data, v.ClienteNome, v.QuantidadeDeItens, v.Total)).Should().Equal(
            (Hoje, "Maria Santos", 2, 21.40m),
            (Hoje.AddDays(-1), "Maria Santos", 1, 5.00m));
    }

    [Fact]
    public async Task A_mesma_chave_enviada_duas_vezes_registra_uma_venda_so()
    {
        var dados = new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m)], Guid.NewGuid());

        var primeira = await Servico.RegistrarAsync(dados);
        var segunda = await Servico.RegistrarAsync(dados);

        segunda.Sucesso.Should().BeTrue();
        segunda.Valor.Should().Be(primeira.Valor);
        (await PrimeiraPaginaAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Chaves_diferentes_registram_duas_vendas()
    {
        var itens = new DadosItemVenda[] { new(_caneta, 1, 2.50m) };

        var primeira = await Servico.RegistrarAsync(new DadosVenda(Hoje, _maria, itens, Guid.NewGuid()));
        var segunda = await Servico.RegistrarAsync(new DadosVenda(Hoje, _maria, itens, Guid.NewGuid()));

        segunda.Valor.Should().NotBe(primeira.Valor);
        (await PrimeiraPaginaAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Sem_chave_cada_envio_registra_uma_venda()
    {
        var dados = new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m)]);

        await Servico.RegistrarAsync(dados);
        await Servico.RegistrarAsync(dados);

        (await PrimeiraPaginaAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Na_corrida_entre_dois_envios_devolve_a_venda_que_o_banco_gravou_primeiro()
    {
        var dados = new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m)], Guid.NewGuid());
        var idDoOutroEnvio = 0;
        _sistema.AntesDoProximoSalvamento(async () =>
            idDoOutroEnvio = (await _sistema.Vendas.RegistrarAsync(dados)).Valor);

        var resultado = await Servico.RegistrarAsync(dados);

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Should().Be(idDoOutroEnvio).And.BePositive();
        (await PrimeiraPaginaAsync()).Should().ContainSingle()
            .Which.Id.Should().Be(resultado.Valor);
    }

    [Fact]
    public async Task Conflito_que_nao_e_da_chave_continua_sendo_erro()
    {
        _sistema.AntesDoProximoSalvamento(async () =>
            (await _sistema.Produtos.ExcluirAsync(_caneta)).Sucesso.Should().BeTrue());

        var registrar = () => Servico.RegistrarAsync(
            new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m)], Guid.NewGuid()));

        await registrar.Should().ThrowAsync<ConflitoAoSalvarException>();
    }

    [Fact]
    public async Task Venda_inexistente_devolve_nulo()
    {
        (await Servico.ObterAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task Lista_em_paginas_de_25_das_mais_recentes_para_as_mais_antigas()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var pagina = await Servico.ListarAsync(1);

        pagina.Numero.Should().Be(1);
        pagina.Tamanho.Should().Be(VendaServico.TamanhoDaPagina).And.Be(25);
        pagina.TotalDeItens.Should().Be(60);
        pagina.TotalDePaginas.Should().Be(3);
        pagina.Itens.Should().HaveCount(25);
        pagina.Itens.Select(v => v.Data).Should().BeInDescendingOrder();
        pagina.Itens[0].Data.Should().Be(Hoje);
        pagina.Itens[^1].Data.Should().Be(Hoje.AddDays(-24));
    }

    [Fact]
    public async Task A_ultima_pagina_traz_as_vendas_que_sobraram()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var pagina = await Servico.ListarAsync(3);

        pagina.Numero.Should().Be(3);
        pagina.Itens.Select(v => v.Data).Should().Equal(Enumerable.Range(50, 10).Select(dias => Hoje.AddDays(-dias)));
        pagina.TemProxima.Should().BeFalse();
    }

    [Fact]
    public async Task No_mesmo_dia_a_venda_de_numero_maior_vem_primeiro_e_nenhuma_se_repete_entre_paginas()
    {
        for (var vez = 0; vez < 30; vez++)
        {
            await Servico.RegistrarAsync(new DadosVenda(Hoje, _maria, [new(_caneta, 1, 2.50m)]));
        }

        var primeira = await Servico.ListarAsync(1);
        var segunda = await Servico.ListarAsync(2);

        var ids = primeira.Itens.Concat(segunda.Itens).Select(v => v.Id).ToList();
        ids.Should().HaveCount(30).And.OnlyHaveUniqueItems().And.BeInDescendingOrder();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(4, 3)]
    [InlineData(99, 3)]
    public async Task Pagina_fora_dos_limites_vira_a_primeira_ou_a_ultima(int pedida, int mostrada)
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var pagina = await Servico.ListarAsync(pedida);

        pagina.Numero.Should().Be(mostrada);
        pagina.Itens.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sem_vendas_devolve_uma_pagina_vazia()
    {
        var pagina = await Servico.ListarAsync(7);

        pagina.Numero.Should().Be(1);
        pagina.Itens.Should().BeEmpty();
        pagina.TotalDeItens.Should().Be(0);
        pagina.TotalDePaginas.Should().Be(1);
    }

    private async Task<IReadOnlyList<VendaResumoDto>> PrimeiraPaginaAsync() => (await Servico.ListarAsync(1)).Itens;

    private async Task RegistrarUmaVendaPorDiaAsync(int quantidade)
    {
        for (var dias = quantidade - 1; dias >= 0; dias--)
        {
            var resultado = await Servico.RegistrarAsync(
                new DadosVenda(Hoje.AddDays(-dias), _maria, [new(_caneta, 1, 2.50m)]));
            resultado.Sucesso.Should().BeTrue();
        }
    }
}
