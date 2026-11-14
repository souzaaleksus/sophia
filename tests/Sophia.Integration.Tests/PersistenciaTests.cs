using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
using Sophia.Application.Painel;
using Sophia.Application.Produtos;
using Sophia.Application.Vendas;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Infrastructure.Persistencia;
using Sophia.Integration.Tests.Apoio;

namespace Sophia.Integration.Tests;

[Collection(ColecaoDoBanco.Nome)]
public sealed class PersistenciaTests(BancoDeTestes banco) : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Grava_e_le_o_cliente_com_o_cpf_convertido()
    {
        var id = await CadastrarClienteAsync("529.982.247-25", "Maria Santos", "(11) 98765-4321");

        await using var contexto = banco.NovoContexto();
        var cliente = await new ClienteRepositorio(contexto).ObterAsync(id);

        cliente!.Cpf.Formatado.Should().Be("529.982.247-25");
        cliente.Telefone.Should().Be("11987654321");
    }

    [Fact]
    public async Task Busca_clientes_pelo_inicio_do_cpf_ou_por_trecho_do_nome()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        await CadastrarClienteAsync("111.444.777-35", "João Lima");

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);

        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar("529982"), null)).Select(c => c.Nome)
            .Should().Equal("Maria Santos");
        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar("lim"), null)).Select(c => c.Nome)
            .Should().Equal("João Lima");
        (await repositorio.ListarAsync(BuscaDeCliente.Interpretar(null), 1)).Select(c => c.Nome)
            .Should().Equal("João Lima");
    }

    [Fact]
    public async Task O_indice_unico_de_cpf_vira_conflito_de_valor_duplicado()
    {
        await CadastrarClienteAsync("529.982.247-25", "Maria Santos");

        var gravarDeNovo = () => CadastrarClienteAsync("52998224725", "Outra Maria");

        (await gravarDeNovo.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.ValorDuplicado);
    }

    [Fact]
    public async Task O_nome_do_produto_e_unico_sem_diferenciar_maiusculas()
    {
        await CadastrarProdutoAsync("Caneta azul", 2.50m);

        await using var contexto = banco.NovoContexto();
        var existe = await new ProdutoRepositorio(contexto).ExisteComNomeAsync("CANETA AZUL", excetoId: null);

        existe.Should().BeTrue();
    }

    [Fact]
    public async Task Grava_a_venda_com_itens_e_le_resumo_e_detalhe()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);

        var vendaId = await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 2, 18.90m)]);

        await using var contexto = banco.NovoContexto();
        var repositorio = new VendaRepositorio(contexto);
        (await repositorio.ListarAsync()).Should().ContainSingle()
            .Which.Should().Be(new VendaResumoDto(vendaId, Hoje, "529.982.247-25", "Maria Santos", 2, 62.80m));
        var detalhe = await repositorio.ObterDetalheAsync(vendaId);
        detalhe!.Itens.Should().Equal(
            new ItemVendaDto("Caneta azul", 10, 2.50m, 25.00m),
            new ItemVendaDto("Caderno 96 folhas", 2, 18.90m, 37.80m));
        detalhe.Total.Should().Be(62.80m);
    }

    [Fact]
    public async Task Resume_as_vendas_do_periodo_e_limita_as_ultimas()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 1, 18.90m)]);
        await RegistrarVendaAsync(maria, [new(caneta, 2, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var repositorio = new VendaRepositorio(contexto);

        (await repositorio.ResumirAsync(Hoje, Hoje)).Should().Be(new ResumoDoPeriodo(2, 48.90m));
        (await repositorio.ResumirAsync(Hoje.AddDays(1), Hoje.AddDays(30))).Should().Be(ResumoDoPeriodo.Vazio);
        (await repositorio.ListarAsync(limite: 1)).Should().ContainSingle().Which.Total.Should().Be(5.00m);
    }

    [Fact]
    public async Task Sabe_se_o_cliente_e_o_produto_ja_tem_vendas()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var joao = await CadastrarClienteAsync("111.444.777-35", "João Lima");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var lapis = await CadastrarProdutoAsync("Lápis", 1m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var clientes = new ClienteRepositorio(contexto);
        var produtos = new ProdutoRepositorio(contexto);

        (await clientes.PossuiVendasAsync(maria)).Should().BeTrue();
        (await clientes.PossuiVendasAsync(joao)).Should().BeFalse();
        (await produtos.FoiVendidoAsync(caneta)).Should().BeTrue();
        (await produtos.FoiVendidoAsync(lapis)).Should().BeFalse();
        (await produtos.FiltrarExistentesAsync([caneta, lapis, 999])).Should().BeEquivalentTo([caneta, lapis]);
    }

    [Fact]
    public async Task A_chave_estrangeira_impede_excluir_cliente_com_venda()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        var repositorio = new ClienteRepositorio(contexto);
        repositorio.Remover((await repositorio.ObterAsync(maria))!);
        var salvar = () => new UnidadeDeTrabalho(contexto).SalvarAsync();

        (await salvar.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.RegistroEmUso);
    }

    [Fact]
    public async Task O_indice_unico_da_chave_de_registro_barra_a_segunda_gravacao()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var chave = Guid.NewGuid();
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        var gravarDeNovo = () => RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        (await gravarDeNovo.Should().ThrowAsync<ConflitoAoSalvarException>())
            .Which.Tipo.Should().Be(TipoDeConflito.ValorDuplicado);
    }

    [Fact]
    public async Task Vendas_sem_chave_de_registro_nao_esbarram_no_indice_unico()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);

        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)]);

        await using var contexto = banco.NovoContexto();
        (await new VendaRepositorio(contexto).ListarAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Acha_o_numero_da_venda_pela_chave_de_registro()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var chave = Guid.NewGuid();
        var vendaId = await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], chave);

        await using var contexto = banco.NovoContexto();
        var repositorio = new VendaRepositorio(contexto);

        (await repositorio.ObterIdPelaChaveAsync(chave)).Should().Be(vendaId);
        (await repositorio.ObterIdPelaChaveAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task Na_corrida_o_servico_devolve_a_venda_que_o_banco_gravou_primeiro()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var dados = new DadosVenda(Hoje, maria, [new(caneta, 1, 2.50m)], Guid.NewGuid());
        var idDoOutroEnvio = 0;

        await using var contexto = banco.NovoContexto();
        var unidade = new UnidadeQueDeixaOutroEnvioGravarAntes(
            new UnidadeDeTrabalho(contexto),
            async () => idDoOutroEnvio = await RegistrarVendaAsync(maria, dados.Itens, dados.Chave));
        var servico = new VendaServico(
            new VendaRepositorio(contexto),
            new ClienteRepositorio(contexto),
            new ProdutoRepositorio(contexto),
            unidade,
            new RelogioParado());

        var resultado = await servico.RegistrarAsync(dados);

        resultado.Valor.Should().Be(idDoOutroEnvio);
        await using var leitura = banco.NovoContexto();
        (await new VendaRepositorio(leitura).ListarAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Totaliza_por_dia_no_banco_so_os_dias_do_periodo_que_tem_venda()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        await RegistrarVendaAsync(maria, [new(caneta, 1, 2.50m)], data: new DateOnly(2026, 11, 6));
        await RegistrarVendaAsync(maria, [new(caneta, 2, 2.50m)], data: new DateOnly(2026, 11, 7));
        await RegistrarVendaAsync(maria, [new(caderno, 1, 18.90m)], data: new DateOnly(2026, 11, 7));
        await RegistrarVendaAsync(maria, [new(caneta, 10, 2.50m), new(caderno, 2, 18.90m)], data: Hoje);

        await using var contexto = banco.NovoContexto();
        var totais = await new VendaRepositorio(contexto).TotaisPorDiaAsync(new DateOnly(2026, 11, 7), Hoje);

        totais.Should().Equal(
            new TotalDoDia(new DateOnly(2026, 11, 7), 23.90m, 2),
            new TotalDoDia(Hoje, 62.80m, 1));
    }

    [Fact]
    public async Task Agrupa_os_mais_vendidos_no_banco_por_quantidade_total_e_nome()
    {
        var maria = await CadastrarClienteAsync("529.982.247-25", "Maria Santos");
        var caneta = await CadastrarProdutoAsync("Caneta azul", 2.50m);
        var caderno = await CadastrarProdutoAsync("Caderno 96 folhas", 18.90m);
        var lapis = await CadastrarProdutoAsync("Lápis", 1.50m);
        var borracha = await CadastrarProdutoAsync("Borracha", 1.50m);
        var regua = await CadastrarProdutoAsync("Régua", 4m);
        await RegistrarVendaAsync(maria, [new(regua, 90, 4m)], data: new DateOnly(2026, 10, 31));
        await RegistrarVendaAsync(maria, [new(caneta, 6, 2.50m), new(lapis, 3, 1.50m)], data: new DateOnly(2026, 11, 2));
        await RegistrarVendaAsync(maria, [new(caneta, 4, 2.40m), new(caderno, 3, 18.90m)], data: new DateOnly(2026, 11, 5));
        await RegistrarVendaAsync(maria, [new(borracha, 3, 1.50m), new(regua, 1, 4m)], data: Hoje);

        await using var contexto = banco.NovoContexto();
        var repositorio = new VendaRepositorio(contexto);
        var doMes = await repositorio.MaisVendidosAsync(new DateOnly(2026, 11, 1), Hoje, 4);

        doMes.Should().Equal(
            new ProdutoMaisVendido(caneta, "Caneta azul", 10, 24.60m),
            new ProdutoMaisVendido(caderno, "Caderno 96 folhas", 3, 56.70m),
            new ProdutoMaisVendido(borracha, "Borracha", 3, 4.50m),
            new ProdutoMaisVendido(lapis, "Lápis", 3, 4.50m));
        (await repositorio.MaisVendidosAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31), 5))
            .Should().BeEmpty();
    }

    private async Task<int> CadastrarClienteAsync(string cpf, string nome, string? telefone = null)
    {
        await using var contexto = banco.NovoContexto();
        var cliente = Cliente.Criar(new DadosCliente(cpf, nome, null, null, telefone), Hoje).Valor;
        new ClienteRepositorio(contexto).Adicionar(cliente);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return cliente.Id;
    }

    private async Task<int> CadastrarProdutoAsync(string nome, decimal valor)
    {
        await using var contexto = banco.NovoContexto();
        var produto = Produto.Criar(new DadosProduto(nome, valor)).Valor;
        new ProdutoRepositorio(contexto).Adicionar(produto);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return produto.Id;
    }

    private async Task<int> RegistrarVendaAsync(
        int clienteId,
        IReadOnlyList<DadosItemVenda> itens,
        Guid? chave = null,
        DateOnly? data = null)
    {
        await using var contexto = banco.NovoContexto();
        var venda = Venda.Registrar(new DadosVenda(data ?? Hoje, clienteId, itens, chave), Hoje).Valor;
        new VendaRepositorio(contexto).Adicionar(venda);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return venda.Id;
    }

    private sealed class RelogioParado : IRelogio
    {
        public DateOnly Hoje => PersistenciaTests.Hoje;
    }

    private sealed class UnidadeQueDeixaOutroEnvioGravarAntes(IUnidadeDeTrabalho unidade, Func<Task> outroEnvio)
        : IUnidadeDeTrabalho
    {
        public async Task SalvarAsync(CancellationToken cancelamento = default)
        {
            await outroEnvio();
            await unidade.SalvarAsync(cancelamento);
        }
    }
}
