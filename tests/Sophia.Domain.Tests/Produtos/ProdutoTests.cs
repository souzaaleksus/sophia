using Sophia.Domain.Produtos;

namespace Sophia.Domain.Tests.Produtos;

public sealed class ProdutoTests
{
    [Fact]
    public void Cria_o_produto_com_o_nome_sem_espacos_nas_pontas()
    {
        var resultado = Produto.Criar(new DadosProduto("  Caneta azul ", 2.50m));

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.Nome.Should().Be("Caneta azul");
        resultado.Valor.ValorUnitario.Should().Be(2.50m);
    }

    [Theory]
    [InlineData("Caderno  Universitário")]
    [InlineData(" Caderno 	 Universitário ")]
    [InlineData("Caderno  Universitário")]
    public void Junta_os_espacos_repetidos_dentro_do_nome(string nome)
    {
        var resultado = Produto.Criar(new DadosProduto(nome, 1m));

        resultado.Valor.Nome.Should().Be("Caderno Universitário");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Exige_o_nome(string? nome)
    {
        var resultado = Produto.Criar(new DadosProduto(nome, 1m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "Informe o nome."));
    }

    [Fact]
    public void Recusa_nome_maior_que_150_caracteres()
    {
        var resultado = Produto.Criar(new DadosProduto(new string('x', 151), 1m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Nome", "O nome deve ter no máximo 150 caracteres."));
    }

    [Fact]
    public void Exige_o_valor_unitario()
    {
        var resultado = Produto.Criar(new DadosProduto("Lápis", null));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ValorUnitario", "Informe o valor unitário."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Recusa_valor_zero_ou_negativo(decimal valor)
    {
        var resultado = Produto.Criar(new DadosProduto("Lápis", valor));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ValorUnitario", "O valor unitário deve ser maior que zero."));
    }

    [Fact]
    public void Recusa_mais_de_duas_casas_decimais()
    {
        var resultado = Produto.Criar(new DadosProduto("Lápis", 1.999m));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ValorUnitario", "O valor unitário aceita no máximo duas casas decimais."));
    }

    [Fact]
    public void Aceita_o_maior_valor_unitario()
    {
        var resultado = Produto.Criar(new DadosProduto("Lápis", 999_999.99m));

        resultado.Sucesso.Should().BeTrue();
        resultado.Valor.ValorUnitario.Should().Be(RegrasComuns.MaiorValorAceito).And.Be(999_999.99m);
    }

    [Theory]
    [InlineData(1_000_000.00)]
    [InlineData(1_000_000.01)]
    public void Recusa_valor_acima_do_maior_valor_unitario(decimal valor)
    {
        var resultado = Produto.Criar(new DadosProduto("Lápis", valor));

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ValorUnitario", "O valor unitário aceita no máximo R$ 999.999,99."));
    }

    [Fact]
    public void Altera_nome_e_valor()
    {
        var produto = Produto.Criar(new DadosProduto("Caneta azul", 2.50m)).Valor;

        var resultado = produto.Alterar(new DadosProduto("Caneta azul fina", 2.75m));

        resultado.Sucesso.Should().BeTrue();
        produto.Nome.Should().Be("Caneta azul fina");
        produto.ValorUnitario.Should().Be(2.75m);
    }

    [Fact]
    public void Alteracao_invalida_nao_muda_nada()
    {
        var produto = Produto.Criar(new DadosProduto("Caneta azul", 2.50m)).Valor;

        produto.Alterar(new DadosProduto("", 0m)).Erros.Should().HaveCount(2);

        produto.Nome.Should().Be("Caneta azul");
        produto.ValorUnitario.Should().Be(2.50m);
    }
}
