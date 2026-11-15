using System.Reflection;
using Sophia.Domain;
using Sophia.Domain.Recursos;

namespace Sophia.Application.Tests;

public sealed class RegrasDeEntradaTests
{
    private const BindingFlags Publicos = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const int Limite = 12_345;

    [Fact]
    public void Todo_texto_do_contrato_e_a_mensagem_de_mesmo_nome_do_dominio()
    {
        var textos = Textos(typeof(RegrasDeEntrada)).ToList();

        textos.Should().NotBeEmpty();
        textos.Should().AllSatisfy(nome => Ler(typeof(RegrasDeEntrada), nome).Should().Be(Ler(typeof(Mensagens), nome), nome));
    }

    [Fact]
    public void O_maior_valor_aceito_e_o_do_dominio()
    {
        RegrasDeEntrada.MaiorValorAceito.Should().Be(RegrasComuns.MaiorValorAceito);
    }

    private static IEnumerable<string> Textos(Type tipo) =>
        tipo.GetProperties(Publicos).Where(propriedade => propriedade.PropertyType == typeof(string)).Select(propriedade => propriedade.Name)
            .Concat(tipo.GetMethods(Publicos).Where(metodo => !metodo.IsSpecialName && metodo.ReturnType == typeof(string)).Select(metodo => metodo.Name));

    private static string? Ler(Type tipo, string membro)
    {
        if (tipo.GetProperty(membro, Publicos) is { } propriedade)
        {
            return (string?)propriedade.GetValue(null);
        }

        var metodo = tipo.GetMethod(membro, Publicos);
        return (string?)metodo?.Invoke(null, metodo.GetParameters().Select(_ => (object)Limite).ToArray());
    }
}
