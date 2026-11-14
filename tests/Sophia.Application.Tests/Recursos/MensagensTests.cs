using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Sophia.Application.Recursos;

namespace Sophia.Application.Tests.Recursos;

public sealed class MensagensTests
{
    private const BindingFlags Publicos = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static TheoryData<string> Membros()
    {
        var dados = new TheoryData<string>();
        foreach (var nome in NomesDosMembros())
        {
            dados.Add(nome);
        }

        return dados;
    }

    [Theory]
    [MemberData(nameof(Membros))]
    public void Toda_mensagem_da_classe_e_lida_do_arquivo_de_recursos(string membro)
    {
        var texto = Ler(membro);

        texto.Should().NotBeNullOrWhiteSpace();
        texto.Should().NotContain("{0}");
    }

    [Fact]
    public void Toda_chave_do_arquivo_de_recursos_tem_mensagem_na_classe()
    {
        var recursos = new ResourceManager(typeof(Mensagens))
            .GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);

        var chaves = recursos!.Cast<DictionaryEntry>().Select(entrada => (string)entrada.Key);

        chaves.Should().BeEquivalentTo(NomesDosMembros());
    }

    private static IEnumerable<string> NomesDosMembros() =>
        typeof(Mensagens).GetProperties(Publicos).Select(propriedade => propriedade.Name)
            .Concat(typeof(Mensagens).GetMethods(Publicos).Where(metodo => !metodo.IsSpecialName).Select(metodo => metodo.Name));

    private static string? Ler(string membro)
    {
        var propriedade = typeof(Mensagens).GetProperty(membro, Publicos);
        if (propriedade is not null)
        {
            return (string?)propriedade.GetValue(null);
        }

        var metodo = typeof(Mensagens).GetMethod(membro, Publicos)!;
        var argumentos = metodo.GetParameters().Select(parametro => Activator.CreateInstance(parametro.ParameterType)).ToArray();
        return (string?)metodo.Invoke(null, argumentos);
    }
}
