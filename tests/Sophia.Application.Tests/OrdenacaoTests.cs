namespace Sophia.Application.Tests;

public sealed class OrdenacaoTests
{
    public enum Coluna
    {
        Nome,
        Valor,
    }

    private static readonly Ordenacao<Coluna> Padrao = new(Coluna.Nome, Decrescente: false);

    [Theory]
    [InlineData("nome", Coluna.Nome, false)]
    [InlineData("-nome", Coluna.Nome, true)]
    [InlineData("valor", Coluna.Valor, false)]
    [InlineData("-valor", Coluna.Valor, true)]
    public void Reconhece_a_coluna_e_o_sentido_pedidos(string pedida, Coluna coluna, bool decrescente)
    {
        var ordem = Ordenacao.Interpretar(pedida, Padrao);

        ordem.Should().Be(new Ordenacao<Coluna>(coluna, decrescente));
        ordem.Texto.Should().Be(pedida);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("--nome")]
    [InlineData("+valor")]
    [InlineData("Valor")]
    [InlineData("-VALOR")]
    [InlineData(" valor")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("cpf")]
    public void Qualquer_outro_valor_cai_na_ordem_padrao(string? pedida)
    {
        Ordenacao.Interpretar(pedida, new Ordenacao<Coluna>(Coluna.Valor, Decrescente: true))
            .Should().Be(new Ordenacao<Coluna>(Coluna.Valor, Decrescente: true));
    }

    [Fact]
    public void O_texto_da_ordem_e_o_nome_da_coluna_com_menos_na_frente_quando_decrescente()
    {
        new Ordenacao<Coluna>(Coluna.Nome, Decrescente: false).Texto.Should().Be("nome");
        new Ordenacao<Coluna>(Coluna.Valor, Decrescente: true).Texto.Should().Be("-valor");
    }
}
