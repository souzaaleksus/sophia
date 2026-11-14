using Sophia.Domain.Vendas;

namespace Sophia.Domain.Tests.Vendas;

public sealed class VendaTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 13);
    private const int Maria = 1;
    private const int Caneta = 10;
    private const int Caderno = 20;

    [Fact]
    public void Registra_a_venda_e_calcula_subtotais_e_total()
    {
        var itens = new[]
        {
            new DadosItemVenda(Caneta, 10, 2.50m),
            new DadosItemVenda(Caderno, 2, 18.90m),
        };

        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, itens), Hoje);

        resultado.Sucesso.Should().BeTrue();
        var venda = resultado.Valor;
        venda.Data.Should().Be(Hoje);
        venda.ClienteId.Should().Be(Maria);
        venda.Itens.Select(i => i.Subtotal).Should().Equal(25.00m, 37.80m);
        venda.Total.Should().Be(62.80m);
    }

    [Fact]
    public void Guarda_a_chave_de_registro_enviada_pelo_formulario()
    {
        var chave = Guid.NewGuid();

        var venda = Venda.Registrar(new DadosVenda(Hoje, Maria, [new DadosItemVenda(Caneta, 1, 2.50m)], chave), Hoje).Valor;

        venda.ChaveDeRegistro.Should().Be(chave);
    }

    [Fact]
    public void Chave_vazia_vale_como_venda_sem_chave()
    {
        var venda = Venda.Registrar(new DadosVenda(Hoje, Maria, [new DadosItemVenda(Caneta, 1, 2.50m)], Guid.Empty), Hoje).Valor;

        venda.ChaveDeRegistro.Should().BeNull();
    }

    [Fact]
    public void Aceita_data_passada()
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje.AddDays(-30), Maria, [new DadosItemVenda(Caneta, 1, 2.50m)]), Hoje);

        resultado.Sucesso.Should().BeTrue();
    }

    [Fact]
    public void Recusa_data_futura()
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje.AddDays(1), Maria, [new DadosItemVenda(Caneta, 1, 2.50m)]), Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Data", "A data da venda não pode estar no futuro."));
    }

    [Fact]
    public void Exige_a_data()
    {
        var resultado = Venda.Registrar(new DadosVenda(null, Maria, [new DadosItemVenda(Caneta, 1, 2.50m)]), Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Data", "Informe a data da venda."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Exige_o_cliente(int? clienteId)
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje, clienteId, [new DadosItemVenda(Caneta, 1, 2.50m)]), Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ClienteId", "Selecione o cliente."));
    }

    [Fact]
    public void Exige_ao_menos_um_item()
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, []), Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("Itens", "Lance ao menos um produto na venda."));
    }

    [Fact]
    public void Aponta_o_erro_na_linha_e_no_campo_do_item()
    {
        var itens = new[]
        {
            new DadosItemVenda(Caneta, 1, 2.50m),
            new DadosItemVenda(null, 0, 0m),
        };

        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, itens), Hoje);

        resultado.Erros.Should().BeEquivalentTo(new[]
        {
            new Erro("ProdutoId", "Selecione o produto.", 1),
            new Erro("Quantidade", "A quantidade deve ser maior que zero.", 1),
            new Erro("ValorUnitario", "O valor unitário deve ser maior que zero.", 1),
        });
    }

    [Fact]
    public void Exige_quantidade_e_valor_informados()
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, [new DadosItemVenda(Caneta, null, null)]), Hoje);

        resultado.Erros.Should().BeEquivalentTo(new[]
        {
            new Erro("Quantidade", "Informe a quantidade.", 0),
            new Erro("ValorUnitario", "Informe o valor unitário.", 0),
        });
    }

    [Fact]
    public void Recusa_valor_do_item_com_mais_de_duas_casas()
    {
        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, [new DadosItemVenda(Caneta, 1, 2.505m)]), Hoje);

        resultado.Erros.Should().ContainSingle()
            .Which.Should().Be(new Erro("ValorUnitario", "O valor unitário aceita no máximo duas casas decimais.", 0));
    }

    [Fact]
    public void Recusa_o_mesmo_produto_em_duas_linhas()
    {
        var itens = new[]
        {
            new DadosItemVenda(Caneta, 1, 2.50m),
            new DadosItemVenda(Caderno, 1, 18.90m),
            new DadosItemVenda(Caneta, 2, 2.50m),
        };

        var resultado = Venda.Registrar(new DadosVenda(Hoje, Maria, itens), Hoje);

        resultado.Erros.Should().ContainSingle().Which.Should().Be(new Erro(
            "ProdutoId",
            "Este produto já foi lançado nesta venda; ajuste a quantidade na linha anterior.",
            2));
    }

    [Fact]
    public void Calcula_o_subtotal_sem_perder_centavos()
    {
        var item = Venda.Registrar(new DadosVenda(Hoje, Maria, [new DadosItemVenda(Caneta, 3, 0.10m)]), Hoje).Valor.Itens.Single();

        item.Subtotal.Should().Be(0.30m);
    }

    [Fact]
    public void Mesmo_cliente_data_e_itens_em_outra_ordem_e_o_mesmo_conteudo()
    {
        var primeira = Registrar(Maria, new DadosItemVenda(Caneta, 10, 2.50m), new DadosItemVenda(Caderno, 2, 18.90m));
        var segunda = Registrar(Maria, new DadosItemVenda(Caderno, 2, 18.90m), new DadosItemVenda(Caneta, 10, 2.50m));

        primeira.TemOMesmoConteudoQue(segunda).Should().BeTrue();
    }

    [Theory]
    [InlineData(2, Caneta, 10, 2.50)]
    [InlineData(Maria, Caderno, 10, 2.50)]
    [InlineData(Maria, Caneta, 9, 2.50)]
    [InlineData(Maria, Caneta, 10, 2.40)]
    public void Outro_cliente_produto_quantidade_ou_valor_nao_e_o_mesmo_conteudo(int cliente, int produto, int quantidade, decimal valor)
    {
        var primeira = Registrar(Maria, new DadosItemVenda(Caneta, 10, 2.50m));
        var segunda = Registrar(cliente, new DadosItemVenda(produto, quantidade, valor));

        primeira.TemOMesmoConteudoQue(segunda).Should().BeFalse();
    }

    [Fact]
    public void Outra_data_ou_outro_numero_de_itens_nao_e_o_mesmo_conteudo()
    {
        var primeira = Registrar(Maria, new DadosItemVenda(Caneta, 10, 2.50m));
        var outraData = Venda.Registrar(new DadosVenda(Hoje.AddDays(-1), Maria, [new DadosItemVenda(Caneta, 10, 2.50m)]), Hoje).Valor;
        var maisItens = Registrar(Maria, new DadosItemVenda(Caneta, 10, 2.50m), new DadosItemVenda(Caderno, 1, 18.90m));

        primeira.TemOMesmoConteudoQue(outraData).Should().BeFalse();
        primeira.TemOMesmoConteudoQue(maisItens).Should().BeFalse();
    }

    private static Venda Registrar(int cliente, params DadosItemVenda[] itens) =>
        Venda.Registrar(new DadosVenda(Hoje, cliente, itens), Hoje).Valor;
}
