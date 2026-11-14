using Sophia.Application.Produtos;
using Sophia.Application.Tests.Apoio;
using Sophia.Domain;
using Sophia.Domain.Clientes;
using Sophia.Domain.Produtos;
using Sophia.Domain.Vendas;
using Sophia.Testes.Banco;

namespace Sophia.Application.Tests.Produtos;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ProdutoServicoTests(BancoDeTestes banco) : IAsyncLifetime, IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);

    private readonly SistemaEmTeste _sistema = new(banco, Hoje);

    private IProdutoServico Servico => _sistema.Produtos;

    public Task InitializeAsync() => banco.LimparAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _sistema.Dispose();

    [Fact]
    public async Task Cadastra_e_devolve_o_identificador()
    {
        var resultado = await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(resultado.Valor)).Should()
            .Be(new ProdutoDto(resultado.Valor, "Caneta azul", 2.50m));
    }

    [Fact]
    public async Task Recusa_nome_repetido_sem_diferenciar_maiusculas()
    {
        await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        var resultado = await Servico.CadastrarAsync(new DadosProduto("CANETA AZUL", 3m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Converte_a_violacao_de_unicidade_do_banco_na_mesma_mensagem()
    {
        _sistema.AntesDoProximoSalvamento(async () =>
            (await _sistema.Produtos.CadastrarAsync(new DadosProduto("CANETA AZUL", 3m))).Sucesso.Should().BeTrue());

        var resultado = await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Altera_o_valor_mantendo_o_proprio_nome()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;

        var resultado = await Servico.AlterarAsync(id, new DadosProduto("Caneta azul", 2.75m));

        resultado.Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id))!.ValorUnitario.Should().Be(2.75m);
    }

    [Fact]
    public async Task Recusa_alteracao_para_o_nome_de_outro_produto()
    {
        await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        var lapis = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        var resultado = await Servico.AlterarAsync(lapis, new DadosProduto("Caneta azul", 1m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Avisa_quando_o_produto_nao_existe()
    {
        var alterar = await Servico.AlterarAsync(999, new DadosProduto("Lápis", 1m));
        var excluir = await Servico.ExcluirAsync(999);

        alterar.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
        excluir.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
    }

    [Fact]
    public async Task Exclui_produto_nunca_vendido()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        (await Servico.ExcluirAsync(id)).Sucesso.Should().BeTrue();
        (await Servico.ObterAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Nao_exclui_produto_ja_vendido()
    {
        var id = (await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;
        var maria = (await _sistema.Clientes.CadastrarAsync(
            new DadosCliente("529.982.247-25", "Maria Santos", null, null, null))).Valor;
        (await _sistema.Vendas.RegistrarAsync(new DadosVenda(Hoje, maria, [new(id, 1, 1m)]))).Sucesso.Should().BeTrue();

        var resultado = await Servico.ExcluirAsync(id);

        resultado.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be("Este produto já foi vendido e não pode ser excluído.");
    }

    [Fact]
    public async Task Lista_em_ordem_alfabetica_e_filtra_pelo_nome()
    {
        await Servico.CadastrarAsync(new DadosProduto("Lápis", 1m));
        await Servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        await Servico.CadastrarAsync(new DadosProduto("Caneta preta", 2.50m));

        (await Servico.ListarAsync(null)).Select(p => p.Nome).Should().Equal("Caneta azul", "Caneta preta", "Lápis");
        (await Servico.ListarAsync("caneta")).Select(p => p.Nome).Should().Equal("Caneta azul", "Caneta preta");
    }
}
