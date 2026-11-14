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

        var pagina = (await Servico.ListarAsync(new ConsultaDeVendas(1))).Pagina;

        pagina.Numero.Should().Be(1);
        pagina.Tamanho.Should().Be(new ParametrosDoSophia().TamanhoDaPagina).And.Be(25);
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

        var pagina = (await Servico.ListarAsync(new ConsultaDeVendas(3))).Pagina;

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

        var primeira = (await Servico.ListarAsync(new ConsultaDeVendas(1))).Pagina;
        var segunda = (await Servico.ListarAsync(new ConsultaDeVendas(2))).Pagina;

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

        var pagina = (await Servico.ListarAsync(new ConsultaDeVendas(pedida))).Pagina;

        pagina.Numero.Should().Be(mostrada);
        pagina.Itens.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sem_vendas_devolve_uma_pagina_vazia()
    {
        var pagina = (await Servico.ListarAsync(new ConsultaDeVendas(7))).Pagina;

        pagina.Numero.Should().Be(1);
        pagina.Itens.Should().BeEmpty();
        pagina.TotalDeItens.Should().Be(0);
        pagina.TotalDePaginas.Should().Be(1);
    }

    [Fact]
    public async Task Filtra_pelo_periodo_com_as_duas_pontas_inclusivas()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(De: Hoje.AddDays(-9), Ate: Hoje.AddDays(-5)));

        lista.Pagina.TotalDeItens.Should().Be(5);
        lista.Pagina.Itens.Select(v => v.Data).Should().Equal(
            Hoje.AddDays(-5), Hoje.AddDays(-6), Hoje.AddDays(-7), Hoje.AddDays(-8), Hoje.AddDays(-9));
    }

    [Fact]
    public async Task Periodo_com_o_inicio_depois_do_fim_e_lido_ao_contrario()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(De: Hoje.AddDays(-5), Ate: Hoje.AddDays(-9)));

        lista.Pagina.TotalDeItens.Should().Be(5);
        lista.Pagina.Itens[0].Data.Should().Be(Hoje.AddDays(-5));
        lista.Pagina.Itens[^1].Data.Should().Be(Hoje.AddDays(-9));
    }

    [Fact]
    public async Task Com_uma_ponta_so_o_periodo_fica_aberto_do_outro_lado()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var aPartirDe = await Servico.ListarAsync(new ConsultaDeVendas(De: Hoje.AddDays(-2)));
        var ate = await Servico.ListarAsync(new ConsultaDeVendas(Ate: Hoje.AddDays(-57)));

        aPartirDe.Pagina.Itens.Select(v => v.Data).Should().Equal(Hoje, Hoje.AddDays(-1), Hoje.AddDays(-2));
        ate.Pagina.Itens.Select(v => v.Data).Should().Equal(Hoje.AddDays(-57), Hoje.AddDays(-58), Hoje.AddDays(-59));
    }

    [Fact]
    public async Task Periodo_sem_vendas_devolve_uma_pagina_vazia()
    {
        await RegistrarUmaVendaPorDiaAsync(5);

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(Pagina: 3, De: Hoje.AddDays(1), Ate: Hoje.AddDays(30)));

        lista.Pagina.Numero.Should().Be(1);
        lista.Pagina.TotalDeItens.Should().Be(0);
        lista.Pagina.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task A_pagina_alem_da_ultima_vale_para_o_periodo_filtrado()
    {
        await RegistrarUmaVendaPorDiaAsync(60);

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(Pagina: 9, De: Hoje.AddDays(-29), Ate: Hoje));

        lista.Pagina.Numero.Should().Be(2);
        lista.Pagina.Itens.Select(v => v.Data).Should().Equal(
            Enumerable.Range(25, 5).Select(dias => Hoje.AddDays(-dias)));
    }

    [Theory]
    [InlineData(null, "-data")]
    [InlineData("-data", "-data")]
    [InlineData("data", "data")]
    [InlineData("-total", "-total")]
    [InlineData("total", "total")]
    [InlineData("nome", "-data")]
    [InlineData("Total", "-data")]
    public async Task Ordena_por_data_ou_total_e_desempata_pelo_numero_no_mesmo_sentido(string? pedida, string valendo)
    {
        var anteontem = await VenderAsync(Hoje.AddDays(-2), (_caneta, 10));
        var ontem = await VenderAsync(Hoje.AddDays(-1), (_caneta, 1));
        var hoje = await VenderAsync(Hoje, (_caderno, 2));
        var ontemDeNovo = await VenderAsync(Hoje.AddDays(-1), (_caneta, 1));
        var esperado = valendo switch
        {
            "data" => new[] { anteontem, ontem, ontemDeNovo, hoje },
            "-total" => [hoje, anteontem, ontemDeNovo, ontem],
            "total" => [ontem, ontemDeNovo, anteontem, hoje],
            _ => [hoje, ontemDeNovo, ontem, anteontem],
        };

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(Ordem: pedida));

        lista.Ordem.Should().Be(valendo);
        lista.Pagina.Itens.Select(v => v.Id).Should().Equal(esperado);
    }

    [Fact]
    public async Task O_total_ordenado_e_a_soma_de_todos_os_itens_da_venda()
    {
        var umItemCaro = await VenderAsync(Hoje, (_caderno, 1));
        var muitosItensBaratos = await VenderAsync(Hoje, (_caneta, 4), (_caderno, 1));

        var lista = await Servico.ListarAsync(new ConsultaDeVendas(Ordem: "-total"));

        lista.Pagina.Itens.Select(v => (v.Id, v.Total)).Should().Equal((muitosItensBaratos, 28.90m), (umItemCaro, 18.90m));
    }

    [Fact]
    public async Task O_tamanho_da_pagina_vem_dos_parametros()
    {
        using var sistema = new SistemaEmTeste(banco, Hoje, new ParametrosDoSophia { TamanhoDaPagina = 10 });
        await RegistrarUmaVendaPorDiaAsync(12);

        var lista = await sistema.Vendas.ListarAsync(new ConsultaDeVendas(2));

        lista.Pagina.Tamanho.Should().Be(10);
        lista.Pagina.TotalDePaginas.Should().Be(2);
        lista.Pagina.Itens.Select(v => v.Data).Should().Equal(Hoje.AddDays(-10), Hoje.AddDays(-11));
    }

    private async Task<int> VenderAsync(DateOnly data, params (int Produto, int Quantidade)[] itens)
    {
        var precos = new Dictionary<int, decimal> { [_caneta] = 2.50m, [_caderno] = 18.90m };
        var resultado = await Servico.RegistrarAsync(new DadosVenda(
            data,
            _maria,
            itens.Select(i => new DadosItemVenda(i.Produto, i.Quantidade, precos[i.Produto])).ToList()));
        resultado.Sucesso.Should().BeTrue();
        return resultado.Valor;
    }

    private async Task<IReadOnlyList<VendaResumoDto>> PrimeiraPaginaAsync() =>
        (await Servico.ListarAsync(new ConsultaDeVendas())).Pagina.Itens;

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
