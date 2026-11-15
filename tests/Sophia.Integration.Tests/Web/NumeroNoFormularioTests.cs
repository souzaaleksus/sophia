using static Sophia.Integration.Tests.Web.FormularioNoServidor;

namespace Sophia.Integration.Tests.Web;

public sealed class NumeroNoFormularioTests
{
    [Theory]
    [InlineData("٣")]
    [InlineData("１２")]
    [InlineData("-٣")]
    public async Task Digito_fora_do_teclado_comum_na_quantidade_e_valor_invalido(string digitado)
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.Quantidade), digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal($"O valor \"{digitado}\" não é válido para Quantidade.");
    }

    [Fact]
    public async Task Quantidade_acima_do_que_o_servidor_comporta_avisa_o_maximo_por_item_com_a_mensagem_do_dominio()
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.Quantidade), "99999999999");

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal("A quantidade aceita no máximo 99.999 por item.");
    }

    [Fact]
    public async Task Quantidade_negativa_enorme_avisa_com_a_mensagem_do_dominio()
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.Quantidade), "-99999999999");

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal("A quantidade deve ser maior que zero.");
    }

    [Theory]
    [InlineData("99999999999")]
    [InlineData("-99999999999")]
    public async Task Numero_enorme_em_campo_que_nao_e_quantidade_e_valor_invalido(string digitado)
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.Codigo), digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal($"O valor \"{digitado}\" não é válido para Codigo.");
    }

    [Theory]
    [InlineData("1000000000000000000000000000000")]
    [InlineData("1.000.000.000.000.000.000.000.000.000.000,00")]
    public async Task Valor_com_mais_digitos_do_que_o_servidor_comporta_avisa_com_a_mensagem_do_dominio(string digitado)
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.ValorUnitario), digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal("O valor unitário aceita no máximo R$ 999.999,99.");
    }

    [Fact]
    public async Task Valor_negativo_enorme_avisa_com_a_mensagem_do_dominio()
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.ValorUnitario), "-1000000000000000000000000000000");

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal("O valor unitário deve ser maior que zero.");
    }

    [Theory]
    [InlineData("٣")]
    [InlineData("１２,50")]
    [InlineData("1.2.3")]
    [InlineData("10,")]
    [InlineData("1,234.56")]
    public async Task Valor_mal_escrito_ou_com_digito_fora_do_teclado_comum_mostra_como_escrever(string digitado)
    {
        var (resultado, erros) = await LigarAsync(nameof(Formulario.ValorUnitario), digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal("Valor inválido. Use 12,50 ou 1.250,00.");
    }

    [Theory]
    [InlineData(nameof(Formulario.QuantidadeExigida), "Informe Quantidade.")]
    [InlineData(nameof(Formulario.ValorUnitarioExigido), "Informe Valor unitário.")]
    [InlineData(nameof(Formulario.DataExigida), "Informe Data da venda.")]
    public async Task Campo_exigido_em_branco_pede_o_campo_pelo_nome(string propriedade, string mensagem)
    {
        var (resultado, erros) = await LigarAsync(propriedade, " ");

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal(mensagem);
    }
}
