using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Sophia.Domain.Recursos;

namespace Sophia.Domain.Tests.Recursos;

public sealed class MensagensTests
{
    private const BindingFlags Publicos = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void Toda_mensagem_da_classe_e_lida_do_arquivo_de_recursos()
    {
        var semTexto = NomesDosMembros()
            .Where(membro => Ler(membro) is not { } texto || string.IsNullOrWhiteSpace(texto) || texto.Contains("{0}", StringComparison.Ordinal))
            .ToList();

        semTexto.Should().BeEmpty("toda mensagem precisa vir preenchida e formatada do arquivo de recursos");
    }

    [Fact]
    public void Toda_chave_do_arquivo_de_recursos_tem_mensagem_na_classe()
    {
        var recursos = new ResourceManager(typeof(Mensagens))
            .GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);

        var chaves = recursos!.Cast<DictionaryEntry>().Select(entrada => (string)entrada.Key);

        chaves.Should().BeEquivalentTo(NomesDosMembros());
    }

    [Fact]
    public void O_dominio_nao_expoe_as_mensagens_nem_as_regras_comuns()
    {
        var publicos = typeof(Erro).Assembly.GetExportedTypes();

        publicos.Should().NotContain([typeof(Mensagens), typeof(RegrasComuns)]);
        publicos.Should().NotContain(tipo => tipo.Namespace == typeof(Mensagens).Namespace);
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
