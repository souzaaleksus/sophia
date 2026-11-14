using System.ComponentModel.DataAnnotations;

namespace Sophia.Application.Tests;

public sealed class ParametrosDoSophiaTests
{
    [Fact]
    public void Os_valores_padrao_sao_os_da_especificacao()
    {
        new ParametrosDoSophia().Should().BeEquivalentTo(new
        {
            TamanhoDaPagina = 25,
            DiasNoGrafico = 7,
            MaisVendidosNoPainel = 5,
            UltimasVendasNoPainel = 5,
            SugestoesNaBusca = 8,
            DiasDoAtalhoDePeriodo = 7,
        });
    }

    [Fact]
    public void Os_valores_padrao_passam_na_validacao()
    {
        Validar(new ParametrosDoSophia()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(nameof(ParametrosDoSophia.TamanhoDaPagina), 0)]
    [InlineData(nameof(ParametrosDoSophia.TamanhoDaPagina), 201)]
    [InlineData(nameof(ParametrosDoSophia.DiasNoGrafico), 1)]
    [InlineData(nameof(ParametrosDoSophia.DiasNoGrafico), 32)]
    [InlineData(nameof(ParametrosDoSophia.MaisVendidosNoPainel), 0)]
    [InlineData(nameof(ParametrosDoSophia.MaisVendidosNoPainel), 51)]
    [InlineData(nameof(ParametrosDoSophia.UltimasVendasNoPainel), -1)]
    [InlineData(nameof(ParametrosDoSophia.UltimasVendasNoPainel), 51)]
    [InlineData(nameof(ParametrosDoSophia.SugestoesNaBusca), 0)]
    [InlineData(nameof(ParametrosDoSophia.SugestoesNaBusca), 51)]
    [InlineData(nameof(ParametrosDoSophia.DiasDoAtalhoDePeriodo), 1)]
    [InlineData(nameof(ParametrosDoSophia.DiasDoAtalhoDePeriodo), 91)]
    public void Valor_fora_da_faixa_e_recusado_apontando_o_parametro(string parametro, int valor)
    {
        var parametros = new ParametrosDoSophia();
        typeof(ParametrosDoSophia).GetProperty(parametro)!.SetValue(parametros, valor);

        Validar(parametros).Should().ContainSingle()
            .Which.MemberNames.Should().Equal(parametro);
    }

    [Theory]
    [InlineData(1, 2, 1, 1, 1, 2)]
    [InlineData(200, 31, 50, 50, 50, 90)]
    public void As_pontas_da_faixa_sao_aceitas(int pagina, int dias, int maisVendidos, int ultimas, int sugestoes, int atalho)
    {
        var parametros = new ParametrosDoSophia
        {
            TamanhoDaPagina = pagina,
            DiasNoGrafico = dias,
            MaisVendidosNoPainel = maisVendidos,
            UltimasVendasNoPainel = ultimas,
            SugestoesNaBusca = sugestoes,
            DiasDoAtalhoDePeriodo = atalho,
        };

        Validar(parametros).Should().BeEmpty();
    }

    private static List<ValidationResult> Validar(ParametrosDoSophia parametros)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(parametros, new ValidationContext(parametros), erros, validateAllProperties: true);
        return erros;
    }
}
