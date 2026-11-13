using Sophia.Application.Abstracoes;
using Sophia.Application.Clientes;
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

    private async Task<int> RegistrarVendaAsync(int clienteId, IReadOnlyList<DadosItemVenda> itens)
    {
        await using var contexto = banco.NovoContexto();
        var venda = Venda.Registrar(new DadosVenda(Hoje, clienteId, itens), Hoje).Valor;
        new VendaRepositorio(contexto).Adicionar(venda);
        await new UnidadeDeTrabalho(contexto).SalvarAsync();
        return venda.Id;
    }
}
