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

    [Theory]
    [InlineData(nameof(Formulario.Quantidade), "99999999999", "O número é maior do que o campo aceita.")]
    [InlineData(nameof(Formulario.Quantidade), "-99999999999", "O número é menor do que o campo aceita.")]
    [InlineData(nameof(Formulario.Codigo), "99999999999", "O número é maior do que o campo aceita.")]
    [InlineData(nameof(Formulario.Codigo), "-99999999999", "O número é menor do que o campo aceita.")]
    [InlineData(nameof(Formulario.ValorUnitario), "1000000000000000000000000000000", "O número é maior do que o campo aceita.")]
    [InlineData(nameof(Formulario.ValorUnitario), "1.000.000.000.000.000.000.000.000.000.000,00", "O número é maior do que o campo aceita.")]
    [InlineData(nameof(Formulario.ValorUnitario), "-1000000000000000000000000000000", "O número é menor do que o campo aceita.")]
    public async Task Numero_que_nao_cabe_no_tipo_do_campo_avisa_pela_leitura_sem_depender_do_nome_do_campo(string propriedade, string digitado, string mensagem)
    {
        var (resultado, erros) = await LigarAsync(propriedade, digitado);

        resultado.IsModelSet.Should().BeFalse();
        erros.Should().Equal(mensagem);
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
