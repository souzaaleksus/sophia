using Sophia.Application.Produtos;
using Sophia.Domain;
using Sophia.Domain.Produtos;
using Sophia.Tests.Fakes;

namespace Sophia.Application.Tests.Produtos;

public sealed class ProdutoServicoTests
{
    private readonly BancoEmMemoria _banco = new();
    private readonly ProdutoServico _servico;

    public ProdutoServicoTests()
    {
        _servico = new ProdutoServico(_banco.Produtos, _banco);
    }

    [Fact]
    public async Task Cadastra_e_devolve_o_identificador()
    {
        var resultado = await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Sucesso.Should().BeTrue();
        (await _servico.ObterAsync(resultado.Valor)).Should()
            .Be(new ProdutoDto(resultado.Valor, "Caneta azul", 2.50m));
    }

    [Fact]
    public async Task Recusa_nome_repetido_sem_diferenciar_maiusculas()
    {
        await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        var resultado = await _servico.CadastrarAsync(new DadosProduto("CANETA AZUL", 3m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Converte_a_violacao_de_unicidade_do_banco_na_mesma_mensagem()
    {
        _banco.SimularCadastroConcorrenteNoProximoSalvamento();

        var resultado = await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Altera_o_valor_mantendo_o_proprio_nome()
    {
        var id = (await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m))).Valor;

        var resultado = await _servico.AlterarAsync(id, new DadosProduto("Caneta azul", 2.75m));

        resultado.Sucesso.Should().BeTrue();
        (await _servico.ObterAsync(id))!.ValorUnitario.Should().Be(2.75m);
    }

    [Fact]
    public async Task Recusa_alteracao_para_o_nome_de_outro_produto()
    {
        await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        var lapis = (await _servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        var resultado = await _servico.AlterarAsync(lapis, new DadosProduto("Caneta azul", 1m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Já existe um produto com este nome."));
    }

    [Fact]
    public async Task Avisa_quando_o_produto_nao_existe()
    {
        var alterar = await _servico.AlterarAsync(999, new DadosProduto("Lápis", 1m));
        var excluir = await _servico.ExcluirAsync(999);

        alterar.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
        excluir.Erros.Should().ContainSingle().Which.Mensagem.Should().Be("Produto não encontrado.");
    }

    [Fact]
    public async Task Exclui_produto_nunca_vendido()
    {
        var id = (await _servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;

        (await _servico.ExcluirAsync(id)).Sucesso.Should().BeTrue();
        (await _servico.ObterAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task Nao_exclui_produto_ja_vendido()
    {
        var id = (await _servico.CadastrarAsync(new DadosProduto("Lápis", 1m))).Valor;
        _banco.RegistrarVendaDoProduto(id);

        var resultado = await _servico.ExcluirAsync(id);

        resultado.Erros.Should().ContainSingle()
            .Which.Mensagem.Should().Be("Este produto já foi vendido e não pode ser excluído.");
    }

    [Fact]
    public async Task Lista_em_ordem_alfabetica_e_filtra_pelo_nome()
    {
        await _servico.CadastrarAsync(new DadosProduto("Lápis", 1m));
        await _servico.CadastrarAsync(new DadosProduto("Caneta azul", 2.50m));
        await _servico.CadastrarAsync(new DadosProduto("Caneta preta", 2.50m));

        (await _servico.ListarAsync(null)).Select(p => p.Nome).Should().Equal("Caneta azul", "Caneta preta", "Lápis");
        (await _servico.ListarAsync("caneta")).Select(p => p.Nome).Should().Equal("Caneta azul", "Caneta preta");
    }
}
